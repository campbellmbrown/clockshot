-- What the tracker saw.
-- Times are UTC Unix milliseconds.
-- Each range starts at start_utc and ends just before end_utc.

-- Windows and macOS ignore case in file names, so the same app can show up as Editor.exe and editor.exe.
-- NOCASE only ignores the case of ASCII letters, which covers almost every executable name.
-- The index needs its own COLLATE, because IFNULL's result compares with BINARY.
CREATE TABLE application (
    id            INTEGER PRIMARY KEY,
    process_name  TEXT NOT NULL COLLATE NOCASE,
    exe_path      TEXT COLLATE NOCASE,
    display_name  TEXT NOT NULL,
    color         TEXT NOT NULL
);
CREATE UNIQUE INDEX ux_application_exe ON application(process_name, IFNULL(exe_path, '') COLLATE NOCASE);

CREATE TABLE activity_segment (
    id             INTEGER PRIMARY KEY,
    start_utc      INTEGER NOT NULL,
    end_utc        INTEGER NOT NULL,
    application_id INTEGER NOT NULL REFERENCES application(id),
    window_title   TEXT NOT NULL
);
CREATE INDEX ix_activity_start ON activity_segment(start_utc);

-- The state is 0 for Active, 1 for Away and 2 for Locked.
CREATE TABLE computer_state_segment (
    id        INTEGER PRIMARY KEY,
    start_utc INTEGER NOT NULL,
    end_utc   INTEGER NOT NULL,
    state     INTEGER NOT NULL
);
CREATE INDEX ix_state_start ON computer_state_segment(start_utc);
