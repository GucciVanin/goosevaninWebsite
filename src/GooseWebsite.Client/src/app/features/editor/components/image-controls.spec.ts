import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { ImageApi, UploadedImage } from '../api/image-api';
import { ImageControls } from './image-controls';

type Controls = {
  inlineAlt: { set(value: string): void };
  chooseCover(event: Event): void;
  chooseInline(event: Event): void;
  removeCover(): void;
};

function pick(file: File | null): Event {
  const input = document.createElement('input');
  input.type = 'file';
  if (file) {
    const transfer = new DataTransfer();
    transfer.items.add(file);
    input.files = transfer.files;
  }

  return { target: input } as unknown as Event;
}

const uploaded: UploadedImage = { id: 1, url: '/uploads/0123456789abcdef0123456789abcdef.png', contentType: 'image/png', sizeBytes: 3, uploadedAtUtc: '2026-10-16T00:00:00Z' };

// The image controls: files are checked for a friendly message, uploads go through the API, and the cover can be
// replaced or removed. The text image needs a description first.
describe('ImageControls', () => {
  let api: jasmine.SpyObj<ImageApi>;
  let fixture: ComponentFixture<ImageControls>;
  let controls: Controls;
  const inserted: string[] = [];

  beforeEach(async () => {
    inserted.length = 0;
    api = jasmine.createSpyObj<ImageApi>('ImageApi', ['upload']);
    await TestBed.configureTestingModule({ imports: [ImageControls], providers: [{ provide: ImageApi, useValue: api }] }).compileComponents();
    fixture = TestBed.createComponent(ImageControls);
    fixture.componentInstance.insert.subscribe((markdown) => inserted.push(markdown));
    fixture.detectChanges();
    controls = fixture.componentInstance as unknown as Controls;
  });

  function text(): string {
    fixture.detectChanges();
    return fixture.nativeElement.textContent as string;
  }

  it('uploads a cover image and shows it with a field for its description', () => {
    api.upload.and.returnValue(of(uploaded));

    controls.chooseCover(pick(new File([new Uint8Array(3)], 'a.png', { type: 'image/png' })));

    expect(fixture.componentInstance.coverImage()).toBe(uploaded.url);
    expect(text()).toContain('Cover image description');
    expect((fixture.nativeElement as HTMLElement).querySelector('img.cover')?.getAttribute('src')).toBe(uploaded.url);
  });

  it('refuses a file that is not an allowed image, or over 5 MB, without calling the API', () => {
    controls.chooseCover(pick(new File(['x'], 'a.gif', { type: 'image/gif' })));
    expect(text()).toContain('Only JPEG, PNG and WebP');

    controls.chooseCover(pick(new File([new Uint8Array(5 * 1024 * 1024 + 1)], 'big.png', { type: 'image/png' })));
    expect(text()).toContain('larger than 5 MB');
    expect(api.upload).not.toHaveBeenCalled();
  });

  it('shows what the server said when it refuses an upload', () => {
    api.upload.and.returnValue(throwError(() => new HttpErrorResponse({ status: 400, error: { detail: 'Only JPEG, PNG and WebP images can be uploaded.' } })));

    controls.chooseCover(pick(new File(['x'], 'a.png', { type: 'image/png' })));

    expect(text()).toContain('Only JPEG, PNG and WebP images can be uploaded.');
    expect(fixture.componentInstance.coverImage()).toBe('');
  });

  it('needs a description before adding an image to the text, then offers Markdown for it', () => {
    api.upload.and.returnValue(of(uploaded));

    controls.chooseInline(pick(new File(['x'], 'a.png', { type: 'image/png' })));
    expect(text()).toContain('Describe the image first');
    expect(api.upload).not.toHaveBeenCalled();

    controls.inlineAlt.set('A red [door]');
    controls.chooseInline(pick(new File(['x'], 'a.png', { type: 'image/png' })));

    expect(inserted).toEqual([`![A red door](${uploaded.url})`]);
  });

  it('removes the cover image and its description', () => {
    fixture.componentInstance.coverImage.set(uploaded.url);
    fixture.componentInstance.coverImageAlt.set('A door');

    controls.removeCover();

    expect(fixture.componentInstance.coverImage()).toBe('');
    expect(fixture.componentInstance.coverImageAlt()).toBe('');
  });
});
