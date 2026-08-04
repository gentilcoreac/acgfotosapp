import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { map } from 'rxjs';
import { ConfirmService } from '../../../../shared/feedback/confirm.service';
import { NotificationService } from '../../../../shared/feedback/notification.service';
import { MarcaAguaService } from '../data/marca-agua.service';
import { PerfilMarcaAgua } from '../domain/marca-agua.model';
import { PerfilMarcaAguaCanvasComponent } from './perfil-marca-agua-canvas.component';
import { PerfilMarcaAguaEditComponent } from './perfil-marca-agua-edit.component';

/**
 * Listado de perfiles de marca de agua (ADR-15, spec 7.2): cada fila renderiza la marca real, no
 * una descripción. Pocos perfiles por tenant — sin paginado, mismo criterio que `TarjetasComponent`.
 */
// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-marca-agua-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, PerfilMarcaAguaCanvasComponent],
  templateUrl: './marca-agua-list.component.html',
  styleUrl: './marca-agua-list.component.scss',
})
export class MarcaAguaListComponent {
  private readonly service = inject(MarcaAguaService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotificationService);
  private readonly confirmService = inject(ConfirmService);

  protected readonly perfilesResource = rxResource({
    stream: () => this.service.crud.getAll().pipe(map((result) => result.items)),
  });

  protected nuevo(): void {
    this.abrirEditor();
  }

  protected editar(perfil: PerfilMarcaAgua): void {
    this.abrirEditor(perfil.id);
  }

  protected eliminar(perfil: PerfilMarcaAgua): void {
    if (perfil.id == null) {
      return;
    }
    const id = perfil.id;
    this.confirmService
      .confirm({ title: 'Eliminar', message: `¿Eliminar el perfil "${perfil.nombre}"?` })
      .subscribe((ok) => {
        if (!ok) {
          return;
        }
        this.service.crud.delete(id).subscribe(() => {
          this.notify.success('Perfil eliminado.');
          this.perfilesResource.reload();
        });
      });
  }

  private abrirEditor(id?: number): void {
    this.dialog
      .open<PerfilMarcaAguaEditComponent, { id?: number }, PerfilMarcaAgua>(PerfilMarcaAguaEditComponent, {
        data: { id },
        width: '1100px',
        maxWidth: '96vw',
        maxHeight: '92vh',
        autoFocus: false,
      })
      .afterClosed()
      .subscribe((saved) => {
        if (saved) {
          this.notify.success('Perfil guardado.');
          this.perfilesResource.reload();
        }
      });
  }
}
