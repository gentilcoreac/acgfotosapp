# core/

Servicios **singleton** y plomería transversal de la app. Se provee una sola vez (root) y
**no** se importa desde features como si fueran componentes reutilizables.

| Carpeta   | Contenido                                                        |
| --------- | ---------------------------------------------------------------- |
| `auth/`   | `AuthStore` (signals), guards funcionales, `authInterceptor`.    |
| `config/` | `AppConfigService` (config runtime vía `provideAppInitializer`). |
| `http/`   | `ApiClient` base tipado, `errorInterceptor`.                     |
| `models/` | Contratos/DTOs transversales.                                    |

Reglas: nada de UI acá; sin estado mutable global (usar signals); sin `HttpClient` crudo en
componentes (siempre vía servicios sobre `ApiClient`).
