import { ChangeDetectionStrategy, Component, DestroyRef, computed, effect, inject, signal } from '@angular/core';
import { FormField, applyEach, form, maxLength, required, validate } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { map } from 'rxjs';
import { ApiError, errorMessages } from '../../../../core/models';
import { EditComponentBase } from '../../../../shared/forms/edit-component-base';
import { TbiButtonComponent } from '../../../../shared/ui/tbi-button/tbi-button.component';
import { TbiSelectComponent, TbiSelectOption } from '../../../../shared/ui/tbi-select/tbi-select.component';
import { TbiSliderComponent } from '../../../../shared/ui/tbi-slider/tbi-slider.component';
import { TbiTextFieldComponent } from '../../../../shared/ui/tbi-text-field/tbi-text-field.component';
import { lookupResource } from '../../../../shared/util/lookup-resource';
import { OpcionesPublicacionService } from '../../publicacion/data/opciones-publicacion.service';
import { OpcionesPublicacion } from '../../publicacion/domain/opciones-publicacion.model';
import { MarcaAguaService } from '../data/marca-agua.service';
import {
  CapaMarcaAgua,
  MODO_COLOCACION,
  MODO_FUSION,
  ModoColocacion,
  ModoFusion,
  POSICION,
  PerfilMarcaAgua,
  Posicion,
} from '../domain/marca-agua.model';
import { ImagenDecodificada, MUESTRAS, MuestraVariante } from '../domain/marca-agua-muestra.util';
import { DisenoTexto, rasterizarTextoComoPng } from '../domain/marca-agua-texto.util';
import { PerfilMarcaAguaCanvasComponent } from './perfil-marca-agua-canvas.component';

/** Fallback cuando el tenant no tiene ninguna `OpcionesPublicacion` default (mismo default que `appsettings.json`, `Fotos:LadoMayorThumb`). */
const LADO_MAYOR_THUMB_FALLBACK = 300;
/** Relación de aspecto de las muestras (3:2, la misma que ya usan list/editor). */
const ASPECTO_MUESTRA = 3 / 2;

/** Una muestra a renderizar en la grilla "simultánea" (spec 7.5/11.3): las 3 sintéticas + la foto propia, si hay. */
interface MuestraTile {
  clave: string;
  etiqueta: string;
  variante: MuestraVariante;
  fotoPropia: ImagenDecodificada | null;
}

/** Grilla de 9 posiciones en orden visual (fila por fila). */
const GRILLA_POSICIONES: readonly Posicion[] = [
  POSICION.ArribaIzquierda,
  POSICION.ArribaCentro,
  POSICION.ArribaDerecha,
  POSICION.CentroIzquierda,
  POSICION.Centro,
  POSICION.CentroDerecha,
  POSICION.AbajoIzquierda,
  POSICION.AbajoCentro,
  POSICION.AbajoDerecha,
];

const MODO_COLOCACION_OPTIONS: TbiSelectOption<ModoColocacion>[] = [
  { value: MODO_COLOCACION.Repetida, label: 'Repetida sobre toda la foto' },
  { value: MODO_COLOCACION.PosicionFija, label: 'En un lugar fijo' },
];

const MODO_FUSION_OPTIONS: TbiSelectOption<ModoFusion>[] = [
  { value: MODO_FUSION.Normal, label: 'Normal — el color tal cual' },
  { value: MODO_FUSION.Superponer, label: 'Superponer — se adapta al fondo' },
  { value: MODO_FUSION.Diferencia, label: 'Diferencia — invierte el fondo' },
];

const POSICION_ETIQUETA: Record<Posicion, string> = {
  [POSICION.ArribaIzquierda]: 'Arriba izquierda',
  [POSICION.ArribaCentro]: 'Arriba centro',
  [POSICION.ArribaDerecha]: 'Arriba derecha',
  [POSICION.CentroIzquierda]: 'Centro izquierda',
  [POSICION.Centro]: 'Centro',
  [POSICION.CentroDerecha]: 'Centro derecha',
  [POSICION.AbajoIzquierda]: 'Abajo izquierda',
  [POSICION.AbajoCentro]: 'Abajo centro',
  [POSICION.AbajoDerecha]: 'Abajo derecha',
};

/** Sólo lo editable acá; el contenido (bytes) de una capa nace con `subirCapa`, nunca se edita. */
type PerfilFormModel = Pick<PerfilMarcaAgua, 'nombre' | 'esDefault' | 'marcarThumb' | 'capas'>;

