using LE_Importer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;

namespace LEImporter
{
    /// <summary>
    /// INFORMATION_SCHEMA-driven generic upsert repository.
    /// Builds a MERGE statement at runtime from the target table's actual
    /// columns (INFORMATION_SCHEMA.COLUMNS) instead of hard-coded SQL.
    /// <para/>
    /// Onboarding a new xlsx/csv type = 1 line in <see cref="Profiles"/>
    /// (plus the <see cref="RateType"/> enum value). No new repository file.
    /// </summary>
    public static class GenericUpsertRepository
    {
        private sealed class TableProfile
        {
            public readonly string TableName;
            public readonly string[] KeyColumns;
            public TableProfile(string tableName, string[] keyColumns)
            {
                TableName = tableName;
                KeyColumns = keyColumns;
            }
        }

        private sealed class ColumnMeta
        {
            public string Name;
            public string DataType;
            public bool IsNullable;
            public int MaxLength; // CHARACTER_MAXIMUM_LENGTH, -1 = MAX
            public int NumericPrecision;
            public int NumericScale;
        }

        // ------------------------------------------------------------------
        // Registry: one line per file type. Keys are validated at runtime
        // against INFORMATION_SCHEMA (warns via exception on mismatch).
        // ------------------------------------------------------------------
        private static readonly Dictionary<RateType, TableProfile> Profiles =
            new Dictionary<RateType, TableProfile>
            {
                { RateType.FACTOR_RATES, new TableProfile("MNR_FACTOR_RATES", new[] { "FACTOR_ID", "TALLY_DESTINATION_CODE", "DESTINATION_CODE", "SPECIES_CODE" }) },
                { RateType.RENEWAL_RATES, new TableProfile("MNR_RENEWAL_RATES", new[] { "RATE_ID", "MANAGEMENT_UNIT_CODE" }) },
                { RateType.STUMPAGE_RATES, new TableProfile("MNR_STUMPAGE_RATES", new[] { "RATE_ID", "PRODUCT_TYPE_CODE" }) },
                { RateType.INV_DETAIL_BY_BOL, new TableProfile("TBL_INV_DETAIL_BY_BOL", new[] { "INVOICE_NUM", "TALLY_ID", "TRANSMISSION_ID", "USER_ID", "MANAGEMENT_UNIT_CODE", "CUSTOMER_ID", "ADDRESS_ID", "LICENCE_NUM", "APPROVAL_NUM", "PROCESSING_SITE_CODE", "SCALING_METHOD_CODE", "SPECIES_CODE", "TALLY_DESTINATION", "BOL", "MASS_SLIP_NUMBER", "MEASURED_UNITS" }) },
            };

