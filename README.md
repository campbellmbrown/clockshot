# ClockShot

[![Build Status](https://github.com/campbellmbrown/clockshot/actions/workflows/ci.yml/badge.svg)](https://github.com/campbellmbrown/clockshot/actions/workflows/ci.yml)

ClockShot is a time tracker for Windows that keeps everything on your own computer.
It needs no account and sends nothing over the internet.

## Status

ClockShot is a work in progress.
It records your time, but can't show it to you yet, and there are no releases.

## Features

- It checks which app and window you're using every second.
- It notices when you're away from the keyboard and mouse, and when the screen is locked.
  You choose in Settings how many minutes without input count as away.
- You can pause tracking from the Tracking menu, the tray icon or the status bar.
  The window and tray icons change while it's paused.
- The status bar shows whether you're active, away, locked or paused, and which window is being recorded.
- While you're active, it takes a screenshot of the window you're using every minute, and shortly after you switch apps.
  A screenshot copies the screen where the window is, so it also shows any window on top of it.
  A screenshot identical to the one before is skipped.
  Screenshots are kept for 30 days, then deleted.
  In Settings you can turn screenshots off, change how often they're taken, and stop the one after an app switch.
  The 30 days is a default that you can change in `settings.json`.
- It stays out of the way in the system tray.
  Closing the window keeps it running there, unless you turn that off in Settings.
- Everything it stores is in one folder that you can open, back up, or delete.
- Starting it while it's already running brings the open window to the front.

## Building from source

See [CONTRIBUTING.md](CONTRIBUTING.md) for what you need and how to build and run it.
A copy run from source keeps its data in `%LOCALAPPDATA%\ClockShot-dev`, away from the folder an installed copy uses.

## Your data

ClockShot keeps its data in `%LOCALAPPDATA%\ClockShot`.
To use another folder, start it with `--data-folder <path>`.

- `clockshot.db` holds what ClockShot has recorded.
  It's saved at least every 15 seconds, so a crash loses very little.
- `settings.json` holds your settings.
- `screenshots\` holds the screenshots, in a folder for each day.
- `logs\` holds a log file for each day, and keeps the newest 14.
  Help > Open log folder opens it.

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md) before you start.

## Licence

ClockShot is released under the [MIT License](LICENSE).
