import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TbiFileResource, TbiFileUploadComponent } from './tbi-file-upload.component';

function changeEventWith(file: File): Event {
  const input = document.createElement('input');
  Object.defineProperty(input, 'files', { value: [file] });
  return { target: input } as unknown as Event;
}

@Component({
  imports: [TbiFileUploadComponent],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <tbi-file-upload
      [readAs]="readAs()"
      accept=".xlsx,.xls"
      [preview]="false"
      (fileSelected)="resource.set($event)"
      (rawFileSelected)="rawFile.set($event)"
    />
  `,
})
class HostComponent {
  readonly readAs = signal<'base64' | 'file'>('base64');
  readonly resource = signal<TbiFileResource | null>(null);
  readonly rawFile = signal<File | null>(null);
}

describe('TbiFileUploadComponent', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;
  let upload: TbiFileUploadComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [HostComponent] }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
    upload = fixture.debugElement.query((de) => de.name === 'tbi-file-upload').componentInstance;
  });

  it("en modo 'file' emite el File crudo sin pasar por FileReader", () => {
    host.readAs.set('file');
    fixture.detectChanges();
    const readerSpy = vi.spyOn(FileReader.prototype, 'readAsDataURL');

    const file = new File(['x'], 'DimTest.xlsx', { type: '' });
    upload['onFileChange'](changeEventWith(file));

    expect(host.rawFile()?.name).toBe('DimTest.xlsx');
    expect(host.resource()).toBeNull();
    expect(readerSpy).not.toHaveBeenCalled();
  });

  it("en modo 'file' sigue validando el accept (extensión no permitida no emite)", () => {
    host.readAs.set('file');
    fixture.detectChanges();

    upload['onFileChange'](changeEventWith(new File(['x'], 'malo.exe', { type: '' })));

    expect(host.rawFile()).toBeNull();
  });

  it("en modo 'file' sigue validando el tamaño máximo", () => {
    host.readAs.set('file');
    fixture.detectChanges();
    const big = new File([new Uint8Array(3 * 1024 * 1024)], 'grande.xlsx', { type: '' });

    upload['onFileChange'](changeEventWith(big));

    expect(host.rawFile()).toBeNull();
  });
});
