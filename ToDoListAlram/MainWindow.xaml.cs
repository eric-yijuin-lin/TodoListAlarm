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




namespace ToDoListAlram
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool enableNotify = true;
        private string allowCloseKey = "";
        private MainViewModel mainViewModel;
        private DispatcherTimer timer = new DispatcherTimer();
        private BypassGuard bypassGuard = new BypassGuard();
        private DateTime _pauseUntil = DateTime.Now;


        public MainWindow()
        {
            InitializeComponent();
            InitializeTodoList();
            InitializeTimer();
            InitializeTodoList();
            InitializeClosingEvents();
        }

        private void InitializeTodoList()
        {
            // _todoList = TodoItem.GetTestList();
            mainViewModel = new MainViewModel();
            try
            {
                mainViewModel.LoadTodoList();
            }
            catch (System.Net.Http.HttpRequestException requestEx)
            {
                MessageBox.Show("HTTP 錯誤：" + requestEx.Message);
            }
            catch (System.FormatException formatEx)
            {
                MessageBox.Show("格式錯誤：" + formatEx.Message);
            }
            this.TodoDataGrid.ItemsSource = mainViewModel.TodoList;
            this.UpdateStatusLabel();
            this.Loaded += (s, e) =>
            {
                this.BringWindowToTop();
            };
        }

        private void InitializeTimer()
        {
            TimeSpan interval = TimeSpan.Zero;
            timer = new DispatcherTimer();
#if DEBUG
            timer.Interval = TimeSpan.FromSeconds(5);
#else
            timer.Interval = TimeSpan.FromMinutes(1);
#endif
            timer.Tick += _timer_Tick;
            timer.Start();
        }

        private void _timer_Tick(object? sender, EventArgs e)
        {
            if (enableNotify && mainViewModel.TodoList.Any())
            {
                this.BringWindowToTop();
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



        private void BringWindowToTop()
        {
            this.Topmost = true;
            if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
            }
            this.Topmost = false;
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = (ComboBoxItem)this.PauseTimeComboBox.SelectedItem;
            int pauseMinute = int.Parse(selectedItem.Tag.ToString()!);
            if (pauseMinute > 10)
            {
                if (mainViewModel.HasUrgentTodoItem)
                {
                    MessageBox.Show("待辦清單中有緊急事項，無法暫停超過 10 分鐘");
                    return;
                }
                this.bypassGuard.RequestPause(this.BypassKeyInput.Text);
                if (!this.bypassGuard.CanPause)
                {
                    MessageBox.Show("密碼錯誤 (yyyyMMddHHm 十一碼的 MD5)");
                    return;
                }
            }
            this.PauseNotify(pauseMinute * 60);
            this.Topmost = false;
        }

        private void ProgramaticCloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.bypassGuard.CanClose)
            {
                this.Close();
            }
            else if (this.bypassGuard.IsSecondVerifying)
            {
                bool valid = this.bypassGuard.RequestCloseBySecondVerify(this.BypassKeyInput.Text);
                if (!valid)
                {
                    MessageBox.Show("密碼錯誤，需以一階 MD5 的前半段再做一次 MD5");
                    return;
                }
                this.Close();
            }
            else
            {
                bool valid = this.bypassGuard.RequestClose(this.BypassKeyInput.Text);
                if (!valid)
                {
                    MessageBox.Show("密碼錯誤 (yyyyMMddHHm 十一碼的 MD5)");
                    return;
                }
                MessageBox.Show("一階認證成功，請以一階 MD5 的前半段再做一次 MD5 當作密碼");
            }
        }



        private void PauseNotify(int seconds)
        {
            enableNotify = false;
            _pauseUntil = DateTime.Now.AddSeconds(seconds);
            this.UpdateStatusLabel();
        }

        private void ResumeNotify()
        {
            enableNotify = true;
            this.BringWindowToTop();
        }

        private void UpdateStatusLabel()
        {
            string pauseUntilString = enableNotify ? "N/A" : _pauseUntil.ToString("hh:mm:ss");
            this.StatusLabel.Content = $"狀態：enableNotify={enableNotify},  pauseUntil={pauseUntilString}";
        }
    }
}