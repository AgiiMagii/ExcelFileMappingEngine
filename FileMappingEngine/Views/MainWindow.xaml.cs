using FileMappingEngine.Lib;
using FileMappingEngine.Lib.Helpers;
using FileMappingEngine.Lib.Models;
using FileMappingEngine.Views;
using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using static FileMappingEngine.Lib.Models.Enums;

namespace FileMappingEngine
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly AppManager appManager;
        private readonly UiHelper helper = new();
        private readonly HashSet<string> _selectedColumns = [];
        private bool _isFirstLoad = false;
        private bool _allowSorting = false;
        private string? _oldColumnName;
        public MainWindow(AppManager appManager)
        {
            InitializeComponent();
            this.appManager = appManager;
        }

        private void LoadFile(string fileName)
        {
            appManager.OpenFile(fileName);

            if (!appManager.HasFile)
                return;

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
            List<string> columns = appManager.GetDataColumns()?.Select(c => c.Name).ToList() ?? new List<string>();
            CbHeaderRow.Items.Clear();
            for (int i = 1; i <= columns.Count; i++)
            {
                CbHeaderRow.Items.Add(i);
            }

        }

        private void GenerateMappingSetButtons()
        {
            using var conn = new Npgsql.NpgsqlConnection("Host=localhost;Port=5432;Username=postgres;Password=GerberaSpotlight;Database=fme");
            conn.Open();
            MessageBox.Show("Connected to PostgreSQL database successfully!");
            mappingPanel.Children.Clear();

            foreach (string mappingPath in appManager.GetExistingMappings())
            {
                Button button = new()
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
            helper.ReloadDataGrid(dataGrid, appManager.Session?.Data?.CurrentData);

            helper.UpdateSelectedColumnHeaders(dataGrid,_selectedColumns);

            if (appManager.Session?.Data?.SortedColumn != null)
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

        private void ChooseFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (appManager.HasFile)
            {
                var session = appManager.Session!;

                if (session.Data.CurrentData.Rows.Count > 0)
                {
                    MessageBoxResult result = MessageBox.Show(
                        "Vai vēlaties ielādēt citu failu? Pašreizējā tabula tiks izdzēsta.",
                        "Apstiprinājums",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.No)
                        return;

                    appManager.CloseCurrentFile(session);
                }
            }

            OpenFileDialog ofd = new()
            {
                Filter = "Excel Files|*.xlsx;*.xls"
            };

            if (ofd.ShowDialog() == true)
            {
                _isFirstLoad = true;
                LoadFile(ofd.FileName);
                _isFirstLoad = false;

                txtFilePath.Text = appManager?.Session?.File?.FileName;
            }
        }

        private void CbHeaderRow_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CbHeaderRow.SelectedIndex >= 0 && appManager.Session?.File != null && appManager.Session.File.FileName != null)
            {
                int headerRowIndex = CbHeaderRow.SelectedIndex + 1;

                appManager.UpdateHeaderRow(headerRowIndex);
                ReloadGrid();
            }
        }

        private void SaveFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (appManager.Session?.File == null)
                return;

            SaveFileDialog sfd = new()
            {
                Filter = "Excel Files|*.xlsx;*.xls",
                FileName = appManager.Session.File.FileName
            };

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

        private void OnDataGridSorting_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if (!_allowSorting)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Vai vēlaties mainīt kārtošanas secību?",
                    "Apstiprinājums",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    e.Handled = true;
                    return;
                }
            }

            _allowSorting = false;

            string columnName = e.Column.Header.ToString() ?? "";

            bool ascending =
                e.Column.SortDirection != ListSortDirection.Ascending;

            appManager.SortData(columnName, ascending);
        }

        private void RestoreSort()
        {
            if (appManager.Session?.Data?.SortedColumn == null)
                return;

            foreach (var column in dataGrid.Columns)
            {
                if (column.Header?.ToString() ==
                    appManager.Session.Data.SortedColumn)
                {
                    column.SortDirection =
                        appManager.Session.Data.SortAscending == true
                        ? ListSortDirection.Ascending
                        : ListSortDirection.Descending;
                }
            }
        }

        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            DataGridColumn column = e.Column;
            e.Column.CanUserSort = true;
            Style style = new(typeof(DataGridColumnHeader), (Style)Application.Current.FindResource(typeof(DataGridColumnHeader)));

            style.Setters.Add(new EventSetter(PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(ColumnHeader_PreviewMouseLeftButtonDown)));
            style.Setters.Add(new EventSetter(ContextMenuOpeningEvent, new ContextMenuEventHandler(ColumnHeader_ContextMenuOpening)));
            column.HeaderStyle = style;
        }
        private void ColumnHeader_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (sender is not DataGridColumnHeader header)
                return;

            if (_selectedColumns.Count > 1)
            {
                header.ContextMenu = CreateColumnHeaderContextMenuMulti(_selectedColumns);
            }
            else
            {
                header.ContextMenu =
                    CreateColumnHeaderContextMenuSingle(
                        header.Column);
            }
        }
        private void ColumnHeader_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DataGridColumnHeader header)
                return;

            Point position = e.GetPosition(header);

            if (position.X >= header.ActualWidth - 5)
                return;

            string? columnName = header.Content?.ToString();

            if (string.IsNullOrWhiteSpace(columnName))
                return;

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                if (!_selectedColumns.Remove(columnName))
                    _selectedColumns.Add(columnName);

                helper.UpdateSelectedColumnHeaders(dataGrid, _selectedColumns);

                e.Handled = true;
                return;
            }

            if (_selectedColumns.Count > 0)
            {
                _selectedColumns.Clear();
                helper.UpdateSelectedColumnHeaders(dataGrid, _selectedColumns);

                e.Handled = true;
                return;
            }
        }

        private ContextMenu CreateColumnHeaderContextMenuSingle(DataGridColumn column)
        {
            ContextMenu menu = new();

            // Remove Column
            MenuItem removeItem = new() { Header = "Remove Column", Tag = column };
            removeItem.Click += RemoveColumnMenuItem_Click;
            menu.Items.Add(removeItem);

            // Add Column right
            MenuItem addItemRight = new() { Header = "Add Column / Right", Tag = column };
            addItemRight.Click += AddColumnMenuItem_Click;
            menu.Items.Add(addItemRight);

            // Add Column left
            MenuItem addItemLeft = new() { Header = "Add Column / Left", Tag = column };
            addItemLeft.Click += AddColumnMenuItem_Click;
            menu.Items.Add(addItemLeft);

            //MenuItem hideItem = new MenuItem { Header = "Hide Column", Tag = column };
            //hideItem.Click += HideColumnMenuItem_Click;
            //menu.Items.Add(hideItem);

            // Rename Column
            MenuItem renameCol = new() { Header = "Rename Column", Tag = column };
            renameCol.Click += RenameColumnMenuItem_Click;
            menu.Items.Add(renameCol);

            // Set Data Type
            MenuItem setFormat = new() { Header = "Set Data Type", Tag = column };
            setFormat.Click += SetDataTypeMenuItem_Click;
            menu.Items.Add(setFormat);

            MenuItem concat = new() { Header = "Merge columns", Tag = column };
            concat.Click += ConcatColumnsMenuItem_Click;
            menu.Items.Add(concat);

            MenuItem writeFormula = new() { Header = "Formula", Tag = column };
            writeFormula.Click += WriteFormulaMenuItem_Click;
            menu.Items.Add(writeFormula);

            menu.Items.Add(new MenuItem { Header = "-" });

            return menu;
        }

        private ContextMenu CreateColumnHeaderContextMenuMulti(HashSet<string> columns)
        {
            ContextMenu menu = new();

            // Remove Column
            MenuItem removeItem = new () { Header = "Remove Column", Tag = columns };
            removeItem.Click += RemoveColumnMenuItems_Click;
            menu.Items.Add(removeItem);

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

        private void RemoveColumnMenuItems_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not HashSet<string> columns)
                return;

            foreach (var columnName in columns)
            {
                if (string.IsNullOrEmpty(columnName))
                    continue;

                appManager.RemoveColumn(columnName);
            }
            ReloadGrid();
            _selectedColumns.Clear();
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

            helper.ReloadDataGrid(dataGrid, appManager.Session?.Data?.CurrentData);

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
            if (appManager.Session?.File == null)
                return;
            appManager.ResetTable();
            ReloadGrid();
        }

        private void SetDataTypeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not DataGridColumn column)
                return;
            string? columnName = column.Header?.ToString();

            if (string.IsNullOrWhiteSpace(columnName))
                return;

            LoadComboForDataTypeOverlay();
            txtDataTypeColumn.Text = columnName;
            ChangeDataTypeOverlay.Visibility = Visibility.Visible;
        }

        private void SaveDataType_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button)
                return;

            if (cmbDataType.SelectedItem is not DataType selectedDataType)
                return;

            string? columnName = txtDataTypeColumn.Text;

            if (string.IsNullOrWhiteSpace(columnName))
                return;

            Type systemType = GetSystemType(selectedDataType);

            appManager.SetColumnDataType(columnName, systemType);

            ChangeDataTypeOverlay.Visibility = Visibility.Collapsed;
        }

        private static Type GetSystemType(DataType dataType)
        {
            return dataType switch
            {
                DataType.Text => typeof(string),
                DataType.Number => typeof(double),
                DataType.Date => typeof(DateTime),
                DataType.Boolean => typeof(bool),

                _ => typeof(object)
            };
        }

        private void CancelDataType_Click(object sender, RoutedEventArgs e)
        {
            ChangeDataTypeOverlay.Visibility = Visibility.Collapsed;
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
            List<ColumnReference> columns = appManager.GetDataColumns() ?? [];

            cmbMergeColumn1.ItemsSource = columns;
            cmbMergeColumn2.ItemsSource = columns;

            cmbMergeColumn1.DisplayMemberPath = "Name";
            cmbMergeColumn2.DisplayMemberPath = "Name";
        }

        private void LoadComboForDataTypeOverlay()
        {
            cmbDataType.ItemsSource = Enum.GetValues<DataType>();
            cmbDataType.SelectedIndex = -1;
        }

        private void SaveMergeColumns_Click(object sender, RoutedEventArgs e)
        {
            if (cmbMergeColumn1.SelectedItem is not ColumnReference first || cmbMergeColumn2.SelectedItem is not ColumnReference second)
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

            FormulaBuilderControl formulaBuilder = new(columnName, appManager);
            Window dialog = new()
            {
                Content = formulaBuilder,
                Title = "Formula Builder",
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                ResizeMode = ResizeMode.NoResize
            };

            if (dialog.ShowDialog() == true)
            {
                ReloadGrid();
            }
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