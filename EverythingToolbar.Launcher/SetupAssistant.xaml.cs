using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using EverythingToolbar.Properties;
using MessageBox = System.Windows.MessageBox;
using RadioButton = System.Windows.Controls.RadioButton;

namespace EverythingToolbar.Launcher
{
    public partial class SetupAssistant
    {
        private readonly string _taskbarShortcutPath = Utils.GetTaskbarShortcutPath();
        private readonly NotifyIcon _icon;
        private const int TotalPages = 3;
        private int _unlockedPages = 1;
        private bool _iconHasChanged;
        private FileSystemWatcher _watcher;

        public SetupAssistant(NotifyIcon icon)
        {
            InitializeComponent();

            _icon = icon;

            AutostartCheckBox.IsChecked = Utils.GetAutostartState();
            HideWindowsSearchCheckBox.IsChecked = !Utils.GetWindowsSearchEnabledState();
            CreateFileWatcher();
            
            if (File.Exists(_taskbarShortcutPath))
            {
                _unlockedPages = Math.Max(3, _unlockedPages);
                Dispatcher.Invoke(() => { SelectPage(2); });
            }

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _icon.Visible = false;

            foreach (RadioButton radio in IconRadioButtons.Children)
            {
                if ((string)radio.Tag == Settings.Default.iconName)
                    radio.IsChecked = true;
            }

            _iconHasChanged = false;
        }

        private void CreateFileWatcher()
        {
            _watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(_taskbarShortcutPath),
                Filter = Path.GetFileName(_taskbarShortcutPath),
                NotifyFilter = NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            _watcher.Created += (source, e) =>
            {
                _iconHasChanged = true;
                _unlockedPages = Math.Max(3, _unlockedPages);
                Dispatcher.Invoke(() => { SelectPage(2); });
            };
            _watcher.Deleted += (source, e) =>
            {
                _unlockedPages = Math.Min(2, _unlockedPages);
                Dispatcher.Invoke(() => { SelectPage(1); });
            };
        }

        private void HideWindowsSearchChanged(object sender, RoutedEventArgs e)
        {
            Utils.SetWindowsSearchEnabledState(HideWindowsSearchCheckBox.IsChecked != null &&
                                               !(bool)HideWindowsSearchCheckBox.IsChecked);
        }

        private void AutostartChanged(object sender, RoutedEventArgs e)
        {
            Utils.SetAutostartState(AutostartCheckBox.IsChecked != null && (bool)AutostartCheckBox.IsChecked);
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            _icon.Visible = true;

            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
            }

            if (!_iconHasChanged)
                return;

            if (MessageBox.Show(Properties.Resources.SetupAssistantRestartExplorerDialogText,
                    Properties.Resources.SetupAssistantRestartExplorerDialogTitle, MessageBoxButton.YesNo) ==
                MessageBoxResult.Yes)
            {
                Utils.ChangeTaskbarPinIcon(Settings.Default.iconName);
            }
        }

        private void OnIconRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            Settings.Default.iconName = ((RadioButton)sender).Tag as string;
            Icon = new BitmapImage(new Uri("pack://application:,,,/" + Settings.Default.iconName));
            _iconHasChanged = true;

            _unlockedPages = Math.Max(2, _unlockedPages);
            UpdatePagination();
        }

        private void SelectPage(int page)
        {
            PaginationTabControl.SelectedIndex = page;
            UpdatePagination();
        }

        private void UpdatePagination()
        {
            PreviousButton.IsEnabled = PaginationTabControl.SelectedIndex > 0;
            NextButton.IsEnabled = PaginationTabControl.SelectedIndex < _unlockedPages - 1;
            PaginationLabel.Text = $"{PaginationTabControl.SelectedIndex + 1} / {TotalPages}";
        }

        private void OnNextPageClicked(object sender, RoutedEventArgs e)
        {
            var nextPage = Math.Min(PaginationTabControl.SelectedIndex + 1, _unlockedPages - 1);
            SelectPage(nextPage);
        }

        private void OnPreviousPageClicked(object sender, RoutedEventArgs e)
        {
            var previousPage = Math.Max(PaginationTabControl.SelectedIndex - 1, 0);
            SelectPage(previousPage);
        }
    }
}