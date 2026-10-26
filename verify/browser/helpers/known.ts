import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import type { TestInfo } from '@playwright/test';

// The known-issues mechanism (TE5). A defect found by a browser test is not fixed inside TE5; it is listed in
// verify/browser/known-issues.json and reported as KNOWN (not PASS) until the story named in `fixedBy` fixes it.
//  - A defect that matches no entry fails the test.
//  - An entry that no longer reproduces fails the test ("remove this known issue").
// Entry: { id, screen, kind, description, fixedBy, browsers? }. `browsers` (optional) limits an entry to the
// engines where the defect is engine-specific.

export interface Defect {
  /** Stable identifier: this is what a known-issues entry matches on (for example "axe:color-contrast"). */
  id: string;
  /** Human-readable evidence, logged in full. */
  detail: string;
}

export interface KnownIssue {
  id: string;
  screen: string;
  kind: string;
  description: string;
  fixedBy: string;
  browsers?: string[];
}

const issues: KnownIssue[] = JSON.parse(readFileSync(join(__dirname, '..', 'known-issues.json'), 'utf8'));

/** Adds a line to the test's log (shown in browser-details.log). */
export function log(info: TestInfo, text: string): void {
  info.annotations.push({ type: 'log', description: text });
}

function applicableTo(all: KnownIssue[], browser: string, screen: string, kind: string): KnownIssue[] {
  return all.filter((issue) => issue.screen === screen && issue.kind === kind && (!issue.browsers || issue.browsers.includes(browser)));
}

/** The pure decision: which problems a check's defects cause, given the known-issues entries. */
export function evaluate(all: KnownIssue[], browser: string, screen: string, kind: string, defects: Defect[]): { problems: string[]; reported: Defect[]; fixedBy: string[] } {
  const applicable = applicableTo(all, browser, screen, kind);
  const known = new Set(applicable.map((issue) => issue.id));
  const unique = [...new Map(defects.map((defect) => [defect.id, defect])).values()];
  const unknown = unique.filter((defect) => !known.has(defect.id));
  const gone = applicable.filter((issue) => !unique.some((defect) => defect.id === issue.id));
  const problems = [
    ...unknown.map((defect) => `new defect ${defect.id}: ${defect.detail}`),
    ...gone.map((issue) => `known issue ${issue.id} no longer reproduces: remove this known issue from verify/browser/known-issues.json`),
  ];
  return { problems, reported: unique, fixedBy: [...new Set(applicable.map((issue) => issue.fixedBy))] };
}

/** Decides the outcome of a check from the defects it found. Throws when the test must fail. */
export function settle(info: TestInfo, screen: string, kind: string, defects: Defect[]): void {
  const browser = info.project.name;
  const { problems, reported, fixedBy } = evaluate(issues, browser, screen, kind, defects);
  const known = new Set(applicableTo(issues, browser, screen, kind).map((issue) => issue.id));
  for (const defect of reported) log(info, `${known.has(defect.id) ? 'known' : 'DEFECT'} ${defect.id}: ${defect.detail}`);
  if (problems.length > 0) throw new Error(problems.join('\n'));
  if (reported.length > 0) {
    info.annotations.push({ type: 'known', description: `${reported.map((defect) => defect.id).join(', ')} (fixed by ${fixedBy.join(', ')})` });
  }
}
