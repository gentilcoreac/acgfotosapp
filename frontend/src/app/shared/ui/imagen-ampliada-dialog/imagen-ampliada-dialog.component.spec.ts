import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';
import { ImagenAmpliadaDialogComponent, ImagenAmpliadaDialogData } from './imagen-ampliada-dialog.component';

describe('ImagenAmpliadaDialogComponent', () => {
  let fixture: ComponentFixture<ImagenAmpliadaDialogComponent>;

  async function setup(data: ImagenAmpliadaDialogData): Promise<void> {
    await TestBed.configureTestingModule({
      imports: [ImagenAmpliadaDialogComponent],
      providers: [{ provide: MAT_DIALOG_DATA, useValue: data }],
    }).compileComponents();

    fixture = TestBed.createComponent(ImagenAmpliadaDialogComponent);
    fixture.detectChanges();
  }

  it('muestra la imagen recibida por data, sin flechas de recorrido (colección de un solo ítem)', async () => {
    await setup({ src: 'data:image/png;base64,abc', alt: 'QR de Ana Pérez' });

    const img = fixture.nativeElement.querySelector('img') as HTMLImageElement;
    expect(img.src).toContain('data:image/png;base64,abc');
    expect(img.alt).toBe('QR de Ana Pérez');
    expect(fixture.nativeElement.querySelector('.nav')).toBeNull();
    expect(fixture.nativeElement.querySelector('.contador')).toBeNull();
  });
});
