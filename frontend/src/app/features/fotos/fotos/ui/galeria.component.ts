import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  linkedSignal,
  signal,
} from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { catchError, concatMap, filter, from, interval, map, of, reduce, switchMap, tap } from 'rxjs';
import { saveBlobResponse } from '../../../../core/http';
import { ConfirmService } from '../../../../shared/feedback/confirm.service';
import { NotificationService } from '../../../../shared/feedback/notification.service';
import {
  TbiSelectComponent,
  TbiSelectOption,
} from '../../../../shared/ui/tbi-select/tbi-select.component';
import { GruposService } from '../../grupos/data/grupos.service';
import { EventosService } from '../../eventos/data/eventos.service';
import { Evento } from '../../eventos/domain/evento.model';
import { FotosService } from '../data/fotos.service';
import { EstadoProcesamientoFoto, Foto } from '../domain/foto.model';
import { FotoImgComponent } from './foto-img.component';
import { FotoPreviewDialogComponent } from './foto-preview-dialog.component';

/** Filtro de participante: todas las fotos del grupo (solo para MIRAR: no es un destino de subida). */
const TODAS = -1;
/** Filtro/destino: fotos grupales del grupo (participanteId null). */
const GRUPALES = 0;

/**
 * Archivos por request: la API acepta hasta 512 MB por tanda, pero mandar de a 10 mantiene cada
 * request corto (feedback de progreso) y acota el reintento si una tanda falla.
 */
const TAMANO_TANDA = 10;

/** Cada cuánto se refresca mientras haya fotos en Pendiente (el worker sigue procesando). */
const POLL_MS = 3000;

/**
 * Pantalla única de fotos del admin (unifica "Subir fotos" + "Galería", pedido 2026-07-14): el
 * fotógrafo elige evento → grupo → participante, sube ahí mismo y VE aparecer las fotos en la grilla
 * (thumbs con watermark, preview ampliado, descarga del original y borrado). El selector de participante
 * es a la vez filtro de la grilla y destino de la subida; con "Todas las fotos" solo se mira.
 */
// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-galeria',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    TbiSelectComponent,
    FotoImgComponent,
  ],
  templateUrl: './galeria.component.html',
  styleUrl: './galeria.component.scss',
})
export class GaleriaComponent {
  private readonly fotosService = inject(FotosService);
  private readonly gruposService = inject(GruposService);
  private readonly eventosService = inject(EventosService);
  private readonly notify = inject(NotificationService);
  private readonly confirmService = inject(ConfirmService);
  private readonly dialog = inject(MatDialog);

  // ── Cascada evento → grupo + selector de participante (filtro de la grilla Y destino de subida) ─────

  private readonly eventosResource = rxResource({
    stream: () =>
      this.eventosService.crud.getAll().pipe(
        map((result) => result.items),
        // el toast de error lo emite el errorInterceptor (global)
        catchError(() => of<Evento[]>([])),
      ),
  });
  protected readonly eventoOptions = computed<TbiSelectOption<number>[]>(
    () => this.eventosResource.value()?.map((e) => ({ value: e.id ?? 0, label: e.nombre })) ?? [],
  );
  protected readonly eventoId = signal<number | null>(null);

  protected readonly grupoId = linkedSignal<number | null, number | null>({
    source: this.eventoId,
    computation: () => null,
  });

  private readonly gruposResource = rxResource({
    params: () => this.eventoId() ?? undefined,
    stream: ({ params: eventoId }) =>
      this.gruposService.crud
        .getAllByCriteria({ eventoId, page: 0, pageSize: 500 })
        .pipe(map((result) => result.items)),
  });
  protected readonly grupoOptions = computed<TbiSelectOption<number>[]>(
    () => this.gruposResource.value()?.map((c) => ({ value: c.id ?? 0, label: c.nombre })) ?? [],
  );

  protected readonly participanteFiltro = linkedSignal<number | null, number>({
    source: this.grupoId,
    computation: () => TODAS,
  });

  private readonly grupoDetalleResource = rxResource({
    params: () => this.grupoId() ?? undefined,
    stream: ({ params: grupoId }) => this.gruposService.crud.getById(grupoId),
  });
  private readonly participantes = computed(() => this.grupoDetalleResource.value()?.participantes ?? []);

