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
                    .Select(t => new ProjectTask(t.Id, t.ProjectId, t.Name, t.IsArchived)));
        }, cancellationToken);

    public async Task<Client> AddClientAsync(string name)
    {
        name = name.Trim();
        var id = await InsertAsync(
            name,
            "INSERT INTO client (name) VALUES (@Name)",
            new { Name = name }).ConfigureAwait(false);
        return new Client(id, name, IsArchived: false);
    }

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

    public async Task<ProjectTask> AddTaskAsync(long projectId, string name)
    {
        name = name.Trim();
        var id = await InsertAsync(
            name,
            "INSERT INTO task (project_id, name) VALUES (@Project, @Name)",
            new { Project = projectId, Name = name }).ConfigureAwait(false);
        return new ProjectTask(id, projectId, name, IsArchived: false);
    }

    /// <summary>
    /// The client stored on a project.
    /// Sub-projects store none, because they take their client from the top-level project.
    /// </summary>
    private static long? OwnClientId(long? clientId, long? parentId) => parentId is null ? clientId : null;

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

    private Task<T> RunAsync<T>(Func<SqliteConnection, T> write) =>
        Task.Run(() =>
        {
            using var connection = database.Open();
            return write(connection);
        });
}
