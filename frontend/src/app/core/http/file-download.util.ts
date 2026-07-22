import { HttpResponse } from '@angular/common/http';

/**
 * Nombre de archivo de un header `Content-Disposition` (o null si no se puede resolver). Soporta la
 * variante extendida `filename*=UTF-8''...` (RFC 5987, tiene prioridad) y la simple `filename="..."`.
 * La API expone el header vía CORS (`WithExposedHeaders`); si un proxy lo filtrara, el caller usa su
 * nombre de fallback.
 */
export function filenameFromContentDisposition(header: string | null): string | null {
  if (!header) return null;

  const extended = /filename\*=(?:UTF-8|utf-8)''([^;]+)/.exec(header);
  if (extended?.[1]) {
    try {
      return decodeURIComponent(extended[1].trim());
    } catch {
      // percent-encoding inválido: cae a la variante simple
    }
  }

  const simple = /filename=(?:"([^"]+)"|([^;]+))/.exec(header);
  const name = (simple?.[1] ?? simple?.[2])?.trim();
  return name || null;
}

/** Dispara la descarga en el browser de un `Blob` ya en memoria: object URL + anchor efímero + revoke. */
export function downloadBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = filename;
  anchor.click();
  URL.revokeObjectURL(url);
}

/**
 * Dispara la descarga en el browser de una respuesta binaria (`ApiClient.getBlob`/`postBlob`). El
 * nombre sale del `Content-Disposition` de la respuesta, con `fallbackName` si el header no viene
 * o no se puede parsear.
 */
export function saveBlobResponse(response: HttpResponse<Blob>, fallbackName: string): void {
  const blob = response.body;
  if (!blob) return;

  const name =
    filenameFromContentDisposition(response.headers.get('Content-Disposition')) ?? fallbackName;
  downloadBlob(blob, name);
}
