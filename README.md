# Logger's Edge - Generic Data Import Utility (LE-Importer)

[![.NET](https://img.shields.io/badge/.NET-C%23-blue.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey.svg)](https://microsoft.com)

A robust, standalone C# desktop application built to streamline monthly batch data ingestion into **Logger's Edge** database tables for **Algonquin Forestry**.

`LE-Importer` parses structured CSV/Excel reports (Stumpage Rates, Renewal Rates, Mass Factors), performs pre-import validation and field alignment, handles dates safely, and executes high-performance Upsert (Insert or Update) operations using a generic data engine.

---

## 📌 Project Overview & Purpose

Algonquin Forestry receives monthly financial and operational CSV/Excel reports that must be accurately loaded into the Logger's Edge SQL database. Manual data entry or executing raw SQL scripts monthly is error-prone and inefficient.

**LE-Importer** bridges this gap by offering:
- **Guided UI Flow:** Select table $\rightarrow$ Select file $\rightarrow$ Validate $\rightarrow$ Import.
- **Generic Engine Architecture:** UI controls are dynamic/configurable, while the underlying CSV parser and DB adapter are fully decoupled and reusable.
- **Data Integrity & Upsert Handling:** Smart column verification, safe date parsing, and configurable unique keys per table to automatically handle updates vs. new inserts.
- **Comprehensive Logging:** Real-time error trapping with visual feedback and line-by-line logging for bad rows.

---

## 🚀 Key Features

- **Generic Import Architecture:** The core file-reading and DB-writing mechanics are fully generic (`Type`-agnostic / Dynamic mapping), allowing straightforward expansion to 4th, 5th, or future file schemas.
- **Automated Schema & Column Alignment:** Verifies CSV headers against target SQL table schemas before touching the database.
- **Robust Date Parsing:** Traps ambiguous date formats (`MM/DD/YYYY`, `YYYY-MM-DD`, `DD-MMM-YYYY`) using flexible datetime parsers to avoid database type crashes.
- **Safe Upsert Operations:** Prevents duplicate primary entries by detecting unique key constraints and executing conditional Updates or Inserts.
- **Batch Processing:** Utilizes `SqlBulkCopy` / `MERGE` patterns for ultra-fast monthly throughput.

---

## 🗄 Target Data Schemas & Files

The current phase supports the following three monthly forestry report streams:

| CSV Source File | Target SQL Schema Table | Description |
| :--- | :--- | :--- |
| `1102874 - PMU - EDT STUMPAGE RATES...CSV` | `MNR_StumpageRates` | Crown stumpage rates per timber spec/region |
| `1102875 - PMU - EDT RENEWAL RATES...CSV` | `MNR_RenewalRates` | Forest renewal trust rates & fund metrics |
| `PMU_-_EDT_MASS_FACTORS...CSV` | `MNR_FactorRates` | Timber volume-to-mass conversion factors |

---

## ⚙️ System Workflow & Application Architecture

```
+-------------------+      +----------------------+      +-----------------------+
|  User Selects     | ---> | Schema Verification  | ---> |   Data Sanitization   |
|  Table & CSV File |      | Column Name Matching |      | Date Parsing & Format |
+-------------------+      +----------------------+      +-----------------------+
                                                                     |
                                                                     v
+-------------------+      +----------------------+      +-----------------------+
| Interactive Error | <--- | Database Ingestion   | <--- | Generic Upsert Engine |
| Log & UI Summary  |      | SQL Server Database  |      | (Insert or Update)    |
+-------------------+      +----------------------+      +-----------------------+
```

---

## 🛠 Tech Stack

- **Language & Framework:** C# (.NET Framework 4.8 / .NET 8 Desktop)
- **UI Technology:** WPF or Windows Forms (Standalone `.exe`)
- **Database:** Microsoft SQL Server / Logger's Edge DB
- **Libraries:**
  - `CsvHelper` / `ExcelDataReader` — Fast, robust file reading
  - `Dapper` or `SqlBulkCopy` — High-performance database operations
  - `Serilog` or `NLog` — System logging

---

## 💻 Getting Started & Installation

### Prerequisites
- Visual Studio 2022 (with .NET Desktop Development workload)
- Microsoft SQL Server (Local, Express, or Remote)
- Logger's Edge DB instance or a clean testing database

### Database Setup
1. Open SQL Server Management Studio (SSMS).
2. Execute the schema creation scripts provided in the `/Schemas` directory:
   ```sql
   -- Run the following scripts in order:
   1. MNR_StumpageRatesSchema.sql
   2. MNR_RenewalRatesSchema.sql
   3. MNR_FactorRatesSchema.sql
   ```

### Building the Project
1. Clone the repository:
   ```bash
   git clone https://github.com/your-username/LE-Importer.git
   ```
2. Open `LE-Importer.sln` in Visual Studio.
3. Restore NuGet packages:
   ```bash
   dotnet restore
   ```
4. Update connection string in `App.config` / `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "LoggersEdgeDB": "Server=localhost;Database=LoggersEdge;Integrated Security=True;TrustServerCertificate=True;"
     }
   }
   ```
5. Build and run the project (`F5`).

---

## 📖 How to Use

1. **Launch `LE-Importer.exe`**.
2. **Select Target Table:** Choose the table you are importing into (e.g., `MNR_StumpageRates`).
3. **Select File:** Click **Browse** and choose the corresponding CSV file.
4. **Validate File:** The tool verifies column names against the selected target table schema.
5. **Run Import:** Click **Execute Import**. The program parses dates, builds batch records, and performs Upsert operations.
6. **Review Results:** Success count and any skipped/errored rows will be displayed in the status window and saved to the log file.

---

## 📄 License

This project is proprietary and built specifically for Logger's Edge / Algonquin Forestry integration. All rights reserved.
