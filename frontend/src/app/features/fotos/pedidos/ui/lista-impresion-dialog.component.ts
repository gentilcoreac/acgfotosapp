import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { finalize, tap } from 'rxjs';
import { downloadBlob } from '../../../../core/http';
import { NotificationService } from '../../../../shared/feedback/notification.service';
import { escapeHtml } from '../../../../shared/util/escape-html';
import { PedidosService } from '../data/pedidos.service';
import {
  ESTADO_PEDIDO,
  ESTADO_PEDIDO_LABEL,
  EstadoPedido,
  GrupoParticipanteImpresion,
  LineaAgregadoImpresion,
  ListaImpresion,
} from '../domain/pedido.model';
import { armarCsvAgregado } from '../util/csv-lista-impresion.util';
import { detectarDesajusteProporcion } from '../util/proporcion-foto.util';

export interface ListaImpresionDialogData {
  eventoId: number;
  eventoNombre: string;
}

/** Estados ofrecidos como checkbox — Cancelado nunca se ofrece (no tiene sentido imprimirlo). */
const ESTADOS_OFRECIDOS: EstadoPedido[] = [
  ESTADO_PEDIDO.Pendiente,
  ESTADO_PEDIDO.Pagado,
  ESTADO_PEDIDO.Impreso,
  ESTADO_PEDIDO.Entregado,
];

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-lista-impresion-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatCheckboxModule, MatDialogModule, MatIconModule, MatProgressBarModule],
  templateUrl: './lista-impresion-dialog.component.html',
  styleUrl: './lista-impresion-dialog.component.scss',
})
export class ListaImpresionDialogComponent {
  protected readonly data = inject<ListaImpresionDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<ListaImpresionDialogComponent>);
  private readonly service = inject(PedidosService);
  private readonly notify = inject(NotificationService);

  protected readonly estadosOfrecidos = ESTADOS_OFRECIDOS;

  /** Pagado preseleccionado (default acordado); el resto queda a elección del fotógrafo. */
  private readonly estadosSeleccionados = signal<ReadonlySet<EstadoPedido>>(new Set([ESTADO_PEDIDO.Pagado]));
  protected readonly puedeGenerar = computed(() => this.estadosSeleccionados().size > 0);

  protected readonly generando = signal(false);
  protected readonly resultado = signal<ListaImpresion | null>(null);

  protected etiquetaEstado(estado: EstadoPedido): string {
    return ESTADO_PEDIDO_LABEL[estado];
  }

  protected estaSeleccionado(estado: EstadoPedido): boolean {
    return this.estadosSeleccionados().has(estado);
  }

  protected toggleEstado(estado: EstadoPedido): void {
    const actual = new Set(this.estadosSeleccionados());
    if (actual.has(estado)) {
      actual.delete(estado);
    } else {
      actual.add(estado);
    }
    this.estadosSeleccionados.set(actual);
    this.resultado.set(null); // el resultado quedó viejo: hay que Generar de nuevo
  }

  protected generar(): void {
    if (!this.puedeGenerar() || this.generando()) {
      return;
    }
    this.generando.set(true);
    this.service
      .getListaImpresion(this.data.eventoId, [...this.estadosSeleccionados()])
      .pipe(
        tap((lista) => this.resultado.set(lista)),
        finalize(() => this.generando.set(false)),
      )
      .subscribe();
  }

  protected imprimir(): void {
    const lista = this.resultado();
    if (!lista) {
      return;
    }
    const ventana = window.open('', '_blank');
    if (!ventana) {
      this.notify.error('El navegador bloqueó la ventana de impresión (popup).');
      return;
    }
    ventana.document.write(armarHtmlListaImpresion(this.data.eventoNombre, this.etiquetasEstados(), lista));
    ventana.document.close();
    ventana.focus();
    ventana.print();
  }

  protected descargarCsv(): void {
    const lista = this.resultado();
    if (!lista) {
      return;
    }
    const nombreArchivo = `lista-impresion-${slug(this.data.eventoNombre)}.csv`;
    downloadBlob(new Blob([armarCsvAgregado(lista.agregado)], { type: 'text/csv;charset=utf-8;' }), nombreArchivo);
  }

  private etiquetasEstados(): string {
    return [...this.estadosSeleccionados()]
      .map((e) => ESTADO_PEDIDO_LABEL[e])
      .join(', ');
  }

  protected cerrar(): void {
    this.dialogRef.close();
  }
}

