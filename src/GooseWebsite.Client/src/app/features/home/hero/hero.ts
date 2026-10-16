import { Component, output } from '@angular/core';
import { profile } from '../../../shared/models/profile';

// The home entry point, built to the landing-page mockup: headline, three-line intro, and one action over the
// animated scene. Gustavo's name, title and location stay available to assistive technology and search.
@Component({
  selector: 'app-hero',
  templateUrl: './hero.html',
  styleUrl: './hero.scss',
})
export class Hero {
  readonly navigate = output<string>();
  protected readonly profile = profile;

  protected onExplore(event: Event): void {
    event.preventDefault();
    this.navigate.emit('work');
  }
}
