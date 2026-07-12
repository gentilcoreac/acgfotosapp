import { Component, viewChild, ChangeDetectionStrategy } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { Observable, of } from 'rxjs';
import { QueryParams } from '../../../core/models/query-params.model';
import { QueryResult } from '../../../core/models/query-result.model';
import { TbiColumn, TbiTableComponent } from './tbi-table.component';

interface Row {
  id: number;
  name: string;
}

@Component({
  imports: [TbiTableComponent],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `<tbi-table [columns]="columns" [fetch]="fetch" [searchable]="true" />`,
})
class HostComponent {
  readonly table = viewChild.required(TbiTableComponent);
  readonly columns: TbiColumn<Row>[] = [
    { key: 'id', header: 'ID', sortable: true },
    { key: 'name', header: 'Nombre', optional: true },
    {
      key: 'estado',
      header: 'Estado',
      align: 'center',
      chip: (row) =>
        row.id === 1 ? { label: 'Activo', tone: 'success', icon: 'check_circle' } : null,
    },
  ];
  queries: QueryParams[] = [];
  result: QueryResult<Row> = { items: [{ id: 1, name: 'alfa' }], totalCount: 1 };
  fetch = (query: QueryParams): Observable<QueryResult<Row>> => {
    this.queries.push(query);
    return of(this.result);
  };
}

describe('TbiTableComponent', () => {
  beforeEach(() => {
    vi.useFakeTimers({ advanceTimeDelta: 1, shouldAdvanceTime: true });
  });
  afterEach(() => {
    vi.useRealTimers();
  });
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [provideNoopAnimations()],
    }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('hace fetch al iniciar (page 0, pageSize 10) y renderiza las filas', () => {
    expect(host.queries.length).toBe(1);
    expect(host.queries[0].page).toBe(0);
    expect(host.queries[0].pageSize).toBe(10);

    const rows = fixture.nativeElement.querySelectorAll('tr[mat-row]');
    expect(rows.length).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('alfa');
  });

  it('renderiza un chip de estado en la columna con `chip`', () => {
    const chip = fixture.nativeElement.querySelector('.tbi-status-chip');
    expect(chip).not.toBeNull();
    expect(chip.textContent).toContain('Activo');
    expect(chip.classList).toContain('tbi-status-chip--success');
  });

  it('reload() resetea a la primera página y vuelve a pedir', () => {
    host.queries = [];
    host.table().reload();
    fixture.detectChanges();
    expect(host.queries.length).toBe(1);
    expect(host.queries[0].page).toBe(0);
  });

  it('refresh() conserva la página actual y vuelve a pedir', () => {
    interface WithOnPage {
      onPage(e: { pageIndex: number; pageSize: number; length: number }): void;
    }
    (host.table() as unknown as WithOnPage).onPage({ pageIndex: 2, pageSize: 10, length: 30 });
    fixture.detectChanges();
    host.queries = [];
    host.table().refresh();
    fixture.detectChanges();
    expect(host.queries.length).toBe(1);
    expect(host.queries[0].page).toBe(2);
  });

  it('busca con debounce: envía searchText y resetea a la primera página', async () => {
    host.queries = [];
    const input: HTMLInputElement = fixture.nativeElement.querySelector('.tbi-table__search-input');
    input.value = 'alf';
    input.dispatchEvent(new Event('input'));
    await vi.advanceTimersByTimeAsync(300);
    fixture.detectChanges();

    expect(host.queries.length).toBe(1);
    expect(host.queries[0].searchText).toBe('alf');
    expect(host.queries[0].page).toBe(0);
  });

  it('oculta una columna opcional desde el menú "Columnas"', () => {
    expect(fixture.nativeElement.textContent).toContain('alfa');

    const btn: HTMLButtonElement = fixture.nativeElement.querySelector('.tbi-table__columns-btn');
    btn.click();
    fixture.detectChanges();

    const items = Array.from(document.querySelectorAll('.mat-mdc-menu-item')) as HTMLElement[];
    const nameItem = items.find((i) => i.textContent?.includes('Nombre'));
    nameItem?.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('alfa');
  });
});
