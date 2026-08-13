import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
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
import { NotificationService } from '../../../../shared/feedback/notification.service';
import { TbiSelectComponent, toSelectOptions } from '../../../../shared/ui/tbi-select/tbi-select.component';
import {
  AgregarCarritoBottomSheetComponent,
  AgregarCarritoBottomSheetData,
} from './agregar-carrito-bottom-sheet.component';
import { FotoFamiliaImgComponent } from './foto-familia-img.component';
import {
  FotoFamiliaPreviewDialogComponent,
  FotoFamiliaPreviewDialogData,
} from './foto-familia-preview-dialog.component';

/** Cantidad de fotos por fila de la grilla ("chico" = más fotos, más chicas; "compacta" = tantas como
 * entren según el ancho de pantalla, sin overlay de texto; "lista" = una por fila con detalle). */
export type DensidadGrilla = 2 | 4 | 'compacta' | 'lista';

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
  imports: [MatBadgeModule, MatButtonModule, MatIconModule, FotoFamiliaImgComponent, TbiSelectComponent],
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
  private readonly notification = inject(NotificationService);

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

  protected readonly densidad = signal<DensidadGrilla>(4);

  protected readonly gridTemplateColumns = computed(() => {
    const d = this.densidad();
    if (d === 'lista') {
      return '1fr';
    }
    if (d === 'compacta') {
      // Auto-fill: la cantidad de columnas sale sola del ancho disponible, sin fijar un número.
      return 'repeat(auto-fill, minmax(72px, 1fr))';
    }
    return `repeat(${d}, 1fr)`;
  });

  /** "Seleccionar varias" (ver `docs/ClaudeDesign`): tildar fotos sin abrir el preview de cada una y
   * mandarlas todas al carrito de una, en el tamaño elegido en `tamanoLoteId` (selector propio, no un
   * default fijo — se puede cambiar antes de agregar). */
  protected readonly modoSeleccion = signal(false);
  protected readonly seleccionadas = signal<ReadonlySet<number>>(new Set());
  protected readonly tamanoLoteId = signal<number | null>(null);

  protected readonly opcionesTamano = computed(() => toSelectOptions(this.tamanosPrecios(), (t) => t.nombre));

  /** Filtro de repaso (pedido de Alberto 2026-07-19): antes de confirmar, ver solo las fotos que ya
   * tienen copias en el carrito, sin tener que ir y volver de `/carrito`. */
  protected readonly soloEnCarrito = signal(false);

  protected readonly fotosFiltradas = computed(() => {
    if (!this.soloEnCarrito()) {
      return this.fotos();
    }
    const idsEnCarrito = new Set(this.carrito.lineas().map((l) => l.fotoId));
    return this.fotos().filter((f) => idsEnCarrito.has(f.id));
  });

  constructor() {
    // Preselecciona el primer tamaño en cuanto llega el catálogo (mismo criterio que `AgregarCarritoComponent`).
    effect(() => {
      const primero = this.tamanosPrecios()[0];
      if (primero && untracked(this.tamanoLoteId) === null) {
        this.tamanoLoteId.set(primero.id);
      }
    });
  }

  protected toggleModoSeleccion(): void {
    this.modoSeleccion.update((v) => !v);
    this.seleccionadas.set(new Set());
  }

  protected toggleSeleccion(fotoId: number): void {
    this.seleccionadas.update((actual) => {
      const siguiente = new Set(actual);
      if (siguiente.has(fotoId)) {
        siguiente.delete(fotoId);
      } else {
        siguiente.add(fotoId);
      }
      return siguiente;
    });
  }

  protected onTileClick(foto: FotoFamilia): void {
    if (this.modoSeleccion()) {
      this.toggleSeleccion(foto.id);
    } else {
      this.verPreview(foto);
    }
  }

  /** Crea una línea de carrito (tamaño elegido en `tamanoLoteId`, 1 copia) por cada foto tildada. */
  protected agregarSeleccionadasAlCarrito(): void {
    const tamanoId = this.tamanoLoteId();
    const tamano = this.tamanosPrecios().find((t) => t.id === tamanoId);
    const seleccionadas = this.seleccionadas();
    if (!tamano || seleccionadas.size === 0) {
      return;
    }

    for (const fotoId of seleccionadas) {
      this.carrito.agregar(fotoId, tamano.id, 1);
    }

    const cantidad = seleccionadas.size;
    this.notification.success(
      `${cantidad} foto${cantidad === 1 ? '' : 's'} agregada${cantidad === 1 ? '' : 's'} al carrito en ${tamano.nombre} (1 copia c/u). Podés sumar más copias desde el carrito.`,
    );
    this.toggleModoSeleccion();
  }

  protected verPreview(foto: FotoFamilia): void {
    const fotos = this.fotosFiltradas();
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
      data: {
        fotoId: foto.id,
        tamanosPrecios: this.tamanosPrecios(),
        onAmpliar: () => this.verPreview(foto),
      } satisfies AgregarCarritoBottomSheetData,
    });
  }

  protected irAlCarrito(): void {
    this.router.navigateByUrl('/carrito');
  }

  protected cerrarSesion(): void {
    this.session.clearSession();
    this.router.navigateByUrl('/canje');
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
