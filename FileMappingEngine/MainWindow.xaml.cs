using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using FileMappingEngine.Lib;
using FileMappingEngine.Lib.Models;
using FileMappingEngine.Lib.Services;
using Microsoft.Win32;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FileMappingEngine
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        AppManager appManager = new AppManager();
        FileTemplate template = new FileTemplate();
        Helper helper = new Helper();
        private bool _isMapping = false;
        private bool _isFirstLoad = false;
        private DataTable dt = new DataTable();
        private List<ActionStep> steps = new List<ActionStep>();
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
            helper.ReloadDataGrid(dataGrid, appManager.CurrentData);
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

        private void MappingSetButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string filePath)
                return;

            MappingSet? mappingSet = LoadMappingSet(filePath);

            if (mappingSet == null)
            {
                MessageBox.Show("Neizdevās ielādēt MappingSet.");
                return;
            }

            ApplyMappingSet(mappingSet);
        }
        private MappingSet? LoadMappingSet(string filePath)
        {
            string json = System.IO.File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<MappingSet>(json);
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
                helper.ReloadDataGrid(dataGrid, appManager.CurrentData);
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
            if (appManager.CurrentData == null || appManager.CurrentData.Columns.Count == 0) return;
            if (template.OrderBy == null)
                template.OrderBy = new List<(string ColumnName, ListSortDirection Direction)>();

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

            // Other options
            MenuItem addItemRight = new MenuItem { Header = "Add Column / Right", Tag = column };
            addItemRight.Click += AddColumnMenuItem_Click;
            menu.Items.Add(addItemRight);

            MenuItem addItemLeft = new MenuItem { Header = "Add Column / Left", Tag = column };
            addItemLeft.Click += AddColumnMenuItem_Click;
            menu.Items.Add(addItemLeft);

            //MenuItem hideItem = new MenuItem { Header = "Hide Column", Tag = column };
            //hideItem.Click += HideColumnMenuItem_Click;
            //menu.Items.Add(hideItem);

            MenuItem renameCol = new MenuItem { Header = "Rename Column", Tag = column };
            renameCol.Click += RenameColumnMenuItem_Click;
            menu.Items.Add(renameCol);

            MenuItem setDataType = new MenuItem { Header = "Set Data Type", Tag = column };
            setDataType.Click += SetDataTypeMenuItem_Click;
            menu.Items.Add(setDataType);

            menu.Items.Add(new MenuItem { Header = "-" });

            return menu;
        }

        private void RemoveColumnMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is DataGridColumn column)
            {
                RemoveColumn(column);
            }
        }
        private void RemoveColumn(DataGridColumn column)
        {
            string? columnName = column.Header?.ToString();
            // Remove column from DataGrid
            dataGrid.Columns.Remove(column);

            // Remove column from DataTable
            columnName = column.Header?.ToString();

            if (!string.IsNullOrEmpty(columnName))
            {
                if (dt.Columns.Contains(columnName))
                    dt.Columns.Remove(columnName);
                if (!_isMapping)
                {
                    if (template.RemovedColumns == null)
                        template.RemovedColumns = new();
                    template.RemovedColumns.Add(columnName);
                    steps.Add(new ActionStep
                    {
                        ActionType = "DeleteColumn",
                        ColumnId = columnName,
                        Order = steps.Count + 1
                    });
                }
            }
        }

        private void AddColumnMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is DataGridColumn column)
            {
                AddColumn(menuItem, column);
            }
        }

        private void ExecuteAddColumn(string columnName, string anchorId, string direction)
        {
            int index = 0;

            if (!string.IsNullOrEmpty(anchorId))
            {
                var anchorColumn = dataGrid.Columns
                    .FirstOrDefault(c => c.Header?.ToString() == anchorId);

                if (anchorColumn != null)
                    index = dataGrid.Columns.IndexOf(anchorColumn);

                if (direction == "Right")
                    index += 1;
            }
            else
            {
                index = dataGrid.Columns.Count;
            }

            var newColumn = new DataColumn(columnName, typeof(string));

            dt.Columns.Add(newColumn);
            newColumn.SetOrdinal(index);

            helper.ReloadDataGrid(dataGrid, dt);
        }
        private void AddColumn(MenuItem menuItem, DataGridColumn column)
        {
            string newColumnName = GenerateColumnName();

            string anchorId = column.Header?.ToString();
            string direction = (string)menuItem.Header == "Add Column / Left"
                ? "Left"
                : "Right";

            // 1. RECORD mapping step
            steps.Add(new ActionStep
            {
                ActionType = "AddColumn",
                ColumnId = newColumnName,
                Order = steps.Count + 1,
                Parameters = new Dictionary<string, object>
                {
                    ["AnchorColumnId"] = anchorId,
                    ["Direction"] = direction
                }
            });

            // 2. EXECUTE same logic
            ExecuteAddColumn(newColumnName, anchorId, direction);
        }
        private string GenerateColumnName()
        {
            string baseName = "NewColumn";
            string name;

            int suffix = dt.Columns.Count + 1;

            do
            {
                name = $"{baseName}{suffix}";
                suffix++;
            }
            while (dt.Columns.Contains(name));

            return name;
        }
        private void AddColumn(string columnName, int index)
        {
            var newColumn = new DataColumn(columnName, typeof(string));

            dt.Columns.Add(newColumn);
            newColumn.SetOrdinal(index);

            helper.ReloadDataGrid(dataGrid, dt);
        }
        private void RenameColumnMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not DataGridColumn column)
                return;

            var oldColumnName = column.Header?.ToString();
            if (string.IsNullOrWhiteSpace(oldColumnName)) return;

            // Lietotāja ievads
            string newColumnName;
            do
            {
                newColumnName = Microsoft.VisualBasic.Interaction.InputBox(
                    "Enter new column name:", "Rename Column", oldColumnName);

                if (string.IsNullOrWhiteSpace(newColumnName))
                    return; // Cancel vai tukšs → atstāj logu

                if (dt.Columns.Contains(newColumnName))
                {
                    MessageBox.Show($"Column '{newColumnName}' already exists! Please enter another name.");
                    // Loop turpina, InputBox parādīsies atkal
                }
                else
                {
                    break; // derīgs nosaukums
                }

            } while (true);

            // Update DataTable
            var dataColumn = dt.Columns[oldColumnName];
            if (dataColumn != null)
                dataColumn.ColumnName = newColumnName;

            // Update DataGridColumn Header tieši, nav jāreload viss grid
            column.Header = newColumnName;

            // Update template mapping
            template.ColumnMappings ??= new();
            template.ColumnMappings[oldColumnName] = newColumnName;
            helper.ReloadDataGrid(dataGrid, dt);
        }

        private void SetDataTypeMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SaveMappingSet(string fileName)
        {
            MappingSet mapping = new MappingSet
            {
                Name = fileName,
                HeaderRow = cbHeaderRow.Text != "" ? int.Parse(cbHeaderRow.Text) : 1,
                Steps = steps
            };
            JsonService.CreateJson(mapping, fileName);
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

            bool? result = sfd.ShowDialog();

            if (result == true)
            {
                SaveMappingSet(sfd.FileName);
            }
        }
        private void ApplyMappingSet(MappingSet mappingSet)
        {
            try
            {
                _isMapping = true;

                cbHeaderRow.SelectedItem = mappingSet.HeaderRow;
                foreach (ActionStep step in mappingSet.Steps.OrderBy(s => s.Order))
                {
                    switch (step.ActionType)
                    {
                        case "DeleteColumn":
                            DataGridColumn? colToRemove = dataGrid.Columns.FirstOrDefault(c => c.Header?.ToString() == step.ColumnId);
                            if (colToRemove != null)
                                RemoveColumn(colToRemove);
                            break;
                        case "AddColumn":
                            {
                                string newColumnName = step.ColumnId!;

                                string? anchorId = step.Parameters?["AnchorColumnId"]?.ToString();
                                string? direction = step.Parameters?["Direction"]?.ToString();

                                int index = 0;

                                if (!string.IsNullOrEmpty(anchorId))
                                {
                                    var anchorColumn = dataGrid.Columns
                                        .FirstOrDefault(c => c.Header?.ToString() == anchorId);

                                    if (anchorColumn != null)
                                        index = dataGrid.Columns.IndexOf(anchorColumn);

                                    if (direction == "Right")
                                        index += 1;
                                }
                                else
                                {
                                    index = dataGrid.Columns.Count;
                                }

                                AddColumn(newColumnName, index);

                                break;
                            }
                        case "RenameColumn":
                            // Šeit varētu izsaukt RenameColumn loģiku, ja saglabātu nepieciešamos parametrus
                            break;
                            // Pievieno citas darbības pēc nepieciešamības
                    }
                }
                //dt = _engine.Execute(dt, mappingSet);

                helper.ReloadDataGrid(dataGrid, dt);
            }
            finally
            {
                _isMapping = false;
            }
        }
    }
}