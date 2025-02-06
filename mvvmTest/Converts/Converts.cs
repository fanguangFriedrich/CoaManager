using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.IO;
using mvvmTest.ViewModel.CoA;
using System.Windows;
using Microsoft.Win32;
using mvvmTest.ViewModel.Common;
using System.Globalization;

namespace mvvmTest.Converts
{
    public class BoolToArrowIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isArrowUp)
            {
                return isArrowUp ? "/Res/up.png" : "/Res/down.png"; // 根据布尔值返回不同图标
            }
            return "/Res/up.png"; // 默认返回向上的箭头图标
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null; // 不需要双向绑定
        }
    }
    public class NameToPhotoPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
            string filePath = Path.Combine(folderPath, string.Format(@"{0}.png", (string)value));

            try
            {
                // 如果文件夹不存在则创建
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                if (!File.Exists(filePath))
                {
                    //CommonFunc.CopyImageFromFileDialog(filePath);
                    File.Copy(Path.Combine(folderPath, "null.png"), filePath, true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // 确保加载到内存
                bitmap.EndInit();
                return bitmap;
            }
        }       

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ListItemToNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return "";
            }
            CoAViewModel coa = (CoAViewModel)value;
            return coa.Name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToPicConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool b = (bool)value;
            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Icon");
            string filePath = Path.Combine(folderPath, "CollectIcon.png");
            BitmapImage bitmap = new BitmapImage();
            if (b)
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;

                try
                {
                    // 如果文件夹不存在则创建
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    if (!File.Exists(filePath))
                    {
                        CopyImageFromFileDialog(filePath);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                
                bitmap.UriSource = new Uri(filePath);
                bitmap.EndInit();
            }          

            return bitmap;
        }

        private void CopyImageFromFileDialog(string targetFilePath)
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

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
