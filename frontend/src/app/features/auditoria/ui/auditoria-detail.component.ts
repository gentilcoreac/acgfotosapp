import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { lookupResource } from '../../../shared/util/lookup-resource';
import { AuditoriaService } from '../data/auditoria.service';

/**
 * Detalle de un registro de auditoría (diálogo read-only). Carga el registro completo por id
 * (`parametros`/`resultContent`, que el listado omite por peso) y lo muestra en filas etiqueta/valor;
 * el cuerpo de los campos largos va en bloques `<pre>`.
 */
@Component({
  selector: 'tbi-auditoria-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, MatButtonModule, MatDialogModule, MatProgressSpinnerModule],
  templateUrl: './auditoria-detail.component.html',
  styleUrl: './auditoria-detail.component.scss',
})
export class AuditoriaDetailComponent {
  private readonly service = inject(AuditoriaService);
  private readonly data = inject<{ id: number }>(MAT_DIALOG_DATA);

  private readonly registroResource = lookupResource(
    () => this.service.getById(this.data.id),
    null,
  );
  protected readonly registro = computed(() => this.registroResource.value() ?? null);
  protected readonly loading = computed(() => this.registroResource.isLoading());
}
