using FileMappingEngine.Lib;
using FileMappingEngine.Lib.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FileMappingEngine.Views
{
    /// <summary>
    /// Interaction logic for FormulaBuilderControl.xaml
    /// </summary>
    public partial class FormulaBuilderControl : UserControl
    {
        private readonly AppManager _appManager;
        private List<ColumnReference>? _columns;
        private readonly string _columnName;
        public FormulaBuilderControl(string columnName, AppManager appManager)
        {
            InitializeComponent();
            _columnName = columnName;
            _appManager = appManager;
        }
        private void FormulaBuilderControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadComboBox();
        }
        private void CancelFormulaButton_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }
        private void SaveFormulaButton_Click(object sender, RoutedEventArgs e)
        {
            string formula = GetFormula();
            _appManager.ApplyFormulaToColumn(_columnName, formula);
            Window.GetWindow(this)?.Close();
        }
        private List<ColumnReference> GetColumns()
        {
            _columns = _appManager.GetDataColumns()
            .Where(c => c.Id != _columnName)
            .ToList();

            return _columns;
        }
        private void LoadComboBox()
        {
            _columns = GetColumns();
            if (_columns != null)
            {
                ColumnSelectorComboBox.ItemsSource = _columns;
                ColumnSelectorComboBox.DisplayMemberPath = "Name";
                ColumnSelectorComboBox.SelectedValuePath = "Id";
            }
        }
        private void SelectedColumnChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ColumnSelectorComboBox.SelectedItem is ColumnReference selectedColumn)
            {
                InsertFormulaText($"[{selectedColumn.Name}]");

                ColumnSelectorComboBox.SelectedItem = null;
            }
        }
        private void FormulaTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string formula = FormulaTextBox.Text;
        }
        private void InsertFormulaText(string text)
        {
            int caret = FormulaTextBox.CaretIndex;

            FormulaTextBox.Text =
                FormulaTextBox.Text.Insert(caret, text);

            FormulaTextBox.CaretIndex = caret + text.Length;

            FormulaTextBox.Focus();
        }
        private string GetFormula()
        {
            string formula = FormulaTextBox.Text.Trim();

            if (!formula.StartsWith("="))
                formula = "=" + formula;

            return formula;
        }
    }
}
