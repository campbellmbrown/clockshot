# ClockShot

[![Build Status](https://github.com/campbellmbrown/clockshot/actions/workflows/ci.yml/badge.svg)](https://github.com/campbellmbrown/clockshot/actions/workflows/ci.yml)

ClockShot is a time tracker for Windows that records which apps you use and when.
It also takes screenshots, so later you can see what you spent your time on.
Everything stays on your own computer.
It needs no account and sends nothing over the internet.

## Status

ClockShot is a work in progress, and there are no releases yet.
It already records your activity and takes screenshots, and the Timeline tab shows them.
You can tag time with a project, a task and a note, and edit, delete or undo tags.
Resizing tags, tag colours and the Projects and Timesheet tabs are still being built.

## What it does

- It records which app and window you're using, and notices when you're away or the screen is locked.
- While you're active, it takes a screenshot of the window you're using every minute and soon after you switch apps.
  You can list apps it never captures, such as a password manager.
  A screenshot copies the part of the screen where the window is.
  It also shows any window on top, even one from an excluded app.
- It keeps screenshots for 30 days, then deletes them.
- On the Timeline tab, hovering over a time shows the screenshot from then.
  It appears beside the pointer, below the timeline or in a separate window, and Ctrl+P switches between them.
  Below the timeline and in the window, the wheel zooms into the screenshot.
  Ctrl+L pauses the preview on the screenshot it's showing, so you can move the pointer away.
- Clicking a block on the timeline shows its details, such as how long it lasted and the applications used in it.
  Dragging across the timeline selects a time range, snapping to the edges of blocks unless you hold Alt.
  While you hover, a dashed line shows where a drag would snap to.
  Dragging an edge of the selection makes it longer or shorter.
  Right-clicking the timeline opens a menu to zoom to the selection, clear it, or fit the view to the working day or the whole day.
  Right-clicking a block outside the selection selects it first.
  Escape clears the selection.
- The details panel can tag the selected block or time range with a project, a task and a note, or add a new project.
  The project box finds a project by every word typed, from its name, its client or the projects it sits inside.
  A blank task means the project in general, and a new task name creates the task.
  Enter on the timeline jumps to the project box, and Enter in the editor tags the time.
  Clicking a tag edits it, Delete deletes it, and Ctrl+Z undoes the last change.
  Tagged time shows on the Tags lane, in a colour picked from the project's name.
- The dividers under the timeline and beside the details panel can be dragged, and their sizes are remembered.
  Double-clicking the details divider hides or shows the panel.
- The status bar shows today's active time.
- You can pause it from the Tracking menu, the tray icon or the status bar.
- Closing its window keeps it running in the system tray.
- A day starts at midnight.
  If you work past midnight, you can start it later in Settings.

The Settings tab lets you change how often screenshots are taken, how long they're kept and more.
Help > Keyboard shortcuts lists every shortcut.

## Getting started

There's no installer yet, so ClockShot has to be built from source.
[CONTRIBUTING.md](CONTRIBUTING.md) says what you need and how to build and run it.
Started with `dotnet run`, it keeps its data in `%LOCALAPPDATA%\ClockShot-dev`, which [launchSettings.json](src/ClockShot.App/Properties/launchSettings.json) sets.

## Your data

ClockShot keeps its data in `%LOCALAPPDATA%\ClockShot`.
To use another folder, start it with `--data-folder <path>`.

- `clockshot.db` and the files whose names start with it hold what ClockShot has recorded.
  ClockShot saves to them at least every 15 seconds, so a crash loses very little.
- `settings.json` holds your settings.
- `screenshots\` holds the screenshots, in a folder for each day.
- `logs\` holds a log file for each day, and keeps the newest 14.
  Help > Open log folder opens it.

To remove everything ClockShot has stored, exit it from File > Exit or the tray icon, then delete its data folder.

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md) before you start.

## Licence

ClockShot is released under the [MIT License](LICENSE).
