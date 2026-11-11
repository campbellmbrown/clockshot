# AGENTS.md

Guidance for AI agents working in this repo.
Read [CONTRIBUTING.md](CONTRIBUTING.md) first.
Everything in it applies to you too.

## Layout

- [src/ClockShot.App](src/ClockShot.App) is the Avalonia desktop app.

## Tests

- Don't use real people, companies, or products in tests or sample data.

## Working with the user

- If something is unclear, ask instead of assuming.
- If a request conflicts with the code, say so and ask how to proceed.
- Don't undo the user's changes without asking.

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
- Use plain punctuation that's easy to type and search for, such as straight quotes.
- Don't start list items with a bold label.
