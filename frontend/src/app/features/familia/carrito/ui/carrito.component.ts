import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';
import { catchError, finalize, of } from 'rxjs';
import { ApiError, errorMessages } from '../../../../core/models';
import {
  CarritoStore,
  FamiliaCatalogoService,
  FamiliaGaleriaService,
  FotoFamilia,
  formatearPrecio,
  PedidoService,
  TamanoPrecio,
} from '../../../../core/familia';
import { FotoFamiliaImgComponent } from '../../mi-album/ui/foto-familia-img.component';
import { TbiButtonComponent } from '../../../../shared/ui/tbi-button/tbi-button.component';
import { TbiTextFieldComponent } from '../../../../shared/ui/tbi-text-field/tbi-text-field.component';

/** Línea del carrito ya cruzada con la foto y el tamaño para mostrarla. */
interface LineaCarritoDetalle {
  fotoId: number;
  tamanoPrecioId: number;
  cantidad: number;
  foto: FotoFamilia;
  tamano: TamanoPrecio;
  subtotal: number;
}

/**
 * Carrito + confirmación de pedido (Fase 2, ADR-07): revisa las líneas agregadas desde la grilla o
 * el preview (`AgregarCarritoComponent`), permite ajustar cantidad/quitar, muestra el total en vivo
 * y confirma con nombre + teléfono de contacto. El precio SIEMPRE sale del catálogo (`tamano`), no
 * de lo que hubiera en el carrito — el snapshot real igualmente lo congela el backend al confirmar.
 */
@Component({
  selector: 'tbi-carrito',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    FotoFamiliaImgComponent,
    TbiButtonComponent,
    TbiTextFieldComponent,
  ],
  templateUrl: './carrito.component.html',
  styleUrl: './carrito.component.scss',
})
export class CarritoComponent {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly carritoStore = inject(CarritoStore);
  private readonly galeriaService = inject(FamiliaGaleriaService);
  private readonly catalogoService = inject(FamiliaCatalogoService);
  private readonly pedidoService = inject(PedidoService);
  private readonly router = inject(Router);

  private readonly fotosResource = rxResource({
    stream: () => this.galeriaService.listar().pipe(catchError(() => of<FotoFamilia[]>([]))),
  });

  private readonly tamanosPreciosResource = rxResource({
    stream: () => this.catalogoService.listarTamanosPrecios().pipe(catchError(() => of<TamanoPrecio[]>([]))),
  });

  protected readonly cargando = computed(
    () => this.fotosResource.isLoading() || this.tamanosPreciosResource.isLoading(),
  );

  /** Cruza las líneas del store con foto/tamaño; una línea cuya foto o tamaño ya no exista se ignora (no debería pasar en uso normal). */
  protected readonly lineas = computed<LineaCarritoDetalle[]>(() => {
    const fotos = this.fotosResource.value() ?? [];
    const tamanos = this.tamanosPreciosResource.value() ?? [];
    if (fotos.length === 0 || tamanos.length === 0) {
      return [];
    }
    return this.carritoStore
      .lineas()
      .map((linea) => {
        const foto = fotos.find((f) => f.id === linea.fotoId);
        const tamano = tamanos.find((t) => t.id === linea.tamanoPrecioId);
        if (!foto || !tamano) {
          return null;
        }
        return { ...linea, foto, tamano, subtotal: linea.cantidad * tamano.precioUnitario };
      })
      .filter((l): l is LineaCarritoDetalle => l !== null);
  });

  protected readonly total = computed(() => this.lineas().reduce((suma, l) => suma + l.subtotal, 0));

  protected readonly confirmando = signal(false);
  protected readonly errors = signal<string[]>([]);

  protected readonly form = this.fb.group({
    nombreContacto: ['', [Validators.required]],
    telefonoContacto: ['', [Validators.required]],
  });

  protected readonly formatearPrecio = formatearPrecio;

  protected sumar(linea: LineaCarritoDetalle): void {
    this.carritoStore.actualizarCantidad(linea.fotoId, linea.tamanoPrecioId, linea.cantidad + 1);
  }

  protected restar(linea: LineaCarritoDetalle): void {
    this.carritoStore.actualizarCantidad(linea.fotoId, linea.tamanoPrecioId, linea.cantidad - 1);
  }

  protected quitar(linea: LineaCarritoDetalle): void {
    this.carritoStore.quitar(linea.fotoId, linea.tamanoPrecioId);
  }

  protected volver(): void {
    this.router.navigateByUrl('/mi-album');
  }

  protected confirmar(): void {
    if (this.form.invalid || this.lineas().length === 0) {
      this.form.markAllAsTouched();
      return;
    }

    this.confirmando.set(true);
    this.errors.set([]);
    const { nombreContacto, telefonoContacto } = this.form.getRawValue();
    this.pedidoService
      .confirmar({
        nombreContacto,
        telefonoContacto,
        items: this.lineas().map((l) => ({
          fotoId: l.fotoId,
          tamanoPrecioId: l.tamanoPrecioId,
          cantidad: l.cantidad,
        })),
      })
      .pipe(finalize(() => this.confirmando.set(false)))
      .subscribe({
        next: (pedido) => {
          this.carritoStore.vaciar();
          this.router.navigate(['/pedido-confirmado'], { state: { pedido } });
        },
        error: (error: ApiError) => {
          this.errors.set(errorMessages(error, ['No se pudo confirmar el pedido.']));
        },
      });
  }
}
