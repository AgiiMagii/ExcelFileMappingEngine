using FileMappingEngine.Lib.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Windows.Controls;

namespace FileMappingEngine.Lib
{
    class Helper
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
    }
}
