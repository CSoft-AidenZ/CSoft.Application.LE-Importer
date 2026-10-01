# LEImporter Changelog

All notable changes to this project are recorded here.
The display version is `appVersion` in `App.config` (single source of truth);
`AssemblyVersion` / `AssemblyFileVersion` in `Properties/AssemblyInfo.cs` are kept in sync.
The window title falls back to the assembly version if `App.config` has no `appVersion` key.

## [1.0.5] - 2026-10-01

### Changed
- `BOL`, `MASS_SLIP_NUMBER`, `MEASURED_UNITS` added to the `INV_DETAIL_BY_BOL` MERGE key (13-col -> 14-col), so
  rows differing only by BOL coexist instead of overwriting each other.
  (A temporary `[DUP-KEY]` diagnostic confirmed the old key collapsed such
  rows, and was removed again in this same release.)
- `MERGE ON` clause is now NULL-safe for NULLABLE key columns (driven by
  `IS_NULLABLE` from `INFORMATION_SCHEMA`): `(Target = Source OR (both NULL))`.
  Without this, NULL-BOL rows would never match and re-imports would INSERT
  duplicates.
- Covering index `IX_TBL_INV_DETAIL_BY_BOL_13KEY` replaced by 14-col
  `IX_TBL_INV_DETAIL_BY_BOL_14KEY` (see migration snippet in
  `Create_TBL_INV_DETAIL_BY_BOL.sql` for existing databases).

## [1.0.4] - 2026-10-01

### Fixed
- Replaced `CHECKSUM` change detection with value-based `EXCEPT` comparison
  in `GenericUpsertRepository`. `CHECKSUM` is representation-sensitive, so
  re-importing an unchanged file still reported every row as updated
  (e.g. 25,993 phantom updates on the PMU factor CSV). Identical rows are
  now reliably skipped regardless of storage types (`CHAR` padding,
  `varchar`/`nvarchar`, `date`/`datetime`, decimal scale).

### Changed
- Import counts are now split into inserted / updated / unchanged-skipped.
  UI, log, and message box show the full breakdown including the file total,
  e.g. `0 inserted, 0 updated, 25,993 unchanged skipped (25,993 total in file)`
  on a no-change re-import.

## [1.0.3] - 2026-10-01

### Added
- New `TableRepository/GenericUpsertRepository.cs`: builds the `MERGE` upsert
  statement at runtime from SQL Server `INFORMATION_SCHEMA.COLUMNS`
  (column list, `DATA_TYPE`, `IS_NULLABLE`) instead of hard-coded SQL.
  New xlsx/csv types only need one registry line (table name + key columns).

### Changed
- Unified all four importers on `CHECKSUM(non-key columns)` change detection
  with `OUTPUT $action`: unchanged rows are skipped and no longer counted.
  `RenewalRates` / `StumpageRates` previously rewrote every matched row and
  counted no-change rows as successes.
- Parameters now use explicit `SqlDbType` inferred from `DATA_TYPE`
  (replacing `AddWithValue`), with `Precision`/`Scale`/`Size` from schema metadata.

### Deprecated (thin wrappers, behavior preserved via delegation)
- `FactorRatesSchemaRepository`, `RenewalRatesSchemaRepository`,
  `StumpageRateRepository`, `InvDetailByBolRepository` now delegate to
  `GenericUpsertRepository`. `GeneralImporter` / UI call sites unchanged.

## [1.0.2] and earlier
- No changelog was kept. History before 1.0.3 is not recorded here.
