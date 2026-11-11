# Contributing

This page explains how to build ClockShot and what a good change looks like.

## Requirements

- Windows 10 or 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Optional: VS Code with the [recommended extensions](.vscode/extensions.json)

## Build, run and format

```shell
dotnet build                          # warnings are errors
dotnet run --project src/ClockShot.App
dotnet format                         # fixes code style, naming and layout
dotnet format --verify-no-changes     # fails if anything needs fixing
```

The build checks compiler and code analysis warnings.
`dotnet format` checks code style, naming and layout against the rules in [.editorconfig](.editorconfig).
[CI](.github/workflows/ci.yml) runs the build and the format check on every push and pull request.
The same commands are available as [VS Code tasks](.vscode/tasks.json).

## How changes are made

- Follow TDD, YAGNI, SOLID, DRY and KISS.
  - Win32 calls and XAML layout can't be test-driven. Check them by running the app.
  - Only add an interface for a second implementation or a test fake, such as the clock, database, OS or dialogs.
- Keep each commit to one change, such as a button, a setting, or a class with its tests.
- Update [README.md](README.md) and this file in the same commit as the feature they describe.
- When you copy files from another project into the repo, such as icons, add that project's licence file next to them.

## Definition of done

- `dotnet build` has no warnings.
- `dotnet format --verify-no-changes` passes.
- The tests the change can affect pass.
- The app starts, and you have checked any UI change by running it.
- The docs are up to date.
