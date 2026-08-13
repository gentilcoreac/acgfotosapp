## 1. Fix del sello

- [x] 1.1 En `tbi-foto-familia-img`, calcular el aspect-ratio del host a partir de
      `FotoFamilia.ancho`/`alto` (ya expuestos por el modelo) y aplicarlo al contenedor donde se pinta
      `.marca-familia`, en vez de dejarlo ocupar el 100% del ancho/alto disponible del padre. Nuevos
      inputs opcionales `ancho`/`alto`; con `fit="contain"` la nueva `.foto--contain` toma
      `aspect-ratio: ancho/alto` y se centra (`:host` pasa a `display:flex; align-items/justify-content:
      center`); con `fit="cover"` (default) `.foto` sigue llenando el 100%×100% como antes, sin cambio
      visual. Único caller con `fit="contain"` (`foto-familia-preview-dialog.component.ts`) actualizado
      para pasar `[ancho]`/`[alto]` desde `FotoFamilia`.
- [x] 1.2 Verificado por los tests nuevos de 1.3 (cover sigue llenando el tile sin `aspect-ratio`
      propio; contain toma la proporción real) — no hay diferencia visual en `fit="cover"` porque
      `.foto` (sin la clase `--contain`) sigue en 100%×100% igual que antes.
- [x] 1.3 Spec nuevo en `foto-familia-img.component.spec.ts`: con `fit="contain"` + `ancho`/`alto`, la
      caja `.foto` lleva la clase `foto--contain` y el `aspect-ratio` inline correcto; con `fit="cover"`
      no lleva esa clase ni aspect-ratio, aunque se le pasen `ancho`/`alto`. 7/7 tests del componente en
      verde.
- [x] 1.4 Verificación manual: abrir el visor con una foto en el flujo real de familia (canje →
      mi-álbum) y confirmar visualmente que el sello ya no invade el diálogo entero. **Confirmado por
      Alberto (2026-08-12): "ya lo probé y anda bien".**

## 2. Cierre

- [x] 2.1 Suite unit del front + lint verdes. 555/555 tests + lint sin errores (corridos junto con el
      resto de los cambios de esta sesión).
- [x] 2.2 Sincronizar la MODIFIED requirement de `visor-fotos` al spec principal al archivar.
