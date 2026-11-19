-- Time tagged with a project and, optionally, a task.
-- Times are UTC Unix milliseconds.
-- Each entry starts at start_utc and ends just before end_utc.
CREATE TABLE time_entry (
    id         INTEGER PRIMARY KEY,
    start_utc  INTEGER NOT NULL,
    end_utc    INTEGER NOT NULL,
    project_id INTEGER NOT NULL REFERENCES project(id),
    task_id    INTEGER REFERENCES task(id),
    note       TEXT
);
CREATE INDEX ix_time_entry_start ON time_entry(start_utc);
