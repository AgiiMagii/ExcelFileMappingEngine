using DocumentFormat.OpenXml.InkML;
using FileMappingEngine.Lib;
using FileMappingEngine.Lib.Database.Entities;
using FileMappingEngine.Lib.Database.Repositories;
using FileMappingEngine.Lib.Helpers;
using FileMappingEngine.Lib.Models;
using FileMappingEngine.Lib.Sessions;
using FileMappingEngine.Resources;
using FileMappingEngine.Views;
using Microsoft.Win32;
using System.ComponentModel;
using System.Data;
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
        // TODO:
        // - Review and refactor the undo functionality
        // - Review reset functionality and consider resetting the header row to default
        // - Refactor RemoveColumns


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

        private async Task LoadFileAsync(string fileName)
        {
            try
            {
                await appManager.OpenFile(fileName);

                if (!appManager.HasFile)
                    return;

                headerRowPanel.IsEnabled = true;

                if (_isFirstLoad)
                {
                    LoadComboBox();
                }

                await GenerateMappingSetButtons();

                ReloadGrid();
            }
            catch (Exception)
            {
                MessageBox.Show(string.Format(UiMessages.Fail_load, UiTerms.File), UiTerms.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task GenerateMappingSetButtons()
        {
            mappingPanel.Children.Clear();

            List<MappingSet> mappings = (await appManager.GetAvailableMappings(appManager.Session!)).ToList();

            foreach (MappingSet mapping in mappings)
            {
                var button = new Button
                {
                    Content = mapping.Name,
                    Tag = mapping.Id
                };

                button.Click += MappingSetButton_Click;
                mappingPanel.Children.Add(button);
            }
        }

        private void LoadComboBox()
        {
            int columnCount = appManager.GetColumnCount();

            CbHeaderRow.Items.Clear();

            for (int i = 1; i <= columnCount; i++)
            {
                CbHeaderRow.Items.Add(i);
            }
        }
        private async void CbHeaderRow_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CbHeaderRow.SelectedIndex < 0 || appManager.Session?.File == null || appManager.Session.File.FileName == null)
                return;

            try
            {
                int headerRowIndex = CbHeaderRow.SelectedIndex + 1;

                await appManager.UpdateHeaderRow(headerRowIndex);
                await GenerateMappingSetButtons();

                ReloadGrid();
            }
            catch (Exception) 
            { 
                MessageBox.Show(string.Format(UiMessages.Fail_change, UiTerms.HeaderRow), UiTerms.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReloadGrid()
        {
            if (appManager.CurrentData == null)
                return;

            helper.ReloadDataGrid(dataGrid, appManager.CurrentData);

            helper.UpdateSelectedColumnHeaders(dataGrid, _selectedColumns);

            if (appManager.Session?.Data?.SortedColumn != null)
            {
                RestoreSort();
            }
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

        private async void ChooseFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (appManager.HasFile)
            {
                MessageBoxResult result = MessageBox.Show(
                        "Vai vēlaties ielādēt citu failu? Pašreizējā tabula tiks izdzēsta.",
                        "Apstiprinājums",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                    return;

                appManager.CloseCurrentFile();
            }

            OpenFileDialog ofd = new()
            {
                Filter = "Excel Files|*.xlsx;*.xls"
            };

            if (ofd.ShowDialog() == true)
            {
                _isFirstLoad = true;
                await LoadFileAsync(ofd.FileName);
                _isFirstLoad = false;

                txtFilePath.Text = appManager?.Session?.File?.FileName;
            }
        }
        private async void MappingSetButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            try
            {
                long id = Convert.ToInt64(button.Tag);

                await appManager.ApplyMappingSetAsync(id);
                ReloadGrid();
            }
            catch (Exception)
            {
                MessageBox.Show(string.Format(UiMessages.Fail_apply, UiTerms.Mapping), UiTerms.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SaveFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (!appManager.HasFile)
            {
                MessageBox.Show(string.Format(UiMessages.Warning_notLoaded, UiTerms.File), UiTerms.Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                SaveFileDialog sfd = new()
                {
                    Filter = "Excel Files|*.xlsx;*.xls",
                    FileName = appManager.CurrentFileName
                };

                if (sfd.ShowDialog() == true &&
                    !string.IsNullOrEmpty(sfd.FileName))
                {
                    appManager.SaveFile(sfd.FileName);

                    MessageBox.Show(string.Format(UiMessages.Success_save, UiTerms.FileCapitalize), UiTerms.Info, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception)
            {
                MessageBox.Show(string.Format(UiMessages.Fail_save, UiTerms.File), UiTerms.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void SaveMappingSet_Click(object sender, RoutedEventArgs e)
        {
            txtFileDefinitionName.Text = appManager.GetFileDefinitionName();

            SaveMappingOverlay.Visibility = Visibility.Visible;
        }
        public async Task SaveMappingSet()
        {
            string fileDefName = txtFileDefinitionName.Text.Trim();
            string mappingName = txtMappingName.Text.Trim();

            if (string.IsNullOrWhiteSpace(fileDefName) || string.IsNullOrWhiteSpace(mappingName))
            {
                MessageBox.Show(string.Format(UiMessages.Warning_fillFielfs), UiTerms.Warning, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            await appManager.SaveMappingSet(fileDefName, mappingName);
        }
        public async void SaveMapping_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await SaveMappingSet();
                SaveMappingOverlay.Visibility = Visibility.Collapsed;
            }
            catch (Exception)
            {
                MessageBox.Show(string.Format(UiMessages.Fail_save, UiTerms.Mapping), UiTerms.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            appManager.UndoLastAction();
            ReloadGrid();
        }
        private void ResetTable_Click(object sender, RoutedEventArgs e)
        {
            if (appManager.Session?.File == null)
                return;
            appManager.ResetTable();
            ReloadGrid();
        }
        private void CancelSaveMapping_Click(object sender, RoutedEventArgs e)
        {
            SaveMappingOverlay.Visibility = Visibility.Collapsed;
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
            MenuItem removeItem = new() { Header = UiMessages.ColumnRemove, Tag = column };
            removeItem.Click += RemoveColumnMenuItem_Click;
            menu.Items.Add(removeItem);

            // Add Column right
            MenuItem addItemRight = new() { Header = UiMessages.ColumnAddAfter, Tag = column };
            addItemRight.Click += AddColumnMenuItem_Click;
            menu.Items.Add(addItemRight);

            // Add Column left
            MenuItem addItemLeft = new() { Header = UiMessages.ColumnAddBefore, Tag = column };
            addItemLeft.Click += AddColumnMenuItem_Click;
            menu.Items.Add(addItemLeft);

            //MenuItem hideItem = new MenuItem { Header = "Hide Column", Tag = column };
            //hideItem.Click += HideColumnMenuItem_Click;
            //menu.Items.Add(hideItem);

            // Rename Column
            MenuItem renameCol = new() { Header = UiMessages.ColumnRename, Tag = column };
            renameCol.Click += RenameColumnMenuItem_Click;
            menu.Items.Add(renameCol);

            // Set Data Type
            MenuItem setFormat = new() { Header = UiMessages.ColumnSetDt, Tag = column };
            setFormat.Click += SetDataTypeMenuItem_Click;
            menu.Items.Add(setFormat);

            MenuItem mergeColumns = new() { Header = UiMessages.ColumnMerge, Tag = column };
            mergeColumns.Click += MergeColumnsMenuItem_Click;
            menu.Items.Add(mergeColumns);

            MenuItem writeFormula = new() { Header = UiMessages.ColumnFormula, Tag = column };
            writeFormula.Click += WriteFormulaMenuItem_Click;
            menu.Items.Add(writeFormula);

            menu.Items.Add(new MenuItem { Header = "-" });

            return menu;
        }
        private ContextMenu CreateColumnHeaderContextMenuMulti(HashSet<string> columns)
        {
            ContextMenu menu = new();

            // Remove Column
            MenuItem removeItem = new () { Header = UiMessages.ColumnRemovePlural, Tag = columns };
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

            try
            {
                appManager.RemoveColumn(columnName);
                ReloadGrid();
            }
            catch (Exception)
            {
                MessageBox.Show(
                    string.Format(UiMessages.Fail_remove, UiTerms.Column),
                    UiTerms.Error,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private void RemoveColumnMenuItems_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not HashSet<string> columns)
                return;

            try
            {
                foreach (var columnName in columns)
                {
                    if (string.IsNullOrEmpty(columnName))
                        continue;

                    appManager.RemoveColumn(columnName);
                }
                ReloadGrid();
                _selectedColumns.Clear();
            }
            catch (Exception)
            {
                MessageBox.Show(
                    string.Format(UiMessages.Fail_remove, UiTerms.ColumnPlural),
                    UiTerms.Error,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private void AddColumnMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not DataGridColumn column)
                return;
            
            string? anchorId = column.Header?.ToString();
            
            ColumnDirection direction = (string)menuItem.Header == UiMessages.ColumnAddBefore
                ? ColumnDirection.Left
                : ColumnDirection.Right;

            if (string.IsNullOrEmpty(anchorId))
                return;
            try
            {
                appManager.AddColumn(direction, anchorId, null);
                ReloadGrid();
            }
            catch (Exception)
            {
                MessageBox.Show(
                    string.Format(UiMessages.Fail_add, UiTerms.Column),
                    UiTerms.Error,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        // Continue from here with code cleanup and optimization
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

            var data = appManager.CurrentData ?? throw new InvalidOperationException("No data available.");

            helper.ReloadDataGrid(dataGrid, data);

            RenameColumnOverlay.Visibility = Visibility.Collapsed;
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
        private void LoadComboForDataTypeOverlay()
        {
            cmbDataType.ItemsSource = Enum.GetValues<DataType>();
            cmbDataType.SelectedIndex = -1;
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

            appManager.SetColumnDataType(columnName, selectedDataType);

            ChangeDataTypeOverlay.Visibility = Visibility.Collapsed;
        }

        private void MergeColumnsMenuItem_Click(object sender, RoutedEventArgs e)
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

            string? columnName = column.Header?.ToString();

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

        private void CancelRenameColumn_Click(object sender, RoutedEventArgs e)
        {
            RenameColumnOverlay.Visibility = Visibility.Collapsed;
        }
        private void CancelMergeColumns_Click(object sender, RoutedEventArgs e)
        {
            MergeColumnsOverlay.Visibility = Visibility.Collapsed;
        }
        private void CancelDataType_Click(object sender, RoutedEventArgs e)
        {
            ChangeDataTypeOverlay.Visibility = Visibility.Collapsed;
        }
    }
}