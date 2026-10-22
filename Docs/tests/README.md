# Test list and reports

The Testing role runs the list below as the last check, three times per test, and writes a report per run here.

## Commands (fixed, so every run is the same)

| Layer | Command | Notes |
| --- | --- | --- |
| .NET unit and API tests | `dotnet test GooseWebsite.slnx -p:SkipClientBuild=true` | Build the client once first. |
| Angular unit tests | `cd src/GooseWebsite.Client; $env:CHROME_BIN="C:\Program Files\Google\Chrome\Application\chrome.exe"; npm test -- --watch=false --karma-config=karma.conf.js --browsers=ChromeHeadlessNoSandbox` | |
| Container runtime tests | `./scripts/verify.ps1 -SkipCi` (PowerShell) | Resets and rebuilds the stack; writes `verify-results/`. Use the CI gate (without `-SkipCi`) once the branch has a pull request. |
| CI | `gh pr checks <number> --watch` | First check; must be green before the container run is reported. |

## Determinism

The container run uses seeded test administrators and a unique suffix per run for created content; the clock is real, so tests compare relative values only. Record the environment (commit, Docker image, date) in each report. A test whose result differs between its three runs is `fail` with reason `nondeterministic`.

## Dynamic list

The runtime tests are the files in `verify/runtime/` plus the xUnit and Karma suites; each story's acceptance tests map to tests by name (see the evidence paragraph of the story in `Docs/product-backlog.md`). A new acceptance criterion adds a test; a fixed security finding adds a regression test; a removed test is recorded in the work item's `decisions.md`.
