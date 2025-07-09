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
using System.Windows.Threading;
using System.Diagnostics;
using System.Security.Cryptography;
using ToDoListAlram.Models;
using Microsoft.Win32;
using ToDoListAlram.Models.Services;
using ToDoListAlram.ModelView;
using ToDoListAlram.View.Converters;
using Google.Apis.Sheets.v4.Data;
using System.ComponentModel;
using Google.Apis.Sheets.v4;




namespace ToDoListAlram
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly TodoListService _todoService;
        private bool enableNotify = true;
        private string allowCloseKey = "";
        private MainViewModel mainViewModel;
        private DispatcherTimer timer = new DispatcherTimer();
        private BypassGuard bypassGuard;
        private DateTime _pauseUntil = DateTime.Now;


        public MainWindow()
        {
            string credPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Credentials", "google-sheet-api.json");
            var sheetService = GoogleCredentialProvider.CreateSheetsApi(credPath);
            _todoService = new TodoListService(sheetService);

            InitializeComponent();
            InitializeTodoList();
            InitializeTimer();
            InitializeClosingEvents();
            InitializeBypassGuard();
            this.UpdateStatusLabel();
            this.Loaded += (s, e) =>
            {
                this.BringWindowToFront();
            };
        }

        private void InitializeTodoList()
        {

            this.mainViewModel = new MainViewModel(_todoService);
            this.ReloadTodoList();
        }

        private void InitializeBypassGuard()
        {
            this.bypassGuard = new BypassGuard();
            this.bypassGuard.ResetTodoList(this.mainViewModel.TodoList);
        }


        private void InitializeTimer()
        {
            string intervalArg = "5";
            var args = Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                if (arg.StartsWith("--interval="))
                {
                    intervalArg = arg.Substring("--interval=".Length);
                }
            }
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(Convert.ToInt32(intervalArg));
            timer.Tick += _timer_Tick;
            timer.Start();
        }

        private void _timer_Tick(object? sender, EventArgs e)
        {
            if (enableNotify && mainViewModel.TodoList.Any())
            {
                this.BringWindowToFront();
            }
            else 
            {
                if (DateTime.Now > _pauseUntil)
                {
                    this.ResumeNotify();
                }
            }
            this.UpdateStatusLabel();
        }

        private void InitializeClosingEvents()
        {
            SystemEvents.SessionEnding += SystemEvents_SessionEnding;
            this.Closing += MainWindow_Closing;
        }

        public void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            //MessageBox.Show($"this.bypassGuard.CanClose: {this.bypassGuard.CanClose}");
            if (!this.bypassGuard.CanClose)
            {
                MessageBox.Show("若要關閉此程式，請關機或輸入強制關閉密碼 (yyyyMMddHHm 十一碼的 MD5)，\n並點選強制關閉按鈕");
                e.Cancel = true;
            }
        }

        public void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
        {
            try
            {
                if (e.Reason == SessionEndReasons.SystemShutdown)
                {
                    this.bypassGuard.RequestCloseByShutDown();
                    MessageBox.Show($"System shutdown: {e.Reason}");
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }



        private void BringWindowToFront()
        {
            this.Topmost = true;
            if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
            }
            this.Left = 400;
            this.Top = 300;
            this.Topmost = false;
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = (ComboBoxItem)this.PauseTimeComboBox.SelectedItem;
            int pauseMinute = Convert.ToInt32(selectedItem.Tag.ToString()!);
            string bypassResult = this.bypassGuard.RequestPause(this.BypassKeyInput.Text, pauseMinute);
            if (bypassResult != "OK")
            {

                MessageBox.Show(bypassResult);
                return;
            }
            this.PauseNotify(pauseMinute * 60);
            this.Topmost = false;
        }

        private void ProgramaticCloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.bypassGuard.IsSecondVerifying)
            {
                string requestResult = this.bypassGuard.RequestCloseBySecondVerify(this.BypassKeyInput.Text);
                if (requestResult != "OK")
                {
                    MessageBox.Show(requestResult);
                    return;
                }
                this.Close();
            }
            else
            {
                string requestResult = this.bypassGuard.RequestClose(this.BypassKeyInput.Text);
                if (requestResult != "OK")
                {
                    MessageBox.Show(requestResult);
                    return;
                }
                MessageBox.Show("一階認證成功，請以一階 MD5 的前半段再做一次 MD5 當作密碼");
                this.BypassKeyInput.Text = "";
            }
        }

        private void ReloadDataButton_Click(object sender, RoutedEventArgs e)
        {
            this.ReloadTodoList();
            this.bypassGuard.ResetTodoList(this.mainViewModel.TodoList);
        }


        private void PauseNotify(int seconds)
        {
            this.enableNotify = false;
            this._pauseUntil = DateTime.Now.AddSeconds(seconds);
            this.BypassKeyInput.Text = "";
            this.UpdateStatusLabel();
            this.UpdatePauseLightImage();
            if (seconds == 25 * 60)
            {
                this.ToggleTomatoImage(true);
            }
        }

        private void ResumeNotify()
        {
            enableNotify = true;
            this.BringWindowToFront();
            this.UpdatePauseLightImage();
            this.ToggleTomatoImage(false);
        }

        private void UpdatePauseLightImage()
        {
            string imageFile = this.enableNotify ? "red_light.png" : "green_light.png";
            this.PauseLightImage.Source = new BitmapImage(
                new Uri($"pack://application:,,,/images/{imageFile}")
            );
        }

        private void ToggleTomatoImage(bool show)
        {
            if (show)
            {
                this.TomatoImage.Visibility = Visibility.Visible;
            }
            else
            {
                this.TomatoImage.Visibility = Visibility.Hidden;
            }
        }

        private void UpdateStatusLabel()
        {
            // TODO: refactor to view model
            string pauseUntilString = enableNotify ? "N/A" : _pauseUntil.ToString("hh:mm:ss");
            this.StatusLabel.Content = $"暫停到={pauseUntilString}";
        }

        private void ReloadTodoList()
        {
            this.mainViewModel.LoadTodoList();
            if (mainViewModel.HasError("Load"))
            {
                string message = mainViewModel.GetErrorMessage("Load");
                MessageBox.Show(message);
                return;
            }
            this.TodoDataGrid.ItemsSource = this.mainViewModel.TodoList;
        }

        private void FilterRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (this.mainViewModel == null) // TODO: 改成從 Window_Loaded 事件便免
            {
                Debug.WriteLine("mainViewModel 尚未初始化，跳過");
                return;
            }
            if (sender is RadioButton rb && rb.Tag is string tag)
            {
                var viewingList = this.mainViewModel.GetFilteredList(tag);
                this.TodoDataGrid.ItemsSource = viewingList;
            }
        }

        private void MarkCompleteButton_Click(object sender, RoutedEventArgs e)
        {
            var checkedItems = this.mainViewModel.TodoList
                .Where(row => row.IsChecked)
                .ToList();
            this.mainViewModel.CompleteTodoItems(checkedItems);

            if (mainViewModel.HasError("Update"))
            {
                string message = mainViewModel.GetErrorMessage("Update");
                MessageBox.Show(message);
                return;
            }
            this.ReloadTodoList();
        }
    }
}