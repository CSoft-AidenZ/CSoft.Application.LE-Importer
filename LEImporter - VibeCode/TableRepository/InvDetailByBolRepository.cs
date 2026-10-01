using System;
using System.Data;

namespace LEImporter
{
    // Thin wrapper: delegates to GenericUpsertRepository (INFORMATION_SCHEMA-driven MERGE).
    class InvDetailByBolRepository
    {
        public static (int Inserted, int Updated, int Unchanged, int ErrorCount) UpsertInvDetailByBol(DataTable dt, IProgress<int> progress, string connectionString)
        {
            return GenericUpsertRepository.Upsert(RateType.INV_DETAIL_BY_BOL, dt, progress, connectionString);
        }
    }
}
