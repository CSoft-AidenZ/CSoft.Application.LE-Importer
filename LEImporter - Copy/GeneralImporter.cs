using LE_Importer;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEImporter
{
    public enum RateType
    {
        Invalid_DATA,
        STUMPAGE_RATES,
        RENEWAL_RATES,
        FACTOR_RATES
    }
    class GeneralImporter
    {

        public static RateType ClassifyRateType(DataTable datatable, out Exception ex)
        {
            ex = null; // Must be assigned before returning in 'out' parameters

            if (datatable == null || datatable.Columns.Count == 0)
            {
                ex = new ArgumentException("The provided DataTable is null or empty.");
                return default;
            }

            // 1. Check for Renewal Rates columns
            if (datatable.Columns.Contains("MANAGEMENT_UNIT_CODE") &&
                datatable.Columns.Contains("MANAGEMENT_UNIT_NAME"))
            {
                return RateType.RENEWAL_RATES;
            }

            // 2. Check for Stumpage Rates columns
            if (datatable.Columns.Contains("PRODUCT_TYPE_CODE") &&
                datatable.Columns.Contains("PRODUCT_TYPE_NAME"))
            {
                return RateType.STUMPAGE_RATES;
            }

            // 3. Check for Factor Rates columns
            if (datatable.Columns.Contains("FACTOR_ID") &&
                datatable.Columns.Contains("TALLY_DESTINATION_CODE"))
            {
                return RateType.FACTOR_RATES;
            }

            // 4. If no signature columns match, set exception and return default
            ex = new ArgumentException("Unrecognized CSV format: Table columns do not match any known RateType schema.");
            return default;
        }
        public static int GeneralImport(RateType rateType, DataTable datatable, IProgress<int> progress, out Exception ex)
        {
            ex = null; // Must be assigned before returning in 'out' parameters

            try
            {
                // 1. Get connection string from DatabaseHelper
                string connString = DatabaseHelper.GetConnectionString(out string errorMsg);

                if (string.IsNullOrEmpty(connString))
                {
                    ex = new InvalidOperationException($"Database connection failed: {errorMsg}");
                    return 0;
                }
                // Notify UI that database setup is complete and execution is starting (50%)
                progress?.Report(20);
                int processedCount = 0;
                switch (rateType)
                {
                    case RateType.STUMPAGE_RATES:
                        var stumpageResult = StumpageRateRepository.UpsertStumpageRates(datatable, progress, connString);
                        processedCount = stumpageResult.InsertedOrUpdatedCount;
                        break;

                    case RateType.RENEWAL_RATES:
                        var renewalResult = RenewalRatesSchemaRepository.UpsertRenewalRates(datatable, progress, connString);
                        processedCount = renewalResult.InsertedOrUpdatedCount;
                        break;

                    case RateType.FACTOR_RATES:
                        var factorResult = FactorRatesSchemaRepository.UpsertFactorRates(datatable, progress, connString);
                        processedCount = factorResult.InsertedOrUpdatedCount;
                        break;

                    default:
                        ex = new ArgumentOutOfRangeException(nameof(rateType), rateType, "Unsupported RateType");
                        return 0;
                }

                // 2. Notify UI that database operation has completed (100%)
                progress?.Report(100);

                // 3. Log Results
                Logger.Success($"Import finished. Processed: {processedCount} rows.");

                return processedCount;
            }
            catch (Exception caughtEx)
            {
                ex = caughtEx; // Pass the exception out to the caller
                Logger.Error("Import failed during execution.", caughtEx);
                return -1;
            }
        }
    }
}
