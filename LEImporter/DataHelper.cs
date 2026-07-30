using System;
using System.Globalization;

namespace LE_Importer
{
    public static class DateHelper
    {
        /// <summary>
        /// Parses CSV dates such as "1042026" (7-digit) or "31032027" (8-digit) formatted as ddMMyyyy.
        /// </summary>
        public static DateTime? ParseCsvDate(object rawValue)
        {
            if (rawValue == null || rawValue == DBNull.Value || string.IsNullOrWhiteSpace(rawValue.ToString()))
                return null;

            string strValue = rawValue.ToString().Trim();

            // Pad with leading zero if 7 digits (e.g. 1042026 -> 01042026)
            if (strValue.Length == 7)
            {
                strValue = "0" + strValue;
            }

            // Expected formats from MNR CSVs
            string[] formats = { "ddMMyyyy",  };//, "yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy" };

            if (DateTime.TryParseExact(strValue, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                return parsedDate;
            }

            // Fallback general parse
            if (DateTime.TryParse(rawValue.ToString(), out parsedDate))
            {
                return parsedDate;
            }

            return null; // Return null if invalid
        }
    }
}