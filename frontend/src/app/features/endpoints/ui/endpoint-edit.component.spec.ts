import type { Mock, MockedObject } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { EndpointsService } from '../data/endpoints.service';
import { Endpoint } from '../domain/endpoint.model';
import { EndpointEditComponent } from './endpoint-edit.component';

describe('EndpointEditComponent', () => {
  let fixture: ComponentFixture<EndpointEditComponent>;
  let save: Mock;
  let getById: Mock;
  let dialogRef: MockedObject<MatDialogRef<EndpointEditComponent, Endpoint>>;

  async function setup(
    data: {
      id?: number;
    } = {},
    loaded?: Endpoint,
  ): Promise<void> {
    save = vi
      .fn()
      .mockName('save')
      .mockImplementation((e: Endpoint) => of({ ...e, id: 1 }));
    getById = vi
      .fn()
      .mockName('getById')
      .mockReturnValue(of(loaded ?? null));
    dialogRef = {
      close: vi.fn().mockName('MatDialogRef.close'),
    } as unknown as MockedObject<MatDialogRef<EndpointEditComponent, Endpoint>>;

    await TestBed.configureTestingModule({
      imports: [EndpointEditComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: data },
        { provide: EndpointsService, useValue: { crud: { getById, save } } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(EndpointEditComponent);
    fixture.detectChanges();
  }

  const setTextField = (index: number, text: string): void => {
    const inputs = fixture.nativeElement.querySelectorAll(
      'tbi-text-field input',
    ) as NodeListOf<HTMLInputElement>;
    inputs[index].value = text;
    inputs[index].dispatchEvent(new Event('input'));
    fixture.detectChanges();
  };

  const submit = (): void => {
    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  };

  it('alta: guarda con los campos requeridos correctos', async () => {
    await setup();
    setTextField(1, 'General'); // moduleName (0=namespace)
    setTextField(2, 'UsuarioController'); // controllerName
    setTextField(3, 'GetAll'); // actionName
    submit();

    expect(save).toHaveBeenCalled();
    const payload = vi.mocked(save).mock.lastCall![0] as Endpoint;
    expect(payload.moduleName).toBe('General');
    expect(payload.controllerName).toBe('UsuarioController');
    expect(payload.actionName).toBe('GetAll');
    expect(payload.activo).toBe(true);
    expect(dialogRef.close).toHaveBeenCalled();
  });

  it('alta: no guarda si faltan campos requeridos', async () => {
    await setup();
    // moduleName vacío — form inválido
    setTextField(1, 'UsuarioController');
    setTextField(2, 'GetAll');
    submit();

    expect(save).not.toHaveBeenCalled();
  });

  it('edición: pide el endpoint por id y parchea el form', async () => {
    const loaded: Endpoint = {
      id: 5,
      namespace: 'AcgFotos.Base.Controllers.Api',
      moduleName: 'General',
      controllerName: 'UsuarioController',
      actionName: 'GetAll',
      httpMethod: 'GET',
      route: 'api/general/usuarios',
      activo: true,
    };
    await setup({ id: 5 }, loaded);

    expect(getById).toHaveBeenCalledWith(5);
    const model = (
      fixture.componentInstance as unknown as {
        model: () => Omit<Endpoint, 'id'>;
      }
    ).model();
    expect(model.moduleName).toBe('General');
    expect(model.route).toBe('api/general/usuarios');
  });

  it('edición: guarda la entidad con id preservado', async () => {
    const loaded: Endpoint = {
      id: 5,
      namespace: 'AcgFotos',
      moduleName: 'General',
      controllerName: 'RolController',
      actionName: 'GetById',
      httpMethod: 'GET',
      route: 'api/general/roles/{id}',
      activo: false,
    };
    await setup({ id: 5 }, loaded);
    submit();

    expect(save).toHaveBeenCalled();
    const payload = vi.mocked(save).mock.lastCall![0] as Endpoint;
    expect(payload.id).toBe(5);
    expect(payload.activo).toBe(false);
  });
});
