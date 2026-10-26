import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ContactPage } from './contact-page';

// F2-US1 tests 2 and 4: an invalid field is identified in words next to it, and nothing is sent until the form is valid.
describe('ContactPage field errors', () => {
  let fixture: ComponentFixture<ContactPage>;
  let http: HttpTestingController;
  let root: HTMLElement;

  const control = (id: string) => root.querySelector<HTMLInputElement>(`#${id}`)!;
  const error = (id: string) => root.querySelector<HTMLElement>(`#${id}-error`);

  async function set(id: string, value: string): Promise<void> {
    const element = control(id);
    element.value = value;
    element.dispatchEvent(new Event(element instanceof HTMLSelectElement ? 'change' : 'input'));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  async function blur(id: string): Promise<void> {
    control(id).dispatchEvent(new Event('blur'));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  async function submit(): Promise<void> {
    root.querySelector<HTMLFormElement>('form')!.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  async function fillValid(): Promise<void> {
    await set('contact-name', 'Test Visitor');
    await set('contact-email', 'visitor@example.test');
    await set('contact-reason', 'Personal note');
    await set('contact-message', 'A question.');
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ContactPage],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ContactPage);
    root = fixture.nativeElement;
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  it('shows no error before the person has touched anything', () => {
    expect(root.querySelectorAll('.gcv-status--alert').length).toBe(0);
    expect(control('contact-name').getAttribute('aria-invalid')).not.toBe('true');
  });

  const cases: { id: string; text: string }[] = [
    { id: 'contact-name', text: 'Enter your name' },
    { id: 'contact-email', text: 'Enter a valid email address' },
    { id: 'contact-reason', text: 'Choose a reason' },
    { id: 'contact-message', text: 'Write a message' },
  ];

  for (const { id, text } of cases) {
    it(`leaving an empty ${id} shows "${text}" next to it, tied with aria-describedby`, async () => {
      await blur(id);
      const message = error(id);
      expect(message?.textContent?.trim()).toBe(text);
      expect(message?.classList).toContain('gcv-status--alert');
      expect(control(id).getAttribute('aria-invalid')).toBe('true');
      expect(control(id).getAttribute('aria-describedby')).toContain(`${id}-error`);
      expect(message?.previousElementSibling).toBe(control(id));
      for (const other of cases.filter((c) => c.id !== id)) expect(error(other.id)).toBeNull();
    });
  }

  it('a malformed email shows the email error and a corrected one removes it', async () => {
    await set('contact-email', 'not-an-email');
    await blur('contact-email');
    expect(error('contact-email')?.textContent?.trim()).toBe('Enter a valid email address');
    expect(control('contact-email').getAttribute('aria-invalid')).toBe('true');
    await set('contact-email', 'visitor@example.test');
    expect(error('contact-email')).toBeNull();
    expect(control('contact-email').getAttribute('aria-invalid')).not.toBe('true');
    expect(control('contact-email').getAttribute('aria-describedby')).toBeNull();
  });

  it('submitting an empty form shows every error and sends nothing', async () => {
    const button = root.querySelector<HTMLButtonElement>('button[type="submit"]')!;
    expect(button.disabled).toBeFalse();
    await submit();
    for (const { id, text } of cases) expect(error(id)?.textContent?.trim()).toBe(text);
    http.expectNone('/api/contact');
  });

  it('a submit attempt on an invalid form moves keyboard focus to the first invalid field', async () => {
    document.body.appendChild(root);
    await set('contact-name', 'Test Visitor');
    control('contact-email').focus();
    await submit();
    expect(document.activeElement).toBe(control('contact-email'));
    await set('contact-email', 'visitor@example.test');
    await submit();
    expect(document.activeElement).toBe(control('contact-reason'));
    root.remove();
  });

  it('submitting a valid form posts once and shows the verification status', async () => {
    await fillValid();
    await submit();
    const request = http.expectOne('/api/contact');
    expect(request.request.method).toBe('POST');
    request.flush({});
    fixture.detectChanges();
    const status = root.querySelector('[role="status"]');
    expect(status?.textContent).toContain('confirm your address');
  });
});
