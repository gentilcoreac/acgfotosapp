import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatBadgeModule } from '@angular/material/badge';
import { MatBottomSheet } from '@angular/material/bottom-sheet';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';
import { catchError, of } from 'rxjs';
import {
  CarritoStore,
  FamiliaCatalogoService,
  FamiliaGaleriaService,
  FamiliaSessionStore,
  FotoFamilia,
  TamanoPrecio,
} from '../../../../core/familia';
import { AgregarCarritoBottomSheetComponent } from './agregar-carrito-bottom-sheet.component';
import { FotoFamiliaImgComponent } from './foto-familia-img.component';
import {
  FotoFamiliaPreviewDialogComponent,
  FotoFamiliaPreviewDialogData,
} from './foto-familia-preview-dialog.component';

/** Cantidad de fotos por fila de la grilla ("chico" = más fotos, más chicas; "lista" = una por fila con detalle). */
export type DensidadGrilla = 2 | 4 | 'lista';

/**
 * Galería mobile-first de la familia (Fase 2): saluda con los datos del canje (sin round-trip,
 * `FamiliaSessionStore` ya los tiene) y muestra la grilla de fotos de la sesión — individuales de
 * cada participante + grupales del/los grupo(s) — con vista ampliada (carrusel, ver
 * `FotoFamiliaPreviewDialogComponent`) al tocar una miniatura. Sin layout admin
 * (`frontend/CLAUDE.md`): pantalla propia, pensada para pantalla de celular.
 */
@Component({
  selector: 'tbi-mi-album',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatBadgeModule, MatButtonModule, MatIconModule, FotoFamiliaImgComponent],
  templateUrl: './mi-album.component.html',
  styleUrl: './mi-album.component.scss',
})
export class MiAlbumComponent {
  private readonly session = inject(FamiliaSessionStore);
  private readonly galeriaService = inject(FamiliaGaleriaService);
  private readonly catalogoService = inject(FamiliaCatalogoService);
  private readonly dialog = inject(MatDialog);
  private readonly bottomSheet = inject(MatBottomSheet);
  private readonly router = inject(Router);

  protected readonly carrito = inject(CarritoStore);

  readonly nombreEvento = this.session.nombreEvento;
  readonly participantes = this.session.participantes;

  readonly saludo = computed(() => {
    const nombres = this.participantes()
      .map((p) => p.nombre)
      .join(' y ');
    return nombres ? `Hola, familia de ${nombres}` : 'Hola';
  });

  protected readonly fotosResource = rxResource({
    stream: () =>
      this.galeriaService.listar().pipe(
        // el toast de error lo emite el errorInterceptor (global)
        catchError(() => of<FotoFamilia[]>([])),
      ),
  });

  protected readonly tamanosPreciosResource = rxResource({
    stream: () => this.catalogoService.listarTamanosPrecios().pipe(catchError(() => of<TamanoPrecio[]>([]))),
  });

  protected readonly fotos = computed(() => this.fotosResource.value() ?? []);
  protected readonly tamanosPrecios = computed(() => this.tamanosPreciosResource.value() ?? []);
  protected readonly cargando = computed(() => this.fotosResource.isLoading());

  /** "Pantallazo general" por default (pedido 2026-07-19); el botón de arriba a la derecha cambia esto. */
  protected readonly densidad = signal<DensidadGrilla>(4);

  protected readonly gridTemplateColumns = computed(() => {
    const d = this.densidad();
    return d === 'lista' ? '1fr' : `repeat(${d}, 1fr)`;
  });

  protected verPreview(foto: FotoFamilia): void {
    const fotos = this.fotos();
    const index = fotos.findIndex((f) => f.id === foto.id);
    this.dialog.open(FotoFamiliaPreviewDialogComponent, {
      data: {
        fotos,
        index: index === -1 ? 0 : index,
        tamanosPrecios: this.tamanosPrecios(),
      } satisfies FotoFamiliaPreviewDialogData,
      autoFocus: false,
      maxWidth: '100vw',
      width: '96vw',
      height: '94vh',
    });
  }

  /** Selector rápido de tamaño+cantidad para esa foto sin salir de la grilla (mobile-first). */
  protected agregarAlCarrito(foto: FotoFamilia, evento: Event): void {
    evento.stopPropagation();
    this.bottomSheet.open(AgregarCarritoBottomSheetComponent, {
      data: { fotoId: foto.id, tamanosPrecios: this.tamanosPrecios() },
    });
  }

  protected irAlCarrito(): void {
    this.router.navigateByUrl('/carrito');
  }

  protected nombreParticipante(foto: FotoFamilia): string {
    if (foto.participanteId == null) {
      return 'Grupal';
    }
    return (
      this.participantes().find((p) => p.id === foto.participanteId)?.nombre ??
      'Foto'
    );
  }
}
