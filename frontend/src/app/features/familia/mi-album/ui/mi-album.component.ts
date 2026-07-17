import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { FamiliaSessionStore } from '../../../../core/familia';

/**
 * Landing post-canje: saluda a la familia con los datos que ya trajo la respuesta del canje (sin
 * round-trip a la API — `FamiliaSessionStore` los persiste). Stub a propósito: la galería real es
 * el próximo ítem del roadmap (Fase 2); acá solo se prueba que la sesión de familia quedó viva.
 */
@Component({
  selector: 'tbi-mi-album',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './mi-album.component.html',
  styleUrl: './mi-album.component.scss',
})
export class MiAlbumComponent {
  private readonly session = inject(FamiliaSessionStore);

  readonly nombreEvento = this.session.nombreEvento;
  readonly participantes = this.session.participantes;

  readonly saludo = computed(() => {
    const nombres = this.participantes()
      .map((p) => p.nombre)
      .join(' y ');
    return nombres ? `Hola, familia de ${nombres}` : 'Hola';
  });
}
