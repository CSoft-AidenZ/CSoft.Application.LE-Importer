using System;
using System.Data;

namespace LEImporter
{
    // Thin wrapper: delegates to GenericUpsertRepository (INFORMATION_SCHEMA-driven MERGE).
    class FactorRatesSchemaRepository
    {
        public static (int Inserted, int Updated, int Unchanged, int ErrorCount) UpsertFactorRates(DataTable dt, IProgress<int> progress, string connectionString)
        {
            return GenericUpsertRepository.Upsert(RateType.FACTOR_RATES, dt, progress, connectionString);
        }
    }
}
