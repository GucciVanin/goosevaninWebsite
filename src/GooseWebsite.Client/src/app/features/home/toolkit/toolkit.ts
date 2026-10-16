import { Component } from '@angular/core';

// Keeps the skills content separate from the layout that presents it.
@Component({
  selector: 'app-toolkit',
  templateUrl: './toolkit.html',
  styleUrl: './toolkit.scss',
})
export class Toolkit {
  protected readonly areas = [
    { index: '01', title: 'Frontend', token: '--gcv-magenta', skills: ['Angular', 'TypeScript', 'HTML & CSS', 'Accessibility'] },
    { index: '02', title: 'Backend', token: '--gcv-cyan', skills: ['.NET', 'Node.js', 'REST', 'Event-driven systems'] },
    { index: '03', title: 'Infrastructure', token: '--gcv-green', skills: ['Azure', 'CI/CD', 'Containers', 'Observability'] },
  ];
}
