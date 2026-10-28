import { Component, output } from '@angular/core';
import { ArtScene } from '../../shared/brand/art-scene';
import { Approach } from './approach/approach';
import { Categories } from './categories/categories';
import { Hero } from './hero/hero';
import { SelectedWork } from './selected-work/selected-work';

// Composes focused home sections without owning their individual presentation logic.
@Component({
  selector: 'app-home-page',
  imports: [ArtScene, Hero, Categories, Approach, SelectedWork],
  templateUrl: './home-page.html',
  styleUrl: './home-page.scss',
})
export class HomePage {
  readonly navigate = output<string>();
}
