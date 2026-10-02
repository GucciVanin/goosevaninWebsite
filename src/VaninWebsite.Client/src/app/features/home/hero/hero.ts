import { Component, output } from '@angular/core';
import { profile } from '../../../shared/models/profile';

// Keeps the introductory content independently tunable from the home-page layout.
@Component({
  selector: 'app-hero',
  templateUrl: './hero.html',
  styleUrl: './hero.scss',
})
export class Hero {
  readonly navigate = output<string>();
  protected readonly profile = profile;
}
