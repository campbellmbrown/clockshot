-- A colour the user chose for a tag, which is a project and task pair.
-- A null task_id means the project's General task.
-- A tag with no row gets an automatic colour, picked from its label.
CREATE TABLE tag_color (
    project_id INTEGER NOT NULL REFERENCES project(id) ON DELETE CASCADE,
    task_id    INTEGER REFERENCES task(id) ON DELETE CASCADE,
    color      TEXT NOT NULL
);
CREATE UNIQUE INDEX ux_tag_color ON tag_color(project_id, IFNULL(task_id, 0));
