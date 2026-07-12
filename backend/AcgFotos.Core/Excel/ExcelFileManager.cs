using ClosedXML.Excel;
using ExcelDataReader;
using AcgFotos.Core.Exceptions;
using AcgFotos.Core.Localization.APIResources;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace AcgFotos.Core.Excel
{
    // Reimplementado sobre ClosedXML (MIT) — antes usaba NPOI, que arrastra el aviso
    // OSMF EULA en cada build y exige aceptarlo explícitamente. La lectura sigue
    // usando ExcelDataReader (MIT) que es liviano y específico para parsing.
    public static class ExcelFileManager
    {
        public static List<T> ReadFile<T>(string path, string[] columns, int startAtRow, bool strictedColumns = true) where T : new()
        {
            var items = new List<T>();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var stream = File.Open(path, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            // strictedColumns está documentado pero no se valida; preservamos el comportamiento previo.
            var row = 0;
            while (reader.Read())
            {
                row++;
                if (startAtRow > row) continue;

                var instance = new T();
                var type = instance.GetType();
                var hasInfo = false;

                for (var column = 0; column < reader.FieldCount && column < columns.Length; column++)
                {
                    var pi = type.GetProperty(columns[column]);
                    if (pi == null) continue;

                    var cellValue = reader.GetValue(column);
                    if (cellValue == null) continue;

                    hasInfo = true;
                    var typeFullName = pi.PropertyType.FullName?.ToLower() ?? string.Empty;
                    var cellString = cellValue.ToString();

                    if (typeFullName == "system.string")
                    {
                        if (!string.IsNullOrEmpty(cellString))
                        {
                            pi.SetValue(instance, cellString);
                        }
                    }
                    else if (typeFullName.Contains("system.datetime"))
                    {
                        try
                        {
                            pi.SetValue(instance, cellValue);
                        }
                        catch (Exception)
                        {
                            DateTime.TryParse(cellString?.Replace(" ", ""), out var fecha);
                            pi.SetValue(instance, fecha);
                        }
                    }
                    else if (typeFullName.Contains("system.decimal"))
                    {
                        if (!string.IsNullOrEmpty(cellString))
                        {
                            pi.SetValue(instance, Convert.ToDecimal(cellString));
                        }
                    }
                    else if (typeFullName.Contains("system.int32"))
                    {
                        if (!string.IsNullOrEmpty(cellString))
                        {
                            pi.SetValue(instance, Convert.ToInt32(cellString));
                        }
                    }
                    else if (typeFullName.Contains("system.int64"))
                    {
                        if (!string.IsNullOrEmpty(cellString))
                        {
                            pi.SetValue(instance, Convert.ToInt64(cellString));
                        }
                    }
                    else
                    {
                        throw new BusinessValidationException(MessagesAPI.ErrorFileProcess);
                    }
                }

                if (hasInfo)
                {
                    items.Add(instance);
                }
            }

            return items;
        }

        public static MemoryStream GenerateFile<T>(List<T> items, string[] columnsName = null) where T : new()
        {
            var table = (DataTable)JsonSerializer.Deserialize(JsonSerializer.Serialize(items), typeof(DataTable));

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Sheet1");

            var columnIndex = 1;
            foreach (DataColumn column in table.Columns)
            {
                var header = columnsName != null && columnsName.Length >= columnIndex
                    ? columnsName[columnIndex - 1]
                    : column.ColumnName;
                worksheet.Cell(1, columnIndex).Value = header;
                columnIndex++;
            }

            var rowIndex = 2;
            foreach (DataRow dsrow in table.Rows)
            {
                var cellIndex = 1;
                foreach (DataColumn col in table.Columns)
                {
                    worksheet.Cell(rowIndex, cellIndex).Value = dsrow[col.ColumnName]?.ToString() ?? string.Empty;
                    cellIndex++;
                }
                rowIndex++;
            }

            worksheet.Columns().AdjustToContents();

            var memoryStream = new MemoryStream();
            workbook.SaveAs(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }

        public static byte[] CreateExcelWithDictionary(List<IDictionary<string, object>> datos, List<string> columnsToColorize = null, string firstPageName = "Page1")
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(firstPageName);

            var headers = datos.First().Keys.Where(key => key != "ID").ToList();

            for (int i = 0; i < headers.Count; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
            }

            int row = 2;
            foreach (var dato in datos)
            {
                for (int i = 0; i < headers.Count; i++)
                {
                    var header = headers[i];
                    var value = dato[header];

                    if (value is DateTime date)
                    {
                        worksheet.Cell(row, i + 1).Value = date.ToString("yyyy/MM/dd");
                    }
                    else if (value == null)
                    {
                        worksheet.Cell(row, i + 1).Value = string.Empty;
                    }
                    else
                    {
                        worksheet.Cell(row, i + 1).Value = value.ToString();
                    }
                }
                row++;
            }

            worksheet.Columns().AdjustToContents();

            if (columnsToColorize != null)
            {
                ApplyColorToColumns(worksheet, headers, columnsToColorize);
            }

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        public static byte[] CreateExcelWithColumnsSettings(List<ColumnSettings> columnSettings, string firstPageName = "Page1")
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(firstPageName);

            for (int i = 0; i < columnSettings.Count; i++)
            {
                worksheet.Cell(1, i + 1).Value = columnSettings[i].ColumnName;
            }

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        /// <summary>
        /// Lee la primera hoja de un XLSX a filas (diccionario encabezado→valor), con lectura de celda
        /// consciente del tipo (fechas/numéricos tipados). Genérico/reutilizable: el mapeo a tipos de
        /// dominio lo hace el caller. Devuelve TODAS las columnas del encabezado.
        /// </summary>
        public static List<Dictionary<string, object>> ReadSheetAsDictionaries(Stream stream)
        {
            var rows = new List<Dictionary<string, object>>();

            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.FirstOrDefault();
            var headerRow = sheet?.FirstRowUsed();
            if (sheet == null || headerRow == null) return rows;

            var headers = new List<(int Column, string Name)>();
            foreach (var cell in headerRow.CellsUsed())
            {
                var name = cell.GetString();
                if (!string.IsNullOrEmpty(name)) headers.Add((cell.Address.ColumnNumber, name));
            }
            if (headers.Count == 0) return rows;

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? headerRow.RowNumber();
            for (var r = headerRow.RowNumber() + 1; r <= lastRow; r++)
            {
                var sheetRow = sheet.Row(r);
                var row = new Dictionary<string, object>(headers.Count);
                foreach (var (column, name) in headers)
                {
                    var value = ReadCellValue(sheetRow.Cell(column));
                    if (value != null) row[name] = value;
                }
                if (row.Count > 0) rows.Add(row);
            }

            return rows;
        }

        private static object ReadCellValue(IXLCell cell)
        {
            var value = cell.Value;
            return value.Type switch
            {
                XLDataType.Blank => null,
                XLDataType.Boolean => value.GetBoolean(),
                XLDataType.Number => value.GetNumber(),
                XLDataType.DateTime => value.GetDateTime(),
                XLDataType.TimeSpan => value.GetTimeSpan(),
                XLDataType.Text => value.GetText(),
                _ => null,
            };
        }

        public static byte[] CreateExcel(string[] headers, string firstPageName = "Page1")
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(firstPageName);

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
            }

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        public class ColumnSettings
        {
            public string ColumnName { get; set; }
            public string DataType { get; set; }
        }

        private static void ApplyColorToColumns(IXLWorksheet worksheet, List<string> headers, List<string> columnsToColorize)
        {
            for (int i = 0; i < headers.Count; i++)
            {
                if (columnsToColorize.Contains(headers[i]))
                {
                    worksheet.Column(i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                }
            }
        }
    }
}