// TODO (Fase 4 - i18n): textos en español por ahora.
// TODO (pendiente de pulir, ver docs/05): el aviso de recorte por posición es uno genérico para
// PosiciónFija, no específico de qué esquina/borde se recorta.
@Component({
  selector: 'tbi-perfil-marca-agua-edit',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormField,
    MatDialogModule,
    MatButtonModule,
    MatCheckboxModule,
    MatIconModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    TbiButtonComponent,
    TbiSelectComponent,
    TbiSliderComponent,
    TbiTextFieldComponent,
    PerfilMarcaAguaCanvasComponent,
  ],
  templateUrl: './perfil-marca-agua-edit.component.html',
  styleUrl: './perfil-marca-agua-edit.component.scss',
})
export class PerfilMarcaAguaEditComponent extends EditComponentBase<PerfilMarcaAgua, PerfilFormModel> {
  private readonly service = inject(MarcaAguaService);
  private readonly opcionesPublicacionService = inject(OpcionesPublicacionService);

  protected readonly crud = this.service.crud;

  // El id real del perfil: nace en el primer `subirCapa` (design.md D14), no en `submit()`. `toEntity`
  // lo usa en vez del id "congelado" que resuelve la base al abrir el diálogo.
  private readonly perfilId = signal<number | undefined>(this.resolveId() as number | undefined);

  protected readonly modoColocacionOptions = MODO_COLOCACION_OPTIONS;
  protected readonly modoFusionOptions = MODO_FUSION_OPTIONS;
  protected readonly grillaPosiciones = GRILLA_POSICIONES;
  protected readonly POSICION = POSICION;
  protected readonly MODO_COLOCACION = MODO_COLOCACION;
  protected readonly MODO_FUSION = MODO_FUSION;

  protected readonly model = signal<PerfilFormModel>({
    nombre: '',
    esDefault: false,
    marcarThumb: true,
    capas: [],
  });

  protected readonly form = form(this.model, (path) => {
    required(path.nombre, { message: 'Requerido' });
    maxLength(path.nombre, 100, { message: 'Máximo 100 caracteres' });
    validate(path.capas, ({ value }) => {
      const n = value().length;
      if (n < 1) {
        return { kind: 'capas-min', message: 'Subí al menos una capa.' };
      }
      return n > 3 ? { kind: 'capas-max', message: 'Un perfil admite hasta 3 capas.' } : undefined;
    });
    applyEach(path.capas, (capa) => {
      validate(capa.escalaPorcentaje, ({ value }) =>
        value() >= 1 && value() <= 100
          ? undefined
          : { kind: 'rango', message: 'La escala debe estar entre 1% y 100%.' },
      );
      validate(capa.margenPorcentaje, ({ value }) =>
        value() >= 0 && value() <= 50
          ? undefined
          : { kind: 'rango', message: 'El margen debe estar entre 0% y 50%.' },
      );
      validate(capa.anguloGrados, ({ value }) =>
        value() >= -180 && value() <= 180
          ? undefined
          : { kind: 'rango', message: 'El ángulo debe estar entre -180° y 180°.' },
      );
      validate(capa.opacidad, ({ value }) =>
        value() >= 0 && value() <= 1
          ? undefined
          : { kind: 'rango', message: 'La opacidad debe estar entre 0 y 1.' },
      );
      validate(capa.posicion, ({ value, valueOf }) =>
        valueOf(capa.modoColocacion) === MODO_COLOCACION.PosicionFija && value() == null
          ? { kind: 'requerido', message: 'Elegí una de las 9 posiciones.' }
          : undefined,
      );
    });
  });

  protected override invalidSubmitMessage =
    'Completá los campos obligatorios resaltados (revisá también las capas).';

  protected readonly subiendo = signal(false);
  protected readonly avisosUltimaSubida = signal<string[]>([]);
  protected readonly capaActivaId = signal<number | null>(null);
  protected readonly verComprimido = signal(true);

  // "Probar con mi foto" (spec 7.5/11.4): además de las 3 muestras sintéticas, una foto real propia.
  protected readonly miFoto = signal<ImageBitmap | null>(null);
  protected readonly miFotoNombre = signal<string | null>(null);
  protected readonly errorMiFoto = signal<string | null>(null);

