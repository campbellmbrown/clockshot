using ClockShot.Core.Models;
using ClockShot.Core.Projects;
using ClockShot.Data.Sqlite;

using Dapper;

using Microsoft.Data.Sqlite;

namespace ClockShot.Data.Projects;

/// <summary>
/// Clients, projects and tasks in SQLite.
/// Names are unique among siblings, ignoring case.
/// </summary>
public sealed class ProjectRepository(SqliteDatabase database)
{
    private const int SqliteUniqueConstraintFailed = 2067;   // SQLITE_CONSTRAINT_UNIQUE

    /// <summary>
    /// Raised after any change, so screens showing projects can reload.
    /// It may be raised on a background thread, so a listener that updates the UI moves to the UI thread first.
    /// </summary>
    public event Action? Changed;

    public Task<ProjectCatalog> LoadCatalogAsync(CancellationToken cancellationToken = default) =>
        Task.Run(() =>
        {
            using var connection = database.Open();
            return new ProjectCatalog(
                connection.Query<(long Id, string Name, bool IsArchived)>("SELECT id, name, is_archived FROM client")
                    .Select(c => new Client(c.Id, c.Name, c.IsArchived)),
                connection.Query<(long Id, long? ClientId, long? ParentId, string Name, bool IsArchived)>(
                        "SELECT id, client_id, parent_id, name, is_archived FROM project")
                    .Select(p => new Project(p.Id, p.ClientId, p.ParentId, p.Name, p.IsArchived)),
                connection.Query<(long Id, long ProjectId, string Name, bool IsArchived)>(
                        "SELECT id, project_id, name, is_archived FROM task")
                    .Select(t => new ProjectTask(t.Id, t.ProjectId, t.Name, t.IsArchived)),
                connection.Query<(long ProjectId, long? TaskId, string Color)>("SELECT project_id, task_id, color FROM tag_color")
                    .Select(c => new TagColor(c.ProjectId, c.TaskId, c.Color)));
        }, cancellationToken);

    /// <exception cref="DuplicateNameException">A client with that name exists.</exception>
    public async Task<Client> AddClientAsync(string name)
    {
        name = name.Trim();
        var id = await InsertAsync(
            name,
            "INSERT INTO client (name) VALUES (@Name)",
            new { Name = name }).ConfigureAwait(false);
        return new Client(id, name, IsArchived: false);
    }

    /// <summary>
    /// Adds a project.
    /// A sub-project takes its client from the top-level project.
    /// </summary>
    /// <exception cref="DuplicateNameException">The parent or client already has a project with that name.</exception>
    public async Task<Project> AddProjectAsync(string name, long? clientId, long? parentId)
    {
        name = name.Trim();
        var ownClientId = OwnClientId(clientId, parentId);
        var id = await InsertAsync(
            name,
            "INSERT INTO project (client_id, parent_id, name) VALUES (@Client, @Parent, @Name)",
            new { Client = ownClientId, Parent = parentId, Name = name }).ConfigureAwait(false);
        return new Project(id, ownClientId, parentId, name, IsArchived: false);
    }

    /// <exception cref="DuplicateNameException">The project already has a task with that name.</exception>
    public async Task<ProjectTask> AddTaskAsync(long projectId, string name)
    {
        name = name.Trim();
        var id = await InsertAsync(
            name,
            "INSERT INTO task (project_id, name) VALUES (@Project, @Name)",
            new { Project = projectId, Name = name }).ConfigureAwait(false);
        return new ProjectTask(id, projectId, name, IsArchived: false);
    }

    /// <exception cref="DuplicateNameException">The new name is taken.</exception>
    public Task RenameAsync(CatalogItem item, string newName)
    {
        newName = newName.Trim();
        var sql = item.Kind switch
        {
            CatalogItemKind.Client => "UPDATE client SET name = @Name WHERE id = @Id",
            CatalogItemKind.Project => "UPDATE project SET name = @Name WHERE id = @Id",
            CatalogItemKind.Task => "UPDATE task SET name = @Name WHERE id = @Id",
            _ => throw UnknownKind(item),
        };
        return WriteRowsAsync(RefuseDuplicateNames(newName, connection => connection.Execute(sql, new { Name = newName, item.Id })));
    }

