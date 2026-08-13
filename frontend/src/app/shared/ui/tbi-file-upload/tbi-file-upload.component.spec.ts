import type { Mock } from 'vitest';
import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { ImagenAmpliadaDialogComponent } from '../imagen-ampliada-dialog/imagen-ampliada-dialog.component';
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

@Component({
  imports: [TbiFileUploadComponent],
  template: `<tbi-file-upload accept="image/*" />`,
})
class HostConPreview {}

describe('TbiFileUploadComponent — preview ampliable', () => {
  let fixture: ComponentFixture<HostConPreview>;
  let dialogOpen: Mock;

  beforeEach(async () => {
    dialogOpen = vi.fn().mockName('dialogOpen');
    await TestBed.configureTestingModule({
      imports: [HostConPreview],
      providers: [{ provide: MatDialog, useValue: { open: dialogOpen } }],
    }).compileComponents();
    fixture = TestBed.createComponent(HostConPreview);
    fixture.detectChanges();
  });

  it('con un archivo elegido, la vista previa se puede ampliar (imagen aislada, sin recorrido)', () => {
    const upload = fixture.debugElement.query((de) => de.name === 'tbi-file-upload')
      .componentInstance as TbiFileUploadComponent;
    // Setea directo el resultado de `onFileChange` en vez de pasar por `FileReader` real (jsdom no
    // completa `readAsDataURL` de forma confiable en el entorno de test).
    upload['pickedDataUrl'].set('data:image/png;base64,eA==');
    upload['pickedName'].set('foto.png');
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.tbi-file__preview-btn') as HTMLButtonElement).click();

    expect(dialogOpen).toHaveBeenCalledWith(
      ImagenAmpliadaDialogComponent,
      expect.objectContaining({ data: expect.objectContaining({ alt: 'foto.png' }) }),
    );
  });

  it('sin archivo elegido no muestra el botón de ampliar', () => {
    expect(fixture.nativeElement.querySelector('.tbi-file__preview-btn')).toBeNull();
  });
});
