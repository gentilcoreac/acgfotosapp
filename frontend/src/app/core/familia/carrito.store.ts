import { Injectable, Signal, computed, effect, inject, signal } from '@angular/core';
import { FamiliaSessionStore } from './familia-session.store';

/** Clave de `sessionStorage` (mismo criterio que la sesión: no debe sobrevivir a otra familia en un dispositivo compartido). */
const STORAGE_KEY = 'acgfotos.familia.carrito';

/** Una foto en un tamaño, N copias. */
export interface LineaCarrito {
  fotoId: number;
  tamanoPrecioId: number;
  cantidad: number;
}

function readStorage(): LineaCarrito[] {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as LineaCarrito[]) : [];
  } catch {
    return [];
  }
}

/**
 * Carrito de la sesión de familia (Fase 2): líneas foto+tamaño+cantidad, persistidas en
 * `sessionStorage` para sobrevivir un F5. No conoce precios — el total en dinero lo calcula quien
 * lo muestra, cruzando `lineas()` con el catálogo (`FamiliaCatalogoService`); acá solo se cuentan
 * cantidades, así el store no depende de un fetch de red para su responsabilidad principal.
 *
 * Seguridad en dispositivo compartido (mismo criterio que la caché de `FamiliaGaleriaService`): al
 * cambiar el token de `FamiliaSessionStore` (nueva sesión / logout), el carrito se vacía — que
 * sobreviviera sería un bug de privacidad (fotos de una familia coladas en el pedido de la siguiente).
 */
@Injectable({ providedIn: 'root' })
export class CarritoStore {
  private readonly sessionStore = inject(FamiliaSessionStore);
  private readonly _lineas = signal<LineaCarrito[]>(readStorage());
  private lastToken: string | null;

  readonly lineas: Signal<LineaCarrito[]> = this._lineas.asReadonly();

  readonly totalItems = computed<number>(() => this._lineas().reduce((suma, l) => suma + l.cantidad, 0));

  constructor() {
    this.lastToken = this.sessionStore.token();

    effect(() => {
      const token = this.sessionStore.token();
      if (token !== this.lastToken) {
        this.vaciar();
      }
      this.lastToken = token;
    });
  }

  /** Cantidad actual de una línea puntual (0 si no está en el carrito). */
  cantidadDe(fotoId: number, tamanoPrecioId: number): number {
    return this._lineas().find((l) => l.fotoId === fotoId && l.tamanoPrecioId === tamanoPrecioId)?.cantidad ?? 0;
  }

  /** Suma de copias pedidas de esa foto en CUALQUIER tamaño — badge recordatorio de la grilla. */
  cantidadTotalDeFoto(fotoId: number): number {
    return this._lineas()
      .filter((l) => l.fotoId === fotoId)
      .reduce((suma, l) => suma + l.cantidad, 0);
  }

  /** Si ya existe la línea, suma cantidad; si no, la crea. */
  agregar(fotoId: number, tamanoPrecioId: number, cantidad: number): void {
    this.actualizar((lineas) => {
      const existente = lineas.find((l) => l.fotoId === fotoId && l.tamanoPrecioId === tamanoPrecioId);
      if (existente) {
        return lineas.map((l) => (l === existente ? { ...l, cantidad: l.cantidad + cantidad } : l));
      }
      return [...lineas, { fotoId, tamanoPrecioId, cantidad }];
    });
  }

  /** Fija la cantidad de una línea existente (la quita si cae a 0 o menos). */
  actualizarCantidad(fotoId: number, tamanoPrecioId: number, cantidad: number): void {
    if (cantidad <= 0) {
      this.quitar(fotoId, tamanoPrecioId);
      return;
    }
    this.actualizar((lineas) =>
      lineas.map((l) => (l.fotoId === fotoId && l.tamanoPrecioId === tamanoPrecioId ? { ...l, cantidad } : l)),
    );
  }

  quitar(fotoId: number, tamanoPrecioId: number): void {
    this.actualizar((lineas) => lineas.filter((l) => !(l.fotoId === fotoId && l.tamanoPrecioId === tamanoPrecioId)));
  }

  /** Cambia el tamaño de una línea existente; si ya había otra línea de esa foto en el tamaño
   * destino, suma las cantidades ahí en vez de dejar dos líneas para el mismo foto+tamaño. */
  cambiarTamano(fotoId: number, tamanoPrecioIdActual: number, tamanoPrecioIdNuevo: number): void {
    if (tamanoPrecioIdActual === tamanoPrecioIdNuevo) {
      return;
    }
    this.actualizar((lineas) => {
      const origen = lineas.find((l) => l.fotoId === fotoId && l.tamanoPrecioId === tamanoPrecioIdActual);
      if (!origen) {
        return lineas;
      }
      const destino = lineas.find((l) => l.fotoId === fotoId && l.tamanoPrecioId === tamanoPrecioIdNuevo);
      if (destino) {
        return lineas
          .filter((l) => l !== origen)
          .map((l) => (l === destino ? { ...l, cantidad: l.cantidad + origen.cantidad } : l));
      }
      return lineas.map((l) => (l === origen ? { ...l, tamanoPrecioId: tamanoPrecioIdNuevo } : l));
    });
  }

  vaciar(): void {
    this._lineas.set([]);
    sessionStorage.removeItem(STORAGE_KEY);
  }

  private actualizar(mutar: (lineas: LineaCarrito[]) => LineaCarrito[]): void {
    const lineas = mutar(this._lineas());
    this._lineas.set(lineas);
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(lineas));
  }
}
