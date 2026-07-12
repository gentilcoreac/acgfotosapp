# Notas abiertas y pendientes

Preguntas sin responder y recordatorios para fases siguientes. Al resolver una, mover la respuesta al documento que corresponda (visión, arquitectura o decisiones) y tacharla acá.

## Preguntas para el fotógrafo (papá) — responder antes de Fase 1

- [ ] **Tamaños de impresión reales** que vende y precios actuales (¿10x15, 13x18, 15x21, 20x30? ¿otros?). Define el seed del catálogo.
- [ ] **¿Vende paquetes/promos?** (ej. "2 grandes + 4 chicas a $X"). Si es central en su venta, adelantar Paquetes de Fase 4 a Fase 3.
- [ ] **Volumen típico**: ¿cuántas fotos por alumno y por curso saca? ¿cuántos alumnos por evento? (dimensiona upload, storage y UI de galería).
- [ ] **¿Cómo imprime?** ¿Laboratorio externo o impresora propia? Define el formato exacto de la "lista de impresión" (¿alcanza un CSV/PDF? ¿el laboratorio pide los archivos con nomenclatura especial?).
- [ ] **¿Retoca las fotos antes de mostrar?** ¿Se suben ya editadas o hace falta re-subir versiones? (¿versión de foto = reemplazo simple?)
- [ ] **Flujo de entrega deseado**: ¿casa por casa igual, punto de encuentro en el colegio, o envío? Afecta qué datos pedir en el pedido (¿dirección?).
- [ ] **¿Cobra seña?** Algunos fotógrafos escolares piden un porcentaje al pedir. Afectaría el flujo de pagos de Fase 3.
- [ ] Validar con él la **tarjetita QR por alumno**: ¿la repartiría el colegio, él en el acto, o por el grupo de WhatsApp de padres?

## Técnicas — resolver en la fase indicada

- [ ] **(Fase 0)** Diseño del watermark: texto/logo, opacidad, patrón repetido en diagonal. Conseguir el logo o nombre comercial del papá.
- [ ] **(Fase 0)** ¿Formato de salida de previews JPEG o WebP? (WebP pesa menos; verificar que ImageSharp lo genere bien con watermark).
- [ ] **(Fase 0)** Node global de la máquina es 22.14 y el shell Angular exige ≥22.22.3: por ahora se usa el Node portátil de `.tools/` — actualizar el Node global cuando sea cómodo y borrar `.tools/`.
- [ ] **(Fase 1)** Revisar qué features de plataforma esconder del menú para el uso real (un solo admin no necesita ver grupos/licencias/tenants): son menús dinámicos por permisos (`gen_Menus`), no hace falta tocar código.
- [ ] **(Fase 1)** Upload masivo desde el navegador: subida directa al bucket con URL firmada de escritura (mejor para archivos grandes) vs. pasar por la API (más simple, permite procesar en línea). Empezar por la API y medir.
- [ ] **(Fase 1)** Auto-asignación de fotos a álbumes: el fotógrafo saca por orden de lista o por carpetas por alumno. Si nombra carpetas por alumno, el upload puede mapear carpeta → álbum automáticamente. Preguntarle cómo organiza hoy.
- [ ] **(Fase 2)** Rate-limiting y lockout en el canje de códigos (fuerza bruta). Ver `AspNetCore.RateLimiting` nativo.
- [ ] **(Fase 2)** EXIF: limpiar metadatos (GPS, equipo) de los derivados antes de servirlos. Son fotos de menores: minimizar datos.
- [ ] **(Fase 3)** Webhook de Mercado Pago necesita URL pública: en dev usar túnel (ngrok/cloudflared) o simulador.
- [ ] **(Fase 4)** Distribución de la app Capacitor: Play Store (USD 25 por única vez) vs APK por link directo. iOS requiere cuenta de developer (USD 99/año) — evaluar si se justifica o si iOS queda solo con la web. Mantener el Angular libre de dependencias solo-navegador para que el empaquetado no duela.
- [ ] **(Deploy)** **Migrar de SQL Server a PostgreSQL** (decisión acordada, ver ADR-09): `DatabaseFactory` con proveedor configurable + Npgsql, regenerar migraciones, portar vistas SQL, Respawn Postgres, sink Serilog. Hasta entonces, evitar SQL con sintaxis exclusiva de SQL Server en el vertical Fotos.
- [ ] **(Deploy)** Elegir hosting y dominio. Backups automáticos de PostgreSQL. Política de retención de fotos tras `FechaExpiracion` (¿borrar originales o archivarlos? — preguntarle: quizá quiera conservarlos para reventa).

## Consideraciones legales / sensibles (no ignorar)

- [ ] Son **fotos de menores**: consentimiento (normalmente lo gestiona el colegio con las familias — confirmar cómo lo maneja hoy). No indexar nada públicamente (`robots.txt` + `noindex` + páginas solo accesibles con código).
- [ ] Datos personales mínimos: nombre y teléfono solo en el pedido; definir cuándo se borran.
- [ ] Si en el futuro hay multi-tenant (otros fotógrafos), revisar términos de responsabilidad sobre el contenido.

## Ideas descartadas (para no re-discutirlas)

- **DRM de video/imagen (EME/Widevine)** para ennegrecer capturas: complejidad absurda para fotos, comportamiento inconsistente por dispositivo, rompe UX, y la foto a la pantalla lo saltea igual. El bloqueo real de capturas va por la app Capacitor con `FLAG_SECURE` (ADR-01, capa 3).
- **Watermark manual por el fotógrafo** (Photoshop/Lightroom antes de subir): horas de trabajo por evento contra costo ≈ cero del sistema, y riesgo fatal de imprimir fotos con la marca. El original se sube limpio; el sistema marca solo los derivados.
- **Microservicios / serverless**: el volumen no lo justifica — ver ADR-04.
