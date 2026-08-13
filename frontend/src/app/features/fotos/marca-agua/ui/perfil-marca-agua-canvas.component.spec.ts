import type { Mock } from 'vitest';
import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { MarcaAguaService } from '../data/marca-agua.service';
import { MODO_COLOCACION, MODO_FUSION, PerfilMarcaAgua } from '../domain/marca-agua.model';
import { PerfilMarcaAguaCanvasComponent } from './perfil-marca-agua-canvas.component';

function perfil(escalaPorcentaje: number, storageKey = 'k1'): PerfilMarcaAgua {
  return {
    id: 1,
    nombre: 'P',
    esDefault: false,
    marcarThumb: true,
    capas: [
      {
        id: 1,
        orden: 0,
        storageKey,
        anchoPx: 200,
        altoPx: 100,
        modoColocacion: MODO_COLOCACION.Repetida,
        posicion: null,
        escalaPorcentaje,
        margenPorcentaje: 5,
        separacionPorcentaje: 30,
        anguloGrados: 0,
        opacidad: 0.8,
        modoFusion: MODO_FUSION.Normal,
      },
    ],
  };
}

@Component({
  imports: [PerfilMarcaAguaCanvasComponent],
  template: `<tbi-perfil-marca-agua-canvas [perfil]="p()" [comprimir]="false" />`,
})
class HostComponent {
  readonly p = signal(perfil(20));
}

describe('PerfilMarcaAguaCanvasComponent', () => {
  let fixture: ComponentFixture<HostComponent>;
  let cargarAssets: Mock;

  beforeEach(async () => {
    cargarAssets = vi
      .fn()
      .mockName('cargarAssets')
      .mockReturnValue(of(new Map([['k1', {} as ImageBitmap]])));

    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [{ provide: MarcaAguaService, useValue: { cargarAssets } }],
    }).compileComponents();

    fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('ajustar la colocación NO vuelve a pedir los assets (el PNG de una capa es inmutable)', async () => {
    const pedidosIniciales = cargarAssets.mock.calls.length;

    fixture.componentInstance.p.set(perfil(60));
    fixture.detectChanges();
    await fixture.whenStable();

    expect(cargarAssets).toHaveBeenCalledTimes(pedidosIniciales);
  });

  it('cambiar el contenido de una capa (otra clave de storage) sí vuelve a pedir los assets', async () => {
    const pedidosIniciales = cargarAssets.mock.calls.length;

    fixture.componentInstance.p.set(perfil(20, 'k2'));
    fixture.detectChanges();
    await fixture.whenStable();

    expect(cargarAssets).toHaveBeenCalledTimes(pedidosIniciales + 1);
  });
});
