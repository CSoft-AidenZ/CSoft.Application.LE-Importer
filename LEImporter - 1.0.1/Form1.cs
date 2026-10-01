using LE_Importer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LEImporter
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        // Property to store the selected file path for later use in the import process
        public string SelectedFilePath { get; private set; }

        // ... Existing Form1 constructor and code ...

        /// <summary>
        /// Handles opening a file dialog to select CSV or Excel files.
        /// </summary>
        private async void btnImportFile_Click(object sender, EventArgs e)
        {

            // 1. Initialize ProgressBar  
            progressBar1.Value = 0;
            progressBar1.Maximum = 100;
            lblSelectedFile.Text = "Preparing data...";
            btnImportFile.Enabled = false; // Disable button for misclicking.


            // 2. Crate Progress
            var progress = new Progress<int>(percent =>
            {
                progressBar1.Value = Math.Min(100, Math.Max(0, percent));
                lblSelectedFile.Text = $"Importing data... {percent}%";
            });


            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                // Set initial directory (optional: defaults to desktop or last used folder)
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                // Filter for CSV and Excel files
                openFileDialog.Filter = "CSV Files (*.csv)|*.csv|Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|All Files (*.*)|*.*";
                openFileDialog.FilterIndex = 1; // Default to CSV
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Title = "Select File to Import";

                // Show the file dialog
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Store the selected file path
                    SelectedFilePath = openFileDialog.FileName;

                    // Display the selected file path in a label or text box (if you have one)
                    if (lblSelectedFile != null)
                    {
                        lblSelectedFile.Text = $"File: {Path.GetFileName(SelectedFilePath)}";
                    }


                    lblSelectedFile.Text = $"File selected successfully:\n{SelectedFilePath}";
                    Logger.Success(lblSelectedFile.Text);
                    try
                    {
                        FileClassifier classifier = new FileClassifier();
                        DataTable result = classifier.FileParse(openFileDialog.OpenFile());

                        // Variables to hold background thread results
                        RateType classifiedResult = RateType.Invalid_DATA;
                        int rowsProcessed = 0;

                        await Task.Run(() =>
                        {
                            // 1. Classify the rate file
                            classifiedResult = GeneralImporter.ClassifyRateType(result, out Exception classifyEx);

                            if (classifiedResult == RateType.Invalid_DATA || classifyEx != null)
                            {
                                // Throwing inside Task.Run will safely bubble up to the outer catch block on the UI thread
                                throw new IOException($"Import File Error: {classifyEx?.Message ?? "Unrecognized CSV format."}");
                            }

                            // 2. Process and Import data to database (passing progress here)
                            rowsProcessed = GeneralImporter.GeneralImport(classifiedResult, result, progress, out Exception importEx);

                            if (importEx != null)
                            {
                                throw importEx; // Escalates import errors to the outer try/catch
                            }
                        });

                        // --- Back on the UI thread here ---
                        lblSelectedFile.Text = $"Import completed! {rowsProcessed} rows are processed.";
                        Logger.Success($"Import finished. Processed: {rowsProcessed} rows.");
                        MessageBox.Show($"Successfully processed {rowsProcessed} rows!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        if (result != null && result.Rows.Count > 0)
                        {

                        }
                        else
                        {
                            MessageBox.Show("The file was parsed, but no rows were found.", "Empty Result", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            Logger.Error("The file was parsed, but no rows were found.");
                        }
                    }
                    catch (Exception ex)
                    {
                        // --- Caught on the UI thread here ---
                        lblSelectedFile.Text = "Import failed.";
                        MessageBox.Show($"Import Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Logger.Error($"Import Error: {ex.Message}");
                    }
                    finally
                    {
                        // Always re-enable the button when done
                        btnImportFile.Enabled = true;
                    }

                    // 4. Recover button.
                    btnImportFile.Enabled = true;
                }
            }
        }
        /// <summary>
        /// Handles the Click event for btnTestConnection.
        /// Uses async/await to keep the UI responsive while testing SQL Server connectivity.
        /// </summary>
        private async void btnTestConnection_Click(object sender, EventArgs e)
        {
            // 1. Disable button & update status during test
            btnTestConnection.Enabled = false;
            lblStatus.Text = "Status: Testing connection to the database server...";
            lblStatus.ForeColor = Color.DarkOrange;

            string errorMsg = string.Empty;
            bool isSuccess = false;

            // 2. Run the connection test asynchronously off the UI thread
            await Task.Run(() =>
            {
                isSuccess = DatabaseHelper.TestConnection(out errorMsg);
            });

            // 3. Update UI based on results
            if (isSuccess)
            {
                lblStatus.Text = "Status: Connected successfully! ";
                lblStatus.ForeColor = Color.ForestGreen;

                Logger.Success("Database connection successful!\nConnected using parameters from ASMDataLayer.ini.");
                MessageBox.Show(
                    "Database connection successful!\nConnected using parameters from ASMDataLayer.ini.",
                    "Connection Test",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            else
            {
                lblStatus.Text = "Status: Connection Failed ";
                lblStatus.ForeColor = Color.Firebrick;

                Logger.Error($"Failed to connect to Database.\n\nDetails:\n{errorMsg}");
                MessageBox.Show(
                    $"Failed to connect to Database.\n\nDetails:\n{errorMsg}",
                    "Connection Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }

            // 4. Re-enable button
            btnTestConnection.Enabled = true;
        }
    }

}
