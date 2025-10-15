# TaskTui

TaskTui is a terminal task manager built with Terminal.Gui for .NET 8 that combines a reusable task list widget, application menu, filters, and multiple calendar-inspired views for organizing your work without leaving the command line.

## Features

- **Rich task metadata** – Capture title, completion state, due date, optional start/end times, priority, tags, and multi-line notes via validation-aware dialogs.
- **Keyboard-driven task list** – Expand/collapse details, navigate with vim-style keys, and trigger add/edit/toggle/delete actions from the reusable `TaskListView` component.
- **Filtering and search** – Quickly switch between all/open/done/today/due/overdue filters or search by title and tag from menus, status bar shortcuts, or global hotkeys
- **Multiple perspectives** – Toggle between the main list and calendar, week strip, or day timeline views, all sharing the same task widget for consistency.
- **Persistent storage** – Tasks are saved as formatted JSON in `~/.tasktui/tasks.json`, making it easy to back up or sync across machines.

## Installation

1. Install the .NET 8 SDK.
2. Clone the repository and restore dependencies (Terminal.Gui 1.19.0 is referenced automatically).
3. Run `dotnet build` to compile the application.

## Usage

- Start the TUI with `dotnet run`.
- Use `a`, `e`, `Space`, and `d` to add, edit, toggle completion, or delete the selected task. `Enter` expands/collapses details.
- Switch filters with `A`, `o`, `D`, `t`, `T`, `O`, or `n`, and search titles/tags with `/` and `#`.
- Press `v`, `c`, or `l` to toggle between task list and calendar views; open the week (`w`) or day (`d`) detail screens from the calendar.
- Hit `?` to show or hide the status bar cheatsheet.

## Data

Task data is stored in JSON under `~/.tasktui/tasks.json`. Deleting the file gives you a fresh start; syncing the folder keeps tasks consistent across machines.

## Development

- Run `dotnet build` (and optionally `dotnet run`) during development.
- The codebase centers on the reusable `TaskListView` plus feature-specific screens (calendar/week/day) that share the same store interface for persistence.

