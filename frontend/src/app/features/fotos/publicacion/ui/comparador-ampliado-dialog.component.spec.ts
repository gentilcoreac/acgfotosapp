import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';
import {
  ComparadorAmpliadoDialogComponent,
  ComparadorAmpliadoDialogData,
} from './comparador-ampliado-dialog.component';

describe('ComparadorAmpliadoDialogComponent', () => {
  let fixture: ComponentFixture<ComparadorAmpliadoDialogComponent>;

  const RESULTADOS = [
    { ladoMayorPedido: 300, ancho: 300, alto: 200, dpi: 50, pesoBytes: 5000, previewUrl: 'blob:a' },
    { ladoMayorPedido: 600, ancho: 600, alto: 400, dpi: 100, pesoBytes: 12000, previewUrl: 'blob:b' },
  ];

  async function setup(data: ComparadorAmpliadoDialogData): Promise<void> {
    await TestBed.configureTestingModule({
      imports: [ComparadorAmpliadoDialogComponent],
      providers: [{ provide: MAT_DIALOG_DATA, useValue: data }],
    }).compileComponents();

    fixture = TestBed.createComponent(ComparadorAmpliadoDialogComponent);
    fixture.detectChanges();
  }

  it('abre en el índice pedido, con recorrido entre las muestras', async () => {
    await setup({ resultados: RESULTADOS, index: 1 });

    expect((fixture.nativeElement.querySelector('.tamano-grande__preview') as HTMLImageElement).src).toContain(
      'blob:b',
    );
    expect(fixture.nativeElement.textContent).toContain('Lado mayor 600px');
    expect(fixture.nativeElement.querySelector('.contador')?.textContent?.trim()).toBe('2 / 2');
  });

  it('avisa cuando el dpi queda por debajo del umbral aceptable', async () => {
    await setup({ resultados: RESULTADOS, index: 0 });

    expect(fixture.nativeElement.textContent).toContain('Se va a ver borroso al imprimir');
  });
});
