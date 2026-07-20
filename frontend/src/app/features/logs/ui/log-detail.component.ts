import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { lookupResource } from '../../../shared/util/lookup-resource';
import { LogsService } from '../data/logs.service';

/**
 * Detalle de una entrada de log (diálogo read-only). Carga el registro COMPLETO por id (el listado va
 * liviano, sin Exception/Properties); muestra los campos pesados en bloques `<pre>`.
 */
@Component({
  selector: 'tbi-log-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, MatButtonModule, MatDialogModule, MatProgressSpinnerModule],
  templateUrl: './log-detail.component.html',
  styleUrl: './log-detail.component.scss',
})
export class LogDetailComponent {
  private readonly service = inject(LogsService);
  private readonly data = inject<{ id: number }>(MAT_DIALOG_DATA);

  private readonly logResource = lookupResource(
    () => this.service.getByIdAllTenants(this.data.id),
    null,
  );
  protected readonly log = computed(() => this.logResource.value() ?? null);
  protected readonly loading = computed(() => this.logResource.isLoading());
}
