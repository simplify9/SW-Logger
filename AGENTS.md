# AGENTS.md

## Critical-journey tests (verify)

This repository's critical journeys are tested in `SW.Logger.IntegrationTests/`; run them with `dotnet test SW.Logger.IntegrationTests`.
They gate releases through the `verify / result` check.

When you change this repository:
- If you change something a journey uses (a route, screen, form, API call, auth or data shape),
  update the affected tests and their mocks or recordings in the same PR, and run them.
- If you add a feature that becomes a core flow, add a journey for it. Keep 3–5 journeys in all:
  replace the least important one instead of growing the list.
- If you remove a flow, remove its test. Never skip, delete or loosen a test just to get green.
- Same rules as when they were written: never a real environment (dev, staging or production),
  and never real credentials or personal data in tests, mocks or recordings.
