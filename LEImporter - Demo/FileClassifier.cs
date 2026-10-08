using DevExpress.Xpo;
using System;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;

namespace LEImporter
{
    public class FileClassifier 
    {


        internal DataTable FileParse(Stream stream)
        {
            // 1. Parameter Validation
            if (stream == null || stream.Length == 0)
            {
                throw new ArgumentException("The provided file stream is empty or null.", nameof(stream));
            }

            try
            {
                CsvParser parser = new CsvParser();
                DataTable result = parser.FileParse(stream);

                // Optional: Ensure the parser returned actual schema or data
                if (result == null)
                {
                    throw new InvalidOperationException("CSV parser returned a null DataTable.");
                }

                return result;
            }
            catch (FormatException ex)
            {
                // Caught when CSV data formatting/types don't match expectations
                throw new Exception($"CSV Format Error: Failed to parse stream. {ex.Message}", ex);
            }
            catch (IOException ex)
            {
                // Caught if there's an issue reading from the Stream (unreadable, closed, etc.)
                throw new Exception($"File Read Error: Could not read from the provided stream. {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // Catch-all for any other unexpected errors during parsing
                throw new Exception($"An unexpected error occurred while parsing the CSV file: {ex.Message}", ex);
            }
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