        // Schema cache per table (case-insensitive key).
        private static readonly ConcurrentDictionary<string, List<ColumnMeta>> SchemaCache =
            new ConcurrentDictionary<string, List<ColumnMeta>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Generic MERGE upsert. Returns (inserted, updated, unchanged, errorCount).
        /// Change detection is value-based (EXCEPT): rows whose non-key values are
        /// semantically identical (NULL-safe, type-insensitive) report no MERGE
        /// action and are counted as unchanged, not rewritten.
        /// </summary>
        public static (int Inserted, int Updated, int Unchanged, int ErrorCount) Upsert(
            RateType rateType, DataTable dt, IProgress<int> progress, string connectionString)
        {
            int errorCount = 0;
            int inserted = 0;
            int updated = 0;
            int unchanged = 0;
            int processedCount = 0;

            if (dt == null || dt.Rows.Count == 0)
                return (0, 0, 0, 0);

            TableProfile profile;
            if (!Profiles.TryGetValue(rateType, out profile))
                throw new ArgumentOutOfRangeException(nameof(rateType), rateType, "No table profile registered for this RateType.");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string is null or empty.", nameof(connectionString));

            List<ColumnMeta> schema = GetTableSchema(profile.TableName, connectionString);
            if (schema.Count == 0)
                throw new InvalidOperationException($"Table '{profile.TableName}' not found or has no columns (INFORMATION_SCHEMA.COLUMNS returned nothing).");

            // Case-insensitive lookup of incoming file columns.
            var fileCols = new Dictionary<string, DataColumn>(StringComparer.OrdinalIgnoreCase);
            foreach (DataColumn c in dt.Columns)
            {
                if (!fileCols.ContainsKey(c.ColumnName))
                    fileCols[c.ColumnName] = c;
            }

            // Merge columns = DB columns (ordinal order) present in the file.
            List<ColumnMeta> mergeCols = schema.Where(c => fileCols.ContainsKey(c.Name)).ToList();

            // Validate configured keys exist in BOTH db schema and file.
            var schemaNames = new HashSet<string>(schema.Select(c => c.Name), StringComparer.OrdinalIgnoreCase);
            foreach (string key in profile.KeyColumns)
            {
                if (!schemaNames.Contains(key))
                    throw new InvalidOperationException($"Key column '{key}' not found in table '{profile.TableName}' (INFORMATION_SCHEMA check).");
                if (!fileCols.ContainsKey(key))
                    throw new InvalidOperationException($"Key column '{key}' missing from input file for '{profile.TableName}'.");
            }

            List<ColumnMeta> keyCols = profile.KeyColumns
                .Select(k => schema.First(c => string.Equals(c.Name, k, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            var keySet = new HashSet<string>(profile.KeyColumns, StringComparer.OrdinalIgnoreCase);
            List<ColumnMeta> nonKeyCols = mergeCols.Where(c => !keySet.Contains(c.Name)).ToList();

            if (mergeCols.Count == 0)
                throw new InvalidOperationException($"No overlapping columns between input file and table '{profile.TableName}'.");

            string mergeQuery = BuildMergeQuery(profile.TableName, mergeCols, keyCols, nonKeyCols);

            int rowCount = dt.Rows.Count;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            using (SqlCommand cmd = new SqlCommand(mergeQuery, conn, transaction))
                            {
                                foreach (ColumnMeta col in mergeCols)
                                {
                                    DataColumn dc = fileCols[col.Name];
                                    object raw = row[dc];
                                    object value = CoerceValue(col, raw);
                                    SqlParameter p = CreateParameter(col, value);
                                    cmd.Parameters.Add(p);
                                }

                                object actionResult = cmd.ExecuteScalar();

                                processedCount++;
                                int dynamicProgress = processedCount * 100 / (rowCount + 1) * 80 / 100;
                                progress?.Report(20 + dynamicProgress);

                                string action = actionResult == null || actionResult == DBNull.Value
                                    ? null
                                    : actionResult.ToString().Trim().ToUpperInvariant();
                                if (action == "INSERT")
                                    inserted++;
                                else if (action == "UPDATE")
                                    updated++;
                                else
                                    unchanged++;
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception($"Transaction failed during {profile.TableName} import: {ex.Message}", ex);
                    }
                }
            }

            return (inserted, updated, unchanged, errorCount);
        }

        /// <summary>Clears the cached INFORMATION_SCHEMA (e.g. after a DDL change).</summary>
        public static void ClearSchemaCache()
        {
            SchemaCache.Clear();
        }

        // ------------------------------------------------------------------
        // INFORMATION_SCHEMA access
        // ------------------------------------------------------------------
        private static List<ColumnMeta> GetTableSchema(string tableName, string connectionString)
        {
            List<ColumnMeta> cached;
            if (SchemaCache.TryGetValue(tableName, out cached))
                return cached;

            const string sql = @"
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE,
       CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @t
ORDER BY ORDINAL_POSITION;";

            var cols = new List<ColumnMeta>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add(new SqlParameter("@t", SqlDbType.NVarChar, 128) { Value = tableName });
                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var meta = new ColumnMeta
                        {
                            Name = r.GetString(0),
                            DataType = (r.IsDBNull(1) ? "nvarchar" : r.GetString(1)).ToLowerInvariant(),
                            IsNullable = !r.IsDBNull(2) && string.Equals(r.GetString(2), "YES", StringComparison.OrdinalIgnoreCase),
                            MaxLength = r.IsDBNull(3) ? 0 : Convert.ToInt32(r.GetValue(3)),
                            NumericPrecision = r.IsDBNull(4) ? 0 : Convert.ToInt32(r.GetValue(4)),
                            NumericScale = r.IsDBNull(5) ? 0 : Convert.ToInt32(r.GetValue(5)),
                        };
                        cols.Add(meta);
                    }
                }
            }

