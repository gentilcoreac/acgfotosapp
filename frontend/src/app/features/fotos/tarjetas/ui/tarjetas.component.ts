import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  linkedSignal,
  signal,
} from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { catchError, map, of } from 'rxjs';
import { NotificationService } from '../../../../shared/feedback/notification.service';
import {
  TbiSelectComponent,
  TbiSelectOption,
} from '../../../../shared/ui/tbi-select/tbi-select.component';
import { escapeHtml } from '../../../../shared/util/escape-html';
import { GruposService } from '../../grupos/data/grupos.service';
import { TarjetaParticipante } from '../../grupos/domain/grupo.model';
import { EventosService } from '../../eventos/data/eventos.service';
import { Evento } from '../../eventos/domain/evento.model';

/**
 * Tarjetas de acceso por grupo (cierre de Fase 1): una por participante con su código y el QR de canje
 * (PNG base64 que ya trae la API). En pantalla se previsualizan; "Imprimir" abre una ventana
 * limpia solo con las tarjetas (así no pelea con el layout del shell) y dispara el print del
 * browser — de ahí salen las tarjetitas para repartir a las familias.
 */
// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-tarjetas',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, TbiSelectComponent],
  templateUrl: './tarjetas.component.html',
  styleUrl: './tarjetas.component.scss',
})
export class TarjetasComponent {
  private readonly gruposService = inject(GruposService);
  private readonly eventosService = inject(EventosService);
  private readonly notify = inject(NotificationService);

  // ── Cascada evento → grupo ───────────────────────────────────────────────────────────────────

  private readonly eventosResource = rxResource({
    stream: () =>
      this.eventosService.crud.getAll().pipe(
        map((result) => result.items),
        // el toast de error lo emite el errorInterceptor (global)
        catchError(() => of<Evento[]>([])),
      ),
  });
  protected readonly eventoOptions = computed<TbiSelectOption<number>[]>(
    () => this.eventosResource.value()?.map((e) => ({ value: e.id ?? 0, label: e.nombre })) ?? [],
  );
  protected readonly eventoId = signal<number | null>(null);

  protected readonly grupoId = linkedSignal<number | null, number | null>({
    source: this.eventoId,
    computation: () => null,
  });

  private readonly gruposResource = rxResource({
    params: () => this.eventoId() ?? undefined,
    stream: ({ params: eventoId }) =>
      this.gruposService.crud
        .getAllByCriteria({ eventoId, page: 0, pageSize: 500 })
        .pipe(map((result) => result.items)),
  });
  protected readonly grupoOptions = computed<TbiSelectOption<number>[]>(
    () => this.gruposResource.value()?.map((c) => ({ value: c.id ?? 0, label: c.nombre })) ?? [],
  );

  // ── Tarjetas del grupo ───────────────────────────────────────────────────────────────────────

  private readonly tarjetasResource = rxResource({
    params: () => this.grupoId() ?? undefined,
    stream: ({ params: grupoId }) => this.gruposService.getTarjetas(grupoId),
  });

  protected readonly tarjetasGrupo = computed(() =>
    this.tarjetasResource.hasValue() ? this.tarjetasResource.value() : undefined,
  );
  protected readonly tarjetas = computed(() => this.tarjetasGrupo()?.tarjetas ?? []);
  protected readonly sinCodigo = computed(() => this.tarjetas().filter((t) => !t.codigo));

  // ── Impresión ────────────────────────────────────────────────────────────────────────────────

  protected imprimir(): void {
    const grupo = this.tarjetasGrupo();
    if (!grupo || this.tarjetas().length === 0) {
      return;
    }

    const ventana = window.open('', '_blank');
    if (!ventana) {
      this.notify.error('El navegador bloqueó la ventana de impresión (popup).');
      return;
    }

    ventana.document.write(armarHtmlImprimible(grupo.nombreEvento, grupo.nombreGrupo, this.tarjetas()));
    ventana.document.close();
    ventana.focus();
    ventana.print();
  }
}

/**
 * Documento imprimible autocontenido: solo las tarjetas, sin el layout del shell. CSS mínimo
 * inline (A4, 2 columnas, corte prohibido dentro de una tarjeta). Los QR van embebidos en base64,
 * así la ventana no necesita auth ni red.
 */
function armarHtmlImprimible(evento: string, grupo: string, tarjetas: TarjetaParticipante[]): string {
  const celdas = tarjetas
    .filter((t) => t.codigo)
    .map(
      (t) => `
      <div class="tarjeta">
        <div class="datos">
          <div class="evento">${escapeHtml(evento)} — ${escapeHtml(grupo)}</div>
          <div class="participante">${escapeHtml(t.nombre)}</div>
          <div class="codigo">${escapeHtml(t.codigo!)}</div>
          <div class="ayuda">Escaneá el QR o ingresá el código para ver las fotos.</div>
        </div>
        <img class="qr" src="data:image/png;base64,${t.qrPngBase64}" alt="QR" />
      </div>`,
    )
    .join('');

  return `<!doctype html>
<html lang="es">
<head>
<meta charset="utf-8" />
<title>Tarjetas — ${escapeHtml(evento)} — ${escapeHtml(grupo)}</title>
<style>
  * { box-sizing: border-box; margin: 0; }
  body { font-family: system-ui, sans-serif; padding: 10mm; }
  .grilla { display: grid; grid-template-columns: 1fr 1fr; gap: 6mm; }
  .tarjeta {
    display: flex; align-items: center; gap: 4mm;
    border: 1px dashed #999; border-radius: 3mm; padding: 4mm;
    break-inside: avoid; page-break-inside: avoid;
  }
  .datos { flex: 1; min-width: 0; }
  .evento { font-size: 9pt; color: #555; }
  .participante { font-size: 12pt; font-weight: 600; margin-top: 1mm; }
  .codigo { font-size: 16pt; font-weight: 700; letter-spacing: 1px; margin-top: 2mm; font-family: Consolas, monospace; }
  .ayuda { font-size: 8pt; color: #555; margin-top: 2mm; }
  .qr { width: 30mm; height: 30mm; }
</style>
</head>
<body>
<div class="grilla">${celdas}</div>
</body>
</html>`;
}

