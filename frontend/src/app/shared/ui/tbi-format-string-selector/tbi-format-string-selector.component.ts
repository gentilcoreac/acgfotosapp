import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  model,
  output,
  signal,
} from '@angular/core';
import { FormValueControl, ValidationError } from '@angular/forms/signals';
import {
  MatAutocompleteModule,
  MatAutocompleteSelectedEvent,
} from '@angular/material/autocomplete';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { EXCEL_FORMAT_GROUPS, detectExcelFormatCategory } from './excel-format-options';

/**
 * Selector de formato estilo Excel con autocomplete agrupado por categoría (número/moneda/fecha/
 * hora/texto/especial/condicionales) — port de `app-excel-format-string-selector` del legacy,
 * reusado tal cual en `table-manage` (columnas/medidas) y, más adelante, en Views (mismo campo
 * `formatString`, confirmado en el legacy: `column-selector-overlay` de Views usa el mismo
 * componente). Sigue siendo un campo de texto libre — el catálogo es sólo sugerencias, no una
 * lista cerrada; cualquier string tipeado se acepta (cae en "Personalizado" si no matchea ninguna
 * categoría conocida).
 */
@Component({
  selector: 'tbi-format-string-selector',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatFormFieldModule, MatInputModule, MatAutocompleteModule],
  template: `
    <mat-form-field appearance="outline" subscriptSizing="dynamic">
      <mat-label>{{ label() }}</mat-label>
      <input
        matInput
        [value]="value() ?? ''"
        [disabled]="disabled()"
        [required]="required()"
        [matAutocomplete]="auto"
        [attr.aria-invalid]="hasError()"
        (input)="handleInput($event)"
        (blur)="touch.emit()"
      />
      <mat-autocomplete #auto="matAutocomplete" (optionSelected)="handleOptionSelected($event)">
        @for (group of filteredGroups(); track group.category) {
          <mat-optgroup [label]="group.category">
            @for (f of group.formats; track f) {
              <mat-option [value]="f">{{ f }}</mat-option>
            }
          </mat-optgroup>
        }
      </mat-autocomplete>
    </mat-form-field>
    @if (hasError()) {
      <p class="tbi-format-string-selector__error" role="alert">{{ errorText() }}</p>
    }
  `,
  styles: `
    :host {
      display: block;
    }
    mat-form-field {
      width: 100%;
    }
    .tbi-format-string-selector__error {
      margin: 0.25rem 1rem 0;
      color: var(--mat-sys-error);
      font: var(--mat-sys-body-small);
    }
  `,
})
export class TbiFormatStringSelectorComponent implements FormValueControl<string | null> {
  readonly label = input<string>('');
  readonly errorMessage = input<string>('Campo inválido');

  readonly value = model<string | null>('');
  readonly disabled = input<boolean>(false);
  readonly required = input<boolean>(false);
  readonly touched = input<boolean>(false);
  readonly dirty = input<boolean>(false);
  readonly errors = input<readonly ValidationError.WithOptionalFieldTree[]>([]);

  readonly touch = output<void>();

  private readonly filterText = signal('');

  protected readonly filteredGroups = computed(() => {
    const term = this.filterText().toLowerCase();
    return EXCEL_FORMAT_GROUPS.map((group) => ({
      category: group.category,
      formats: term ? group.formats.filter((f) => f.toLowerCase().includes(term)) : group.formats,
    })).filter((group) => group.formats.length > 0);
  });

  /** Categoría del valor actual — sin uso visual hoy (no hay badge, confirmado contra el legacy);
   * se expone igual porque el catálogo de sugerencias se arma agrupado por la misma función. */
  protected readonly category = computed(() => detectExcelFormatCategory(this.value() ?? ''));

  protected hasError(): boolean {
    return this.errors().length > 0 && (this.touched() || this.dirty());
  }

  protected errorText(): string {
    return this.errors()[0]?.message ?? this.errorMessage();
  }

  handleInput(event: Event): void {
    const next = (event.target as HTMLInputElement).value;
    this.value.set(next);
    this.filterText.set(next);
  }

  protected handleOptionSelected(event: MatAutocompleteSelectedEvent): void {
    this.value.set(event.option.value as string);
    this.filterText.set('');
  }
}
