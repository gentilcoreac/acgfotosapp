import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { NotificationService } from '../../feedback/notification.service';
import { ImagenAmpliadaDialogComponent } from '../imagen-ampliada-dialog/imagen-ampliada-dialog.component';

/**
 * Recurso de archivo que emite `tbi-file-upload`. `base64` es el contenido **sin** el prefijo
 * `data:<mime>;base64,` (lo que espera la API: `Convert.FromBase64String`). Calza con el
 * `TenantResourceDto` de la API (`{ base64, name, extension, mimeType }`).
 */
export interface TbiFileResource {
  base64: string;
  name: string;
  extension: string;
  mimeType: string;
}

/**
 * Selector de archivo del design system: lee un `File`, valida tamaño, lo convierte a base64 y
 * emite un `TbiFileResource`. Para imágenes muestra preview (del archivo elegido o de la URL
 * actual en modo edición). Primer componente de upload del front nuevo; reutilizable.
 *
 * Presentacional (no `ControlValueAccessor`): el padre conserva el `TbiFileResource`/URL en su
 * propio estado (el flujo de archivos del Tenant mezcla `*File` + `*Url`, no encaja en un único
 * form control). `fileSelected` emite al elegir; `removed` al limpiar.
 */
@Component({
  selector: 'tbi-file-upload',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule],
  template: `
    <div class="tbi-file">
      <span class="tbi-file__label">{{ label() }}</span>
      <div class="tbi-file__row">
        @if (preview() && previewUrl()) {
          <button
            type="button"
            class="tbi-file__preview-btn"
            aria-label="Ampliar vista previa"
            (click)="ampliar()"
          >
            <img class="tbi-file__preview" [src]="previewUrl()" alt="" />
          </button>
        }
        <div class="tbi-file__controls">
          <button matButton="outlined" type="button" (click)="fileInput.click()">
            <mat-icon>upload</mat-icon>
            Elegir
          </button>
          @if (pickedName()) {
            <span class="tbi-file__name">{{ pickedName() }}</span>
          }
          @if (previewUrl() || pickedName()) {
            <button matIconButton type="button" aria-label="Quitar" (click)="clear()">
              <mat-icon>close</mat-icon>
            </button>
          }
        </div>
        <input #fileInput type="file" hidden [accept]="accept()" (change)="onFileChange($event)" />
      </div>
    </div>
  `,
  styles: `
    :host {
      display: block;
    }
    .tbi-file {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }
    .tbi-file__label {
      font: var(--mat-sys-body-medium);
      color: var(--mat-sys-on-surface-variant);
    }
    .tbi-file__row {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }
    .tbi-file__preview-btn {
      flex: none;
      padding: 0;
      border: none;
      background: none;
      cursor: pointer;
      border-radius: 4px;

      &:focus-visible {
        outline: 2px solid var(--mat-sys-primary);
        outline-offset: 2px;
      }
    }
    .tbi-file__preview {
      display: block;
      width: 56px;
      height: 56px;
      object-fit: contain;
      border: 1px solid var(--mat-sys-outline-variant);
      border-radius: 4px;
      background: var(--mat-sys-surface-container);
    }
    .tbi-file__controls {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }
    .tbi-file__name {
      font: var(--mat-sys-body-small);
      color: var(--mat-sys-on-surface-variant);
      max-width: 160px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
  `,
})
export class TbiFileUploadComponent {
  readonly label = input<string>('Archivo');
  /** Filtro del input file (mime o extensiones). */
  readonly accept = input<string>('image/*');
  /** URL del recurso actual (modo edición): se muestra como preview si no se eligió uno nuevo. */
  readonly currentUrl = input<string | null>(null);
  /** Muestra preview de imagen (del archivo elegido o de `currentUrl`). */
  readonly preview = input<boolean>(true);
  /** Tamaño máximo permitido, en MB. */
  readonly maxSizeMb = input<number>(2);
  /** Cómo entregar el archivo: `'base64'` emite `fileSelected` (recursos chicos embebidos en JSON);
   * `'file'` emite `rawFileSelected` con el `File` crudo (multipart, sin pasar por FileReader). */
  readonly readAs = input<'base64' | 'file'>('base64');

  readonly fileSelected = output<TbiFileResource>();
  readonly rawFileSelected = output<File>();
  readonly removed = output<void>();

  private readonly notify = inject(NotificationService);
  private readonly dialog = inject(MatDialog);

  /** data-URL del archivo recién elegido (preview inmediato, antes de guardar). */
  protected readonly pickedDataUrl = signal<string | null>(null);
  protected readonly pickedName = signal<string | null>(null);

  protected readonly previewUrl = computed(() => this.pickedDataUrl() ?? this.currentUrl());

  protected onFileChange(event: Event): void {
    const el = event.target as HTMLInputElement;
    const file = el.files?.[0];
    if (!file) {
      return;
    }
    if (file.size > this.maxSizeMb() * 1024 * 1024) {
      this.notify.error(`El archivo supera el máximo de ${this.maxSizeMb()} MB.`);
      el.value = '';
      return;
    }
    if (!this.matchesAccept(file)) {
      this.notify.error('El tipo de archivo no está permitido.');
      el.value = '';
      return;
    }
    if (this.readAs() === 'file') {
      this.pickedName.set(file.name);
      this.rawFileSelected.emit(file);
      el.value = ''; // permite re-elegir el mismo archivo
      return;
    }
    const reader = new FileReader();
    reader.onload = () => {
      const dataUrl = String(reader.result);
      // Quita el prefijo "data:<mime>;base64," → base64 puro (lo que decodifica la API).
      const base64 = dataUrl.includes(',') ? dataUrl.slice(dataUrl.indexOf(',') + 1) : dataUrl;
      const extension = file.name.includes('.')
        ? file.name.slice(file.name.lastIndexOf('.') + 1)
        : '';
      this.pickedDataUrl.set(dataUrl);
      this.pickedName.set(file.name);
      this.fileSelected.emit({ base64, name: file.name, extension, mimeType: file.type });
    };
    reader.readAsDataURL(file);
    el.value = ''; // permite re-elegir el mismo archivo
  }

  /** El `accept` del input file es solo un hint del picker (el usuario puede elegir "Todos los
   * archivos"): acá se valida en serio el tipo/extensión contra ese mismo filtro. */
  private matchesAccept(file: File): boolean {
    const tokens = this.accept()
      .split(',')
      .map((t) => t.trim().toLowerCase())
      .filter(Boolean);
    if (tokens.length === 0) {
      return true;
    }
    const mime = file.type.toLowerCase();
    const ext = file.name.includes('.')
      ? file.name.slice(file.name.lastIndexOf('.')).toLowerCase()
      : '';
    return tokens.some((token) =>
      token.startsWith('.')
        ? token === ext
        : token.endsWith('/*')
          ? mime.startsWith(token.slice(0, -1))
          : token === mime,
    );
  }

  /** La vista previa es una imagen suelta (el archivo recién elegido, o el recurso actual en modo
   * edición), sin colección — se amplía sola, sin recorrido (openspec/changes/visor-fotos-cobertura-total). */
  protected ampliar(): void {
    const src = this.previewUrl();
    if (!src) {
      return;
    }
    this.dialog.open(ImagenAmpliadaDialogComponent, {
      data: { src, alt: this.pickedName() ?? this.label() },
      autoFocus: false,
      maxWidth: '100vw',
      width: '96vw',
      height: '94vh',
    });
  }

  protected clear(): void {
    this.pickedDataUrl.set(null);
    this.pickedName.set(null);
    this.removed.emit();
  }
}
