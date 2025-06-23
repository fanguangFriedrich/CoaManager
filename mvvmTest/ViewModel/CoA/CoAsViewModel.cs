using mvvmTest.ViewModel.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using Microsoft.Win32;
using System.IO;
using System.Windows.Media.Imaging;
using System.Collections;
using CoATool.Helper;
using System.Windows.Input;
using System.ComponentModel;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System.Windows.Media;
using PipeCommunicationLibrary;

namespace mvvmTest.ViewModel.CoA
{
    public class CoAsViewModel : INotifyPropertyChanged
    {
        private CoAViewModel _SelectCoA;
        private BaseCommand _ouputCommand;
        private BaseCommand _imputCommand;
        private bool _toClose = false;
        private bool _IsPositiveSequence = false;
        private string _iconPath;
        private DatabaseHelper databaseHelper = new DatabaseHelper();
        private string _richText="";
        private ObservableCollection<CoAViewModel> _CoAs= new ObservableCollection<CoAViewModel>();
        private ObservableCollection<CoAViewModel> _FindCoAs = new ObservableCollection<CoAViewModel>();
        private string _SelectFamilyName;
        private PipeServerService pipeServerService;
        public ChartValues<int> SeriesValues { get; set; }=new ChartValues<int>();
        public SeriesCollection PieSeriesCollection { get; set; } = new SeriesCollection();

        public string SelectFamilyName
        {
            get { return _SelectFamilyName; }
            set
            {
                _SelectFamilyName = value;
                OnPropertyChanged("SelectFamilyName");
            }
        }
        public ObservableCollection<CoAViewModel> CoAs
        {
            get { return _CoAs; }
            set
            {
                _CoAs = value;
                OnPropertyChanged("CoAs");
            }
        }
        public ObservableCollection<CoAViewModel> FindCoAs
        {
            get { return _FindCoAs; }
            set
            {
                _FindCoAs = value;
                OnPropertyChanged("FindCoAs");
                PopulateChartData(FindCoAs);
                LoadPieChartData(FindCoAs);
            }
        }
        public ObservableCollection<string> XLabels { get; set; }=new ObservableCollection<string>() {};
        public string RichText
        {
            get => _richText;
            set 
            {
                if(_richText != null)
                {
                    _richText = value;
                    OnPropertyChanged("RichText");
                }
                
            }
        }

        public bool IsPositiveSequence
        {
            get
            {
                return _IsPositiveSequence;
            }
            set
            {
                _IsPositiveSequence = value;
                OnPropertyChanged("IsPositiveSequence");
            }
        }
        public string IconPath
        {
            get { return _iconPath; }
            set
            {
                _iconPath = value;
                OnPropertyChanged("IconPath");
            }
        }
        public CoAViewModel SelectCoA
        {
            get
            {
                if (_SelectCoA == null)
                {
                    _SelectCoA = new CoAViewModel();
                }
                return _SelectCoA;
            }
            set
            {
                _SelectCoA = value;
                OnPropertyChanged("SelectCoA");
                CopyTextNoCopyToClipboard(_SelectCoA);
            }
        }

