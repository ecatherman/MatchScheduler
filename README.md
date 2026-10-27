# 🏐 Volleyball Scheduler

A small Windows app for quickly building a rotating volleyball schedule for a medium to large group.

**[Download the latest version](https://github.com/ecatherman/MatchScheduler/releases/latest/download/VolleyballScheduler-win-x64.exe)**
(single file, no install needed)

## What it does

- Keep a list of players with a desired position, phone number and email (only the name is required).
- Pick who is coming by tapping names, or add someone new on the spot by typing a name and pressing Enter.
- Choose the length of each time slot, the number of courts, and the ideal number of players per team.
- Get a round-by-round schedule where sitting out rotates fairly, teams are reshuffled every round, and
  desired positions are spread across teams.
- Save schedules, or copy one as plain text to paste into a group chat.

## Your data

Everything is stored locally in a SQLite database at
`%LocalAppData%\VolleyballScheduler\scheduler.db`. Nothing is sent anywhere. Deleting that folder resets the app.

Windows may show a "protected your PC" warning the first time, because the app isn't code-signed.
Choose **More info**, then **Run anyway**.

## Building from source

Requires the .NET 10 SDK on Windows.

```
git clone --recurse-submodules https://github.com/ecatherman/MatchScheduler.git
cd REPO
dotnet run --project MatchScheduler
```

The `CathermanCore` submodule provides the shared database base context (`BaseContext`).

## Releasing

Push a version tag and GitHub Actions builds the single-file exe and attaches it to a release:

```
git tag v1.0.0
git push origin v1.0.0
```

## License

[MIT](LICENSE)
