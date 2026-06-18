using ClosedXML.Excel;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing;
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
        private bool _isFirstLoad = false;
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
            appManager.ApplyMappingSet(filePath);
            helper.ReloadDataGrid(dataGrid, appManager.CurrentData);
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
            MenuItem setDataType = new MenuItem { Header = "Set Data Type", Tag = column };
            setDataType.Click += SetDataTypeMenuItem_Click;
            menu.Items.Add(setDataType);

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
            helper.ReloadDataGrid(dataGrid, appManager.CurrentData);
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
            appManager.AddColumn(direction, anchorId);
            helper.ReloadDataGrid(dataGrid, appManager.CurrentData);
        }
        
        private void RenameColumnMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not DataGridColumn column)
                return;

            string? oldColumnName = column.Header?.ToString();

            if (string.IsNullOrWhiteSpace(oldColumnName))
                return;

            string newColumnName;
            do
            {
                newColumnName = Microsoft.VisualBasic.Interaction.InputBox(
                    "Enter new column name:", "Rename Column", oldColumnName);

                if (string.IsNullOrWhiteSpace(newColumnName))
                    return; // Cancel vai tukšs → atstāj logu

                if (appManager.IsColumnNameTaken(newColumnName))
                {
                    MessageBox.Show($"Column '{newColumnName}' already exists! Please enter another name.");
                    // Loop turpina, InputBox parādīsies atkal
                }
                else
                {
                    break; // derīgs nosaukums
                }

            } while (true);

            appManager.RenameColumn(oldColumnName, newColumnName);
            helper.ReloadDataGrid(dataGrid, appManager.CurrentData);
        }

        private void SetDataTypeMenuItem_Click(object sender, RoutedEventArgs e)
        {

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
        
    }
}