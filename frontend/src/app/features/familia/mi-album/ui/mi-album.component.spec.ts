import { TestBed } from '@angular/core/testing';
import { FamiliaSessionStore } from '../../../../core/familia';
import { MiAlbumComponent } from './mi-album.component';

describe('MiAlbumComponent', () => {
  function create(): { cmp: MiAlbumComponent; el: HTMLElement } {
    const fixture = TestBed.createComponent(MiAlbumComponent);
    fixture.detectChanges();
    return { cmp: fixture.componentInstance, el: fixture.nativeElement as HTMLElement };
  }

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({ imports: [MiAlbumComponent] });
  });

  afterEach(() => sessionStorage.clear());

  it('saluda con el/los nombre(s) de la sesión', () => {
    TestBed.inject(FamiliaSessionStore).setSession({
      token: 't',
      validTo: new Date(Date.now() + 60000).toISOString(),
      eventoId: 1,
      nombreEvento: 'Egresados 2026',
      participantes: [{ id: 100, nombre: 'Ana Pérez' }],
    });

    const { cmp, el } = create();
    expect(cmp.saludo()).toBe('Hola, familia de Ana Pérez');
    expect(el.textContent).toContain('Egresados 2026');
  });

  it('con más de un participante los une con "y"', () => {
    TestBed.inject(FamiliaSessionStore).setSession({
      token: 't',
      validTo: new Date(Date.now() + 60000).toISOString(),
      eventoId: 1,
      nombreEvento: 'Egresados 2026',
      participantes: [
        { id: 100, nombre: 'Ana Pérez' },
        { id: 101, nombre: 'José López' },
      ],
    });

    const { cmp } = create();
    expect(cmp.saludo()).toBe('Hola, familia de Ana Pérez y José López');
  });
});