        public RelayCommand NameCheckCommand { get; }
        public RelayCommand AddNewItemCommand { get; }
        public RelayCommand<CoAViewModel> DeleteItemCommand { get; }
        public RelayCommand<CoAViewModel> PasteCommand { get; }
        public RelayCommand<CoAViewModel> CopyCommand { get; }
        public RelayCommand<CoAViewModel> CollectCommand { get; }
        public RelayCommand<CoAViewModel> ScreenshotCommand { get; }
        public RelayCommand GetSelectFamilyNameCommand { get; }
        public BaseCommand OuputCommand
        {
            get
            {
                if (_ouputCommand == null)
                {
                    _ouputCommand = new BaseCommand(new Action<object>(o =>
                    {
                        OpenFileDialog openFileDialog = new OpenFileDialog();
                        openFileDialog.Title = "选择数据源文件";
                        openFileDialog.Filter = "xml文件|*.xml";
                        openFileDialog.FileName = string.Empty;
                        openFileDialog.FilterIndex = 1;
                        openFileDialog.Multiselect = false;
                        openFileDialog.RestoreDirectory = true;
                        openFileDialog.CheckFileExists = false;
                        openFileDialog.DefaultExt = "txt";
                        if (openFileDialog.ShowDialog() == true)
                        {
                            string selectedFile = openFileDialog.FileName;
                            ReadWriteXmlFileList.WriteToXmlFileList<ObservableCollection<CoAViewModel>>(selectedFile, CoAs, false);
                        }
                    }));
                }
                return _ouputCommand;
            }
        }
        public RelayCommand OuputDatabaseCommand { get; }
        public RelayCommand InputDatabaseCommand { get; }
        public BaseCommand ImputCommand
        {
            get
            {
                if (_imputCommand == null)
                {
                    _imputCommand = new BaseCommand(new Action<object>(o =>
                    {
                        OpenFileDialog openFileDialog = new OpenFileDialog();
                        openFileDialog.Title = "选择数据源文件";
                        openFileDialog.Filter = "xml文件|*.xml";
                        openFileDialog.FileName = string.Empty;
                        openFileDialog.FilterIndex = 1;
                        openFileDialog.Multiselect = false;
                        openFileDialog.RestoreDirectory = true;
                        openFileDialog.CheckFileExists = true;
                        openFileDialog.DefaultExt = "txt";
                        if (openFileDialog.ShowDialog() == true)
                        {
                            string selectedFile = openFileDialog.FileName;
                            CoAs = ReadWriteXmlFileList.ReadFromXmlFileList<ObservableCollection<CoAViewModel>>(selectedFile);
                        }
                    }));
                }
                return _imputCommand;
            }
        }
        public RelayCommand<string> FindCommand { get; }
        public RelayCommand PasteImageCommand { get; }
        public RelayCommand SortByRateCommand { get; }
        public RelayCommand ChangePositiveSequenceCommand { get; }
        public bool ToClose
        {
            get
            {
                return _toClose;
            }
            set
            {
                _toClose = value;
                if (_toClose)
                {
                    OnPropertyChanged("ToClose");
                }
            }
        }

