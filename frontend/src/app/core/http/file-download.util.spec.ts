import { HttpHeaders, HttpResponse } from '@angular/common/http';
import { filenameFromContentDisposition, saveBlobResponse } from './file-download.util';

describe('filenameFromContentDisposition', () => {
  it('extrae el nombre de la variante simple con comillas', () => {
    expect(filenameFromContentDisposition('attachment; filename="DimTest.xlsx"')).toBe(
      'DimTest.xlsx',
    );
  });

  it('extrae el nombre de la variante simple sin comillas', () => {
    expect(filenameFromContentDisposition('attachment; filename=DimTest.xlsx')).toBe(
      'DimTest.xlsx',
    );
  });

  it('prioriza la variante extendida UTF-8 y decodifica el percent-encoding', () => {
    expect(
      filenameFromContentDisposition(
        `attachment; filename="fallback.xlsx"; filename*=UTF-8''Dim%20A%C3%B1o.xlsx`,
      ),
    ).toBe('Dim Año.xlsx');
  });

  it('devuelve null sin header o sin filename', () => {
    expect(filenameFromContentDisposition(null)).toBeNull();
    expect(filenameFromContentDisposition('inline')).toBeNull();
  });
});

describe('saveBlobResponse', () => {
  it('descarga con el nombre del Content-Disposition', () => {
    const clicks: string[] = [];
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:x');
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (
      this: HTMLAnchorElement,
    ) {
      clicks.push(this.download);
    });

    const response = new HttpResponse<Blob>({
      body: new Blob(['x']),
      headers: new HttpHeaders({ 'Content-Disposition': 'attachment; filename="Modelo.xlsx"' }),
    });
    saveBlobResponse(response, 'fallback.xlsx');

    expect(clicks).toEqual(['Modelo.xlsx']);
    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:x');
  });

  it('usa el fallback si el header no viene (p.ej. filtrado por un proxy)', () => {
    const clicks: string[] = [];
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:x');
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (
      this: HTMLAnchorElement,
    ) {
      clicks.push(this.download);
    });

    saveBlobResponse(new HttpResponse<Blob>({ body: new Blob(['x']) }), 'fallback.xlsx');

    expect(clicks).toEqual(['fallback.xlsx']);
  });
});
