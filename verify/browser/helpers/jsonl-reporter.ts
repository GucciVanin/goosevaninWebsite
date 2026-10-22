import { appendFileSync, writeFileSync } from 'node:fs';
import type { FullConfig, Reporter, TestCase, TestResult } from '@playwright/test/reporter';

// Writes one JSON line per test per browser: {result, name, ms, detail}. verify/stages/40-browser.sh turns the
// lines into the verification log and JSON. The full detail (every violation) goes to the details file.
const target = process.env.VERIFY_BROWSER_RESULTS ?? '/tmp/browser-results.jsonl';
const details = process.env.VERIFY_BROWSER_DETAILS ?? `${target}.details.log`;
const ansi = new RegExp(String.fromCharCode(27) + '\\[[0-9;]*m', 'g');

export default class JsonlReporter implements Reporter {
  onBegin(_config: FullConfig): void {
    writeFileSync(target, '');
    writeFileSync(details, '');
  }

  onTestEnd(test: TestCase, result: TestResult): void {
    const project = test.parent.project()?.name ?? 'unknown';
    const known = result.annotations.find((a) => a.type === 'known');
    const logs = result.annotations.filter((a) => a.type === 'log').map((a) => a.description ?? '');
    const errors = result.errors.map((e) => (e.message ?? '').replace(ansi, ''));

    let outcome: 'PASS' | 'FAIL' | 'KNOWN' | 'SKIP';
    let detail = '';
    if (result.status === 'skipped') outcome = 'SKIP';
    else if (result.status === 'passed') {
      outcome = known ? 'KNOWN' : 'PASS';
      detail = known?.description ?? '';
    } else {
      outcome = 'FAIL';
      detail = errors.join(' | ');
    }

    const name = `[${project}] ${test.title}`;
    const oneLine = detail.replace(/\s+/g, ' ').trim().slice(0, 600);
    appendFileSync(target, JSON.stringify({ result: outcome, name, ms: Math.round(result.duration), detail: oneLine }) + '\n');
    if (logs.length > 0 || errors.length > 0) {
      appendFileSync(details, `${outcome} ${name}\n${[...logs, ...errors].map((l) => `    ${l.replace(/\n/g, '\n    ')}`).join('\n')}\n`);
    }
  }
}
