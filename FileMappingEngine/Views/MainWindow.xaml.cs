using FileMappingEngine.Lib;
using FileMappingEngine.Lib.Models;
using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace FileMappingEngine
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        AppManager appManager = new AppManager();
        Helper helper = new Helper();
        private bool _isFirstLoad = false;
        private string? _oldColumnName;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void LoadFile(string fileName)
        {
            appManager.OpenFile(fileName);

            if (appManager.CurrentData == null)
            {
                MessageBox.Show("Failed to load file.");
                return;
            }

            headerRowPanel.IsEnabled = true;

            if (_isFirstLoad)
            {
                LoadComboBox();
            }

            GenerateMappingSetButtons();
            ReloadGrid();
        }

        private void LoadComboBox()
        {
            cbHeaderRow.Items.Clear();
            for (int i = 1; i <= appManager.CurrentData.Rows.Count; i++)
            {
                cbHeaderRow.Items.Add(i);
            }

        }

        private void GenerateMappingSetButtons()
        {
            mappingPanel.Children.Clear();

            foreach (string mappingPath in appManager.GetExistingMappings())
            {
                Button button = new Button
                {
                    Content = System.IO.Path.GetFileNameWithoutExtension(mappingPath),
                    Tag = mappingPath,
                    Margin = new Thickness(5),
                    Padding = new Thickness(10)
                };

                button.Click += MappingSetButton_Click;

                mappingPanel.Children.Add(button);
            }
        }

        private void ReloadGrid()
        {
            helper.ReloadDataGrid(dataGrid, appManager.CurrentData);

            if (appManager.CurrentFile?.SortedColumn != null)
            {
                RestoreSort();
            }
        }

        private void MappingSetButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string filePath)
                return;
            appManager.ApplyMappingSet(filePath);
            ReloadGrid();
        }

        private void chooseFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (appManager.CurrentFile?.CurrentData != null && appManager.CurrentData.Rows.Count > 0)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Vai vēlaties ielādēt citu failu? Pašreizējā tabula tiks izdzēsta.",
                    "Apstiprinājums",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    return;
                }
                appManager.CloseCurrentFile();
            }

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Excel Files|*.xlsx;*.xls";

            if (ofd.ShowDialog() == true)
            {
                _isFirstLoad = true;
                LoadFile(ofd.FileName);
                _isFirstLoad = false;

                txtFilePath.Text = appManager?.CurrentFile?.FileName;
            }
        }

        private void cbHeaderRow_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbHeaderRow.SelectedIndex >= 0 && appManager.CurrentFile != null && appManager.CurrentFile.FileName != null)
            {
                int headerRowIndex = cbHeaderRow.SelectedIndex + 1;

                appManager.UpdateHeaderRow(headerRowIndex);
                ReloadGrid();
            }
        }

        private void saveFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (appManager.CurrentFile == null)
                return;

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Excel Files|*.xlsx;*.xls";
            sfd.FileName = appManager.CurrentFile.FileName;

            if (sfd.ShowDialog() == true)
            {
                if (!string.IsNullOrEmpty(sfd.FileName))
                {
                    appManager.SaveFile(sfd.FileName);

                    MessageBox.Show(
                        "Fails saglabāts!",
                        "Info",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        private void SaveMappingSet_Click(object sender, RoutedEventArgs e)
        {
            SaveMappingSetToJson();
        }

        private void dataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            string columnName = e.Column.Header.ToString() ?? "";

            bool ascending =
                e.Column.SortDirection != ListSortDirection.Ascending;

            appManager.SortData(columnName, ascending);
        }

        private void RestoreSort()
        {
            if (appManager.CurrentFile?.SortedColumn == null)
                return;

            foreach (var column in dataGrid.Columns)
            {
                if (column.Header?.ToString() ==
                    appManager.CurrentFile.SortedColumn)
                {
                    column.SortDirection =
                        appManager.CurrentFile.SortAscending == true
                        ? ListSortDirection.Ascending
                        : ListSortDirection.Descending;
                }
            }
        }

        private void dataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            DataGridColumn column = e.Column;
            System.Windows.Style style = new System.Windows.Style(typeof(DataGridColumnHeader));
            // Add context menu to each column header
            style.Setters.Add(new Setter(ContextMenuProperty, CreateColumnHeaderContextMenu(column)));

            column.HeaderStyle = style;
        }

        private ContextMenu CreateColumnHeaderContextMenu(DataGridColumn column)
        {
            ContextMenu menu = new ContextMenu();

            // Remove Column
            MenuItem removeItem = new MenuItem { Header = "Remove Column", Tag = column };
            removeItem.Click += RemoveColumnMenuItem_Click;
            menu.Items.Add(removeItem);

            // Add Column right
            MenuItem addItemRight = new MenuItem { Header = "Add Column / Right", Tag = column };
            addItemRight.Click += AddColumnMenuItem_Click;
            menu.Items.Add(addItemRight);

            // Add Column left
            MenuItem addItemLeft = new MenuItem { Header = "Add Column / Left", Tag = column };
            addItemLeft.Click += AddColumnMenuItem_Click;
            menu.Items.Add(addItemLeft);

            //MenuItem hideItem = new MenuItem { Header = "Hide Column", Tag = column };
            //hideItem.Click += HideColumnMenuItem_Click;
            //menu.Items.Add(hideItem);

            // Rename Column
            MenuItem renameCol = new MenuItem { Header = "Rename Column", Tag = column };
            renameCol.Click += RenameColumnMenuItem_Click;
            menu.Items.Add(renameCol);

            // Set Data Type
            MenuItem setFormat = new MenuItem { Header = "Set Data Type", Tag = column };
            setFormat.Click += SetDataTypeMenuItem_Click;
            menu.Items.Add(setFormat);

            MenuItem concat = new MenuItem { Header = "Merge columns", Tag = column };
            concat.Click += ConcatColumnsMenuItem_Click;
            menu.Items.Add(concat);

            MenuItem writeFormula = new MenuItem { Header = "Formula", Tag = column };
            writeFormula.Click += WriteFormulaMenuItem_Click;
            menu.Items.Add(writeFormula);

            menu.Items.Add(new MenuItem { Header = "-" });

            return menu;
        }

        private void RemoveColumnMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not DataGridColumn column)
                return;

            string? columnName = column.Header?.ToString();

            if (string.IsNullOrEmpty(columnName))
                return;

            appManager.RemoveColumn(columnName);
            ReloadGrid();
        }

        private void AddColumnMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not DataGridColumn column)
                return;
            
            string? anchorId = column.Header?.ToString();
            
            string direction = (string)menuItem.Header == "Add Column / Left"
                ? "Left"
                : "Right";
            if (string.IsNullOrEmpty(anchorId))
                return;
            appManager.AddColumn(direction, anchorId, null);
            ReloadGrid();
        }
        
        private void RenameColumnMenuItem_Click(object sender, RoutedEventArgs e)
        {

            if (sender is not MenuItem menuItem || menuItem.Tag is not DataGridColumn column)
                return;

            _oldColumnName = column.Header?.ToString();

            if (string.IsNullOrWhiteSpace(_oldColumnName))
                return;

            txtCurrentColumnName.Text = _oldColumnName;
            txtNewColumnName.Text = _oldColumnName;

            txtRenameValidation.Visibility = Visibility.Collapsed;

            RenameColumnOverlay.Visibility = Visibility.Visible;
        }

        private void SaveRenameColumn_Click(object sender, RoutedEventArgs e)
        {
            string newName = txtNewColumnName.Text.Trim();

            if (string.IsNullOrWhiteSpace(newName))
            {
                txtRenameValidation.Text = "Column name cannot be empty.";
                txtRenameValidation.Visibility = Visibility.Visible;
                return;
            }

            if (appManager.IsColumnNameTaken(newName))
            {
                txtRenameValidation.Text = $"Column '{newName}' already exists.";
                txtRenameValidation.Visibility = Visibility.Visible;
                return;
            }

            appManager.RenameColumn(_oldColumnName!, newName);

            helper.ReloadDataGrid(dataGrid, appManager.CurrentData);

            RenameColumnOverlay.Visibility = Visibility.Collapsed;
        }

        private void CancelRenameColumn_Click(object sender, RoutedEventArgs e)
        {
            RenameColumnOverlay.Visibility = Visibility.Collapsed;
        }

        private void CancelMergeColumns_Click(object sender, RoutedEventArgs e)
        {
            MergeColumnsOverlay.Visibility = Visibility.Collapsed;
        }

        private void ResetTable_Click(object sender, RoutedEventArgs e)
        {
            if (appManager.CurrentFile == null)
                return;
            appManager.ResetTable();
            ReloadGrid();
        }

        private void SetDataTypeMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ConcatColumnsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not DataGridColumn column)
                return;
            string? columnName = column.Header?.ToString(); // kolonna, kurai tiks pielietota formula
            if (string.IsNullOrWhiteSpace(columnName))
                return;

            LoadCombosForMergeOverlay();

            MergeColumnsOverlay.Visibility = Visibility.Visible;
        }

        private void LoadCombosForMergeOverlay()
        {
            List<ColumnReference> columns = appManager.GetDataColumns() ?? new List<ColumnReference>();

            cmbMergeColumn1.ItemsSource = columns;
            cmbMergeColumn2.ItemsSource = columns;

            cmbMergeColumn1.DisplayMemberPath = "Name";
            cmbMergeColumn2.DisplayMemberPath = "Name";
        }

        private void SaveMergeColumns_Click(object sender, RoutedEventArgs e)
        {
            ColumnReference? first =
                cmbMergeColumn1.SelectedItem as ColumnReference;

            ColumnReference? second =
                cmbMergeColumn2.SelectedItem as ColumnReference;

            if (first == null || second == null)
                return;

            string separator = txtMergeSeparator.Text;

            string resultName = txtMergeResultColumn.Text;

            appManager.MergeColumns(first, second, separator, resultName);
            ReloadGrid();
            MergeColumnsOverlay.Visibility = Visibility.Collapsed;
        }

        private void WriteFormulaMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not DataGridColumn column)
                return;
            string? columnName = column.Header?.ToString(); // kolonna, kurai tiks pielietota formula
            if (string.IsNullOrWhiteSpace(columnName))
                return;
            // izveidot controller vai window, kurā ir combo box vai cts vizuāls elements,
            // kur lietotājs var izvēlēties dataGrid esošo kolonnu nosaukumus, kurus ievietot firmulā.
            // Piemēram, "Price" * "Total_amount"
            // Tajā pašā window ir textbox, kur tiek ievadīta šī vai jebkura cita formula.
            // Window piedāvā dažādas opcijas, piemēram, saglabāt formulu vai atcelt.
            // Ja lietotājs izvēlas saglabāt, tad formula tiek saglabāta konkrētajai kolonnai.

            //if (string.IsNullOrWhiteSpace(formula))
            //    return; // Cancel vai tukšs → atstāj logu
            //appManager.SetColumnFormula(columnName, formula);
            ReloadGrid();
        }

        public void SaveMappingSetToJson()
        {
            string folderPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MappingSets");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                InitialDirectory = folderPath,
                Filter = "JSON files (*.json)|*.json",
                OverwritePrompt = true,
                AddExtension = true,
                DefaultExt = "json",
                FileName = "NewMappingSet.json"
            };

            if (sfd.ShowDialog() == true)
            {
                appManager.SaveMappingSet(sfd.FileName);
            }
        }
        
        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            appManager.UndoLastAction();
            ReloadGrid();
        }
    }
}