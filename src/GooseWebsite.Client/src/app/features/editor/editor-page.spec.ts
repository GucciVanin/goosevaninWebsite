import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AuthApi } from '../auth/api/auth-api';
import { AuthenticatedUser } from '../auth/models/authenticated-user';
import { StoryApi } from './api/story-api';
import { EditorPage } from './editor-page';
import { StorySummary } from './models/story';

type Page = {
  search: { set(value: string): void };
  filter: { set(value: string): void };
  pendingDelete: { set(value: number | null): void };
  confirmDelete(story: StorySummary): void;
};

function summary(id: number, title: string, isPublished: boolean): StorySummary {
  return {
    id,
    title,
    slug: `s-${id}`,
    category: 'Notes',
    isPublished,
    createdAtUtc: '2026-10-01T10:00:00Z',
    updatedAtUtc: '2026-10-0' + id + 'T10:00:00Z',
    publishedAtUtc: isPublished ? '2026-10-02T10:00:00Z' : null,
  };
}

// The workspace: every story with its state in words, filters, search, empty states and confirmed deletion.
describe('EditorPage', () => {
  let api: jasmine.SpyObj<StoryApi>;
  let fixture: ComponentFixture<EditorPage>;

  async function open(list$ = of([summary(1, 'Alpha trip', true), summary(2, 'Beta notes', false)])): Promise<Page> {
    api = jasmine.createSpyObj<StoryApi>('StoryApi', ['list', 'remove']);
    api.list.and.returnValue(list$);
    const auth = jasmine.createSpyObj<AuthApi>('AuthApi', ['getCurrentUser']);
    auth.getCurrentUser.and.returnValue(of({ displayName: 'Site administrator' } as unknown as AuthenticatedUser));
    await TestBed.configureTestingModule({
      imports: [EditorPage],
      providers: [provideRouter([]), { provide: StoryApi, useValue: api }, { provide: AuthApi, useValue: auth }],
    }).compileComponents();
    fixture = TestBed.createComponent(EditorPage);
    fixture.detectChanges();
    return fixture.componentInstance as unknown as Page;
  }

  function text(): string {
    fixture.detectChanges();
    return fixture.nativeElement.textContent as string;
  }

  it('lists each story with its state in words and its last update date', async () => {
    await open();

    const rows = (fixture.nativeElement as HTMLElement).querySelectorAll('.story-list li');
    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('Alpha trip');
    expect(rows[0].textContent).toContain('Published');
    expect(rows[1].textContent).toContain('Draft');
    expect(rows[1].textContent).toContain('Updated 2026-10-02');
    expect(rows[1].querySelector('a')?.getAttribute('href')).toBe('/editor/stories/2');
  });

  it('filters by status and searches by title', async () => {
    const page = await open();

    page.filter.set('draft');
    expect(text()).toContain('Beta notes');
    expect(text()).not.toContain('Alpha trip');

    page.filter.set('all');
    page.search.set('ALPHA');
    expect(text()).toContain('Alpha trip');
    expect(text()).not.toContain('Beta notes');

    page.search.set('nothing like this');
    expect(text()).toContain('No story matches');
  });

  it('says so when there are no stories yet, and offers New story', async () => {
    await open(of([]));

    expect(text()).toContain('There are no stories yet');
    expect(text()).toContain('New story');
  });

  it('says so when the stories cannot be loaded', async () => {
    await open(throwError(() => new Error('down')) as never);

    expect(text()).toContain('could not be loaded');
  });

  it('deletes only after the explicit confirmation, and then removes the row', async () => {
    const page = await open();
    api.remove.and.returnValue(of(undefined));

    page.pendingDelete.set(2);
    expect(text()).toContain('for good?');
    expect(api.remove).not.toHaveBeenCalled();

    page.confirmDelete(summary(2, 'Beta notes', false));

    expect(api.remove).toHaveBeenCalledWith(2);
    expect(text()).toContain('Deleted "Beta notes".');
    expect((fixture.nativeElement as HTMLElement).querySelectorAll('.story-list li').length).toBe(1);
  });

  it('keeps the row when the deletion fails', async () => {
    const page = await open();
    api.remove.and.returnValue(throwError(() => new Error('nope')));

    page.confirmDelete(summary(2, 'Beta notes', false));

    expect(text()).toContain('could not be deleted');
    expect((fixture.nativeElement as HTMLElement).querySelectorAll('.story-list li').length).toBe(2);
  });
});
