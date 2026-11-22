# ClockShot

[![Build Status](https://github.com/campbellmbrown/clockshot/actions/workflows/ci.yml/badge.svg)](https://github.com/campbellmbrown/clockshot/actions/workflows/ci.yml)

ClockShot is a time tracker for Windows that records which apps you use and when.
It also takes screenshots, so later you can see what you spent your time on.
Everything stays on your own computer.
It needs no account and sends nothing over the internet.

## Status

ClockShot is a work in progress, and there are no releases yet.

## What it does

### Recording

- ClockShot records which app and window you're using, and notices when you're away or the screen is locked.
- While you're active, it takes a screenshot of the window you're using every minute and soon after you switch apps.
  You can list apps it never captures, such as a password manager.
  A screenshot copies the part of the screen where the window is, so it also shows any window on top.
  While a window from an excluded app covers part of it, ClockShot waits until that window is closed, minimised or moved off it.
- It keeps screenshots for 30 days, then deletes them.
- A day starts at midnight.
  If you work past midnight, you can start it later in Settings.

### Timeline

- On the Timeline tab, hovering over a time shows the screenshot from then.
  It appears beside the pointer, below the timeline or in a separate window, and Ctrl+P switches between them.
  Below the timeline and in the window, the wheel zooms into the screenshot.
  Ctrl+L pauses the preview on the screenshot it's showing, so you can move the pointer away.
- Clicking a block on the timeline shows its details, such as how long it lasted and the applications used in it.
- Dragging across the timeline selects a time range, snapping to the edges of blocks unless you hold Alt.
  While you hover, a dashed line shows where a drag would snap to.
  Dragging an edge of the selection makes it longer or shorter.
  Right-clicking the timeline opens a menu to zoom to the selection, clear it, or fit the view to the working day or the whole day.
  Right-clicking a block outside the selection selects it first.
  Escape clears the selection.
- The dividers under the timeline and beside the details panel can be dragged, and their sizes are remembered.
  Double-clicking the details divider hides or shows the panel.

### Tagging

- The details panel can tag the selected block or time range with a project, a task and a note.
  It can also add or edit a project, with its client and the project it sits inside.
- The project box finds a project by every word typed, from its name, its client or the projects it sits inside.
  A blank task means the project in general, and a new task name creates the task.
- Enter on the timeline jumps to the project box, and Enter in the editor tags the time.
  Clicking a tag edits it, Delete deletes it, and Ctrl+Z undoes the last change.
- Buttons for the six most recently used project and task pairs fill in the editor in one click.
- Dragging a tag's edge makes it longer or shorter, and trims any tag it now overlaps.
- Tagged time shows on the Tags lane, in a colour picked from its project and task, or one you choose.
- When you come back after being away for 10 minutes or more, it asks what you were doing, so you can tag that time.
  Settings can change how long that is, or turn the question off.

### Projects tab

- The Projects tab shows each client with its projects, and the time tagged to each one.
  You can choose whether the time covers this week, this month or all time.
- Under each project are the projects inside it, then its tasks.
  Time tagged with a blank task shows on a General row under its project.
  Task and General rows show a small square in their tag's colour.
- The search box finds a client, project or task with every word typed in its name.
  It also shows what each match sits inside, and everything inside it.
- Buttons above the tree add a client, a project or a task.
  A new project starts with the selected client, or inside the selected project.
  A new task goes under the selected project.
- Right-clicking a client, project or task opens a menu for it.
  A client's menu adds a project for that client or renames the client.
  A project's menu adds a project inside it or a task under it.
  It can also edit the project, with its client and the project it sits inside.
  A task's menu renames the task.
- The right-click menu can archive a client, project or task.
  An archived one, and everything inside it, is hidden on this tab and when you tag time.
  Its tagged time still counts in the totals.
  The Show archived box shows archived ones again, dimmed.
- Delete on that menu only removes a client with no projects, or a project or task with no tagged time.
  Otherwise ClockShot suggests archiving it instead.

### Timesheet tab

- The Timesheet tab shows the time tagged to each client, project and task, with a column for each day and totals.
  It opens on this week, and you can pick another period or choose the first and last day.
  A range longer than 31 days has a column for each week instead.
  Time that crosses the start of a day is split between the days.
- The arrow beside a client or project hides or shows what's inside it.
- A Notes column lists the notes on each task's tagged time.
- Rounding rounds each task's time on each day to the nearest 6, 15 or 30 minutes, or up to the next one.
  The totals add up the rounded times, and the choice is remembered.
  It can also be set on the Settings tab.
- Copy table copies the timesheet as a table to paste into a spreadsheet or a web timesheet.
  Save as CSV, or File > Export timesheet (Ctrl+E), saves it as a CSV file.
  Both have a line for each task, or General for time without a task, with times in decimal hours such as 1.25.
  Right-clicking a row offers Copy this row, which copies just that row.

### Status bar and tray

- The status bar shows today's active and tagged time.
- You can pause recording from the Tracking menu, the tray icon or the status bar.
- Closing the window keeps ClockShot running in the system tray.
- It can start when you sign in to Windows, and then show only its tray icon.
  Both choices are on the Settings tab.

### Settings and shortcuts

The Settings tab lets you change how often screenshots are taken, how long they're kept and more.
Help > Keyboard shortcuts lists every shortcut.

## Getting started

There's no installer yet, so ClockShot has to be built from source.
[CONTRIBUTING.md](CONTRIBUTING.md) says what you need and how to build and run it.
Started with `dotnet run`, it keeps its data in `%LOCALAPPDATA%\ClockShot-dev`, which [launchSettings.json](src/ClockShot.App/Properties/launchSettings.json) sets.

## Your data

ClockShot keeps its data in `%LOCALAPPDATA%\ClockShot`.
To use another folder, start it with `--data-folder <path>`.
File > Open data folder opens it, and the Settings tab shows where it is.

- `clockshot.db` and the files whose names start with it hold what ClockShot has recorded.
  ClockShot saves to them at least every 15 seconds, so a crash loses very little.
- `settings.json` holds your settings.
- `screenshots\` holds the screenshots, in a folder for each day.
- `logs\` holds a log file for each day, and keeps the newest 14.
  Help > Open log folder opens it.
- `backups\` holds the copies of `clockshot.db` that Back up now on the Settings tab makes.
  Each is named by the time it was made.
  Screenshots and settings aren't backed up.
  To restore a backup, exit ClockShot from File > Exit or the tray icon, then copy the backup over `clockshot.db`.
  Then delete `clockshot.db-wal` and `clockshot.db-shm` if they're there, because they hold changes to the old database.

To remove everything ClockShot has stored, exit it from File > Exit or the tray icon, then delete its data folder.

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md) before you start.

## Licence

ClockShot is released under the [MIT License](LICENSE).
Third-party licences are in [THIRD-PARTY-NOTICES.txt](src/ClockShot.App/THIRD-PARTY-NOTICES.txt), which ships next to the app.
