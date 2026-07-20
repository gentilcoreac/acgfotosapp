import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  model,
  output,
  signal,
} from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormValueControl, ValidationError } from '@angular/forms/signals';
import {
  MatAutocompleteModule,
  MatAutocompleteSelectedEvent,
} from '@angular/material/autocomplete';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Observable, Subject, catchError, debounceTime, distinctUntilChanged, map, of } from 'rxjs';

/** Item del `tbi-search-select`: id + etiqueta visible. */
export interface TbiSearchSelectItem {
  id: number;
  label: string;
}

/** Búsqueda server-side: recibe el término y devuelve coincidencias (id + label). */
export type TbiSearchSelectFetch = (term: string) => Observable<TbiSearchSelectItem[]>;

/**
 * Multi-select **con búsqueda server-side** del design system: input con `mat-autocomplete`
 * (typeahead) + los elegidos como **chips removibles** debajo. `FormValueControl` nativo cuyo valor
 * es `TbiSearchSelectItem[]` (id + label, para no tener que re-resolver nombres). Pensado para
 * elegir N entidades cuando el universo es grande (no carga todo: consulta por término). El
 * consumidor inyecta la función `search`. Aísla Material detrás del wrapper `tbi-*`.
 */
@Component({
  selector: 'tbi-search-select',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatFormFieldModule,
    MatInputModule,
    MatAutocompleteModule,
    MatChipsModule,
    MatIconModule,
  ],
  templateUrl: './tbi-search-select.component.html',
  styleUrl: './tbi-search-select.component.scss',
})
export class TbiSearchSelectComponent implements FormValueControl<TbiSearchSelectItem[]> {
  readonly label = input<string>('');
  readonly placeholder = input<string>('');
  /** Búsqueda server-side; se invoca con debounce mientras se tipea. */
  readonly search = input.required<TbiSearchSelectFetch>();
  /** Mínimo de caracteres para disparar la búsqueda. */
  readonly minChars = input<number>(1);

  readonly value = model<TbiSearchSelectItem[]>([]);
  readonly disabled = input<boolean>(false);
  readonly required = input<boolean>(false);
  readonly touched = input<boolean>(false);
  readonly dirty = input<boolean>(false);
  readonly errors = input<readonly ValidationError.WithOptionalFieldTree[]>([]);
  readonly touch = output<void>();

  /** Mismo criterio que `tbi-text-field`: error visible si el usuario ya tocó/modificó el campo. */
  protected readonly hasError = computed(
    () => this.errors().length > 0 && (this.touched() || this.dirty()),
  );
  protected readonly errorText = computed(() => this.errors()[0]?.message ?? 'Campo inválido');

  /** Término debounceado que dispara la búsqueda (alimentado por `term$`, ver constructor). */
  private readonly debouncedTerm = signal('');
  /** Refetchea sola cuando cambia el término (cancela el pedido en vuelo anterior). */
  private readonly searchResource = rxResource({
    params: () => ({ term: this.debouncedTerm(), minChars: this.minChars() }),
    stream: ({ params }) =>
      params.term.length < params.minChars
        ? of<TbiSearchSelectItem[]>([])
        : this.search()(params.term).pipe(catchError(() => of<TbiSearchSelectItem[]>([]))),
  });
  /** Opciones a mostrar: las encontradas que no estén ya seleccionadas. */
  protected readonly filteredOptions = computed(() => {
    const selectedIds = new Set(this.value().map((i) => i.id));
    return (this.searchResource.value() ?? []).filter((o) => !selectedIds.has(o.id));
  });

  private readonly term$ = new Subject<string>();

  constructor() {
    this.term$
      .pipe(
        debounceTime(250),
        map((t) => t.trim()),
        distinctUntilChanged(),
        takeUntilDestroyed(),
      )
      .subscribe((term) => this.debouncedTerm.set(term));
  }

  protected onSearchInput(term: string): void {
    this.term$.next(term);
  }

  /** El input nunca muestra el objeto de la opción seleccionada (se limpia al elegir). */
  protected readonly displayWith = (): string => '';

  protected add(event: MatAutocompleteSelectedEvent, input: HTMLInputElement): void {
    this.addItem(event.option.value as TbiSearchSelectItem);
    input.value = '';
    // Limpia ya (no espera el debounce de `term$`, pensado para tipeo de usuario): setear el signal
    // directo dispara el refetch sola, vía `params`.
    this.debouncedTerm.set('');
  }

  addItem(item: TbiSearchSelectItem): void {
    if (this.disabled() || this.value().some((i) => i.id === item.id)) {
      return;
    }
    this.value.update((items) => [...items, item]);
  }

  remove(item: TbiSearchSelectItem): void {
    if (this.disabled()) {
      return;
    }
    this.value.update((items) => items.filter((i) => i.id !== item.id));
  }
}
