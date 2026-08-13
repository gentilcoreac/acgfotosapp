import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialogModule } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TbiVisorFotosComponent } from './tbi-visor-fotos.component';

interface FotoDePrueba {
  id: number;
  nombre: string;
}

@Component({
  imports: [TbiVisorFotosComponent, MatDialogModule],
  template: `
    <tbi-visor-fotos [items]="fotos()" [(index)]="index">
      <ng-template let-foto let-i="index">
        <span class="item">{{ i }}:{{ foto.nombre }}</span>
      </ng-template>
      <span visorEsquinaInferiorDerecha class="archivo">nombre.jpg</span>
      <span visorBajoContador class="marca">✓</span>
    </tbi-visor-fotos>
  `,
})
class HostComponent {
  readonly fotos = signal<FotoDePrueba[]>([
    { id: 1, nombre: 'a' },
    { id: 2, nombre: 'b' },
    { id: 3, nombre: 'c' },
  ]);
  readonly index = signal(0);
}

describe('TbiVisorFotosComponent', () => {
  let fixture: ComponentFixture<HostComponent>;

  function texto(selector: string): string {
    return (fixture.nativeElement.querySelector(selector) as HTMLElement | null)?.textContent?.trim() ?? '';
  }

  function click(selector: string): void {
    (fixture.nativeElement.querySelector(selector) as HTMLButtonElement).click();
    fixture.detectChanges();
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [provideNoopAnimations()],
    }).compileComponents();

    fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('muestra el item del índice pedido, con su posición', () => {
    fixture.componentInstance.index.set(1);
    fixture.detectChanges();

    expect(texto('.item')).toBe('1:b');
  });

  it('informa la posición dentro de la colección', () => {
    expect(texto('.contador')).toBe('1 / 3');
  });

  it('recorre la colección con las flechas', () => {
    click('.nav--next');
    expect(texto('.item')).toBe('1:b');

    click('.nav--prev');
    expect(texto('.item')).toBe('0:a');
  });

  it('el índice recorrido vuelve al caller', () => {
    click('.nav--next');
    expect(fixture.componentInstance.index()).toBe(1);
  });

  it('proyecta los distintivos que aporta cada pantalla sobre la foto', () => {
    expect(texto('.archivo')).toBe('nombre.jpg');
    expect(texto('.marca')).toBe('✓');
  });

  it('con una sola foto no ofrece recorrido ni contador', async () => {
    fixture.componentInstance.fotos.set([{ id: 1, nombre: 'unica' }]);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('.nav--next')).toBeNull();
    expect(fixture.nativeElement.querySelector('.contador')).toBeNull();
    expect(texto('.item')).toBe('0:unica');
  });

  it('el contador y el cerrar se pueden apagar, para pantallas que los ofrecen en otro lado', async () => {
    TestBed.resetTestingModule();
    @Component({
      imports: [TbiVisorFotosComponent, MatDialogModule],
      template: `
        <tbi-visor-fotos [items]="fotos" [mostrarContador]="false" [mostrarCerrar]="false">
          <ng-template let-foto><span class="item">{{ foto.nombre }}</span></ng-template>
        </tbi-visor-fotos>
      `,
    })
    class SinChrome {
      readonly fotos = [
        { id: 1, nombre: 'a' },
        { id: 2, nombre: 'b' },
      ];
    }

    await TestBed.configureTestingModule({
      imports: [SinChrome],
      providers: [provideNoopAnimations()],
    }).compileComponents();
    const f = TestBed.createComponent(SinChrome);
    f.detectChanges();
    await f.whenStable();

    expect(f.nativeElement.querySelector('.contador')).toBeNull();
    expect(f.nativeElement.querySelector('.cerrar')).toBeNull();
    // El recorrido sigue disponible: apagar el chrome no apaga la navegación.
    expect(f.nativeElement.querySelector('.nav--next')).not.toBeNull();
  });

  it('sin fotos no rompe ni renderiza el template del caller', async () => {
    fixture.componentInstance.fotos.set([]);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('.item')).toBeNull();
  });
});
