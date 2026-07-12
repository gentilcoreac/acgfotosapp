import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormField, form, maxLength, required } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { EditComponentBase } from '../../../shared/forms/edit-component-base';
import { TbiButtonComponent } from '../../../shared/ui/tbi-button/tbi-button.component';
import {
  TbiSelectComponent,
  TbiSelectOption,
} from '../../../shared/ui/tbi-select/tbi-select.component';
import { TbiTextFieldComponent } from '../../../shared/ui/tbi-text-field/tbi-text-field.component';
import { EndpointsService } from '../data/endpoints.service';
import { Endpoint } from '../domain/endpoint.model';

/** DB limita `httpMethod` a 10 chars sin regla en la API (`EndpointDtoValidator`): un valor
 * inválido crea una fila basura en la tabla que alimenta la autorización → select cerrado. */
const HTTP_METHOD_OPTIONS: TbiSelectOption<string>[] = [
  { value: '', label: '(Cualquiera)' },
  { value: 'GET', label: 'GET' },
  { value: 'POST', label: 'POST' },
  { value: 'PUT', label: 'PUT' },
  { value: 'PATCH', label: 'PATCH' },
  { value: 'DELETE', label: 'DELETE' },
];

// TODO (Fase 4 - i18n): textos en español por ahora.

interface EndpointFormModel {
  namespace: string;
  moduleName: string;
  controllerName: string;
  actionName: string;
  httpMethod: string;
  route: string;
  activo: boolean;
}

@Component({
  selector: 'tbi-endpoint-edit',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormField,
    MatDialogModule,
    MatButtonModule,
    MatProgressBarModule,
    MatSlideToggleModule,
    TbiTextFieldComponent,
    TbiSelectComponent,
    TbiButtonComponent,
  ],
  templateUrl: './endpoint-edit.component.html',
  styleUrl: './endpoint-edit.component.scss',
})
export class EndpointEditComponent extends EditComponentBase<Endpoint, EndpointFormModel> {
  private readonly service = inject(EndpointsService);
  protected readonly crud = this.service.crud;
  private loaded?: Endpoint;

  protected readonly httpMethodOptions = HTTP_METHOD_OPTIONS;

  protected readonly model = signal<EndpointFormModel>({
    namespace: '',
    moduleName: '',
    controllerName: '',
    actionName: '',
    httpMethod: '',
    route: '',
    activo: true,
  });

  protected readonly form = form(this.model, (path) => {
    required(path.moduleName, { message: 'Requerido' });
    maxLength(path.moduleName, 100, { message: 'Máximo 100 caracteres' });
    required(path.controllerName, { message: 'Requerido' });
    maxLength(path.controllerName, 100, { message: 'Máximo 100 caracteres' });
    required(path.actionName, { message: 'Requerido' });
    maxLength(path.actionName, 100, { message: 'Máximo 100 caracteres' });
    maxLength(path.route, 500, { message: 'Máximo 500 caracteres' });
  });

  protected toEntity(): Endpoint {
    // Mapeo explícito (no `...this.model()`): si el modelo gana campos UI-only, no viajan a la API.
    const value = this.model();
    return {
      ...this.loaded,
      namespace: value.namespace,
      moduleName: value.moduleName,
      controllerName: value.controllerName,
      actionName: value.actionName,
      httpMethod: value.httpMethod,
      route: value.route,
      activo: value.activo,
    };
  }

  protected patchForm(entity: Endpoint): void {
    this.loaded = entity;
    this.model.set({
      namespace: entity.namespace ?? '',
      moduleName: entity.moduleName ?? '',
      controllerName: entity.controllerName ?? '',
      actionName: entity.actionName ?? '',
      httpMethod: entity.httpMethod ?? '',
      route: entity.route ?? '',
      activo: entity.activo ?? true,
    });
  }
}
