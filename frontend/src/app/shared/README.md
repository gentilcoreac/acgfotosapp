# shared/

Componentes, pipes y directivas **reutilizables y sin estado de negocio**. Reemplaza a los
componentes `Mvz*` del proyecto original. Prefijo de selectores: `tbi-`.

| Carpeta | Contenido                                                                      |
| ------- | ------------------------------------------------------------------------------ |
| `ui/`   | Design system: `tbi-button`, `tbi-table`, `tbi-select`, `tbi-text-field`, etc. |

Reglas: standalone + `OnPush`, `input()/output()/model()`, sin dependencias a features, sin
llamadas HTTP. Los wrappers encapsulan el UI kit (**Angular Material + CDK**) para permitir swap futuro.
