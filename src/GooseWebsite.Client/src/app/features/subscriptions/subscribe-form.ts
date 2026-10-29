import { HttpErrorResponse } from '@angular/common/http';
import { Component, ElementRef, inject, input, signal } from '@angular/core';
import { FormsModule, NgForm, NgModel } from '@angular/forms';
import { SubscriptionApi } from './subscription-api';

// The subscribe form (F10-US1), used in the footer and in the home section. Labelled inputs, a honeypot people never
// reach, errors and states in words. The server answers the same way for every address, so the success text never
// claims that the address was accepted.
@Component({
  selector: 'app-subscribe-form',
  imports: [FormsModule],
  templateUrl: './subscribe-form.html',
  styleUrl: './subscribe-form.scss',
})
export class SubscribeForm {
  private readonly api = inject(SubscriptionApi);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  /** Keeps ids unique when the form appears more than once on a page. */
  readonly idPrefix = input('subscribe');
  /** The accessible name of the form. */
  readonly label = input('Subscribe to new stories and projects');

  protected readonly isSubmitting = signal(false);
  protected readonly done = signal('');
  protected readonly error = signal('');
  // Set once a submit was attempted, so every invalid field shows its error even if it was never visited.
  protected readonly attempted = signal(false);

  protected shows(control: NgModel): boolean {
    return !!control.invalid && (!!control.touched || this.attempted());
  }

  private focusFirstInvalid(form: NgForm): void {
    const name = Object.keys(form.controls).find((key) => form.controls[key].invalid);
    if (name) this.host.nativeElement.querySelector<HTMLElement>(`[name="${name}"]`)?.focus();
  }

  protected submit(form: NgForm): void {
    if (form.invalid) {
      this.attempted.set(true);
      this.focusFirstInvalid(form);
      return;
    }
    if (this.isSubmitting()) return;
    const value = form.value as { name: string; email: string; website?: string };
    this.isSubmitting.set(true);
    this.error.set('');
    this.api.subscribe(value.name, value.email, value.website ?? '').subscribe({
      next: ({ message }) => {
        this.isSubmitting.set(false);
        this.attempted.set(false);
        this.done.set(message);
      },
      error: (response: HttpErrorResponse) => {
        this.isSubmitting.set(false);
        this.error.set(
          response.status === 429
            ? 'Too many attempts. Please try again in a few minutes.'
            : 'The subscription could not be completed. Please try again later.',
        );
      },
    });
  }
}
