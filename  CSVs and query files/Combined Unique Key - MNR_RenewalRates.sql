/****** Script for SelectTopNRows command from SSMS  ******/
SELECT TOP (1000) [TYPE]
      ,[RATE_ID]
      ,[MANAGEMENT_UNIT_CODE]
      ,[MANAGEMENT_UNIT_NAME]
      ,[SPECIES_GROUP_CODE]
      ,[SPECIES_GROUP_NAME]
      ,[RATE_TYPE_CODE]
      ,[RATE_TYPE_NAME]
      ,[RATE]
      ,[EFFECTIVE_DATE]
      ,[EXPIRY_DATE]
      ,[ID]
  FROM [AZ_TEST_IMPORTER].[dbo].[MNR_RENEWAL_RATES]

   SELECT 
    [RATE_ID],
    [MANAGEMENT_UNIT_CODE],
    COUNT(*) AS [DuplicateCount]
FROM 
    [MNR_RENEWAL_RATES] -- Replace with your actual table name
GROUP BY 
    [RATE_ID],
    [MANAGEMENT_UNIT_CODE]
HAVING 
    COUNT(*) > 1;
