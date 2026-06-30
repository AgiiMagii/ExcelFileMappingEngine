using FileMappingEngine.Lib.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace FileMappingEngine.Lib
{
    class UiHelper
    {
        private bool _isUpdating = false;
        public void ReloadDataGrid(DataGrid dataGrid, DataTable dt)
        {
            if (_isUpdating) return;

            _isUpdating = true;

            dataGrid.Columns.Clear();
            dataGrid.ItemsSource = null;
            dataGrid.ItemsSource = dt.DefaultView;

            _isUpdating = false;
        }
        public void UpdateSelectedColumnHeaders(DataGrid dataGrid, HashSet<string> selectedColumns)
        {
            foreach (DataGridColumnHeader header in FindVisualChildren<DataGridColumnHeader>(dataGrid))
            {
                string columnName = header.Content?.ToString() ?? "";

                if (selectedColumns.Contains(columnName))
                {
                    header.Background = Brushes.LightBlue;
                }
                else
                {
                    header.ClearValue(DataGridColumnHeader.BackgroundProperty);
                }
            }
        }
        private IEnumerable<T> FindVisualChildren<T>(DependencyObject obj) where T : DependencyObject
        {
            if (obj == null)
                yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);

                if (child is T t)
                    yield return t;

                foreach (var childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }
    }
}
