# Worker O2 - W2 (R1-G6): dependency audit

## Files
- `src/GooseWebsite.Client/package.json`: `@angular/{common,compiler,core,forms,platform-browser,router,compiler-cli}` ^20.3.33 -> ^20.3.34; `markdown-it` ^15.0.2 -> ^15.0.3.
- `src/GooseWebsite.Client/package-lock.json`: those resolve to 20.3.34 / 15.0.3 (plus transitive 3.0.2 -> 3.0.4). Targeted update only: `npm audit fix` was tried first and rejected because it also rewrote dev tooling (new nested `@angular-devkit/*`, `qs`). Plain `npm install <pkg>@20.3.34` failed with ERESOLVE (the Angular packages pin each other's peers exactly), so the old Angular entries were removed from the lockfile and `npm install` re-resolved them.
- `Docs/architecture.md` section 11: the audit row corrected (it claimed the high findings were only the Karma chain; the gate showed otherwise for production). Now says production is clean and the full audit's remaining highs are build tooling.

## Results
- `npm audit --omit=dev` (client): 0 vulnerabilities. `verify/browser`: 0. No moderates remain in production, so nothing to justify.
- Karma: 200/200 SUCCESS (markdown tests included).
- `@rollup/rollup-linux-x64-gnu` kept: still in `optionalDependencies` of package.json and in the lockfile (`node_modules/@rollup/rollup-linux-x64-gnu` 4.59.0). Doubt: the old lockfile root entry listed it also under `dependencies` as exact "4.59.0", which did not match package.json; npm dropped that stale line. `npm ci` consistency is better now; the Docker build is proven in the final verification run.
- Container verification: run once at the end of O3 (covers O1 and O2 together).

## Doubts
- The full `npm audit` (with dev) still reports 7 high / 3 moderate / 1 low in dev tooling; fixes are breaking. Documented, not applied.
