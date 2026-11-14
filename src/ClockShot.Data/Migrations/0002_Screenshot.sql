-- Times are UTC Unix milliseconds.
-- The image path is relative to the screenshot folder, with '/' between folders.
CREATE TABLE screenshot (
    id         INTEGER PRIMARY KEY,
    taken_utc  INTEGER NOT NULL,
    image_path TEXT NOT NULL,
    width      INTEGER NOT NULL,
    height     INTEGER NOT NULL
);
CREATE INDEX ix_screenshot_taken ON screenshot(taken_utc);
