import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TbiCarouselComponent } from './tbi-carousel.component';

@Component({
  imports: [TbiCarouselComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <tbi-carousel [items]="items()" [(index)]="index">
      <ng-template let-item let-i="index">
        <span class="etiqueta">{{ item }} ({{ i }})</span>
      </ng-template>
    </tbi-carousel>
  `,
})
class HostComponent {
  readonly items = signal(['a', 'b', 'c']);
  readonly index = signal(0);
}

describe('TbiCarouselComponent', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;
  let el: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [provideNoopAnimations()],
    }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
    el = fixture.nativeElement as HTMLElement;
  });

  const etiqueta = (): string => el.querySelector('.etiqueta')?.textContent?.trim() ?? '';

  it('renderiza el item actual vía el template proyectado', () => {
    expect(etiqueta()).toBe('a (0)');
  });

  it('siguiente/anterior navegan y dan la vuelta en las puntas', () => {
    el.querySelector<HTMLButtonElement>('.tbi-carousel__nav--next')!.click();
    fixture.detectChanges();
    expect(etiqueta()).toBe('b (1)');

    el.querySelector<HTMLButtonElement>('.tbi-carousel__nav--next')!.click();
    fixture.detectChanges();
    expect(etiqueta()).toBe('c (2)');

    el.querySelector<HTMLButtonElement>('.tbi-carousel__nav--next')!.click();
    fixture.detectChanges();
    expect(etiqueta()).toBe('a (0)'); // dio la vuelta

    el.querySelector<HTMLButtonElement>('.tbi-carousel__nav--prev')!.click();
    fixture.detectChanges();
    expect(etiqueta()).toBe('c (2)'); // vuelta para atrás
  });

  it('las flechas de teclado navegan con el carrusel enfocado', () => {
    const root = el.querySelector('.tbi-carousel') as HTMLElement;
    root.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true }));
    fixture.detectChanges();
    expect(etiqueta()).toBe('b (1)');
  });

  it('el índice es bindable desde afuera (two-way)', () => {
    host.index.set(2);
    fixture.detectChanges();
    expect(etiqueta()).toBe('c (2)');
  });

  it('con un solo item, no muestra flechas ni contador', () => {
    host.items.set(['único']);
    fixture.detectChanges();
    expect(el.querySelector('.tbi-carousel__nav')).toBeNull();
    expect(el.querySelector('.tbi-carousel__contador')).toBeNull();
  });

  it('con items vacíos no rompe: no renderiza el template proyectado (evita `item` undefined en el caller)', () => {
    host.items.set([]);
    fixture.detectChanges();
    expect(el.querySelector('.etiqueta')).toBeNull();
  });
});
