import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
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
  protected readonly isSubmitting = signal(false);
  protected readonly submitted = signal(false);
  protected readonly error = signal('');

  protected submit(form: { name: string; email: string; reason: string; message: string; website?: string }): void {
    this.isSubmitting.set(true);
    this.error.set('');
    this.http.post('/api/contact', form).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.submitted.set(true);
      },
      error: () => {
        this.isSubmitting.set(false);
        this.error.set('The message could not be sent. Please try again or email me directly.');
      },
    });
  }
}
