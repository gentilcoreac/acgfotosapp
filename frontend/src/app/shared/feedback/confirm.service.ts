import { Injectable, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Observable, map } from 'rxjs';
import { ConfirmData, ConfirmDialogComponent } from './confirm-dialog.component';

/** Abre el diálogo de confirmación y emite `true`/`false` según la elección del usuario. */
@Injectable({ providedIn: 'root' })
export class ConfirmService {
  private readonly dialog = inject(MatDialog);

  confirm(data: ConfirmData): Observable<boolean> {
    return this.dialog
      .open(ConfirmDialogComponent, { data, width: '380px' })
      .afterClosed()
      .pipe(map((result) => result === true));
  }
}
