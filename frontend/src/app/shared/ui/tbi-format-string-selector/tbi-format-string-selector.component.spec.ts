import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TbiFormatStringSelectorComponent } from './tbi-format-string-selector.component';

describe('TbiFormatStringSelectorComponent', () => {
  let fixture: ComponentFixture<TbiFormatStringSelectorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TbiFormatStringSelectorComponent],
      providers: [provideNoopAnimations()],
    }).compileComponents();
    fixture = TestBed.createComponent(TbiFormatStringSelectorComponent);
    fixture.detectChanges();
  });

  it('arranca con todas las categorías del catálogo (sin filtro)', () => {
    const groups = fixture.componentInstance['filteredGroups']();
    expect(groups.length).toBeGreaterThan(0);
    expect(groups.some((g) => g.category === 'Moneda')).toBe(true);
  });

  it('tipear filtra las sugerencias por substring', () => {
    const input: HTMLInputElement = fixture.nativeElement.querySelector('input');
    input.value = '%';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const groups = fixture.componentInstance['filteredGroups']();
    expect(groups.every((g) => g.formats.every((f) => f.includes('%')))).toBe(true);
    expect(fixture.componentInstance.value()).toBe('%');
  });

  it('un valor sin match cae en "Personalizado"', () => {
    fixture.componentInstance.value.set('mi-formato-raro');
    expect(fixture.componentInstance['category']()).toBe('Personalizado');
  });

  it('un formato de fecha típico se clasifica como "Fecha"', () => {
    fixture.componentInstance.value.set('dd/mm/yyyy');
    expect(fixture.componentInstance['category']()).toBe('Fecha');
  });
});
