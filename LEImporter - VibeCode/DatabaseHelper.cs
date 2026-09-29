using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;

namespace LE_Importer
{
    public static class DatabaseHelper
    {
        private const string ConfigFileName = "ASMDataLayer.ini";

        /// <summary>
        /// Reads ABC.ASMDataLayer.ini, parses INI key-value pairs, and builds a SQL Server connection string.
        /// </summary>
        public static string GetConnectionString(out string errorMessage)
        {
            errorMessage = string.Empty;

            // Resolve full file path
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);

            if (!File.Exists(filePath))
            {
                errorMessage = $"Configuration file '{ConfigFileName}' was not found in:\n{AppDomain.CurrentDomain.BaseDirectory}";
                return string.Empty;
            }

            try
            {
                // Simple INI File Parser
                var iniData = ParseIniFile(filePath);

                // Ensure the required keys exist in [Database_1]
                if (!iniData.ContainsKey("SERVER_NAME") || !iniData.ContainsKey("DATABASE_NAME"))
                {
                    errorMessage = $"Missing 'SERVER_NAME' or 'DATABASE_NAME' inside '{ConfigFileName}'.";
                    return string.Empty;
                }

                string serverName = iniData["SERVER_NAME"];
                string databaseName = iniData["DATABASE_NAME"];

                if (string.IsNullOrWhiteSpace(serverName) || string.IsNullOrWhiteSpace(databaseName))
                {
                    errorMessage = $"'SERVER_NAME' or 'DATABASE_NAME' is empty in '{ConfigFileName}'.";
                    Logger.Error($"Failed to acquire database info.");
                    return string.Empty;
                }

                // Construct SQL Server Connection String (Using Windows Authentication / Integrated Security)
                // Adjust TrustServerCertificate if needed
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder
                {
                    DataSource = serverName,
                    InitialCatalog = databaseName,
                    IntegratedSecurity = false, // Must be false to use Username & Password
                    UserID = "sa",           // Your SQL username (e.g. "sa" or custom user)
                    Password = "4Score&7Yrs",         // Your SQL password
                    TrustServerCertificate = true,
                    ConnectTimeout = 5 // 15 seconds timeout
                };

                // Log Results

                Logger.Success($"Acquired server info: {iniData.ContainsKey("SERVER_NAME")}");
                Logger.Success($"Acquired database info: {iniData.ContainsKey("DATABASE_NAME")}");
                return builder.ConnectionString;

            }
            catch (Exception ex)
            {
                errorMessage = $"Error reading or parsing '{ConfigFileName}': {ex.Message}";
                return string.Empty;
            }
        }

        /// <summary>
        /// Tests database connectivity using the parameters extracted from ASMDataLayer.ini.
        /// </summary>
        public static bool TestConnection(out string errorMessage)
        {
            string connString = GetConnectionString(out errorMessage);

            // Fail early if file reading or parsing failed
            if (string.IsNullOrEmpty(connString))
            {
                return false;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open(); // Attempt connection
                    conn.Close();
                    Logger.Success($"Connection to database works.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"SQL Server Connection Failed:\n{ex.Message}";
                Logger.Error($"SQL Server Connection Failed:\n{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Helper function to parse Key=Value lines from an INI file.
        /// </summary>
        private static Dictionary<string, string> ParseIniFile(string filePath)
        {
            var keyValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string rawLine in File.ReadAllLines(filePath))
            {
                string line = rawLine.Trim();

                // Skip comments and section header lines like [Database_1]
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";") || line.StartsWith("#") || (line.StartsWith("[") && line.EndsWith("]")))
                {
                    continue;
                }

                int equalIdx = line.IndexOf('=');
                if (equalIdx > 0)
                {
                    string key = line.Substring(0, equalIdx).Trim();
                    string value = line.Substring(equalIdx + 1).Trim();

                    // Key collisions default to latest
                    keyValues[key] = value;
                }
            }

            return keyValues;
        }
    }
}