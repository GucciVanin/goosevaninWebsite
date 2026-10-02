import { Component, output } from '@angular/core';
import { Approach } from './approach/approach';
import { Hero } from './hero/hero';
import { SelectedWork } from './selected-work/selected-work';
import { Toolkit } from './toolkit/toolkit';

// Composes focused home sections without owning their individual presentation logic.
@Component({
  selector: 'app-home-page',
  imports: [Hero, Approach, SelectedWork, Toolkit],
  templateUrl: './home-page.html',
  styleUrl: './home-page.scss',
})
export class HomePage {
  readonly navigate = output<string>();
}
