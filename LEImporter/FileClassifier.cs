using DevExpress.Xpo;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LEImporter
{
    public class FileClassifier
    {
        internal DataTable FileParse(Stream stream)
        {
            return FileParse(stream, null);
        }

        internal DataTable FileParse(Stream stream, string fileName)
        {
            // 1. Parameter Validation
            if (stream == null || stream.Length == 0)
            {
                throw new ArgumentException("The provided file stream is empty or null.", nameof(stream));
            }

            try
            {
                string ext = null;
                try { ext = string.IsNullOrWhiteSpace(fileName) ? null : Path.GetExtension(fileName).ToLowerInvariant(); } catch { ext = null; }

                DataTable result;
                if (ext == ".xlsx")
                {
                    ExcelParser xlsxParser = new ExcelParser();
                    result = xlsxParser.FileParse(stream);
                }
                else if (ext == ".xls")
                {
                    throw new NotSupportedException("Legacy .xls is not supported. Please re-save the file as .xlsx and retry.");
                }
                else if (ext == ".csv")
                {
                    CsvParser csvParser = new CsvParser();
                    result = csvParser.FileParse(stream);
                }
                else
                {
                    // No (usable) extension: sniff content. xlsx is a ZIP (PK..).
                    if (LooksLikeXlsx(stream))
                    {
                        ExcelParser xlsxParser = new ExcelParser();
                        result = xlsxParser.FileParse(stream);
                    }
                    else
                    {
                        CsvParser csvParser = new CsvParser();
                        result = csvParser.FileParse(stream);
                    }
                }

                // Optional: Ensure the parser returned actual schema or data
                if (result == null)
                {
                    throw new InvalidOperationException("File parser returned a null DataTable.");
                }

                return result;
            }
            catch (FormatException ex)
            {
                // Caught when data formatting/types don't match expectations
                throw new Exception($"File Format Error: Failed to parse stream. {ex.Message}", ex);
            }
            catch (IOException ex)
            {
                // Caught if there's an issue reading from the Stream (unreadable, closed, etc.)
                throw new Exception($"File Read Error: Could not read from the provided stream. {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // Catch-all for any other unexpected errors during parsing
                throw new Exception($"An unexpected error occurred while parsing the file: {ex.Message}", ex);
            }
        }

        private static bool LooksLikeXlsx(Stream stream)
        {
            try
            {
                if (stream == null || !stream.CanRead || stream.Length < 4)
                    return false;
                long pos = 0;
                bool canSeek = stream.CanSeek;
                if (canSeek) { pos = stream.Position; stream.Seek(0, SeekOrigin.Begin); }
                byte[] sig = new byte[2];
                int read = stream.Read(sig, 0, 2);
                if (canSeek) { stream.Seek(pos, SeekOrigin.Begin); }
                return read == 2 && sig[0] == (byte)'P' && sig[1] == (byte)'K';
            }
            catch { return false; }
        }
    }

    /// <summary>
    /// Reads .xlsx workbooks via EPPlus and returns the best matching sheet as a DataTable(string).
    /// Auto-detect: prefers the INV Detail by BOL signature (INVOICE_NUM+TALLY_ID+BOL),
    /// then legacy Factor/Renewal/Stumpage signatures, then 2nd sheet ("Data"), then first non-empty.
    /// </summary>
    public class ExcelParser
    {
        internal DataTable FileParse(Stream stream)
        {
            if (stream == null || !stream.CanRead)
            {
                throw new ArgumentException("The provided stream is invalid or cannot be read.");
            }

            // EPPlus needs a seekable stream: buffer it.
            using (MemoryStream ms = new MemoryStream())
            {
                if (stream.CanSeek) { stream.Seek(0, SeekOrigin.Begin); }
                stream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);

                using (ExcelPackage pkg = new ExcelPackage(ms))
                {
                    if (pkg.Workbook == null || pkg.Workbook.Worksheets == null || pkg.Workbook.Worksheets.Count == 0)
                        throw new InvalidOperationException("The Excel workbook contains no worksheets.");

                    ExcelWorksheet picked = PickWorksheet(pkg);
                    if (picked == null)
                        throw new InvalidOperationException("No importable worksheet found in the Excel workbook.");

                    return WorksheetToDataTable(picked);
                }
            }
        }

        private static ExcelWorksheet PickWorksheet(ExcelPackage pkg)
        {
            var sheets = pkg.Workbook.Worksheets.ToList();

            // 1. New INV Detail by BOL signature
            foreach (var ws in sheets)
            {
                var headers = ReadHeaderSet(ws);
                if (headers.Contains("INVOICE_NUM") && headers.Contains("TALLY_ID") && headers.Contains("BOL"))
                    return ws;
            }
            // 2. Legacy signatures (keep old CSV behaviour for xlsx)
            foreach (var ws in sheets)
            {
                var headers = ReadHeaderSet(ws);
                if ((headers.Contains("MANAGEMENT_UNIT_CODE") && headers.Contains("MANAGEMENT_UNIT_NAME"))
                    || (headers.Contains("PRODUCT_TYPE_CODE") && headers.Contains("PRODUCT_TYPE_NAME"))
                    || (headers.Contains("FACTOR_ID") && headers.Contains("TALLY_DESTINATION_CODE")))
                    return ws;
            }
            // 3. Prefer 2nd sheet ("Data" in the sample) when it has content
            if (sheets.Count >= 2 && HasContent(sheets[1]))
                return sheets[1];
            // 4. First non-empty sheet
            foreach (var ws in sheets)
            {
                if (HasContent(ws)) return ws;
            }
            return sheets[0];
        }

        private static HashSet<string> ReadHeaderSet(ExcelWorksheet ws)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (ws == null || ws.Dimension == null) return set;
            int cols = ws.Dimension.End.Column;
            for (int c = 1; c <= cols; c++)
            {
                string h = null;
                try { h = (ws.Cells[1, c].Text ?? string.Empty).Trim(); } catch { h = string.Empty; }
                if (string.IsNullOrWhiteSpace(h))
                {
                    try
                    {
                        object v = ws.Cells[1, c].Value;
                        h = v != null ? v.ToString().Trim() : string.Empty;
                    }
                    catch { h = string.Empty; }
                }
                if (!string.IsNullOrWhiteSpace(h)) set.Add(h);
            }
            return set;
        }

        private static bool HasContent(ExcelWorksheet ws)
        {
            return ws != null && ws.Dimension != null && ws.Dimension.End.Row >= 1 && ws.Dimension.End.Column >= 1;
        }

        private static DataTable WorksheetToDataTable(ExcelWorksheet ws)
        {
            DataTable dataTable = new DataTable();

            if (ws.Dimension == null)
                return dataTable;

            int startCol = ws.Dimension.Start.Column;
            int endCol = ws.Dimension.End.Column;
            int startRow = ws.Dimension.Start.Row;
            int endRow = ws.Dimension.End.Row;

            // Header row = first row of the used range
            for (int c = startCol; c <= endCol; c++)
            {
                string h = null;
                try { h = (ws.Cells[startRow, c].Text ?? string.Empty).Trim(); } catch { h = string.Empty; }
                if (string.IsNullOrWhiteSpace(h))
                {
                    try { object v = ws.Cells[startRow, c].Value; h = v != null ? v.ToString().Trim() : string.Empty; }
                    catch { h = string.Empty; }
                }
                if (string.IsNullOrWhiteSpace(h))
                    h = $"Column_{dataTable.Columns.Count + 1}";
                if (dataTable.Columns.Contains(h))
                    h += $"_{dataTable.Columns.Count}";
                dataTable.Columns.Add(h, typeof(string));
            }

            for (int r = startRow + 1; r <= endRow; r++)
            {
                bool allBlank = true;
                DataRow row = dataTable.NewRow();
                for (int c = startCol; c <= endCol; c++)
                {
                    object val = null;
                    try { val = ws.Cells[r, c].Value; } catch { val = null; }
                    if (val == null)
                    {
                        row[c - startCol] = DBNull.Value;
                        continue;
                    }
                    string s;
                    if (val is DateTime dt)
                        s = dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    else if (val is double d)
                        s = d.ToString(CultureInfo.InvariantCulture);
                    else if (val is float f)
                        s = ((double)f).ToString(CultureInfo.InvariantCulture);
                    else if (val is decimal dec)
                        s = dec.ToString(CultureInfo.InvariantCulture);
                    else
                        s = val.ToString().Trim();

                    if (!string.IsNullOrEmpty(s)) allBlank = false;
                    row[c - startCol] = string.IsNullOrEmpty(s) ? (object)DBNull.Value : s;
                }
                if (!allBlank)
                    dataTable.Rows.Add(row);
            }

            return dataTable;
        }
    }

    public class CsvParser
    {
        internal DataTable FileParse(Stream stream)
        {
            DataTable dataTable = new DataTable();

            if (stream == null || !stream.CanRead)
            {
                throw new ArgumentException("The provided stream is invalid or cannot be read.");
            }

            // Ensure stream is positioned at the start
            if (stream.CanSeek && stream.Position != 0)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            using (StreamReader reader = new StreamReader(stream))
            {
                bool isHeaderRow = true;

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    // Skip empty lines
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Regex split to handle commas inside quotes: e.g., "1,000", "Ontario"
                    string[] fields = Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

                    // Clean quotes around parsed fields
                    for (int i = 0; i < fields.Length; i++)
                    {
                        fields[i] = fields[i].Trim(' ', '"');
                    }

                    // Process Header Line
                    if (isHeaderRow)
                    {
                        foreach (string header in fields)
                        {
                            string colName = string.IsNullOrWhiteSpace(header) ? $"Column_{dataTable.Columns.Count + 1}" : header;

                            // Prevent duplicate column names in DataTable
                            if (dataTable.Columns.Contains(colName))
                            {
                                colName += $"_{dataTable.Columns.Count}";
                            }

                            dataTable.Columns.Add(colName, typeof(string));
                        }
                        isHeaderRow = false;
                    }
                    else // Process Data Lines
                    {
                        DataRow row = dataTable.NewRow();
                        for (int i = 0; i < dataTable.Columns.Count; i++)
                        {
                            if (i < fields.Length)
                            {
                                row[i] = fields[i];
                            }
                            else
                            {
                                row[i] = DBNull.Value;
                            }
                        }
                        dataTable.Rows.Add(row);
                    }
                }
            }

            return dataTable;
        }
    }
}
