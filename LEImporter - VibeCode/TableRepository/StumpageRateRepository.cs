using System;
using System.Data;

namespace LEImporter
{
    // Thin wrapper: delegates to GenericUpsertRepository (INFORMATION_SCHEMA-driven MERGE).
    class StumpageRateRepository
    {
        public static (int Inserted, int Updated, int Unchanged, int ErrorCount) UpsertStumpageRates(DataTable dt, IProgress<int> progress, string connectionString)
        {
            return GenericUpsertRepository.Upsert(RateType.STUMPAGE_RATES, dt, progress, connectionString);
        }
    }
}
