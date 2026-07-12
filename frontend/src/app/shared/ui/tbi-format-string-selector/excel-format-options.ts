/**
 * Catálogo de formatos estilo Excel agrupados por categoría, para el autocomplete de
 * `tbi-format-string-selector`. Port del legacy `excel-format-string-selector.component.ts`
 * (`groupedExcelFormats`) — set representativo por categoría, no la lista exhaustiva original (no
 * se pudo recuperar completa del legacy; la estructura/categorías sí son fieles).
 */
export interface ExcelFormatGroup {
  readonly category: string;
  readonly formats: readonly string[];
}

export const EXCEL_FORMAT_GROUPS: readonly ExcelFormatGroup[] = [
  {
    category: 'Números',
    formats: ['0', '0.00', '#,##0', '#,##0.00', '0%', '0.00%', '0.00E+00'],
  },
  {
    category: 'Moneda',
    formats: ['$#,##0.00', '[$$-en-US]#,##0.00', '[$€-es-ES]#,##0.00', '#,##0.00 "€"'],
  },
  {
    category: 'Fecha',
    formats: ['dd/mm/yyyy', 'mm/dd/yyyy', 'yyyy-mm-dd', 'dd-mmm-yyyy', 'dddd, dd de mmmm de yyyy'],
  },
  {
    category: 'Hora',
    formats: ['hh:mm', 'hh:mm:ss', 'hh:mm AM/PM'],
  },
  {
    category: 'Texto',
    formats: ['@'],
  },
  {
    category: 'Especial',
    formats: ['00000', '(000) 000-0000'],
  },
  {
    category: 'Condicionales',
    formats: ['[>=1000000]#,##0,," M";[>=1000]#,##0,," K";#,##0'],
  },
];

/**
 * Clasifica un formato tipeado por el usuario en una categoría (sólo para agrupar visualmente el
 * autocomplete — port fiel de `detectCategory` del legacy, mismo criterio de regex).
 */
export function detectExcelFormatCategory(format: string): string {
  if (!format) {
    return 'Personalizado';
  }
  if (/%/.test(format)) return 'Números';
  if (/E\+0+/.test(format)) return 'Números';
  if (/@/.test(format) || /^".+"$/.test(format)) return 'Texto';
  if (/d.*m.*y|y.*m.*d/i.test(format)) return 'Fecha';
  if (/h.*:.*m|AM\/PM/i.test(format)) return 'Hora';
  if (/\[\$.*]/.test(format) || /\$/.test(format) || /€/.test(format)) return 'Moneda';
  if (/00000|\(\d{3}\)/.test(format)) return 'Especial';
  if (/>=\d+.*;/.test(format)) return 'Condicionales';
  return 'Personalizado';
}
