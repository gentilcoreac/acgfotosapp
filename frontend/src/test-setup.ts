// Setup global de tests (Vitest, jsdom). jsdom no implementa `window.matchMedia` (a diferencia de
// un browser real, que es lo que corría antes con Karma) — se polyfillea para que cualquier código
// que lo use (detección de tema oscuro del SO, media queries) sea testeable con `vi.spyOn`.
if (!window.matchMedia) {
  window.matchMedia = (query: string): MediaQueryList =>
    ({
      matches: false,
      media: query,
      onchange: null,
      addListener: () => undefined,
      removeListener: () => undefined,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
      dispatchEvent: () => false,
    }) as MediaQueryList;
}
