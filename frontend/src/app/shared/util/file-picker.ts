/**
 * Abre el diálogo nativo de archivos SIN un input en el template — para acciones de fila que no
 * tienen formulario (p.ej. "Importar Excel" de un listado). Resuelve con el `File` elegido o `null`
 * si el usuario cancela. Para flujos con formulario/preview usar `tbi-file-upload`.
 */
export function pickFile(accept: string): Promise<File | null> {
  return new Promise((resolve) => {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = accept;
    input.onchange = () => resolve(input.files?.[0] ?? null);
    input.oncancel = () => resolve(null);
    input.click();
  });
}