  protected readonly participanteOptions = computed<TbiSelectOption<number>[]>(() => [
    { value: TODAS, label: 'Todas las fotos' },
    { value: GRUPALES, label: 'Grupales del grupo' },
    ...this.participantes().map((a) => ({ value: a.id, label: a.nombre })),
  ]);
  private readonly participantePorId = computed(
    () => new Map(this.participantes().map((a) => [a.id, a.nombre])),
  );

  // ── Fotos del grupo (el filtro por participante se aplica en memoria: son pocas por grupo) ──────────

  protected readonly fotosResource = rxResource({
    params: () => this.grupoId() ?? undefined,
    stream: ({ params: grupoId }) =>
      this.fotosService.listar(grupoId).pipe(catchError(() => of<Foto[]>([]))),
  });

  protected readonly fotos = computed(() => {
    const filtro = this.participanteFiltro();
    const fotos = this.fotosResource.value() ?? [];
    if (filtro === TODAS) {
      return fotos;
    }
    if (filtro === GRUPALES) {
      return fotos.filter((f) => f.participanteId == null);
    }
    return fotos.filter((f) => f.participanteId === filtro);
  });

  protected readonly cantidadPendientes = computed(
    () =>
      (this.fotosResource.value() ?? []).filter(
        (f) => f.estadoProcesamiento === EstadoProcesamientoFoto.Pendiente,
      ).length,
  );

