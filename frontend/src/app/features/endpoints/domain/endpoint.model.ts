import { EditableEntity } from '../../../shared/forms/edit-component-base';

export interface Endpoint extends EditableEntity {
  id?: number;
  namespace: string;
  moduleName: string;
  controllerName: string;
  actionName: string;
  httpMethod: string;
  route: string;
  activo: boolean;
}
