using LEImporter;
using System;
using System.Data;

namespace LE_Importer
{
    // Thin wrapper: delegates to GenericUpsertRepository (INFORMATION_SCHEMA-driven MERGE).
    public class RenewalRatesSchemaRepository
    {
        public static (int Inserted, int Updated, int Unchanged, int ErrorCount) UpsertRenewalRates(DataTable dt, IProgress<int> progress, string connectionString)
        {
            return GenericUpsertRepository.Upsert(RateType.RENEWAL_RATES, dt, progress, connectionString);
        }
    }
}
