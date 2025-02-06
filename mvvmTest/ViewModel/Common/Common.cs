using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;

namespace mvvmTest.ViewModel.Common
{
    public class CommonFunc
    {
        public static void CopyImageFromFileDialog(string targetFilePath)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "PNG Files (*.png)|*.png";
                openFileDialog.Title = "Select PNG Image File";

                if (openFileDialog.ShowDialog() == true)
                {
                    string sourceFilePath = openFileDialog.FileName;

                    File.Copy(sourceFilePath, targetFilePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while copying file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
