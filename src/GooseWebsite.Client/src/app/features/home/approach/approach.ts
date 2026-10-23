import { Component } from '@angular/core';
import { profile } from '../../../shared/models/profile';

// Isolates the engineering-approach section so it can be reused and revised independently.
@Component({
  selector: 'app-approach',
  templateUrl: './approach.html',
  styleUrl: './approach.scss',
})
export class Approach {
  protected readonly profile = profile;
}