  constructor() {
    // Si hay fotos procesándose (p. ej. recién subidas), la grilla se refresca sola.
    interval(POLL_MS)
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        if (this.cantidadPendientes() > 0 && !this.subiendo()) {
          this.fotosResource.reload();
        }
      });
  }

  // ── Subida (al destino del selector de participante; con "Todas" no hay destino → no se sube) ───────

  protected readonly archivos = signal<File[]>([]);
  protected readonly subiendo = signal(false);
  protected readonly tandaActual = signal(0);
  protected readonly totalTandas = signal(0);

  /** Con "Todas las fotos" no hay destino de subida: hay que elegir grupales o un participante. */
  protected readonly destinoElegido = computed(() => this.participanteFiltro() !== TODAS);
  protected readonly puedeSubir = computed(
    () =>
      this.grupoId() != null &&
      this.destinoElegido() &&
      this.archivos().length > 0 &&
      !this.subiendo(),
  );

  protected agregarArchivos(event: Event): void {
    const input = event.target as HTMLInputElement;
    const nuevos = Array.from(input.files ?? []);
    if (nuevos.length) {
      // Dedupe por identidad práctica (nombre + tamaño): re-elegir la misma carpeta no duplica.
      const clave = (f: File) => `${f.name}|${f.size}`;
      const existentes = new Set(this.archivos().map(clave));
      this.archivos.update((actuales) => [
        ...actuales,
        ...nuevos.filter((f) => !existentes.has(clave(f))),
      ]);
    }
    input.value = ''; // permite volver a elegir los mismos archivos tras quitarlos
  }

  protected quitarArchivo(archivo: File): void {
    this.archivos.update((actuales) => actuales.filter((f) => f !== archivo));
  }

  protected limpiarArchivos(): void {
    this.archivos.set([]);
  }

  protected subir(): void {
    const grupoId = this.grupoId();
    if (grupoId == null || !this.puedeSubir()) {
      return;
    }
    const participanteId = this.participanteFiltro() === GRUPALES ? null : this.participanteFiltro();

    const tandas: File[][] = [];
    const archivos = this.archivos();
    for (let i = 0; i < archivos.length; i += TAMANO_TANDA) {
      tandas.push(archivos.slice(i, i + TAMANO_TANDA));
    }

    this.subiendo.set(true);
    this.tandaActual.set(0);
    this.totalTandas.set(tandas.length);

    from(tandas)
      .pipe(
        // Secuencial a propósito: no saturar la conexión del fotógrafo ni el pipeline de la API.
        concatMap((tanda) =>
          this.fotosService.subir(grupoId, participanteId, tanda).pipe(
            tap(() => this.tandaActual.update((t) => t + 1)),
            map((fotos) => fotos.length),
          ),
        ),
        reduce((total, subidasEnTanda) => total + subidasEnTanda, 0),
      )
      .subscribe({
        next: (subidas) => {
          this.notify.success(`${subidas} foto(s) subidas. Generando copias con marca de agua…`);
          this.archivos.set([]);
        },
        // Si una tanda falla (el toast lo emite el interceptor), las anteriores YA quedaron
        // subidas: se refresca la grilla para verlas y los archivos siguen elegidos para
        // reintentar (el fotógrafo quita de la lista los que ya ve subidos).
        error: () => this.terminarSubida(),
        complete: () => this.terminarSubida(),
      });
  }

  private terminarSubida(): void {
    this.subiendo.set(false);
    this.fotosResource.reload();
  }

  // ── Acciones sobre una foto ──────────────────────────────────────────────────────────────────

  protected verPreview(foto: Foto): void {
    if (foto.estadoProcesamiento !== EstadoProcesamientoFoto.Lista) {
      return;
    }
    // Casi pantalla completa (pedido 2026-07-16: el preview quedaba chico y con scroll lateral).
    this.dialog.open(FotoPreviewDialogComponent, {
      data: foto,
      autoFocus: false,
      maxWidth: '100vw',
      width: '96vw',
      height: '94vh',
    });
  }

  protected descargarOriginal(foto: Foto): void {
    this.fotosService
      .descargarOriginal(foto.id)
      .subscribe((response) => saveBlobResponse(response, foto.nombreArchivoOriginal));
  }

  // ── Regenerar (spec 9.2, ADR-15 D9/D12): por evento, con el conteo real ANTES de encolar ────────

  protected readonly regenerando = signal(false);

  protected regenerarEvento(): void {
    const eventoId = this.eventoId();
    if (eventoId == null) {
      return;
    }
    this.fotosService.contarParaRegenerar(eventoId).subscribe((conteo) => {
      if (conteo === 0) {
        this.notify.success('No hay fotos para regenerar en este evento.');
        return;
      }
      this.confirmService
        .confirm({
          title: 'Regenerar fotos',
          message: `Se van a regenerar ${conteo} foto(s) con la marca de agua y las opciones de publicación vigentes del evento. Puede tardar unos minutos.`,
        })
        .pipe(
          filter((confirmado) => confirmado),
          tap(() => this.regenerando.set(true)),
          switchMap(() => this.fotosService.regenerar(eventoId)),
        )
        .subscribe({
          next: (encoladas) => {
            this.regenerando.set(false);
            this.notify.success(`${encoladas} foto(s) encoladas para regenerar.`);
            this.fotosResource.reload();
          },
          error: () => this.regenerando.set(false),
        });
    });
  }

  protected borrar(foto: Foto): void {
    this.confirmService
      .confirm({
        title: 'Eliminar foto',
        message: `¿Eliminar "${foto.nombreArchivoOriginal}"? Se borra también del almacenamiento.`,
      })
      .pipe(
        filter((confirmado) => confirmado),
        switchMap(() => this.fotosService.borrar(foto.id)),
      )
      .subscribe(() => {
        this.notify.success('Foto eliminada.');
        this.fotosResource.reload();
      });
  }

  // ── Presentación ─────────────────────────────────────────────────────────────────────────────

  protected esLista(foto: Foto): boolean {
    return foto.estadoProcesamiento === EstadoProcesamientoFoto.Lista;
  }

  protected esError(foto: Foto): boolean {
    return foto.estadoProcesamiento === EstadoProcesamientoFoto.Error;
  }

  protected nombreParticipante(foto: Foto): string {
    if (foto.participanteId == null) {
      return 'Grupal';
    }
    return this.participantePorId().get(foto.participanteId) ?? `Participante ${foto.participanteId}`;
  }

  protected nombreDestino(): string {
    return this.participanteFiltro() === GRUPALES
      ? 'las grupales del grupo'
      : `el álbum de ${this.participantePorId().get(this.participanteFiltro()) ?? 'ese participante'}`;
  }

  protected tamanoLegible(bytes: number): string {
    if (bytes >= 1024 * 1024) {
      return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
    }
    return `${Math.max(1, Math.round(bytes / 1024))} KB`;
  }
}
