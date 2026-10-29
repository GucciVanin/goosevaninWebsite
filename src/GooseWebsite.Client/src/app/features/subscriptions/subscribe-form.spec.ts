import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SubscribeForm } from './subscribe-form';

// F10-US1: the subscribe form has labelled inputs, a honeypot people never see, errors and states in words, and
// it sends nothing until name and email are usable.
describe('SubscribeForm', () => {
  let fixture: ComponentFixture<SubscribeForm>;
  let http: HttpTestingController;
  let root: HTMLElement;

  const input = (id: string) => root.querySelector<HTMLInputElement>(`#${id}`)!;
  const error = (id: string) => root.querySelector<HTMLElement>(`#${id}-error`);
  const generic = 'Thanks. If that address can be subscribed, a confirmation email is on its way. Open the link in it to finish.';

  async function settle(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  async function set(id: string, value: string): Promise<void> {
    const element = input(id);
    element.value = value;
    element.dispatchEvent(new Event('input'));
    await settle();
  }

  async function submit(): Promise<void> {
    root.querySelector<HTMLFormElement>('form')!.dispatchEvent(new Event('submit'));
    await settle();
  }

  async function create(idPrefix = 'subscribe'): Promise<void> {
    fixture = TestBed.createComponent(SubscribeForm);
    fixture.componentRef.setInput('idPrefix', idPrefix);
    root = fixture.nativeElement;
    await settle();
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SubscribeForm],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
    await create();
  });

  afterEach(() => http.verify());

  it('labels the name and email inputs and names the form', () => {
    expect(root.querySelector('label[for="subscribe-name"]')?.textContent?.trim()).toBe('Your name');
    expect(root.querySelector('label[for="subscribe-email"]')?.textContent?.trim()).toBe('Email address');
    expect(input('subscribe-name').getAttribute('autocomplete')).toBe('name');
    expect(input('subscribe-email').type).toBe('email');
    expect(root.querySelector('form')?.getAttribute('aria-label')).toBeTruthy();
    expect(root.querySelector('button[type="submit"]')?.textContent).toContain('Subscribe');
  });

  it('keeps a honeypot field that people never see or reach', () => {
    const trap = root.querySelector<HTMLInputElement>('input[name="website"]')!;

    expect(trap).not.toBeNull();
    expect(trap.tabIndex).toBe(-1);
    expect(trap.getAttribute('autocomplete')).toBe('off');
    expect(trap.closest('[aria-hidden="true"]')).not.toBeNull();
    expect(root.querySelector('label[for="' + trap.id + '"]')).toBeNull();
  });

  it('uses ids that stay unique when the form appears twice on a page', async () => {
    const first = root;
    await create('footer');

    const ids = [...first.querySelectorAll('[id]'), ...root.querySelectorAll('[id]')].map((element) => element.id);
    expect(new Set(ids).size).toBe(ids.length);
  });

  it('shows no error before the person has touched anything', () => {
    expect(root.querySelectorAll('.gcv-status--alert').length).toBe(0);
  });

  it('submitting an empty form shows both errors in words and sends nothing', async () => {
    await submit();

    expect(error('subscribe-name')?.textContent?.trim()).toBe('Enter your name');
    expect(error('subscribe-email')?.textContent?.trim()).toBe('Enter a valid email address');
    expect(input('subscribe-name').getAttribute('aria-invalid')).toBe('true');
    expect(input('subscribe-name').getAttribute('aria-describedby')).toContain('subscribe-name-error');
    http.expectNone('/api/subscriptions');
  });

  it('moves keyboard focus to the first invalid field after a failed submit', async () => {
    document.body.appendChild(root);
    await set('subscribe-name', 'Ada Visitor');
    await submit();

    expect(document.activeElement).toBe(input('subscribe-email'));
    root.remove();
  });

  it('rejects a malformed email and clears the error when corrected', async () => {
    await set('subscribe-name', 'Ada Visitor');
    await set('subscribe-email', 'not-an-email');
    await submit();
    expect(error('subscribe-email')?.textContent?.trim()).toBe('Enter a valid email address');

    await set('subscribe-email', 'ada@example.test');
    expect(error('subscribe-email')).toBeNull();
    http.expectNone('/api/subscriptions');
  });

  it('limits the name to 100 characters', async () => {
    expect(input('subscribe-name').maxLength).toBe(100);
  });

  it('posts the name, email and an empty honeypot once, saying "Subscribing" while it waits, then shows the answer in words', async () => {
    await set('subscribe-name', 'Ada Visitor');
    await set('subscribe-email', 'ada@example.test');
    await submit();

    const request = http.expectOne('/api/subscriptions');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ name: 'Ada Visitor', email: 'ada@example.test', website: '' });
    expect(root.querySelector('button[type="submit"]')?.textContent).toContain('Subscribing');
    expect(root.querySelector<HTMLButtonElement>('button[type="submit"]')?.disabled).toBeTrue();

    request.flush({ message: generic });
    fixture.detectChanges();

    const status = root.querySelector('[role="status"]');
    expect(status?.textContent).toContain(generic);
    expect(root.querySelector('form')).toBeNull();
  });

  it('sends whatever a script put in the honeypot, so the server can drop it', async () => {
    await set('subscribe-name', 'Bot');
    await set('subscribe-email', 'bot@example.test');
    const trap = root.querySelector<HTMLInputElement>('input[name="website"]')!;
    trap.value = 'http://spam.example';
    trap.dispatchEvent(new Event('input'));
    await settle();
    await submit();

    expect(http.expectOne('/api/subscriptions').request.body.website).toBe('http://spam.example');
  });

  it('says in words when too many attempts were made and keeps the form usable', async () => {
    await set('subscribe-name', 'Ada Visitor');
    await set('subscribe-email', 'ada@example.test');
    await submit();

    http.expectOne('/api/subscriptions').flush({}, { status: 429, statusText: 'Too Many Requests' });
    fixture.detectChanges();

    expect(root.querySelector('[role="alert"]')?.textContent).toContain('Too many attempts');
    expect(root.querySelector('form')).not.toBeNull();
    expect(root.querySelector<HTMLButtonElement>('button[type="submit"]')?.disabled).toBeFalse();
  });

  it('says in words that something went wrong for any other failure', async () => {
    await set('subscribe-name', 'Ada Visitor');
    await set('subscribe-email', 'ada@example.test');
    await submit();

    http.expectOne('/api/subscriptions').flush({}, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    expect(root.querySelector('[role="alert"]')?.textContent).toContain('could not be completed');
  });
});
