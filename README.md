# ClockShot

[![Build Status](https://github.com/campbellmbrown/clockshot/actions/workflows/ci.yml/badge.svg)](https://github.com/campbellmbrown/clockshot/actions/workflows/ci.yml)

ClockShot is a time tracker for Windows that keeps everything on your own computer.
It needs no account and sends nothing over the internet.

## Status

ClockShot is a work in progress.
It doesn't track time yet, and there are no releases.

## Features

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

- `settings.json` holds your settings.
- `logs\` holds a log file for each day, and keeps the newest 14.
  Help > Open log folder opens it.

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md) before you start.

## Licence

ClockShot is released under the [MIT License](LICENSE).
