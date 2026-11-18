# ClockShot

[![Build Status](https://github.com/campbellmbrown/clockshot/actions/workflows/ci.yml/badge.svg)](https://github.com/campbellmbrown/clockshot/actions/workflows/ci.yml)

ClockShot is a time tracker for Windows that records which apps you use and when.
It also takes screenshots, so later you can see what you spent your time on.
Everything stays on your own computer.
It needs no account and sends nothing over the internet.

## Status

ClockShot is a work in progress, and there are no releases yet.
It already records your activity and takes screenshots, and the Timeline tab shows them.
Selecting and tagging time on the timeline is still being built.

## What it does

- It records which app and window you're using, and notices when you're away or the screen is locked.
- While you're active, it takes a screenshot of the window you're using every minute and soon after you switch apps.
  You can list apps it never captures, such as a password manager.
  A screenshot copies the part of the screen where the window is.
  It also shows any window on top, even one from an excluded app.
- It keeps screenshots for 30 days, then deletes them.
- On the Timeline tab, hovering over a time shows the screenshot from then.
  It appears beside the pointer, below the timeline or in a separate window, and Ctrl+P switches between them.
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
