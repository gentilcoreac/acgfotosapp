import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';
import { of } from 'rxjs';
import { MarcaAguaService } from '../data/marca-agua.service';
import { MODO_COLOCACION, MODO_FUSION, PerfilMarcaAgua } from '../domain/marca-agua.model';
import {
  MarcaAguaPreviewAmpliadaDialogComponent,
  MarcaAguaPreviewAmpliadaDialogData,
} from './marca-agua-preview-ampliada-dialog.component';

const PERFIL: PerfilMarcaAgua = {
  id: 1,
  nombre: 'Estándar',
  esDefault: true,
  marcarThumb: true,
  capas: [
    {
      id: 1,
      orden: 0,
      storageKey: 'k1',
      anchoPx: 200,
      altoPx: 100,
      modoColocacion: MODO_COLOCACION.Repetida,
      posicion: null,
      escalaPorcentaje: 20,
      margenPorcentaje: 5,
      separacionPorcentaje: 30,
      anguloGrados: 0,
      opacidad: 0.8,
      modoFusion: MODO_FUSION.Normal,
    },
  ],
};

describe('MarcaAguaPreviewAmpliadaDialogComponent', () => {
  let fixture: ComponentFixture<MarcaAguaPreviewAmpliadaDialogComponent>;

  async function setup(data: MarcaAguaPreviewAmpliadaDialogData): Promise<void> {
    const cargarAssets: Mock = vi.fn().mockName('cargarAssets').mockReturnValue(of(new Map()));

    await TestBed.configureTestingModule({
      imports: [MarcaAguaPreviewAmpliadaDialogComponent],
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: data },
        { provide: MarcaAguaService, useValue: { cargarAssets } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(MarcaAguaPreviewAmpliadaDialogComponent);
    fixture.detectChanges();
    await fixture.whenStable();
  }

  it('renderiza el canvas de la muestra pedida, sin flechas de recorrido (colección de un solo ítem)', async () => {
    await setup({
      perfil: PERFIL,
      variante: 'clara',
      fotoPropia: null,
      comprimir: false,
      calidad: 55,
      ancho: 960,
      alto: 640,
      ariaLabel: 'Vista previa sobre muestra Clara',
    });

    expect(fixture.nativeElement.querySelector('tbi-perfil-marca-agua-canvas')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.nav')).toBeNull();
    expect(fixture.nativeElement.querySelector('.contador')).toBeNull();
  });
});
