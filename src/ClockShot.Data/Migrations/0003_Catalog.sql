-- The clients, projects and tasks that time is tagged with.

CREATE TABLE client (
    id          INTEGER PRIMARY KEY,
    name        TEXT NOT NULL UNIQUE COLLATE NOCASE,
    is_archived INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE project (
    id          INTEGER PRIMARY KEY,
    client_id   INTEGER REFERENCES client(id),
    parent_id   INTEGER REFERENCES project(id),
    name        TEXT NOT NULL COLLATE NOCASE,
    is_archived INTEGER NOT NULL DEFAULT 0
);
-- A unique index treats NULLs as different, so a missing client or parent counts as 0 to make sibling names clash.
CREATE UNIQUE INDEX ux_project_sibling_name ON project(IFNULL(client_id, 0), IFNULL(parent_id, 0), name);

CREATE TABLE task (
    id          INTEGER PRIMARY KEY,
    project_id  INTEGER NOT NULL REFERENCES project(id),
    name        TEXT NOT NULL COLLATE NOCASE,
    is_archived INTEGER NOT NULL DEFAULT 0,
    UNIQUE (project_id, name)
);
