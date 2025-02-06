using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using mvvmTest.ViewModel.CoA;
using mvvmTest.ViewModel.Common;
using System.Windows.Controls.Primitives;
using static System.Net.Mime.MediaTypeNames;
using System.Threading;
using System.Reflection;
using AdonisUI.Controls;

namespace mvvmTest
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : AdonisWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new CoAsViewModel();
            WindowManager.Register<MainWindow>("MainWindow");
            CoAsViewModel tmp = (CoAsViewModel)DataContext;
            Closing += tmp.Close;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsImage())
            {
                var encoder = new PngBitmapEncoder();
                CoAViewModel tmpCoA = listBoxCoAs.SelectedItem as CoAViewModel;
                string SavedImagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                SavedImagePath = System.IO.Path.Combine(SavedImagePath, string.Format(@"{0}.png", tmpCoA.Name));

                encoder.Frames.Add(BitmapFrame.Create(Clipboard.GetImage()));

                using (var stream = new FileStream(SavedImagePath, FileMode.Create))
                {
                    encoder.Save(stream);
                }

                tmpCoA.Name =tmpCoA.Name;
            }
        }

        private void Image_Drop(object sender, DragEventArgs e)
        {
            var MyImage = sender as System.Windows.Controls.Image;
            CoAViewModel tmpCoA =listBoxCoAs.SelectedItem as CoAViewModel;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string sourceFilePath = files[0];
                    string targetPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images"); // 目标路径
                    string destFilePath = System.IO.Path.Combine(targetPath, string.Format(@"{0}.png", tmpCoA.Name));

                    try
                    {
                        // 复制文件
                        File.Copy(sourceFilePath, destFilePath, true);
                        tmpCoA.Name =tmpCoA.Name;
                    }
                    catch (Exception ex)
                    {
                        AdonisUI.Controls.MessageBox.Show($"文件复制失败: {ex.Message}");
                    }
                }
            }
        }

        private void Image_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0 && System.IO.Path.GetExtension(files[0]).ToLower() == ".png")
                {
                    e.Effects = DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
        }

        private void findTB_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AdonisUI.Controls.MessageBox.Show("hell");
            }
            
        }
    }
}
