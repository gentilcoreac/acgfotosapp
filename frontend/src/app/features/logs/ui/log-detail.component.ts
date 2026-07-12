import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LogsService } from '../data/logs.service';
import { LogInfo } from '../domain/log.model';

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
export class LogDetailComponent implements OnInit {
  private readonly service = inject(LogsService);
  private readonly data = inject<{ id: number }>(MAT_DIALOG_DATA);

  protected readonly log = signal<LogInfo | null>(null);
  protected readonly loading = signal(true);

  ngOnInit(): void {
    this.service.getByIdAllTenants(this.data.id).subscribe({
      next: (log) => {
        this.log.set(log);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
