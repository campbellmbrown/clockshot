# Contributing

This page explains how to build ClockShot and what a good change looks like.

## Requirements

- Windows 10 or 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Optional: VS Code with the [recommended extensions](.vscode/extensions.json)

## Build, test, and format

```shell
dotnet build                          # warnings are errors
dotnet test                           # runs every test
dotnet format                         # fixes code style, naming, and layout
dotnet format --verify-no-changes     # fails if anything needs fixing
```

- The build checks compiler and code analysis warnings.
- `dotnet format` checks code style, naming, and layout against the rules in [.editorconfig](.editorconfig).
- [CI](.github/workflows/ci.yml) runs the build, all the tests, and the format check on every push and pull request.
- The same commands are available as [VS Code tasks](.vscode/tasks.json).

## Run the app

```shell
dotnet run --project src/ClockShot.App
```

Running from the repo keeps the app's data in `%LOCALAPPDATA%\ClockShot-dev`, set by [launchSettings.json](src/ClockShot.App/Properties/launchSettings.json).
This keeps dev runs away from the data of the copy you use every day.

## How changes are made

- Follow TDD, YAGNI, SOLID, DRY, and KISS.
- Split each test into steps marked `// Arrange`, `// Act`, and `// Assert`.
  Leave out a step the test doesn't need.
  - Win32 calls and XAML layout can't be test-driven.
    Check them by running the app.
  - Only add an interface for a second implementation or a test fake, such as the clock, database, OS, or dialogs.
- Keep unit tests fast.
  Don't make them wait on real time with delays or timeouts.
  Pass in a `TimeProvider` and use a fake one in the test, or pass the timeout in.
- Keep each commit to one change, such as a button, a setting, or a class with its tests.
- Remove code that a change leaves unused in the same commit.
- When you rename something, update every use.
  Don't leave an alias behind for the old name.
- Update the docs in the same commit as the change they describe.
- When you copy files from another project into the repo, such as icons, add that project's licence file next to them.
- When you add a package or a third-party asset, credit it in the About dialog, in [AboutViewModel.cs](src/ClockShot.App/ViewModels/AboutViewModel.cs).

## Code style

No tool checks these guidelines because they need judgement.
They aren't hard rules and can be broken when there's a good reason.

- Aim to keep most methods under 50 lines.
- Avoid nesting more than three levels deep.
  Deeper logic often reads better as a method with a clear name.
- Prefer three parameters or fewer for methods.
  When a method needs more, check whether some of them belong together in a type.
- Avoid boolean parameters that choose behaviour.
  Two methods with clear names usually read better.
  If a boolean is the right fit, pass it as a named argument, such as `Load(includeArchived: true)`.
- Name arguments whose values don't explain themselves, such as literal numbers or strings.
  Put each on its own line when there are several.
- Name boolean properties as affirmative phrases, such as `IsPaused`, `HasScreenshot`, or `CanUndo`.
- Prefer clear names to short ones.
  Avoid abbreviations.
  Single letters are fine only where they're the usual name, such as `x` and `y` for coordinates, or in a short lambda.
- Use comments to explain why, not what.
- Give numbers that aren't obvious a name.
  Explain the value's source next to its definition.
- Order members by kind and then by access, following StyleCop's [SA1201](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1201.md) and [SA1202](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1202.md).
  For example, fields come before constructors, and public members come before private ones.
- Keep members private unless another class needs them.
- Declare variables in the smallest scope that needs them.
- In XAML, put each attribute on its own line when an element has more than two.
  Always do this for the root element.
- Aim for methods that are easy to unit test.
  If one is hard to test, consider changing the method.

## Writing

These apply to docs, code comments, and commit messages.

- State the purpose before the details.
- Cut anything without a clear purpose.
- Keep each sentence short and focused on one point.
- Use plain, literal language instead of clever phrasing.
  A reader shouldn't have to pause to work out what you mean.
- In Markdown, put each sentence on its own line.
  Then editing a sentence changes only its own line, instead of reflowing the rest of the paragraph.
- Link to any file you mention.

## Definition of done

- `dotnet build` has no warnings.
- `dotnet format --verify-no-changes` passes.
- Tests affected by the change pass.
- The app starts, and you have checked any UI change by running it.
- The docs are up to date.
