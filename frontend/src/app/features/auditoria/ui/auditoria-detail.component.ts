import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuditoriaService } from '../data/auditoria.service';
import { Auditoria } from '../domain/auditoria.model';

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
export class AuditoriaDetailComponent implements OnInit {
  private readonly service = inject(AuditoriaService);
  private readonly data = inject<{ id: number }>(MAT_DIALOG_DATA);

  protected readonly registro = signal<Auditoria | null>(null);
  protected readonly loading = signal(true);

  ngOnInit(): void {
    this.service.getById(this.data.id).subscribe({
      next: (registro) => {
        this.registro.set(registro);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
