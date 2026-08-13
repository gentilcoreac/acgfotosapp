import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { of } from 'rxjs';
import { ConfirmService } from '../../../../shared/feedback/confirm.service';
import { NotificationService } from '../../../../shared/feedback/notification.service';
import { MarcaAguaService } from '../data/marca-agua.service';
import { MODO_COLOCACION, MODO_FUSION, PerfilMarcaAgua } from '../domain/marca-agua.model';
import { MarcaAguaListComponent } from './marca-agua-list.component';
import { MarcaAguaPreviewAmpliadaDialogComponent } from './marca-agua-preview-ampliada-dialog.component';

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

describe('MarcaAguaListComponent', () => {
  let fixture: ComponentFixture<MarcaAguaListComponent>;
  let dialogOpen: Mock;
  let cargarAssets: Mock;

  async function setup(): Promise<void> {
    dialogOpen = vi.fn().mockName('dialogOpen');
    cargarAssets = vi.fn().mockName('cargarAssets').mockReturnValue(of(new Map()));

    await TestBed.configureTestingModule({
      imports: [MarcaAguaListComponent],
      providers: [
        {
          provide: MarcaAguaService,
          useValue: { crud: { getAll: vi.fn().mockReturnValue(of({ items: [PERFIL], totalCount: 1 })) }, cargarAssets },
        },
        { provide: MatDialog, useValue: { open: dialogOpen } },
        { provide: NotificationService, useValue: { success: vi.fn(), error: vi.fn() } },
        { provide: ConfirmService, useValue: { confirm: vi.fn() } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(MarcaAguaListComponent);
    fixture.detectChanges();
    await fixture.whenStable();
  }

  it('al hacer click en la marca renderizada la abre ampliada (imagen aislada, sin recorrido)', async () => {
    await setup();

    (fixture.nativeElement.querySelector('.perfil__canvas-btn') as HTMLButtonElement).click();

    expect(dialogOpen).toHaveBeenCalledWith(
      MarcaAguaPreviewAmpliadaDialogComponent,
      expect.objectContaining({
        data: expect.objectContaining({ perfil: PERFIL, ariaLabel: 'Marca del perfil Estándar' }),
      }),
    );
  });
});