function slug(texto: string): string {
  return texto
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '') // acentos, tras NFD
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/(^-|-$)/g, '');
}

/**
 * Documento imprimible autocontenido con las dos vistas (mismo patrón que
 * `tarjetas.component.ts`: HTML inline, sin auth ni red — la ventana ya tiene los datos). Ancho
 * de las filas con advertencia de proporción marcado con ⚠ (best-effort, ver `proporcion-foto.util`).
 */
function armarHtmlListaImpresion(eventoNombre: string, etiquetasEstados: string, lista: ListaImpresion): string {
  const filasAgregado = lista.agregado
    .map((linea: LineaAgregadoImpresion) => {
      const advertencia = detectarDesajusteProporcion(linea.tamanoPrecioNombre, linea.anchoFoto, linea.altoFoto);
      return `
      <tr>
        <td>${escapeHtml(linea.nombreArchivoOriginal)}</td>
        <td>${escapeHtml(linea.tamanoPrecioNombre)}</td>
        <td class="num">${linea.cantidadTotal}</td>
        <td class="aviso">${advertencia ? '⚠ proporción' : ''}</td>
      </tr>`;
    })
    .join('');

  const gruposDetalle = lista.detalle
    .map((grupo: GrupoParticipanteImpresion) => {
      const lineas = grupo.lineas
        .map(
          (l) => `<li>${escapeHtml(l.tamanoPrecioNombre)} × ${l.cantidad} — ${escapeHtml(l.nombreArchivoOriginal)}</li>`,
        )
        .join('');
      return `
      <div class="participante">
        <h3>${escapeHtml(grupo.participanteNombre)}</h3>
        <ul>${lineas}</ul>
      </div>`;
    })
    .join('');

  return `<!doctype html>
<html lang="es">
<head>
<meta charset="utf-8" />
<title>Lista de impresión — ${escapeHtml(eventoNombre)}</title>
<style>
  * { box-sizing: border-box; }
  body { font-family: system-ui, sans-serif; padding: 10mm; color: #222; }
  h1 { font-size: 14pt; margin: 0 0 2mm; }
  h2 { font-size: 12pt; margin: 8mm 0 3mm; border-bottom: 1px solid #999; padding-bottom: 1mm; }
  h3 { font-size: 10.5pt; margin: 0 0 1mm; }
  .meta { font-size: 8.5pt; color: #555; margin-bottom: 6mm; }
  table { width: 100%; border-collapse: collapse; font-size: 9.5pt; }
  th, td { text-align: left; padding: 1mm 2mm; border-bottom: 1px solid #ddd; }
  td.num { text-align: right; }
  td.aviso { color: #a15c00; white-space: nowrap; }
  .participante { break-inside: avoid; page-break-inside: avoid; margin-bottom: 4mm; }
  .participante ul { margin: 0; padding-left: 5mm; font-size: 9.5pt; }
</style>
</head>
<body>
<h1>Lista de impresión — ${escapeHtml(eventoNombre)}</h1>
<div class="meta">Generado ${escapeHtml(new Date().toLocaleString('es-AR'))} — Estados: ${escapeHtml(etiquetasEstados)}</div>

<h2>Para el laboratorio (agregado por foto + tamaño)</h2>
<table>
  <thead><tr><th>Foto</th><th>Tamaño</th><th class="num">Cantidad</th><th></th></tr></thead>
  <tbody>${filasAgregado}</tbody>
</table>

<h2>Para repartir (por participante)</h2>
${gruposDetalle}
</body>
</html>`;
}
