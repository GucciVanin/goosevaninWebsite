import { Component, ElementRef, inject, signal } from '@angular/core';
import { FormsModule, NgForm, NgModel } from '@angular/forms';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { profile } from '../../shared/models/profile';

// Keeps contact submission and verification state isolated from site-wide UI.
@Component({
  selector: 'app-contact-page',
  imports: [FormsModule],
  templateUrl: './contact-page.html',
  styleUrl: './contact-page.scss',
})
export class ContactPage {
  protected readonly profile = profile;
  private readonly http = inject(HttpClient);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  protected readonly isSubmitting = signal(false);
  protected readonly submitted = signal(false);
  protected readonly error = signal('');
  // Set once a submit was attempted, so every invalid field shows its error even if it was never visited.
  protected readonly attempted = signal(false);

  // An error shows once the field was left or a submit was attempted; it is words, not colour alone.
  protected shows(control: NgModel): boolean {
    return !!control.invalid && (!!control.touched || this.attempted());
  }

  // Moves keyboard focus to the first invalid field so a screen-reader user hears what is wrong.
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
    const value = form.value as { name: string; email: string; reason: string; message: string; website?: string };
    this.isSubmitting.set(true);
    this.error.set('');
    this.http.post('/api/contact', value).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.attempted.set(false);
        this.submitted.set(true);
      },
      error: (response: HttpErrorResponse) => {
        this.isSubmitting.set(false);
        this.error.set(response.error?.error ?? 'The message could not be sent. Please try again or email me directly.');
      },
    });
  }
}