            SchemaCache[tableName] = cols;
            return cols;
        }

        // ------------------------------------------------------------------
        // MERGE builder (value-based change detection via EXCEPT)
        // ------------------------------------------------------------------
        // EXCEPT compares by value, not representation: NULL = NULL, CHAR
        // padding is ignored, and 72.5 = 72.5000 across decimal scales/types.
        // CHECKSUM was tried first but is representation-sensitive (false
        // "changed" on identical rows), so it was replaced on correctness grounds.
        private static string BuildMergeQuery(
            string tableName, List<ColumnMeta> mergeCols, List<ColumnMeta> keyCols, List<ColumnMeta> nonKeyCols)
        {
            string t = Quote(tableName);
            string colList = string.Join(", ", mergeCols.Select(c => Quote(c.Name)));
            string paramList = string.Join(", ", mergeCols.Select(c => "@" + c.Name));
            // NULL-safe key matching: a NULLABLE key column (e.g. BOL) must match
            // NULL to NULL, since NULL = NULL is never true in SQL. Nullability
            // comes from INFORMATION_SCHEMA so this stays correct for new tables.
            string onClause = string.Join(" AND ", keyCols.Select(k =>
                k.IsNullable
                    ? "(Target." + Quote(k.Name) + " = Source." + Quote(k.Name)
                        + " OR (Target." + Quote(k.Name) + " IS NULL AND Source." + Quote(k.Name) + " IS NULL))"
                    : "Target." + Quote(k.Name) + " = Source." + Quote(k.Name)));

            string updateSet;
            string matchedGuard = string.Empty;
            if (nonKeyCols.Count > 0)
            {
                updateSet = string.Join(", ", nonKeyCols.Select(c =>
                    "Target." + Quote(c.Name) + " = Source." + Quote(c.Name)));
                string sourceSelect = string.Join(", ", nonKeyCols.Select(c => "Source." + Quote(c.Name)));
                string targetSelect = string.Join(", ", nonKeyCols.Select(c => "Target." + Quote(c.Name)));
                matchedGuard = $" AND EXISTS (SELECT {sourceSelect} EXCEPT SELECT {targetSelect})";
            }
            else
            {
                // Degenerate case: key-only overlap. No-op update to keep MERGE valid.
                ColumnMeta k = keyCols[0];
                updateSet = "Target." + Quote(k.Name) + " = Source." + Quote(k.Name);
            }

            string sourceVals = string.Join(", ", mergeCols.Select(c => "Source." + Quote(c.Name)));

            return $@"
MERGE INTO dbo.{t} AS Target
USING (VALUES ({paramList})) AS Source ({colList})
ON {onClause}
WHEN MATCHED{matchedGuard} THEN
    UPDATE SET {updateSet}
WHEN NOT MATCHED THEN
    INSERT ({colList})
    VALUES ({sourceVals})
OUTPUT $action;";
        }

        private static string Quote(string identifier)
        {
            return "[" + identifier.Replace("]", "]]") + "]";
        }

        // ------------------------------------------------------------------
        // Type mapping: INFORMATION_SCHEMA.DATA_TYPE -> SqlDbType + coercion
        // ------------------------------------------------------------------
        private static SqlParameter CreateParameter(ColumnMeta col, object value)
        {
            SqlDbType dbType = MapSqlDbType(col.DataType);
            var p = new SqlParameter("@" + col.Name, dbType) { Value = value ?? DBNull.Value };

            // Size for string types (skip -1 = MAX).
            switch (col.DataType)
            {
                case "char":
                case "varchar":
                case "nchar":
                case "nvarchar":
                    if (col.MaxLength > 0)
                        p.Size = col.MaxLength;
                    break;
            }

            // Precision/Scale for decimal/numeric.
            if ((col.DataType == "decimal" || col.DataType == "numeric") && value is decimal)
            {
                if (col.NumericPrecision > 0)
                    p.Precision = (byte)Math.Min(col.NumericPrecision, 38);
                if (col.NumericScale >= 0)
                    p.Scale = (byte)Math.Min(col.NumericScale, 38);
            }

            return p;
        }

        private static SqlDbType MapSqlDbType(string dataType)
        {
            switch (dataType)
            {
                case "bigint": return SqlDbType.BigInt;
                case "int": return SqlDbType.Int;
                case "smallint": return SqlDbType.SmallInt;
                case "tinyint": return SqlDbType.TinyInt;
                case "bit": return SqlDbType.Bit;
                case "decimal":
                case "numeric": return SqlDbType.Decimal;
                case "money": return SqlDbType.Money;
                case "smallmoney": return SqlDbType.SmallMoney;
                case "float": return SqlDbType.Float;
                case "real": return SqlDbType.Real;
                case "date": return SqlDbType.Date;
                case "datetime": return SqlDbType.DateTime;
                case "datetime2": return SqlDbType.DateTime2;
                case "smalldatetime": return SqlDbType.SmallDateTime;
                case "datetimeoffset": return SqlDbType.DateTimeOffset;
                case "time": return SqlDbType.Time;
                case "char": return SqlDbType.Char;
                case "varchar": return SqlDbType.VarChar;
                case "nchar": return SqlDbType.NChar;
                case "nvarchar":
                case "ntext": return SqlDbType.NVarChar;
                case "text": return SqlDbType.Text;
                case "uniqueidentifier": return SqlDbType.UniqueIdentifier;
                default: return SqlDbType.NVarChar;
            }
        }

        private static bool IsNullLike(object raw)
        {
            if (raw == null || raw == DBNull.Value)
                return true;
            if (raw is string s)
            {
                s = s.Trim();
                return s.Length == 0 || s == "NA" || s == "-";
            }
            return false;
        }

        private static object CoerceValue(ColumnMeta col, object raw)
        {
            bool nullLike = IsNullLike(raw);
            string s = nullLike ? null : raw.ToString().Trim();
            if (raw is DateTime)
                s = null; // handled by date branch via ParseCsvDate

            switch (col.DataType)
            {
                // ---- integers ----
                case "bigint":
                case "int":
                case "smallint":
                case "tinyint":
                case "bit":
                    {
                        if (nullLike)
                            return col.IsNullable ? (object)DBNull.Value : 0;
                        long parsed;
                        if (long.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed) ||
                            long.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out parsed))
                        {
                            if (col.DataType == "bit")
                                return parsed == 0 ? (object)0 : 1;
                            if (col.DataType == "bigint")
                                return parsed;
                            if (col.DataType == "smallint")
                                return (short)Math.Max(short.MinValue, Math.Min(short.MaxValue, parsed));
                            if (col.DataType == "tinyint")
                                return (byte)Math.Max(byte.MinValue, Math.Min(byte.MaxValue, parsed));
                            return (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, parsed));
                        }
                        bool b;
                        if (col.DataType == "bit" && bool.TryParse(s, out b))
                            return b ? 1 : 0;
                        return col.IsNullable ? (object)DBNull.Value : 0;
                    }

                // ---- decimals / doubles ----
                case "decimal":
                case "numeric":
                case "money":
                case "smallmoney":
                case "float":
                case "real":
                    {
                        if (nullLike)
                            return col.IsNullable ? (object)DBNull.Value : DefaultNumber(col.DataType);
                        decimal dec;
                        if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out dec) ||
                            decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out dec))
                        {
                            if (col.DataType == "float")
                                return (double)dec;
                            if (col.DataType == "real")
                                return (float)dec;
                            return dec;
                        }
                        return col.IsNullable ? (object)DBNull.Value : DefaultNumber(col.DataType);
                    }

                // ---- dates ----
                case "date":
                case "datetime":
                case "datetime2":
                case "smalldatetime":
                case "datetimeoffset":
                    {
                        DateTime? parsed = DateHelper.ParseCsvDate(raw);
                        if (parsed.HasValue)
                            return parsed.Value;
                        return (object)DBNull.Value;
                    }

                case "time":
                    {
                        if (nullLike)
                            return (object)DBNull.Value;
                        TimeSpan ts;
                        if (TimeSpan.TryParse(s, out ts))
                            return ts;
                        DateTime dtv;
                        if (DateTime.TryParse(s, out dtv))
                            return dtv.TimeOfDay;
                        return (object)DBNull.Value;
                    }

                case "uniqueidentifier":
                    {
                        if (nullLike)
                            return (object)DBNull.Value;
                        Guid g;
                        if (Guid.TryParse(s, out g))
                            return g;
                        return (object)DBNull.Value;
                    }

                // ---- strings (default) ----
                default:
                    {
                        if (nullLike)
                            return col.IsNullable ? (object)DBNull.Value : string.Empty;
                        string val = raw.ToString().Trim();
                        if (col.MaxLength > 0 && val.Length > col.MaxLength)
                            val = val.Substring(0, col.MaxLength);
                        return val;
                    }
            }
        }

        private static object DefaultNumber(string dataType)
        {
            if (dataType == "float")
                return (double)0;
            if (dataType == "real")
                return (float)0;
            return (decimal)0;
        }
    }
}