        public CoAsViewModel()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "default.xml");
            if (File.Exists(filePath))
            {
                CoAs = ReadWriteXmlFileList.ReadFromXmlFileList<ObservableCollection<CoAViewModel>>(filePath);
                CoAs = new ObservableCollection<CoAViewModel>(
                    CoAs.GroupBy(coa => coa.Name)
                        .Select(group => group.First()) // 只保留第一个出现的
                );
                SelectCoA = CoAs.FirstOrDefault();
                FindCoAs = CoAs;
            }
            IconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Icon\\find.png");
            DeleteItemCommand = new RelayCommand<CoAViewModel>(DeleteItem);
            PasteCommand = new RelayCommand<CoAViewModel>(PasteText);
            CopyCommand = new RelayCommand<CoAViewModel>(CopyText);
            CollectCommand = new RelayCommand<CoAViewModel>(GetCollect);
            FindCommand = new RelayCommand<string>(Find);
            PasteImageCommand = new RelayCommand(PasteImage);
            SortByRateCommand = new RelayCommand(SortByRate);
            ChangePositiveSequenceCommand = new RelayCommand(ChangePositiveSequence);
            OuputDatabaseCommand =new RelayCommand(OuputDatabase);
            InputDatabaseCommand = new RelayCommand(InputDatabase);
            AddNewItemCommand = new RelayCommand(AddNewItem);
            NameCheckCommand = new RelayCommand(NameCheck);
            ScreenshotCommand = new RelayCommand<CoAViewModel>(CoaScreenshot);
            GetSelectFamilyNameCommand = new RelayCommand(GetSelectFamilyName);
            HandyControl.Controls.Screenshot.Snapped += ScreenshotCaptured;
            pipeServerService = Application.Current.Properties["PipeServerService"] as PipeServerService;
            pipeServerService.RecvCommonFamilyOccurred += (commonFamily) =>
            {
                // 处理接收到的 CommonFamily 数据
                SelectFamilyName = commonFamily.FamilyName;
            };
            //SeriesValues = new ChartValues<ObservablePoint> {new ObservablePoint(2.2, 5.4) ,new ObservablePoint(3.6, 9.6),
            //    new ObservablePoint(9.9, 5.2),
            //    new ObservablePoint(8.1, 4.7),
            //    new ObservablePoint(5.3, 7.1)};

        }

        private void GetSelectFamilyName()
        {
            try
            {
                

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void CoaScreenshot(CoAViewModel tmpCoA)
        {
            if (tmpCoA == null)
            {
                return;
            }
            try
            {
                HandyControl.Controls.Screenshot screenshot = new HandyControl.Controls.Screenshot();
                screenshot.Start();
              
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ScreenshotCaptured(object sender, HandyControl.Data.FunctionEventArgs<System.Windows.Media.ImageSource> e)
        {
            ImageSource targetImageSource = e.Info;

            try
            {
                if (targetImageSource == null)
                {
                    throw new Exception("无法获取截图像数据");
                }

                string savedImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");

                if (SelectCoA != null)
                {
                    savedImagePath = Path.Combine(savedImagePath, $"{SelectCoA.Name}.png");
                }
                else
                {
                    return;
                }

                // 确保保存目录存在
                Directory.CreateDirectory(Path.GetDirectoryName(savedImagePath));

                SaveImageAsPng(targetImageSource, savedImagePath);

                SelectCoA.Name = SelectCoA.Name;

                MessageBox.Show($"图像已保存到: {savedImagePath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"截图失败: {ex.Message}");
            }
            
        }

        private void NameCheck()
        {
            try
            {
                var duplicateItem = CoAs.FirstOrDefault(coa => coa.Name == SelectCoA.Name && coa != SelectCoA);
                if (duplicateItem != null)
                {
                    var messageBox = new AdonisUI.Controls.MessageBoxModel
                    {
                        Text = "发现有重名元素，选择删除哪个CoA",
                        Caption = "警告",
                        Icon = AdonisUI.Controls.MessageBoxImage.Warning,
                        Buttons = new[]
                        {
                            AdonisUI.Controls.MessageBoxButtons.Yes("没有选中的重名CoA"),
                            AdonisUI.Controls.MessageBoxButtons.No("当前选中的CoA"),
                        },

                        IsSoundEnabled = false,
                    };

                    AdonisUI.Controls.MessageBox.Show(messageBox);

                    switch (messageBox.Result)
                    {
                        case AdonisUI.Controls.MessageBoxResult.Yes:
                            CoAs.Remove(duplicateItem);
                            break;
                        default:
                            CoAs.Remove(SelectCoA);
                            SelectCoA = duplicateItem;
                            break;
                    }

                    FindCoAs = CoAs;
                }
               
               
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void AddNewItem()
        {
            try
            {
                CoAs.Add(new CoAViewModel());
                FindCoAs = CoAs;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void OuputDatabase()
        {
            try
            {
                databaseHelper.SaveCoAsToDatabase(CoAs);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void InputDatabase()
        {
            try
            {
                // 1️⃣ 显示加载状态
                Mouse.OverrideCursor = Cursors.Wait;

                // 2️⃣ 在后台线程执行数据库加载，防止 UI 阻塞
                var loadedCoAs = await Task.Run(() => databaseHelper.LoadCoAsFromDatabase());

                // 3️⃣ 更新 ObservableCollection 内容
                CoAs.Clear();
                foreach (var item in loadedCoAs)
                {
                    CoAs.Add(item);
                }

                // 4️⃣ 更新 UI 绑定数据
                FindCoAs = CoAs;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                // 5️⃣ 恢复鼠标状态
                Mouse.OverrideCursor = null;
            }
        }

        private void ChangePositiveSequence()
        {
            try
            {
                IsPositiveSequence = !IsPositiveSequence;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void SortByRate()
        {
            try
            {
                List<CoAViewModel> sortedList;
                if (IsPositiveSequence)
                {
                    sortedList = FindCoAs.OrderBy(coa => coa.Rate).ToList();
                }
                else
                {
                    sortedList = FindCoAs.OrderByDescending(coa => coa.Rate).ToList();
                }
                FindCoAs = new ObservableCollection<CoAViewModel>(sortedList);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void PasteImage()
        {
            try
            {
                if (Clipboard.ContainsData(DataFormats.Bitmap))
                {
                    // 获取剪贴板上的图像
                    BitmapSource image = Clipboard.GetImage();

                    if (image == null)
                    {
                        throw new Exception("无法获取剪贴板图像数据");
                    }

                    string savedImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");

                    if (SelectCoA != null)
                    {
                        savedImagePath = Path.Combine(savedImagePath, $"{SelectCoA.Name}.png");
                    }
                    else
                    {
                        return;
                    }

                    // 确保保存目录存在
                    Directory.CreateDirectory(Path.GetDirectoryName(savedImagePath));

                    SaveImageAsPng(image, savedImagePath);

                    SelectCoA.Name = SelectCoA.Name;

                    MessageBox.Show($"图像已保存到: {savedImagePath}");
                }
                else
                {
                    throw new Exception("剪贴板中没有图像数据");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void SaveImageAsPng(BitmapSource bitmapSource, string filePath)
        {
            using (var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
            {
                var pngEncoder = new PngBitmapEncoder();
                pngEncoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                pngEncoder.Save(fileStream);
            }
        }

        public void SaveImageAsPng(ImageSource imageSource, string filePath)
        {
            BitmapSource bitmapSource = null;

            if (imageSource is BitmapSource bitmap)
            {
                bitmapSource = bitmap;
            }
            else
            {
                // 通用方法：将任何ImageSource渲染到Visual上
                var visual = new DrawingVisual();

                // 获取图像的实际尺寸
                double width = imageSource.Width;
                double height = imageSource.Height;

                // 如果Width/Height为NaN，尝试从其他属性获取尺寸
                if (double.IsNaN(width) || double.IsNaN(height))
                {
                    if (imageSource is DrawingImage drawingImg)
                    {
                        var bounds = drawingImg.Drawing.Bounds;
                        width = bounds.IsEmpty ? 100 : bounds.Width;
                        height = bounds.IsEmpty ? 100 : bounds.Height;
                    }
                    else
                    {
                        // 默认尺寸
                        width = double.IsNaN(width) ? 100 : width;
                        height = double.IsNaN(height) ? 100 : height;
                    }
                }

                // 确保尺寸有效
                if (width <= 0 || double.IsInfinity(width)) width = 100;
                if (height <= 0 || double.IsInfinity(height)) height = 100;

                using (var context = visual.RenderOpen())
                {
                    context.DrawImage(imageSource, new Rect(0, 0, width, height));
                }

                var renderBitmap = new RenderTargetBitmap(
                    (int)Math.Ceiling(width),
                    (int)Math.Ceiling(height),
                    96, 96,
                    PixelFormats.Pbgra32);

                renderBitmap.Render(visual);
                bitmapSource = renderBitmap;
            }

            using (var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
            {
                var pngEncoder = new PngBitmapEncoder();
                pngEncoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                pngEncoder.Save(fileStream);
            }
        }

        private void DeleteItem(CoAViewModel coa)
        {
            // 获取当前索引
            int index = CoAs.IndexOf(coa);

            // 删除指定的 CoA
            CoAs.Remove(coa);

            // 如果删除的是最后一个元素，选择前一个元素
            if (index < CoAs.Count && index >= 0)
            {
                SelectCoA = CoAs[Math.Max(0, index - 1)];
            }
            else
            {
                // 如果删除的是最后一个元素并且列表为空，选择 null 或其他默认值
                SelectCoA = null;
            }

            FindCoAs = CoAs;
        }
        private void PasteText(CoAViewModel tmpCoA)
        {
            string folderPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoAFile");
            string filePath = System.IO.Path.Combine(folderPath, string.Format(@"{0}.txt", tmpCoA.Name));

            try
            {
                // 如果文件夹不存在则创建
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                if (!File.Exists(filePath))
                {
                    using (StreamWriter writer = File.CreateText(filePath))
                    {

                    }
                }

                RichText = Clipboard.GetText();
                File.WriteAllText(filePath, RichText);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CopyText(CoAViewModel tmpCoA)
        {


            try
            {
                string folderPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoAFile");
                string filePath = System.IO.Path.Combine(folderPath, string.Format(@"{0}.txt", tmpCoA.Name));
                // 如果文件夹不存在则创建
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                if (!File.Exists(filePath))
                {
                    using (StreamWriter writer = File.CreateText(filePath))
                    {

                    }
                }

                Clipboard.SetText(File.ReadAllText(filePath));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CopyTextNoCopyToClipboard(CoAViewModel tmpCoA)
        {


            try
            {
                string folderPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoAFile");
                string filePath = System.IO.Path.Combine(folderPath, string.Format(@"{0}.txt", tmpCoA.Name));
                // 如果文件夹不存在则创建
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                if (!File.Exists(filePath))
                {
                    using (StreamWriter writer = File.CreateText(filePath))
                    {

                    }
                }

                // 检查文件是否为空
                FileInfo fileInfo = new FileInfo(filePath);
                if (fileInfo.Length == 0)
                {
                    RichText = "文件为空";
                }
                else
                {
                    RichText = File.ReadAllText(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void GetCollect(CoAViewModel tmpCoA)
        {
            int index = FindCoAs.IndexOf(tmpCoA);

            if (index != -1)
            {

                FindCoAs[index].IsCollect = FindCoAs[index].IsCollect ? false : true;
            }
        }
        private void Find(string tmp)
        {
            var results = new ObservableCollection<CoAViewModel>(
                CoAs.Where(coa => coa.Name.Contains(tmp))
            );

            FindCoAs = results; // 更新数据源，通知界面更新
        }
        public void Close(object sender, System.ComponentModel.CancelEventArgs e)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "default.xml");
            ReadWriteXmlFileList.WriteToXmlFileList<ObservableCollection<CoAViewModel>>(filePath, CoAs, false);
        }

        public void PopulateChartData(IEnumerable<CoAViewModel> coAs)
        {
            // 清空现有数据
            XLabels.Clear();
            SeriesValues.Clear();

            // 按 CompleteTime 的日期部分分组并统计数量
            var groupedByDate = coAs
                .GroupBy(coa => coa.CompleteTime.Date)  // 按日期分组
                .Select(group => new KeyValuePair<DateTime, int>(group.Key, group.Count()))  // 统计每个日期的数量
                .ToList();  // 转换为列表

            // 填充 XLabels 和 SeriesValues
            foreach (var item in groupedByDate)
            {
                // 将 DateTime 转换为字符串并添加到 XLabels
                XLabels.Add(item.Key.ToShortDateString());

                // 添加 int 值到 SeriesValues
                SeriesValues.Add(item.Value);
            }
        }
        public void LoadPieChartData(IEnumerable<CoAViewModel> coAs)
        {
            // 清空旧数据
            PieSeriesCollection.Clear();

            // 按 Culture 进行分组统计
            var groupedData = coAs
                .GroupBy(coa => coa.Culture) // 根据 Culture 分组
                .Select(group => new { Culture = group.Key, Count = group.Count() }) // 统计数量
                .ToList();

            // 将数据添加到 SeriesCollection
            foreach (var data in groupedData)
            {
                PieSeriesCollection.Add(new PieSeries
                {
                    Title = data.Culture.ToString(), // 分组名称
                    Values = new ChartValues<int> { data.Count }, // 统计的数量
                    DataLabels = true // 显示标签
                });
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this,
                    new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