    /// <summary>
    /// Saves a project's name and place (client and parent).
    /// Moving it under another project gives it that project's client.
    /// </summary>
    /// <exception cref="DuplicateNameException">The new parent or client already has a project with that name.</exception>
    /// <exception cref="ArgumentException">The new parent is the project itself or one of its sub-projects.</exception>
    public Task UpdateProjectAsync(Project project)
    {
        var name = project.Name.Trim();
        return WriteRowsAsync(RefuseDuplicateNames(name, connection =>
        {
            ThrowIfInsideItself(connection, project);
            return connection.Execute(
                "UPDATE project SET name = @Name, client_id = @Client, parent_id = @Parent WHERE id = @Id",
                new { project.Id, Name = name, Client = OwnClientId(project.ClientId, project.ParentId), Parent = project.ParentId });
        }));
    }

    /// <summary>
    /// Chooses a tag's colour.
    /// A null <paramref name="taskId"/> means the project's General bucket.
    /// A null <paramref name="colorHex"/> goes back to the automatic colour.
    /// </summary>
    public Task SetTagColorAsync(long projectId, long? taskId, string? colorHex) => WriteRowsAsync(connection =>
    {
        using var transaction = connection.BeginTransaction();
        var tag = new { ProjectId = projectId, TaskId = taskId, Color = colorHex };
        var changed = connection.Execute("DELETE FROM tag_color WHERE project_id = @ProjectId AND task_id IS @TaskId", tag, transaction);
        if (colorHex is not null)
        {
            changed += connection.Execute("INSERT INTO tag_color (project_id, task_id, color) VALUES (@ProjectId, @TaskId, @Color)", tag, transaction);
        }

        transaction.Commit();
        return changed;
    });

    private static ArgumentOutOfRangeException UnknownKind(CatalogItem item) =>
        new(nameof(item), item.Kind, "Unknown catalog item kind.");

    /// <summary>
    /// The client stored on a project.
    /// Sub-projects store none, because they take their client from the top-level project.
    /// </summary>
    private static long? OwnClientId(long? clientId, long? parentId) => parentId is null ? clientId : null;

    /// <summary>
    /// Walks up from the project's new parent, to stop a project being moved inside itself.
    /// </summary>
    private static void ThrowIfInsideItself(SqliteConnection connection, Project project)
    {
        var ancestorId = project.ParentId;
        while (ancestorId is { } id)
        {
            if (id == project.Id)
            {
                throw new ArgumentException("A project can't be inside itself or one of its sub-projects.", nameof(project));
            }

            ancestorId = connection.ExecuteScalar<long?>("SELECT parent_id FROM project WHERE id = @Id", new { Id = id });
        }
    }

    /// <summary>
    /// Wraps a write so a name clash becomes a <see cref="DuplicateNameException"/> for <paramref name="name"/>.
    /// </summary>
    private static Func<SqliteConnection, T> RefuseDuplicateNames<T>(string name, Func<SqliteConnection, T> write) =>
        connection =>
        {
            try
            {
                return write(connection);
            }
            catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == SqliteUniqueConstraintFailed)
            {
                throw new DuplicateNameException(name);
            }
        };

    /// <summary>
    /// Inserts a row that has a name.
    /// </summary>
    /// <param name="name">The name reported if it clashes with a sibling's.</param>
    /// <returns>The new row's id.</returns>
    private async Task<long> InsertAsync(string name, string insertSql, object parameters)
    {
        var id = await RunAsync(RefuseDuplicateNames(name, connection =>
        {
            connection.Execute(insertSql, parameters);
            return connection.ExecuteScalar<long>("SELECT last_insert_rowid()");
        })).ConfigureAwait(false);
        Changed?.Invoke();
        return id;
    }

    /// <summary>
    /// Runs a write that returns how many rows it changed, and announces the change only when there was one.
    /// </summary>
    private async Task<bool> WriteRowsAsync(Func<SqliteConnection, int> write)
    {
        var changed = await RunAsync(write).ConfigureAwait(false) > 0;
        if (changed)
        {
            Changed?.Invoke();
        }

        return changed;
    }

    private Task<T> RunAsync<T>(Func<SqliteConnection, T> write) =>
        Task.Run(() =>
        {
            using var connection = database.Open();
            return write(connection);
        });
}