  /** Las 3 muestras sintéticas se muestran SIMULTÁNEAS (spec 7.5, ya no una por vez con selector) +
   * la foto propia al final, si se subió una. */
  protected readonly muestrasTiles = computed<MuestraTile[]>(() => {
    const tiles: MuestraTile[] = MUESTRAS.map((m) => ({
      clave: m.clave,
      etiqueta: m.etiqueta,
      variante: m.clave,
      fotoPropia: null,
    }));
    const miFoto = this.miFoto();
    if (miFoto) {
      tiles.push({ clave: 'mi-foto', etiqueta: 'Mi foto', variante: 'mixta', fotoPropia: miFoto });
    }
    return tiles;
  });

  // Vista previa al tamaño REAL de thumb (spec 11.5): la marca se compone como % del ancho de CADA
  // derivado por separado — a un thumb chico la misma marca puede quedar ilegible aunque no haya
  // compresión de por medio (problema de cantidad de píxeles, distinto al de la compresión WebP que
  // ya cubre "Ver comprimido"). Usa las opciones de publicación default del tenant como referencia
  // representativa (un perfil no está atado a un evento puntual, puede terminar usado con cualquiera).
  private readonly opcionesResource = lookupResource(
    () =>
      this.opcionesPublicacionService.crud.getAll().pipe(map((r) => r.items)),
    [] as OpcionesPublicacion[],
  );
  private readonly opcionesDefault = computed(() =>
    (this.opcionesResource.value() ?? []).find((o) => o.esDefault) ?? null,
  );
  protected readonly ladoMayorThumbUsado = computed(
    () => this.opcionesDefault()?.ladoMayorThumb ?? LADO_MAYOR_THUMB_FALLBACK,
  );
  protected readonly nombreOpcionesThumb = computed(
    () => this.opcionesDefault()?.nombre ?? 'default del sistema, sin opciones propias del estudio',
  );
  protected readonly thumbAncho = computed(() => this.ladoMayorThumbUsado());
  protected readonly thumbAlto = computed(() => Math.round(this.thumbAncho() / ASPECTO_MUESTRA));

  // "Diseñar texto" (D1/ADR-15 §2): una forma más de fabricar el PNG de una capa, no parte del
  // contrato — el resultado sube por el mismo `subirCapa` que un archivo elegido a mano.
  protected readonly disenandoTexto = signal(false);
  protected readonly disenoTexto = signal<DisenoTexto>({
    texto: '',
    color: '#ffffff',
    negrita: true,
    contorno: false,
  });
  protected readonly previewTextoUrl = signal<string | null>(null);
  protected readonly generandoDiseno = signal(false);

  /** Vista en vivo: refleja el modelo tal cual se está editando, no lo último persistido. */
  protected readonly perfilVista = computed<PerfilMarcaAgua>(() => ({
    id: this.perfilId(),
    ...this.model(),
  }));

  constructor() {
    super();
    // Vista previa del texto mientras se diseña: rasteriza con los mismos parámetros que va a subir,
    // así lo que se ve acá es exactamente el PNG que se sube (D1: no hay dos implementaciones).
    effect(() => {
      const abierto = this.disenandoTexto();
      const diseno = this.disenoTexto();
      if (!abierto) {
        return;
      }
      void this.regenerarPreviewTexto(diseno);
    });
    inject(DestroyRef).onDestroy(() => {
      this.limpiarPreviewTexto();
      this.miFoto()?.close();
    });
  }

  protected readonly capaActiva = computed<CapaMarcaAgua | undefined>(() => {
    const id = this.capaActivaId();
    return this.model().capas.find((c) => c.id === id);
  });

  protected subirArchivo(event: Event): void {
    const input = event.target as HTMLInputElement;
    const archivo = input.files?.[0];
    input.value = ''; // permite re-elegir el mismo archivo si hace falta
    if (!archivo) {
      return;
    }
    this.subirCapaDesdeArchivo(archivo);
  }

  protected async elegirMiFoto(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const archivo = input.files?.[0];
    input.value = '';
    if (!archivo) {
      return;
    }
    this.errorMiFoto.set(null);
    try {
      const bitmap = await createImageBitmap(archivo);
      this.miFoto()?.close();
      this.miFoto.set(bitmap);
      this.miFotoNombre.set(archivo.name);
    } catch {
      this.errorMiFoto.set('No se pudo leer la imagen. Probá con otro archivo.');
    }
  }

