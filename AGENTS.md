# AGENTS.md

Guidance for AI agents working in this repo.
Read [CONTRIBUTING.md](CONTRIBUTING.md) first.
Everything in it applies to you too.

## Layout

- [src/ClockShot.App](src/ClockShot.App) is the Avalonia desktop app.
- [src/ClockShot.Core](src/ClockShot.Core) holds the domain model and pure logic, with no UI, database, or OS code.
- [src/ClockShot.Data](src/ClockShot.Data) stores data on disk.
- [tests](tests) has a test project for each project it tests.

## Working with the user

- If something is unclear, ask instead of assuming.
- If a request conflicts with the code, say so and ask how to proceed.
- Don't undo the user's changes without asking.
- Don't change anything beyond the task without asking first.
  When you report back, list every change the user didn't ask for.
- Apply the guidelines in CONTRIBUTING.md with judgement.
  Fix code a newcomer would have to stop and work out.
  Keep common C# idioms, such as `ex` in a catch or `(_, e)` in an event handler.
- Before you report back, read your whole diff as a reviewer would, against CONTRIBUTING.md's code style.
  If a newcomer would stop to work out a line, fix it.
  Passing tests and a clean build don't count as this review.

## Git

- Check `git status` before staging.
  Don't stage changes the task didn't make without asking.
- Stage specific files with `git add <path>`, not `git add -A`.
- Never rewrite history.
  Don't amend, rebase, or force push.
  Fix a commit with a new one.

## Writing

- End each sentence with a full stop.
  Don't join clauses with semicolons, em dashes, or chains of commas.
- Write complete sentences.
  Each one must make sense on its own, so don't start one with "So" or "Because".
- Use plain ASCII that's easy to type and search for, such as straight quotes.
  Write "...", "-" and ">" instead of typographic characters.
- Don't start list items with a bold label.

## Code comments

- Keep a comment to one short sentence where you can.
  Say why, not what.
  Leave it out when the name already says it.
- Put `<summary>` tags on their own lines, even around one sentence.
  Keep other doc tags, such as `<param>` and `<exception>`, on one line unless the text doesn't fit.
- Leave out examples unless one makes an unclear term or format clear.
  Never name files or tools that aren't in this repo.
- Don't escape markup such as `&lt;`.
- Reread every comment you added before you report back.
