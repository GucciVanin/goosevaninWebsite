import { Component } from '@angular/core';
import { SubscribeForm } from './subscribe-form';

// A short home section that invites visitors to subscribe (F10-US1). The same form also sits in the footer.
@Component({
  selector: 'app-subscribe-section',
  imports: [SubscribeForm],
  templateUrl: './subscribe-section.html',
  styleUrl: './subscribe-section.scss',
})
export class SubscribeSection {}
