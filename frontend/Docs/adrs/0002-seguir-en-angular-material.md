# ADR-0002: Seguir en Angular Material (no migrar a MWC, Tailwind ni headless)

- **Estado:** Aceptado · **Fecha:** 2026-06-13 · **Decisores:** Alberto Gentilcore

> Surge al evaluar mejoras de _look & feel_. Disparador: la duda de si Angular Material está
> "muriendo" tras ver que **Material Web Components está en maintenance mode**.

## Contexto

Antes de invertir en pulir la UI, se planteó la pregunta de fondo: **¿conviene seguir en Angular
Material, o la gente ya lo está reemplazando (p. ej. por Tailwind)?** El miedo concreto nació de este
hilo: <https://github.com/material-components/material-web/discussions/5642>, donde se anuncia que
**"MWC is in maintenance mode"**.

**Aclaración clave — son dos proyectos distintos que se confunden:**

| Paquete                                 | Qué es                                                        | Estado (jun 2026)                              |
| --------------------------------------- | ------------------------------------------------------------- | ---------------------------------------------- |
| `@material/web` (**MWC**)               | Web components de Material, **framework-agnósticos** (Google) | **Maintenance mode** (el del hilo)             |
| `@angular/material` (**lo que usamos**) | Librería de **componentes para Angular** (equipo Angular)     | **Activo** — ya soporta Angular v21 (ene 2026) |

El freeze de MWC **no afecta** a Material para Angular. Cuando se anunció, el equipo de Angular fue
explícito: harían un fork de los componentes que necesitaban y seguirían desarrollándolos con
normalidad; el Angular CDK y Material para Angular **no se ven afectados**. Nuestro stack es
`@angular/material` v20 con M3 (`mat.theme()`, `--mat-sys-*`) — no toca MWC en ningún lado.

**Tailwind no es el dilema que parece.** "¿Material o Tailwind?" está mal planteado: **no compiten**.
Angular Material es una librería de **componentes** (table, dialog, datepicker, autocomplete — con su
accesibilidad, focus management y ARIA). Tailwind es **utilidades de CSS** (clases de layout/spacing);
no aporta un solo componente. Nadie "reemplaza Material por Tailwind"; a lo sumo se usan **juntos**
(Material para componentes, Tailwind para layout en vez de SCSS).

## Opciones

- **A — Seguir en Angular Material (elegida).** Componentes Material + SCSS por componente sobre los
  tokens `--mat-sys-*`. Es el estado actual.
- **B — Angular Material + Tailwind.** Aditivo: Material para los componentes, Tailwind para reemplazar
  el SCSS repetido de layout (`display:flex; gap; padding`). Reversible, bajo riesgo. Decisión de DX,
  no de supervivencia. Ver "Consideraciones futuras".
- **C — Headless (Angular CDK + Tailwind).** Usar solo `@angular/cdk` (overlay, a11y, drag-drop) y
  construir TODO el visual a mano (estilo Spartan/ng, shadcn-like). **Descartada (hoy):** obliga a
  reescribir todos los `tbi-*` (CVAs sobre `mat-form-field`/`mat-select`/etc.), el theming runtime por
  tenant y revalidar la suite de tests. Cambio "brusco" sin retorno claro.
- **D — Reemplazar por otra librería (PrimeNG, Taiga UI, Nebular…).** **Descartada:** mismo costo de
  reescritura que C, perdiendo además la integración M3 ya hecha (theming por tenant, dark mode,
  branding del login).

## Decisión

**Opción A.** Seguir en Angular Material. Razones:

1. **El miedo está mal dirigido:** el hilo es de MWC (`@material/web`), un paquete que el proyecto **no
   usa**. `@angular/material` sigue activo y con soporte vigente.
2. **Inversión ya hecha en M3:** tokens `--mat-sys-*`, theming runtime por tenant (ADR-0001, 3 fases),
   dark mode, branding del login y ~8 componentes `tbi-*` construidos encima. Migrar = tirar semanas de
   trabajo verificado.
3. **Las mejoras de _look & feel_ no requieren cambiar de stack:** son CSS/layout sobre los tokens que ya
   existen. El "se ve más pro" se consigue sin tocar la base.

## Consecuencias

**A favor:**

- Cero reescritura; las mejoras visuales son incrementales y de bajo riesgo.
- Se mantiene la accesibilidad/ARIA de Material "gratis".
- El theming por tenant y el dark mode (ADR-0001) siguen valiendo tal cual.

**En contra / a vigilar:**

- Dependemos de la cadencia del equipo de Angular para Material (riesgo bajo: es Google y va por v21).
- El SCSS de layout por componente se repite (candidato a Tailwind — ver abajo).

## Consideraciones futuras (no urgente)

- **Tailwind como complemento (opción B), si molesta el SCSS repetido.** Dónde aportaría: el layout y el
  spacing que hoy se repiten en cada `*.component.scss` (`.list { display:flex; gap; padding }`,
  `.edit__general { display:flex; gap }`, etc.). Tailwind los volvería utilidades en el template
  (`class="flex gap-4 p-6"`) y achicaría el SCSS a lo verdaderamente específico. Es **aditivo y
  reversible** (se integra con el preset de `@angular/cdk`); **no** se tocan Material ni los `--mat-sys-*`
  (Tailwind haría layout, Material seguiría aportando color/tipografía vía tokens). Evaluar con un spike
  chico si el SCSS de layout se vuelve molesto; **no es necesario** para las mejoras visuales planeadas.

Ver [ADR-0001](0001-runtime-theming-m3-css-variables.md).
