import type { TestInfo } from '@playwright/test';
import type { Result } from 'axe-core';
import { log, type Defect } from './known';

/**
 * Turns axe violations into defects, one per rule and element: `axe:<rule>:<element selector>`. A known-issues entry
 * therefore names the element it covers, and a later violation of the same rule on another element is a new defect.
 * Critical and serious violations block; the others are only logged.
 */
export function axeDefects(info: TestInfo, violations: Result[]): Defect[] {
  const blocking: Defect[] = [];
  for (const violation of violations) {
    const blockingImpact = violation.impact === 'critical' || violation.impact === 'serious';
    for (const node of violation.nodes) {
      const target = node.target.join(' ');
      const detail = `${violation.impact}: ${violation.help} (${target})`;
      if (blockingImpact) blocking.push({ id: `axe:${violation.id}:${target}`, detail });
      else log(info, `axe (not blocking) ${violation.id} ${detail}`);
    }
  }
  return blocking;
}
