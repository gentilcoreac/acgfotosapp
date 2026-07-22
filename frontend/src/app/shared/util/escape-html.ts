/** Escapa texto para interpolarlo en HTML armado a mano (documentos imprimibles standalone). */
export function escapeHtml(texto: string): string {
  return texto
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;');
}
