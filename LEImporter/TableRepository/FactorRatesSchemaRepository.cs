using LE_Importer;
using System;
using System.Data;
using System.Data.SqlClient;

namespace LEImporter
{
    class FactorRatesSchemaRepository
    {
        public static (int InsertedOrUpdatedCount, int ErrorCount) UpsertFactorRates(DataTable dt, IProgress<int> progress, string connectionString)
        {
            int errorCount = 0;
            int rowCount = dt.Rows.Count;

            // No matter the row is updated or not, count for progress.
            int processedCount = 0;

            // Successfully updated/inserted row count
            int successCount = 0;

            if (dt == null || dt.Rows.Count == 0)
                return (0, 0);

            // MERGE Query using FACTOR_ID + EFFECTIVE_DATE as the composite key match
            /*string mergeQuery = @"
                MERGE INTO MNR_FACTOR_RATES AS Target
                USING (
                    VALUES (
                        @TYPE, @FACTOR_ID, @APPROVAL_NUMBER, @SCALING_METHOD_CODE,
                        @TALLY_DESTINATION_CODE, @TALLY_SPECIES_CODE, @TALLY_GRADE, @CONTAINER_CODE,
                        @CHARGE_TYPE, @LICENCE_NUMBER, @ACCOUNT_CODE, @MANAGEMENT_UNIT,
                        @REDUCTION_RATE, @EFFECTIVE_DATE, @EXPIRY_DATE, @DESTINATION_CODE,
                        @PRODUCT_TYPE, @DESTINATION_TYPE, @TAX_CODE, @TAX_RATE,
                        @SPECIES_CODE, @SPECIES_GROUP, @GRADE, @DESTINATION_SHARE,
                        @SPECIES_SHARE, @GRADE_SHARE, @MASS_VOLUME_RATIO, @TREE_LENGTH_TABLE,
                        @CONTAINER_VOLUME, @UNDERSIZE_DEDUCTION, @CULL_DEDUCTION
                    )
                ) AS Source (
                    TYPE, FACTOR_ID, APPROVAL_NUMBER, SCALING_METHOD_CODE,
                    TALLY_DESTINATION_CODE, TALLY_SPECIES_CODE, TALLY_GRADE, CONTAINER_CODE,
                    CHARGE_TYPE, LICENCE_NUMBER, ACCOUNT_CODE, MANAGEMENT_UNIT,
                    REDUCTION_RATE, EFFECTIVE_DATE, EXPIRY_DATE, DESTINATION_CODE,
                    PRODUCT_TYPE, DESTINATION_TYPE, TAX_CODE, TAX_RATE,
                    SPECIES_CODE, SPECIES_GROUP, GRADE, DESTINATION_SHARE,
                    SPECIES_SHARE, GRADE_SHARE, MASS_VOLUME_RATIO, TREE_LENGTH_TABLE,
                    CONTAINER_VOLUME, UNDERSIZE_DEDUCTION, CULL_DEDUCTION
                )
                ON  Target.FACTOR_ID = Source.FACTOR_ID
                AND Target.TALLY_DESTINATION_CODE = Source.TALLY_DESTINATION_CODE
                AND Target.DESTINATION_CODE = Source.DESTINATION_CODE
                AND Target.SPECIES_CODE = Source.SPECIES_CODE

                -- If row exists, UPDATE
                WHEN MATCHED THEN
                    UPDATE SET 
                        Target.TYPE = Source.TYPE,
                        Target.APPROVAL_NUMBER = Source.APPROVAL_NUMBER,
                        Target.SCALING_METHOD_CODE = Source.SCALING_METHOD_CODE,
                        Target.TALLY_SPECIES_CODE = Source.TALLY_SPECIES_CODE,
                        Target.TALLY_GRADE = Source.TALLY_GRADE,
                        Target.CONTAINER_CODE = Source.CONTAINER_CODE,
                        Target.CHARGE_TYPE = Source.CHARGE_TYPE,
                        Target.LICENCE_NUMBER = Source.LICENCE_NUMBER,
                        Target.ACCOUNT_CODE = Source.ACCOUNT_CODE,
                        Target.MANAGEMENT_UNIT = Source.MANAGEMENT_UNIT,
                        Target.REDUCTION_RATE = Source.REDUCTION_RATE,
                        Target.EFFECTIVE_DATE = Source.EFFECTIVE_DATE,
                        Target.EXPIRY_DATE = Source.EXPIRY_DATE,
                        Target.PRODUCT_TYPE = Source.PRODUCT_TYPE,
                        Target.DESTINATION_TYPE = Source.DESTINATION_TYPE,
                        Target.TAX_CODE = Source.TAX_CODE,
                        Target.TAX_RATE = Source.TAX_RATE,
                        Target.SPECIES_GROUP = Source.SPECIES_GROUP,
                        Target.GRADE = Source.GRADE,
                        Target.DESTINATION_SHARE = Source.DESTINATION_SHARE,
                        Target.SPECIES_SHARE = Source.SPECIES_SHARE,
                        Target.GRADE_SHARE = Source.GRADE_SHARE,
                        Target.MASS_VOLUME_RATIO = Source.MASS_VOLUME_RATIO,
                        Target.TREE_LENGTH_TABLE = Source.TREE_LENGTH_TABLE,
                        Target.CONTAINER_VOLUME = Source.CONTAINER_VOLUME,
                        Target.UNDERSIZE_DEDUCTION = Source.UNDERSIZE_DEDUCTION,
                        Target.CULL_DEDUCTION = Source.CULL_DEDUCTION

                -- If row is new, INSERT
                WHEN NOT MATCHED THEN
                    INSERT (
                        TYPE, FACTOR_ID, APPROVAL_NUMBER, SCALING_METHOD_CODE,
                        TALLY_DESTINATION_CODE, TALLY_SPECIES_CODE, TALLY_GRADE, CONTAINER_CODE,
                        CHARGE_TYPE, LICENCE_NUMBER, ACCOUNT_CODE, MANAGEMENT_UNIT,
                        REDUCTION_RATE, EFFECTIVE_DATE, EXPIRY_DATE, DESTINATION_CODE,
                        PRODUCT_TYPE, DESTINATION_TYPE, TAX_CODE, TAX_RATE,
                        SPECIES_CODE, SPECIES_GROUP, GRADE, DESTINATION_SHARE,
                        SPECIES_SHARE, GRADE_SHARE, MASS_VOLUME_RATIO, TREE_LENGTH_TABLE,
                        CONTAINER_VOLUME, UNDERSIZE_DEDUCTION, CULL_DEDUCTION
                    )
                    VALUES (
                        Source.TYPE, Source.FACTOR_ID, Source.APPROVAL_NUMBER, Source.SCALING_METHOD_CODE,
                        Source.TALLY_DESTINATION_CODE, Source.TALLY_SPECIES_CODE, Source.TALLY_GRADE, Source.CONTAINER_CODE,
                        Source.CHARGE_TYPE, Source.LICENCE_NUMBER, Source.ACCOUNT_CODE, Source.MANAGEMENT_UNIT,
                        Source.REDUCTION_RATE, Source.EFFECTIVE_DATE, Source.EXPIRY_DATE, Source.DESTINATION_CODE,
                        Source.PRODUCT_TYPE, Source.DESTINATION_TYPE, Source.TAX_CODE, Source.TAX_RATE,
                        Source.SPECIES_CODE, Source.SPECIES_GROUP, Source.GRADE, Source.DESTINATION_SHARE,
                        Source.SPECIES_SHARE, Source.GRADE_SHARE, Source.MASS_VOLUME_RATIO, Source.TREE_LENGTH_TABLE,
                        Source.CONTAINER_VOLUME, Source.UNDERSIZE_DEDUCTION, Source.CULL_DEDUCTION
            );";*/

            string mergeQuery = @"
                    MERGE INTO MNR_FACTOR_RATES AS Target
                    USING (
                        VALUES (
                            @TYPE, @FACTOR_ID, @APPROVAL_NUMBER, @SCALING_METHOD_CODE,
                            @TALLY_DESTINATION_CODE, @TALLY_SPECIES_CODE, @TALLY_GRADE, @CONTAINER_CODE,
                            @CHARGE_TYPE, @LICENCE_NUMBER, @ACCOUNT_CODE, @MANAGEMENT_UNIT,
                            @REDUCTION_RATE, @EFFECTIVE_DATE, @EXPIRY_DATE, @DESTINATION_CODE,
                            @PRODUCT_TYPE, @DESTINATION_TYPE, @TAX_CODE, @TAX_RATE,
                            @SPECIES_CODE, @SPECIES_GROUP, @GRADE, @DESTINATION_SHARE,
                            @SPECIES_SHARE, @GRADE_SHARE, @MASS_VOLUME_RATIO, @TREE_LENGTH_TABLE,
                            @CONTAINER_VOLUME, @UNDERSIZE_DEDUCTION, @CULL_DEDUCTION
                        )
                    ) AS Source (
                        TYPE, FACTOR_ID, APPROVAL_NUMBER, SCALING_METHOD_CODE,
                        TALLY_DESTINATION_CODE, TALLY_SPECIES_CODE, TALLY_GRADE, CONTAINER_CODE,
                        CHARGE_TYPE, LICENCE_NUMBER, ACCOUNT_CODE, MANAGEMENT_UNIT,
                        REDUCTION_RATE, EFFECTIVE_DATE, EXPIRY_DATE, DESTINATION_CODE,
                        PRODUCT_TYPE, DESTINATION_TYPE, TAX_CODE, TAX_RATE,
                        SPECIES_CODE, SPECIES_GROUP, GRADE, DESTINATION_SHARE,
                        SPECIES_SHARE, GRADE_SHARE, MASS_VOLUME_RATIO, TREE_LENGTH_TABLE,
                        CONTAINER_VOLUME, UNDERSIZE_DEDUCTION, CULL_DEDUCTION
                    )
                    ON  Target.FACTOR_ID = Source.FACTOR_ID
                    AND Target.TALLY_DESTINATION_CODE = Source.TALLY_DESTINATION_CODE
                    AND Target.DESTINATION_CODE = Source.DESTINATION_CODE
                    AND Target.SPECIES_CODE = Source.SPECIES_CODE

                    -- When match and at least one column contains difference, do UPDATE
                    WHEN MATCHED AND CHECKSUM(
                        Target.TYPE, Target.APPROVAL_NUMBER, Target.SCALING_METHOD_CODE, 
                        Target.TALLY_SPECIES_CODE, Target.TALLY_GRADE, Target.CONTAINER_CODE,
                        Target.CHARGE_TYPE, Target.LICENCE_NUMBER, Target.ACCOUNT_CODE, 
                        Target.MANAGEMENT_UNIT, Target.REDUCTION_RATE, Target.EFFECTIVE_DATE, 
                        Target.EXPIRY_DATE, Target.PRODUCT_TYPE, Target.DESTINATION_TYPE, 
                        Target.TAX_CODE, Target.TAX_RATE, Target.SPECIES_GROUP, 
                        Target.GRADE, Target.DESTINATION_SHARE, Target.SPECIES_SHARE, 
                        Target.GRADE_SHARE, Target.MASS_VOLUME_RATIO, Target.TREE_LENGTH_TABLE, 
                        Target.CONTAINER_VOLUME, Target.UNDERSIZE_DEDUCTION, Target.CULL_DEDUCTION
                    ) <> CHECKSUM(
                        Source.TYPE, Source.APPROVAL_NUMBER, Source.SCALING_METHOD_CODE, 
                        Source.TALLY_SPECIES_CODE, Source.TALLY_GRADE, Source.CONTAINER_CODE,
                        Source.CHARGE_TYPE, Source.LICENCE_NUMBER, Source.ACCOUNT_CODE, 
                        Source.MANAGEMENT_UNIT, Source.REDUCTION_RATE, Source.EFFECTIVE_DATE, 
                        Source.EXPIRY_DATE, Source.PRODUCT_TYPE, Source.DESTINATION_TYPE, 
                        Source.TAX_CODE, Source.TAX_RATE, Source.SPECIES_GROUP, 
                        Source.GRADE, Source.DESTINATION_SHARE, Source.SPECIES_SHARE, 
                        Source.GRADE_SHARE, Source.MASS_VOLUME_RATIO, Source.TREE_LENGTH_TABLE, 
                        Source.CONTAINER_VOLUME, Source.UNDERSIZE_DEDUCTION, Source.CULL_DEDUCTION
                    ) THEN
                        UPDATE SET 
                            Target.TYPE = Source.TYPE,
                            Target.APPROVAL_NUMBER = Source.APPROVAL_NUMBER,
                            Target.SCALING_METHOD_CODE = Source.SCALING_METHOD_CODE,
                            Target.TALLY_SPECIES_CODE = Source.TALLY_SPECIES_CODE,
                            Target.TALLY_GRADE = Source.TALLY_GRADE,
                            Target.CONTAINER_CODE = Source.CONTAINER_CODE,
                            Target.CHARGE_TYPE = Source.CHARGE_TYPE,
                            Target.LICENCE_NUMBER = Source.LICENCE_NUMBER,
                            Target.ACCOUNT_CODE = Source.ACCOUNT_CODE,
                            Target.MANAGEMENT_UNIT = Source.MANAGEMENT_UNIT,
                            Target.REDUCTION_RATE = Source.REDUCTION_RATE,
                            Target.EFFECTIVE_DATE = Source.EFFECTIVE_DATE,
                            Target.EXPIRY_DATE = Source.EXPIRY_DATE,
                            Target.PRODUCT_TYPE = Source.PRODUCT_TYPE,
                            Target.DESTINATION_TYPE = Source.DESTINATION_TYPE,
                            Target.TAX_CODE = Source.TAX_CODE,
                            Target.TAX_RATE = Source.TAX_RATE,
                            Target.SPECIES_GROUP = Source.SPECIES_GROUP,
                            Target.GRADE = Source.GRADE,
                            Target.DESTINATION_SHARE = Source.DESTINATION_SHARE,
                            Target.SPECIES_SHARE = Source.SPECIES_SHARE,
                            Target.GRADE_SHARE = Source.GRADE_SHARE,
                            Target.MASS_VOLUME_RATIO = Source.MASS_VOLUME_RATIO,
                            Target.TREE_LENGTH_TABLE = Source.TREE_LENGTH_TABLE,
                            Target.CONTAINER_VOLUME = Source.CONTAINER_VOLUME,
                            Target.UNDERSIZE_DEDUCTION = Source.UNDERSIZE_DEDUCTION,
                            Target.CULL_DEDUCTION = Source.CULL_DEDUCTION

                    -- if the data is new, do INSERT
                    WHEN NOT MATCHED THEN
                        INSERT (
                            TYPE, FACTOR_ID, APPROVAL_NUMBER, SCALING_METHOD_CODE,
                            TALLY_DESTINATION_CODE, TALLY_SPECIES_CODE, TALLY_GRADE, CONTAINER_CODE,
                            CHARGE_TYPE, LICENCE_NUMBER, ACCOUNT_CODE, MANAGEMENT_UNIT,
                            REDUCTION_RATE, EFFECTIVE_DATE, EXPIRY_DATE, DESTINATION_CODE,
                            PRODUCT_TYPE, DESTINATION_TYPE, TAX_CODE, TAX_RATE,
                            SPECIES_CODE, SPECIES_GROUP, GRADE, DESTINATION_SHARE,
                            SPECIES_SHARE, GRADE_SHARE, MASS_VOLUME_RATIO, TREE_LENGTH_TABLE,
                            CONTAINER_VOLUME, UNDERSIZE_DEDUCTION, CULL_DEDUCTION
                        )
                        VALUES (
                            Source.TYPE, Source.FACTOR_ID, Source.APPROVAL_NUMBER, Source.SCALING_METHOD_CODE,
                            Source.TALLY_DESTINATION_CODE, Source.TALLY_SPECIES_CODE, Source.TALLY_GRADE, Source.CONTAINER_CODE,
                            Source.CHARGE_TYPE, Source.LICENCE_NUMBER, Source.ACCOUNT_CODE, Source.MANAGEMENT_UNIT,
                            Source.REDUCTION_RATE, Source.EFFECTIVE_DATE, Source.EXPIRY_DATE, Source.DESTINATION_CODE,
                            Source.PRODUCT_TYPE, Source.DESTINATION_TYPE, Source.TAX_CODE, Source.TAX_RATE,
                            Source.SPECIES_CODE, Source.SPECIES_GROUP, Source.GRADE, Source.DESTINATION_SHARE,
                            Source.SPECIES_SHARE, Source.GRADE_SHARE, Source.MASS_VOLUME_RATIO, Source.TREE_LENGTH_TABLE,
                            Source.CONTAINER_VOLUME, Source.UNDERSIZE_DEDUCTION, Source.CULL_DEDUCTION
                        )

                    OUTPUT $action;
            ";



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
                                // Helper functions for safe values
                                // Returns empty string "" instead of DBNull.Value for NOT NULL string columns
                                object SafeString(string col, string defaultValue = "") =>
                                    dt.Columns.Contains(col) && row[col] != DBNull.Value ? row[col].ToString() : defaultValue;

                                // Returns 0 instead of DBNull.Value for NOT NULL int columns
                                object SafeInt(string col, int defaultValue = 0) =>
                                    dt.Columns.Contains(col) && row[col] != DBNull.Value && int.TryParse(row[col].ToString(), out int val) ? val : defaultValue;

                                // Returns 0.0 instead of DBNull.Value for NOT NULL float/double columns
                                object SafeDouble(string col, double defaultValue = 0.0) =>
                                    dt.Columns.Contains(col) && row[col] != DBNull.Value && double.TryParse(row[col].ToString(), out double val) ? val : defaultValue;

                                DateTime? effDate = dt.Columns.Contains("EFFECTIVE_DATE") ? DateHelper.ParseCsvDate(row["EFFECTIVE_DATE"]) : null;
                                DateTime? expDate = dt.Columns.Contains("EXPIRY_DATE") ? DateHelper.ParseCsvDate(row["EXPIRY_DATE"]) : null;


                                // Binding parameters
                                cmd.Parameters.AddWithValue("@TYPE", SafeString("TYPE"));
                                cmd.Parameters.AddWithValue("@FACTOR_ID", SafeInt("FACTOR_ID"));
                                cmd.Parameters.AddWithValue("@APPROVAL_NUMBER", SafeString("APPROVAL_NUMBER"));
                                cmd.Parameters.AddWithValue("@SCALING_METHOD_CODE", SafeString("SCALING_METHOD_CODE"));
                                cmd.Parameters.AddWithValue("@TALLY_DESTINATION_CODE", SafeString("TALLY_DESTINATION_CODE"));
                                cmd.Parameters.AddWithValue("@TALLY_SPECIES_CODE", SafeString("TALLY_SPECIES_CODE"));
                                cmd.Parameters.AddWithValue("@TALLY_GRADE", SafeString("TALLY_GRADE"));
                                cmd.Parameters.AddWithValue("@CONTAINER_CODE", SafeString("CONTAINER_CODE"));
                                cmd.Parameters.AddWithValue("@CHARGE_TYPE", SafeString("CHARGE_TYPE"));
                                cmd.Parameters.AddWithValue("@LICENCE_NUMBER", SafeString("LICENCE_NUMBER"));
                                cmd.Parameters.AddWithValue("@ACCOUNT_CODE", SafeString("ACCOUNT_CODE"));
                                cmd.Parameters.AddWithValue("@MANAGEMENT_UNIT", SafeString("MANAGEMENT_UNIT"));

                                cmd.Parameters.AddWithValue("@REDUCTION_RATE", SafeDouble("REDUCTION_RATE"));
                                cmd.Parameters.AddWithValue("@EFFECTIVE_DATE", effDate.HasValue ? (object)effDate.Value : DBNull.Value);
                                cmd.Parameters.AddWithValue("@EXPIRY_DATE", expDate.HasValue ? (object)expDate.Value : DBNull.Value);

                                cmd.Parameters.AddWithValue("@DESTINATION_CODE", SafeString("DESTINATION_CODE"));
                                cmd.Parameters.AddWithValue("@PRODUCT_TYPE", SafeString("PRODUCT_TYPE"));
                                cmd.Parameters.AddWithValue("@DESTINATION_TYPE", SafeString("DESTINATION_TYPE"));
                                cmd.Parameters.AddWithValue("@TAX_CODE", SafeString("TAX_CODE"));

                                cmd.Parameters.AddWithValue("@TAX_RATE", SafeDouble("TAX_RATE"));
                                cmd.Parameters.AddWithValue("@SPECIES_CODE", SafeString("SPECIES_CODE"));
                                cmd.Parameters.AddWithValue("@SPECIES_GROUP", SafeString("SPECIES_GROUP"));
                                cmd.Parameters.AddWithValue("@GRADE", SafeString("GRADE"));

                                cmd.Parameters.AddWithValue("@DESTINATION_SHARE", SafeDouble("DESTINATION_SHARE"));
                                cmd.Parameters.AddWithValue("@SPECIES_SHARE", SafeDouble("SPECIES_SHARE"));
                                cmd.Parameters.AddWithValue("@GRADE_SHARE", SafeDouble("GRADE_SHARE"));
                                cmd.Parameters.AddWithValue("@MASS_VOLUME_RATIO", SafeDouble("MASS_VOLUME_RATIO"));
                                cmd.Parameters.AddWithValue("@TREE_LENGTH_TABLE", SafeDouble("TREE_LENGTH_TABLE"));
                                cmd.Parameters.AddWithValue("@CONTAINER_VOLUME", SafeDouble("CONTAINER_VOLUME"));
                                cmd.Parameters.AddWithValue("@UNDERSIZE_DEDUCTION", SafeDouble("UNDERSIZE_DEDUCTION"));
                                cmd.Parameters.AddWithValue("@CULL_DEDUCTION", SafeDouble("CULL_DEDUCTION"));

                                object actionResult = cmd.ExecuteScalar();

                                processedCount++;
                                int dynamicProgress = processedCount * 100 / (rowCount + 1) * 80 / 100;
                                progress?.Report(20 + dynamicProgress);

                                if (actionResult != null && actionResult != DBNull.Value)
                                {
                                    successCount++;
                                }
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception($"Transaction failed during Factor Rates import: {ex.Message}", ex);
                    }
                }
            }

            return (successCount, errorCount);
        }
    }
}
