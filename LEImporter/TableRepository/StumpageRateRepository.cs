using LE_Importer;
using System;
using System.Data;
using System.Data.SqlClient;

namespace LEImporter
{
    class StumpageRateRepository
    {


        public static (int InsertedOrUpdatedCount, int ErrorCount) UpsertStumpageRates(DataTable dt, IProgress<int> progress, string connectionString)
        {
            int successCount = 0;
            int errorCount = 0;

            if (dt == null || dt.Rows.Count == 0)
                return (0, 0);

            // MERGE Query: Checks for existing record using Composite Unique Key 
            // (PRODUCT_TYPE_CODE + SPECIES_GROUP_CODE + RATE_TYPE_CODE + EFFECTIVE_DATE)
            string mergeQuery = @"
                MERGE INTO MNR_STUMPAGE_RATES AS Target
                USING (
                    VALUES (
                        @TYPE, @RATE_ID, @PRODUCT_TYPE_CODE, @PRODUCT_TYPE_NAME,
                        @SPECIES_GROUP_CODE, @SPECIES_GROUP_NAME, @RATE_TYPE_CODE,
                        @RATE_TYPE_NAME, @RATE, @EFFECTIVE_DATE, @EXPIRY_DATE
                    )
                ) AS Source (
                    TYPE, RATE_ID, PRODUCT_TYPE_CODE, PRODUCT_TYPE_NAME,
                    SPECIES_GROUP_CODE, SPECIES_GROUP_NAME, RATE_TYPE_CODE,
                    RATE_TYPE_NAME, RATE, EFFECTIVE_DATE, EXPIRY_DATE
                )
                ON Target.RATE_ID = Source.RATE_ID
               AND Target.PRODUCT_TYPE_CODE = Source.PRODUCT_TYPE_CODE

                -- If row exists, UPDATE
                WHEN MATCHED THEN
                    UPDATE SET 
                        Target.TYPE = Source.TYPE,
                        Target.PRODUCT_TYPE_NAME = Source.PRODUCT_TYPE_NAME,
                        Target.SPECIES_GROUP_NAME = Source.SPECIES_GROUP_NAME,
                        Target.SPECIES_GROUP_CODE = Source.SPECIES_GROUP_CODE,
                        Target.RATE_TYPE_NAME = Source.RATE_TYPE_NAME,
                        Target.RATE_TYPE_CODE = Source.RATE_TYPE_CODE,
                        Target.RATE = Source.RATE,
                        Target.EFFECTIVE_DATE = Source.EFFECTIVE_DATE,
                        Target.EXPIRY_DATE = Source.EXPIRY_DATE

                -- If row is new, INSERT
                WHEN NOT MATCHED THEN
                    INSERT (
                        TYPE, RATE_ID, PRODUCT_TYPE_CODE, PRODUCT_TYPE_NAME,
                        SPECIES_GROUP_CODE, SPECIES_GROUP_NAME, RATE_TYPE_CODE,
                        RATE_TYPE_NAME, RATE, EFFECTIVE_DATE, EXPIRY_DATE
                    )
                    VALUES (
                        Source.TYPE, Source.RATE_ID, Source.PRODUCT_TYPE_CODE, Source.PRODUCT_TYPE_NAME,
                        Source.SPECIES_GROUP_CODE, Source.SPECIES_GROUP_NAME, Source.RATE_TYPE_CODE,
                        Source.RATE_TYPE_NAME, Source.RATE, Source.EFFECTIVE_DATE, Source.EXPIRY_DATE
                    );";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        int rowCount = dt.Rows.Count;

                        foreach (DataRow row in dt.Rows)
                        {
                            using (SqlCommand cmd = new SqlCommand(mergeQuery, conn, transaction))
                            {

                                // Safe Mapping & Null Handling
                                cmd.Parameters.AddWithValue("@TYPE", row["TYPE"] ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@RATE_ID", Convert.ToInt32(row["RATE_ID"]));
                                cmd.Parameters.AddWithValue("@PRODUCT_TYPE_CODE", row["PRODUCT_TYPE_CODE"] ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@PRODUCT_TYPE_NAME", row["PRODUCT_TYPE_NAME"] ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@SPECIES_GROUP_CODE", row["SPECIES_GROUP_CODE"] ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@SPECIES_GROUP_NAME", row["SPECIES_GROUP_NAME"] ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@RATE_TYPE_CODE", row["RATE_TYPE_CODE"] ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@RATE_TYPE_NAME", row["RATE_TYPE_NAME"] ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@RATE", Convert.ToDecimal(row["RATE"]));

                                // Date Parsing
                                DateTime? effDate = DateHelper.ParseCsvDate(row["EFFECTIVE_DATE"]);
                                DateTime? expDate = DateHelper.ParseCsvDate(row["EXPIRY_DATE"]);

                                cmd.Parameters.AddWithValue("@EFFECTIVE_DATE", effDate.HasValue ? (object)effDate.Value : DBNull.Value);
                                cmd.Parameters.AddWithValue("@EXPIRY_DATE", expDate.HasValue ? (object)expDate.Value : DBNull.Value);


                                if (cmd.ExecuteNonQuery() > 0)
                                {
                                    successCount++;

                                    int dynamicProgress = successCount *100 / (rowCount + 1)/80;
                                    progress?.Report(20 + dynamicProgress);
                                }


                            }
                        }

                        // Commit transaction if all rows processed successfully
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception($"Transaction failed during import: {ex.Message}", ex);
                    }
                }
            }

            return (successCount, errorCount);
        }
    }
}
