using LE_Importer;
using System;
using System.Data;
using System.Data.SqlClient;

namespace LEImporter
{
    class InvDetailByBolRepository
    {
        public static (int InsertedOrUpdatedCount, int ErrorCount) UpsertInvDetailByBol(DataTable dt, IProgress<int> progress, string connectionString)
        {
            int errorCount = 0;
            int rowCount = dt.Rows.Count;
            int processedCount = 0;
            int successCount = 0;

            if (dt == null || dt.Rows.Count == 0)
                return (0, 0);

            // 13-col combined key (BOL excluded per user request, BOL is NULLABLE).
            string mergeQuery = @"
                    MERGE INTO TBL_INV_DETAIL_BY_BOL AS Target
                    USING (
                        VALUES (
                            @INVOICE_NUM, @TALLY_ID, @TRANSMISSION_ID, @USER_ID, @USER_NAME,
                            @INVOICE_DATE, @MANAGEMENT_UNIT_NAME, @MANAGEMENT_UNIT_CODE,
                            @CUSTOMER_NAME, @CUSTOMER_ID, @ADDRESS_ID, @CUSTOMER_TYPE_CODE,
                            @TALLY_TYPE_CODE, @LICENCE_NUM, @LICENSEE_NAME, @APPROVAL_NUM,
                            @APPROVAL_TYPE, @PROCESSING_SITE_CODE, @PROCESSING_SITE_NAME,
                            @SCALING_METHOD_CODE, @SPECIES_CODE, @SPECIES_NAME, @GRADE,
                            @SPECIES_GROUP, @PRODUCT_SECTOR, @TALLY_DESTINATION, @TALLY_SPECIES,
                            @BOL, @MASS_SLIP_NUMBER, @TAX_CODE, @SCALING_DATE, @MEASURED_UNITS,
                            @VOLUME, @VOLUME_UNDERSIZE, @MINIMUM_STUMPAGE_RATE, @MINIMUM_STUMPAGE,
                            @RESIDUAL_STUMPAGE_RATE, @RESIDUAL_STUMPAGE, @EXPORT_ADMIN_RATE, @EXPORT_ADMIN,
                            @RENEWAL_RATE, @RENEWAL, @FORESTRY_FUTURES_RATE, @FORESTRY_FUTURES,
                            @FOREST_RESOURCE_INVENTORY_RATE, @FOREST_RESOURCE_INVENTORY,
                            @FOREST_MANAGEMENT_RATE, @FOREST_MANAGEMENT,
                            @LFMC_CONVERSION_RATE, @LFMC_CONVERSION,
                            @TREATY_LANDS_RATE, @TREATY_LANDS, @TOTAL_VALUE
                        )
                    ) AS Source (
                        INVOICE_NUM, TALLY_ID, TRANSMISSION_ID, USER_ID, USER_NAME,
                        INVOICE_DATE, MANAGEMENT_UNIT_NAME, MANAGEMENT_UNIT_CODE,
                        CUSTOMER_NAME, CUSTOMER_ID, ADDRESS_ID, CUSTOMER_TYPE_CODE,
                        TALLY_TYPE_CODE, LICENCE_NUM, LICENSEE_NAME, APPROVAL_NUM,
                        APPROVAL_TYPE, PROCESSING_SITE_CODE, PROCESSING_SITE_NAME,
                        SCALING_METHOD_CODE, SPECIES_CODE, SPECIES_NAME, GRADE,
                        SPECIES_GROUP, PRODUCT_SECTOR, TALLY_DESTINATION, TALLY_SPECIES,
                        BOL, MASS_SLIP_NUMBER, TAX_CODE, SCALING_DATE, MEASURED_UNITS,
                        VOLUME, VOLUME_UNDERSIZE, MINIMUM_STUMPAGE_RATE, MINIMUM_STUMPAGE,
                        RESIDUAL_STUMPAGE_RATE, RESIDUAL_STUMPAGE, EXPORT_ADMIN_RATE, EXPORT_ADMIN,
                        RENEWAL_RATE, RENEWAL, FORESTRY_FUTURES_RATE, FORESTRY_FUTURES,
                        FOREST_RESOURCE_INVENTORY_RATE, FOREST_RESOURCE_INVENTORY,
                        FOREST_MANAGEMENT_RATE, FOREST_MANAGEMENT,
                        LFMC_CONVERSION_RATE, LFMC_CONVERSION,
                        TREATY_LANDS_RATE, TREATY_LANDS, TOTAL_VALUE
                    )
                    ON  Target.INVOICE_NUM = Source.INVOICE_NUM
                    AND Target.TALLY_ID = Source.TALLY_ID
                    AND Target.TRANSMISSION_ID = Source.TRANSMISSION_ID
                    AND Target.USER_ID = Source.USER_ID
                    AND Target.MANAGEMENT_UNIT_CODE = Source.MANAGEMENT_UNIT_CODE
                    AND Target.CUSTOMER_ID = Source.CUSTOMER_ID
                    AND Target.ADDRESS_ID = Source.ADDRESS_ID
                    AND Target.LICENCE_NUM = Source.LICENCE_NUM
                    AND Target.APPROVAL_NUM = Source.APPROVAL_NUM
                    AND Target.PROCESSING_SITE_CODE = Source.PROCESSING_SITE_CODE
                    AND Target.SCALING_METHOD_CODE = Source.SCALING_METHOD_CODE
                    AND Target.SPECIES_CODE = Source.SPECIES_CODE
                    AND Target.TALLY_DESTINATION = Source.TALLY_DESTINATION

                    -- When match and at least one non-key column differs, do UPDATE
                    WHEN MATCHED AND CHECKSUM(
                        Target.USER_NAME, Target.INVOICE_DATE, Target.MANAGEMENT_UNIT_NAME,
                        Target.CUSTOMER_NAME, Target.CUSTOMER_TYPE_CODE, Target.TALLY_TYPE_CODE,
                        Target.LICENSEE_NAME, Target.APPROVAL_TYPE, Target.PROCESSING_SITE_NAME,
                        Target.SPECIES_NAME, Target.GRADE, Target.SPECIES_GROUP, Target.PRODUCT_SECTOR,
                        Target.TALLY_SPECIES, Target.BOL, Target.MASS_SLIP_NUMBER, Target.TAX_CODE,
                        Target.SCALING_DATE, Target.MEASURED_UNITS, Target.VOLUME, Target.VOLUME_UNDERSIZE,
                        Target.MINIMUM_STUMPAGE_RATE, Target.MINIMUM_STUMPAGE,
                        Target.RESIDUAL_STUMPAGE_RATE, Target.RESIDUAL_STUMPAGE,
                        Target.EXPORT_ADMIN_RATE, Target.EXPORT_ADMIN,
                        Target.RENEWAL_RATE, Target.RENEWAL,
                        Target.FORESTRY_FUTURES_RATE, Target.FORESTRY_FUTURES,
                        Target.FOREST_RESOURCE_INVENTORY_RATE, Target.FOREST_RESOURCE_INVENTORY,
                        Target.FOREST_MANAGEMENT_RATE, Target.FOREST_MANAGEMENT,
                        Target.LFMC_CONVERSION_RATE, Target.LFMC_CONVERSION,
                        Target.TREATY_LANDS_RATE, Target.TREATY_LANDS, Target.TOTAL_VALUE
                    ) <> CHECKSUM(
                        Source.USER_NAME, Source.INVOICE_DATE, Source.MANAGEMENT_UNIT_NAME,
                        Source.CUSTOMER_NAME, Source.CUSTOMER_TYPE_CODE, Source.TALLY_TYPE_CODE,
                        Source.LICENSEE_NAME, Source.APPROVAL_TYPE, Source.PROCESSING_SITE_NAME,
                        Source.SPECIES_NAME, Source.GRADE, Source.SPECIES_GROUP, Source.PRODUCT_SECTOR,
                        Source.TALLY_SPECIES, Source.BOL, Source.MASS_SLIP_NUMBER, Source.TAX_CODE,
                        Source.SCALING_DATE, Source.MEASURED_UNITS, Source.VOLUME, Source.VOLUME_UNDERSIZE,
                        Source.MINIMUM_STUMPAGE_RATE, Source.MINIMUM_STUMPAGE,
                        Source.RESIDUAL_STUMPAGE_RATE, Source.RESIDUAL_STUMPAGE,
                        Source.EXPORT_ADMIN_RATE, Source.EXPORT_ADMIN,
                        Source.RENEWAL_RATE, Source.RENEWAL,
                        Source.FORESTRY_FUTURES_RATE, Source.FORESTRY_FUTURES,
                        Source.FOREST_RESOURCE_INVENTORY_RATE, Source.FOREST_RESOURCE_INVENTORY,
                        Source.FOREST_MANAGEMENT_RATE, Source.FOREST_MANAGEMENT,
                        Source.LFMC_CONVERSION_RATE, Source.LFMC_CONVERSION,
                        Source.TREATY_LANDS_RATE, Source.TREATY_LANDS, Source.TOTAL_VALUE
                    ) THEN
                        UPDATE SET
                            Target.USER_NAME = Source.USER_NAME,
                            Target.INVOICE_DATE = Source.INVOICE_DATE,
                            Target.MANAGEMENT_UNIT_NAME = Source.MANAGEMENT_UNIT_NAME,
                            Target.CUSTOMER_NAME = Source.CUSTOMER_NAME,
                            Target.CUSTOMER_TYPE_CODE = Source.CUSTOMER_TYPE_CODE,
                            Target.TALLY_TYPE_CODE = Source.TALLY_TYPE_CODE,
                            Target.LICENSEE_NAME = Source.LICENSEE_NAME,
                            Target.APPROVAL_TYPE = Source.APPROVAL_TYPE,
                            Target.PROCESSING_SITE_NAME = Source.PROCESSING_SITE_NAME,
                            Target.SPECIES_NAME = Source.SPECIES_NAME,
                            Target.GRADE = Source.GRADE,
                            Target.SPECIES_GROUP = Source.SPECIES_GROUP,
                            Target.PRODUCT_SECTOR = Source.PRODUCT_SECTOR,
                            Target.TALLY_SPECIES = Source.TALLY_SPECIES,
                            Target.BOL = Source.BOL,
                            Target.MASS_SLIP_NUMBER = Source.MASS_SLIP_NUMBER,
                            Target.TAX_CODE = Source.TAX_CODE,
                            Target.SCALING_DATE = Source.SCALING_DATE,
                            Target.MEASURED_UNITS = Source.MEASURED_UNITS,
                            Target.VOLUME = Source.VOLUME,
                            Target.VOLUME_UNDERSIZE = Source.VOLUME_UNDERSIZE,
                            Target.MINIMUM_STUMPAGE_RATE = Source.MINIMUM_STUMPAGE_RATE,
                            Target.MINIMUM_STUMPAGE = Source.MINIMUM_STUMPAGE,
                            Target.RESIDUAL_STUMPAGE_RATE = Source.RESIDUAL_STUMPAGE_RATE,
                            Target.RESIDUAL_STUMPAGE = Source.RESIDUAL_STUMPAGE,
                            Target.EXPORT_ADMIN_RATE = Source.EXPORT_ADMIN_RATE,
                            Target.EXPORT_ADMIN = Source.EXPORT_ADMIN,
                            Target.RENEWAL_RATE = Source.RENEWAL_RATE,
                            Target.RENEWAL = Source.RENEWAL,
                            Target.FORESTRY_FUTURES_RATE = Source.FORESTRY_FUTURES_RATE,
                            Target.FORESTRY_FUTURES = Source.FORESTRY_FUTURES,
                            Target.FOREST_RESOURCE_INVENTORY_RATE = Source.FOREST_RESOURCE_INVENTORY_RATE,
                            Target.FOREST_RESOURCE_INVENTORY = Source.FOREST_RESOURCE_INVENTORY,
                            Target.FOREST_MANAGEMENT_RATE = Source.FOREST_MANAGEMENT_RATE,
                            Target.FOREST_MANAGEMENT = Source.FOREST_MANAGEMENT,
                            Target.LFMC_CONVERSION_RATE = Source.LFMC_CONVERSION_RATE,
                            Target.LFMC_CONVERSION = Source.LFMC_CONVERSION,
                            Target.TREATY_LANDS_RATE = Source.TREATY_LANDS_RATE,
                            Target.TREATY_LANDS = Source.TREATY_LANDS,
                            Target.TOTAL_VALUE = Source.TOTAL_VALUE

                    WHEN NOT MATCHED THEN
                        INSERT (
                            INVOICE_NUM, TALLY_ID, TRANSMISSION_ID, USER_ID, USER_NAME,
                            INVOICE_DATE, MANAGEMENT_UNIT_NAME, MANAGEMENT_UNIT_CODE,
                            CUSTOMER_NAME, CUSTOMER_ID, ADDRESS_ID, CUSTOMER_TYPE_CODE,
                            TALLY_TYPE_CODE, LICENCE_NUM, LICENSEE_NAME, APPROVAL_NUM,
                            APPROVAL_TYPE, PROCESSING_SITE_CODE, PROCESSING_SITE_NAME,
                            SCALING_METHOD_CODE, SPECIES_CODE, SPECIES_NAME, GRADE,
                            SPECIES_GROUP, PRODUCT_SECTOR, TALLY_DESTINATION, TALLY_SPECIES,
                            BOL, MASS_SLIP_NUMBER, TAX_CODE, SCALING_DATE, MEASURED_UNITS,
                            VOLUME, VOLUME_UNDERSIZE, MINIMUM_STUMPAGE_RATE, MINIMUM_STUMPAGE,
                            RESIDUAL_STUMPAGE_RATE, RESIDUAL_STUMPAGE, EXPORT_ADMIN_RATE, EXPORT_ADMIN,
                            RENEWAL_RATE, RENEWAL, FORESTRY_FUTURES_RATE, FORESTRY_FUTURES,
                            FOREST_RESOURCE_INVENTORY_RATE, FOREST_RESOURCE_INVENTORY,
                            FOREST_MANAGEMENT_RATE, FOREST_MANAGEMENT,
                            LFMC_CONVERSION_RATE, LFMC_CONVERSION,
                            TREATY_LANDS_RATE, TREATY_LANDS, TOTAL_VALUE
                        )
                        VALUES (
                            Source.INVOICE_NUM, Source.TALLY_ID, Source.TRANSMISSION_ID, Source.USER_ID, Source.USER_NAME,
                            Source.INVOICE_DATE, Source.MANAGEMENT_UNIT_NAME, Source.MANAGEMENT_UNIT_CODE,
                            Source.CUSTOMER_NAME, Source.CUSTOMER_ID, Source.ADDRESS_ID, Source.CUSTOMER_TYPE_CODE,
                            Source.TALLY_TYPE_CODE, Source.LICENCE_NUM, Source.LICENSEE_NAME, Source.APPROVAL_NUM,
                            Source.APPROVAL_TYPE, Source.PROCESSING_SITE_CODE, Source.PROCESSING_SITE_NAME,
                            Source.SCALING_METHOD_CODE, Source.SPECIES_CODE, Source.SPECIES_NAME, Source.GRADE,
                            Source.SPECIES_GROUP, Source.PRODUCT_SECTOR, Source.TALLY_DESTINATION, Source.TALLY_SPECIES,
                            Source.BOL, Source.MASS_SLIP_NUMBER, Source.TAX_CODE, Source.SCALING_DATE, Source.MEASURED_UNITS,
                            Source.VOLUME, Source.VOLUME_UNDERSIZE, Source.MINIMUM_STUMPAGE_RATE, Source.MINIMUM_STUMPAGE,
                            Source.RESIDUAL_STUMPAGE_RATE, Source.RESIDUAL_STUMPAGE, Source.EXPORT_ADMIN_RATE, Source.EXPORT_ADMIN,
                            Source.RENEWAL_RATE, Source.RENEWAL, Source.FORESTRY_FUTURES_RATE, Source.FORESTRY_FUTURES,
                            Source.FOREST_RESOURCE_INVENTORY_RATE, Source.FOREST_RESOURCE_INVENTORY,
                            Source.FOREST_MANAGEMENT_RATE, Source.FOREST_MANAGEMENT,
                            Source.LFMC_CONVERSION_RATE, Source.LFMC_CONVERSION,
                            Source.TREATY_LANDS_RATE, Source.TREATY_LANDS, Source.TOTAL_VALUE
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
                                object SafeString(string col, string defaultValue = "") =>
                                    dt.Columns.Contains(col) && row[col] != DBNull.Value ? row[col].ToString() : defaultValue;

                                object SafeStringNullable(string col) =>
                                    dt.Columns.Contains(col) && row[col] != DBNull.Value && !string.IsNullOrWhiteSpace(row[col].ToString())
                                        ? (object)row[col].ToString() : DBNull.Value;

                                object SafeInt(string col, int defaultValue = 0) =>
                                    dt.Columns.Contains(col) && row[col] != DBNull.Value && int.TryParse(row[col].ToString(), out int val) ? val : defaultValue;

                                object SafeDecimal(string col, decimal defaultValue = 0m)
                                {
                                    if (!dt.Columns.Contains(col) || row[col] == DBNull.Value) return defaultValue;
                                    string s = row[col].ToString().Trim();
                                    if (s == "NA" || s == "-" || s == string.Empty) return defaultValue;
                                    if (decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal v)) return v;
                                    if (decimal.TryParse(s, out v)) return v;
                                    return defaultValue;
                                }

                                DateTime? invDate = dt.Columns.Contains("INVOICE_DATE") ? DateHelper.ParseCsvDate(row["INVOICE_DATE"]) : null;
                                DateTime? scalingDate = dt.Columns.Contains("SCALING_DATE") ? DateHelper.ParseCsvDate(row["SCALING_DATE"]) : null;

                                cmd.Parameters.AddWithValue("@INVOICE_NUM", SafeInt("INVOICE_NUM"));
                                cmd.Parameters.AddWithValue("@TALLY_ID", SafeInt("TALLY_ID"));
                                cmd.Parameters.AddWithValue("@TRANSMISSION_ID", SafeInt("TRANSMISSION_ID"));
                                cmd.Parameters.AddWithValue("@USER_ID", SafeInt("USER_ID"));
                                cmd.Parameters.AddWithValue("@USER_NAME", SafeStringNullable("USER_NAME"));
                                cmd.Parameters.AddWithValue("@INVOICE_DATE", invDate.HasValue ? (object)invDate.Value : DBNull.Value);
                                cmd.Parameters.AddWithValue("@MANAGEMENT_UNIT_NAME", SafeStringNullable("MANAGEMENT_UNIT_NAME"));
                                cmd.Parameters.AddWithValue("@MANAGEMENT_UNIT_CODE", SafeString("MANAGEMENT_UNIT_CODE"));
                                cmd.Parameters.AddWithValue("@CUSTOMER_NAME", SafeStringNullable("CUSTOMER_NAME"));
                                cmd.Parameters.AddWithValue("@CUSTOMER_ID", SafeInt("CUSTOMER_ID"));
                                cmd.Parameters.AddWithValue("@ADDRESS_ID", SafeInt("ADDRESS_ID"));
                                cmd.Parameters.AddWithValue("@CUSTOMER_TYPE_CODE", SafeStringNullable("CUSTOMER_TYPE_CODE"));
                                cmd.Parameters.AddWithValue("@TALLY_TYPE_CODE", SafeStringNullable("TALLY_TYPE_CODE"));
                                cmd.Parameters.AddWithValue("@LICENCE_NUM", SafeString("LICENCE_NUM"));
                                cmd.Parameters.AddWithValue("@LICENSEE_NAME", SafeStringNullable("LICENSEE_NAME"));
                                cmd.Parameters.AddWithValue("@APPROVAL_NUM", SafeInt("APPROVAL_NUM"));
                                cmd.Parameters.AddWithValue("@APPROVAL_TYPE", SafeStringNullable("APPROVAL_TYPE"));
                                cmd.Parameters.AddWithValue("@PROCESSING_SITE_CODE", SafeString("PROCESSING_SITE_CODE"));
                                cmd.Parameters.AddWithValue("@PROCESSING_SITE_NAME", SafeStringNullable("PROCESSING_SITE_NAME"));
                                cmd.Parameters.AddWithValue("@SCALING_METHOD_CODE", SafeString("SCALING_METHOD_CODE"));
                                cmd.Parameters.AddWithValue("@SPECIES_CODE", SafeString("SPECIES_CODE"));
                                cmd.Parameters.AddWithValue("@SPECIES_NAME", SafeStringNullable("SPECIES_NAME"));
                                cmd.Parameters.AddWithValue("@GRADE", SafeStringNullable("GRADE"));
                                cmd.Parameters.AddWithValue("@SPECIES_GROUP", SafeStringNullable("SPECIES_GROUP"));
                                cmd.Parameters.AddWithValue("@PRODUCT_SECTOR", SafeStringNullable("PRODUCT_SECTOR"));
                                cmd.Parameters.AddWithValue("@TALLY_DESTINATION", SafeString("TALLY_DESTINATION"));
                                cmd.Parameters.AddWithValue("@TALLY_SPECIES", SafeStringNullable("TALLY_SPECIES"));
                                cmd.Parameters.AddWithValue("@BOL", SafeStringNullable("BOL"));
                                cmd.Parameters.AddWithValue("@MASS_SLIP_NUMBER", SafeStringNullable("MASS_SLIP_NUMBER"));
                                cmd.Parameters.AddWithValue("@TAX_CODE", SafeStringNullable("TAX_CODE"));
                                cmd.Parameters.AddWithValue("@SCALING_DATE", scalingDate.HasValue ? (object)scalingDate.Value : DBNull.Value);
                                cmd.Parameters.AddWithValue("@MEASURED_UNITS", SafeDecimal("MEASURED_UNITS"));
                                cmd.Parameters.AddWithValue("@VOLUME", SafeDecimal("VOLUME"));
                                cmd.Parameters.AddWithValue("@VOLUME_UNDERSIZE", SafeDecimal("VOLUME_UNDERSIZE"));
                                cmd.Parameters.AddWithValue("@MINIMUM_STUMPAGE_RATE", SafeDecimal("MINIMUM_STUMPAGE_RATE"));
                                cmd.Parameters.AddWithValue("@MINIMUM_STUMPAGE", SafeDecimal("MINIMUM_STUMPAGE"));
                                cmd.Parameters.AddWithValue("@RESIDUAL_STUMPAGE_RATE", SafeDecimal("RESIDUAL_STUMPAGE_RATE"));
                                cmd.Parameters.AddWithValue("@RESIDUAL_STUMPAGE", SafeDecimal("RESIDUAL_STUMPAGE"));
                                cmd.Parameters.AddWithValue("@EXPORT_ADMIN_RATE", SafeDecimal("EXPORT_ADMIN_RATE"));
                                cmd.Parameters.AddWithValue("@EXPORT_ADMIN", SafeDecimal("EXPORT_ADMIN"));
                                cmd.Parameters.AddWithValue("@RENEWAL_RATE", SafeDecimal("RENEWAL_RATE"));
                                cmd.Parameters.AddWithValue("@RENEWAL", SafeDecimal("RENEWAL"));
                                cmd.Parameters.AddWithValue("@FORESTRY_FUTURES_RATE", SafeDecimal("FORESTRY_FUTURES_RATE"));
                                cmd.Parameters.AddWithValue("@FORESTRY_FUTURES", SafeDecimal("FORESTRY_FUTURES"));
                                cmd.Parameters.AddWithValue("@FOREST_RESOURCE_INVENTORY_RATE", SafeDecimal("FOREST_RESOURCE_INVENTORY_RATE"));
                                cmd.Parameters.AddWithValue("@FOREST_RESOURCE_INVENTORY", SafeDecimal("FOREST_RESOURCE_INVENTORY"));
                                cmd.Parameters.AddWithValue("@FOREST_MANAGEMENT_RATE", SafeDecimal("FOREST_MANAGEMENT_RATE"));
                                cmd.Parameters.AddWithValue("@FOREST_MANAGEMENT", SafeDecimal("FOREST_MANAGEMENT"));
                                cmd.Parameters.AddWithValue("@LFMC_CONVERSION_RATE", SafeDecimal("LFMC_CONVERSION_RATE"));
                                cmd.Parameters.AddWithValue("@LFMC_CONVERSION", SafeDecimal("LFMC_CONVERSION"));
                                cmd.Parameters.AddWithValue("@TREATY_LANDS_RATE", SafeDecimal("TREATY_LANDS_RATE"));
                                cmd.Parameters.AddWithValue("@TREATY_LANDS", SafeDecimal("TREATY_LANDS"));
                                cmd.Parameters.AddWithValue("@TOTAL_VALUE", SafeDecimal("TOTAL_VALUE"));

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
                        throw new Exception($"Transaction failed during INV Detail by BOL import: {ex.Message}", ex);
                    }
                }
            }

            return (successCount, errorCount);
        }
    }
}