  protected quitarMiFoto(): void {
    this.miFoto()?.close();
    this.miFoto.set(null);
    this.miFotoNombre.set(null);
  }

  protected abrirDisenoTexto(): void {
    this.disenandoTexto.set(true);
  }

  protected cancelarDisenoTexto(): void {
    this.disenandoTexto.set(false);
    this.limpiarPreviewTexto();
  }

  protected actualizarDisenoTexto(cambios: Partial<DisenoTexto>): void {
    this.disenoTexto.update((d) => ({ ...d, ...cambios }));
  }

  protected async confirmarDisenoTexto(): Promise<void> {
    const diseno = this.disenoTexto();
    if (!diseno.texto.trim()) {
      return;
    }

    this.generandoDiseno.set(true);
    this.errors.set([]);
    let archivo: File;
    try {
      archivo = await rasterizarTextoComoPng(diseno);
    } catch {
      this.generandoDiseno.set(false);
      this.errors.set(['No se pudo generar la imagen del texto.']);
      return;
    }
    this.generandoDiseno.set(false);
    this.disenandoTexto.set(false);
    this.limpiarPreviewTexto();
    this.subirCapaDesdeArchivo(archivo);
  }

  private async regenerarPreviewTexto(diseno: DisenoTexto): Promise<void> {
    if (!diseno.texto.trim()) {
      this.limpiarPreviewTexto();
      return;
    }
    try {
      // Ancho de vista previa chico: el destino real (1600px, D6) es innecesario para sólo mirarlo.
      const archivo = await rasterizarTextoComoPng(diseno, 400);
      const url = URL.createObjectURL(archivo);
      this.limpiarPreviewTexto();
      this.previewTextoUrl.set(url);
    } catch {
      this.limpiarPreviewTexto();
    }
  }

  private limpiarPreviewTexto(): void {
    const actual = this.previewTextoUrl();
    if (actual) {
      URL.revokeObjectURL(actual);
    }
    this.previewTextoUrl.set(null);
  }

  private subirCapaDesdeArchivo(archivo: File): void {
    this.subiendo.set(true);
    this.errors.set([]);
    this.avisosUltimaSubida.set([]);
    this.service.subirCapa(this.perfilId() ?? null, this.model().nombre.trim() || null, archivo).subscribe({
      next: (subida) => {
        this.subiendo.set(false);
        this.perfilId.set(subida.perfil.id);
        this.model.update((m) => ({ ...m, capas: [...m.capas, subida.capa] }));
        this.capaActivaId.set(subida.capa.id);
        this.avisosUltimaSubida.set(subida.avisos);
        this.dirtyExtra.set(true);
      },
      error: (error: ApiError) => {
        this.subiendo.set(false);
        this.errors.set(errorMessages(error));
      },
    });
  }

  protected etiquetaPosicion(pos: Posicion): string {
    return POSICION_ETIQUETA[pos];
  }

  protected elegirCapa(id: number): void {
    this.capaActivaId.set(id);
    this.avisosUltimaSubida.set([]);
  }

  protected quitarCapaActiva(): void {
    const id = this.capaActivaId();
    if (id == null) {
      return;
    }
    this.model.update((m) => ({ ...m, capas: m.capas.filter((c) => c.id !== id) }));
    this.dirtyExtra.set(true);
    const restante = this.model().capas[0];
    this.capaActivaId.set(restante?.id ?? null);
  }

  /** Actualiza un campo de la capa activa (colocación). */
  protected actualizarCapaActiva(cambios: Partial<CapaMarcaAgua>): void {
    const id = this.capaActivaId();
    if (id == null) {
      return;
    }
    this.model.update((m) => ({
      ...m,
      capas: m.capas.map((c) => (c.id === id ? { ...c, ...cambios } : c)),
    }));
  }

  protected toEntity(): PerfilMarcaAgua {
    const value = this.model();
    return {
      id: this.perfilId(),
      nombre: value.nombre.trim(),
      esDefault: value.esDefault,
      marcarThumb: value.marcarThumb,
      capas: value.capas,
    };
  }

  protected patchForm(entity: PerfilMarcaAgua): void {
    this.perfilId.set(entity.id);
    const capas = [...entity.capas].sort((a, b) => a.orden - b.orden);
    this.model.set({
      nombre: entity.nombre,
      esDefault: entity.esDefault,
      marcarThumb: entity.marcarThumb,
      capas,
    });
    this.capaActivaId.set(capas[0]?.id ?? null);
  }
}
