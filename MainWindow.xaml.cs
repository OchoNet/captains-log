using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using CaptainsLog.Models;
using CaptainsLog.Utils;

namespace CaptainsLog
{
    public partial class MainWindow : Window
    {
        // declaring variables to use
        private List<TaskEntry> _tasks = new();
        private string _currentFilePath = string.Empty;
        private bool _isSelectionMode = false;
        private List<CalendarDayButton>? _cachedCalendarButtons = null;


        private bool _isAppActive = true;

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            _isAppActive = true;

            HighlightLogDates();
            DisplayTasks();
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            _isAppActive = false;
        }


        public MainWindow()
        {
            EnsureRequiredFolders(); // create folders if missing
            InitializeComponent();
            LoadTodayLog(); // load today's Log file on launch
        }

        private void LoadTodayLog()
        {
            var today = DateTime.Now;
            var log = new DailyLog { Date = today };

            // make sure Logs folder exists before using it
            string logsFolder = GetLogsFolder();
            if (!Directory.Exists(logsFolder))
                EnsureRequiredFolders();
            _currentFilePath = Path.Combine(logsFolder, log.GetFileName());

            // grab the Tasks from the Log file and show them
            try
            {
                _tasks = CsvHelper.ReadLogFile(_currentFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to read log file:\n{ex.Message}", "Read Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _tasks = new List<TaskEntry>();
            }
            DisplayTasks();
        }

        private void DisplayTasks()
        {
            TaskPanel.Children.Clear(); // reset before drawing

            foreach (var task in _tasks)
            {
                string statusIcon = task.IsComplete ? "✅" : "❌";

                var rowGrid = new Grid
                {
                    Margin = new Thickness(5),
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Task columns
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 0: Checkbox
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // 1: Task Text
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 2: "Hours Spent:"
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 3: Time

                int currentColumn = 0;

                // checkbox for Selection Mode
                if (_isSelectionMode)
                {
                    var selectionCheckbox = new CheckBox
                    {
                        Margin = new Thickness(5, 0, 10, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                        Tag = task
                    };
                    Grid.SetColumn(selectionCheckbox, currentColumn++);
                    rowGrid.Children.Add(selectionCheckbox);
                }
                else
                {
                    currentColumn++; // skip column 0
                }

                // Task name with status (✅ or ❌)
                var label = new Label
                {
                    Content = $"{statusIcon} {task}",
                    Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(label, currentColumn++);
                rowGrid.Children.Add(label);

                // label before the time
                var hoursLabel = new TextBlock
                {
                    Text = "Hours Spent:",
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(hoursLabel, currentColumn++);
                rowGrid.Children.Add(hoursLabel);

                // editable or static time display
                if (_isSelectionMode)
                {
                    var timeBox = new TextBox
                    {
                        Text = task.Time,
                        Width = 60,
                        Tag = task,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    timeBox.PreviewTextInput += (s, e) =>
                    {
                        e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, @"[\d\.]");
                    };

                    Grid.SetColumn(timeBox, currentColumn);
                    rowGrid.Children.Add(timeBox);
                }
                else
                {
                    var timeDisplay = new TextBlock
                    {
                        Text = task.Time,
                        Width = 60,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextAlignment = TextAlignment.Right
                    };
                    Grid.SetColumn(timeDisplay, currentColumn);
                    rowGrid.Children.Add(timeDisplay);
                }

                rowGrid.ContextMenu = CreateContextMenu(task);
                TaskPanel.Children.Add(rowGrid);
            }
        }

        private ContextMenu CreateContextMenu(TaskEntry task)
        {
            var contextMenu = new ContextMenu();

            // complete Task
            var markComplete = new MenuItem { Header = "Mark as Complete" };
            markComplete.Click += (s, e) =>
            {
                task.IsComplete = true;
                try
                {
                    CsvHelper.WriteLogFile(_currentFilePath, _tasks);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to write log file:\n{ex.Message}", "Write Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                DisplayTasks();
            };

            // un-complete Task
            var markIncomplete = new MenuItem { Header = "Mark as Incomplete" };
            markIncomplete.Click += (s, e) =>
            {
                task.IsComplete = false;
                try
                {
                    CsvHelper.WriteLogFile(_currentFilePath, _tasks);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to write log file:\n{ex.Message}", "Write Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                DisplayTasks();
            };

            // delete this Task from the Log file
            var deleteItem = new MenuItem { Header = "Delete Task" };
            deleteItem.Click += (s, e) =>
            {
                var result = MessageBox.Show($"Delete task \"{task.Title}\"?", "Confirm Delete", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    _tasks.Remove(task);
                    try
                    {
                        CsvHelper.WriteLogFile(_currentFilePath, _tasks);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to write log file:\n{ex.Message}", "Write Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    DisplayTasks();
                }
            };

            contextMenu.Items.Add(markComplete);
            contextMenu.Items.Add(markIncomplete);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(deleteItem);

            return contextMenu;
        }

        // Calendar coloring and handling
        private void LogCalendar_Loaded(object sender, RoutedEventArgs e)
        {
            HighlightLogDates();
        }

        private void LogCalendar_DisplayDateChanged(object sender, CalendarDateChangedEventArgs e)
        {
            HighlightLogDates();
        }

        private void LogCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LogCalendar.SelectedDate is DateTime selectedDate)
            {
                // change the Log file path to match selected date
                string logsFolder = GetLogsFolder();
                if (!Directory.Exists(logsFolder))
                    EnsureRequiredFolders();

                var log = new DailyLog { Date = selectedDate };
                _currentFilePath = Path.Combine(logsFolder, log.GetFileName());


                // reload and redraw
                try
                {
                    _tasks = CsvHelper.ReadLogFile(_currentFilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to read log file:\n{ex.Message}", "Read Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    _tasks = new List<TaskEntry>();
                }
                DisplayTasks();
            }
        }

        private void HighlightLogDates()
        {
            if (!_isAppActive) return;
            _cachedCalendarButtons = null;
            var calendar = LogCalendar;
            if (calendar == null) return; // safety guard

            var logsFolder = GetLogsFolder();
            if (!Directory.Exists(logsFolder))
                EnsureRequiredFolders();

            // Gather all known log dates (just by filename)
            string[] logFiles;
            try
            {
                logFiles = Directory.GetFiles(logsFolder, "Log*.csv");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to read logs directory:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var logDates = new HashSet<DateTime>();

            foreach (var file in logFiles)
            {
                var filename = Path.GetFileNameWithoutExtension(file);
                if (DateTime.TryParseExact(filename.Replace("Log", ""), "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var date))
                {
                    logDates.Add(date.Date);
                }
            }

            // Delay styling until buttons are generated
            calendar.Dispatcher.InvokeAsync(() =>
            {
                if (_cachedCalendarButtons == null)
                {
                    _cachedCalendarButtons = FindVisualChildren<CalendarDayButton>(calendar).ToList();
                }

                foreach (var button in _cachedCalendarButtons)
                {
                    if (button.DataContext is DateTime date)
                    {
                        if (logDates.Contains(date.Date))
                        {
                            button.Background = new SolidColorBrush(Colors.LightGreen);
                            button.ToolTip = "Log file exists for this day";
                        }
                        else
                        {
                            button.ClearValue(Button.BackgroundProperty);
                            button.ToolTip = null;
                        }
                    }
                }
            });
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject? depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    var child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T t)
                    {
                        yield return t;
                    }

                    foreach (var childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        // save Log file
        private void SaveLog()
        {
            // go row by row and update time fields
            foreach (var child in TaskPanel.Children)
            {
                if (child is Grid row)
                {
                    TaskEntry? task = null;
                    TextBox? timeBox = null;

                    foreach (var element in row.Children)
                    {
                        if (element is CheckBox cb && cb.Tag is TaskEntry taggedTask)
                        {
                            task = taggedTask;
                        }
                        else if (element is TextBox tb && tb.Tag is TaskEntry)
                        {
                            timeBox = tb;
                        }
                    }

                    // update time value
                    if (task != null && timeBox != null)
                    {
                        task.Time = timeBox.Text;
                    }
                }
            }

            // save Log File
            try
            {
                CsvHelper.WriteLogFile(_currentFilePath, _tasks);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to write log file:\n{ex.Message}", "Write Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            MessageBox.Show("Log saved.", "Save", MessageBoxButton.OK, MessageBoxImage.Information);
            ExitSelectionMode();
            DisplayTasks();
        }

        // manual refresh button
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadTodayLog();
        }

        // save Log button
        private void SaveLog_Click(object sender, RoutedEventArgs e)
        {
            SaveLog();
            ExitSelectionMode();
            DisplayTasks();
        }

        // user added new Tasks to the input area
        private void AddTasks_Click(object sender, RoutedEventArgs e)
        {
            var lines = NewTasksInput.Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) return;

            bool fileJustCreated = !File.Exists(_currentFilePath);

            foreach (var raw in lines)
            {
                string account = "MISC";
                double time = 0;
                var line = raw.Trim();

                // grab account tag
                var accountMatch = Regex.Match(line, @"#([A-Za-z0-9_]+)");
                if (accountMatch.Success)
                {
                    account = accountMatch.Groups[1].Value.ToUpper();
                    line = line.Replace(accountMatch.Value, "").Trim();
                }

                // grab time tag
                var timeMatch = Regex.Match(line, @"@(\d+(\.\d{1,2})?)");
                if (timeMatch.Success)
                {
                    if (double.TryParse(timeMatch.Groups[1].Value, out double parsed))
                        time = parsed;

                    line = line.Replace(timeMatch.Value, "").Trim();
                }

                // create new Task with what we parsed
                var newTask = new TaskEntry
                {
                    IsComplete = false,
                    Title = line,
                    Time = time.ToString("0.##"),
                    Account = account
                };

                _tasks.Add(newTask);
            }

            NewTasksInput.Text = string.Empty;
            try
            {
                CsvHelper.WriteLogFile(_currentFilePath, _tasks);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to write log file:\n{ex.Message}", "Write Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            DisplayTasks(); // redraw everything

            if (fileJustCreated)
            {
                MessageBox.Show(
                    $"A new Log file was created for {LogCalendar.SelectedDate?.ToString("MMMM dd, yyyy") ?? "today"}.",
                    "New Log File Created",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        // turns Selection Mode on
        private void SelectionModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            _isSelectionMode = true;
            SaveButton.Visibility = Visibility.Visible;
            DeleteButton.Visibility = Visibility.Visible;
            StatusButton.Visibility = Visibility.Visible;
            RefreshButton.Visibility = Visibility.Collapsed;
            SelectAllCheckbox.Visibility = Visibility.Visible;
            SelectAllCheckbox.IsChecked = false;                // Reset on entry
            DisplayTasks();
        }

        // select all Tasks
        private void SelectAllCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            SetAllTaskCheckboxes(true);
        }

        // de-select all Tasks
        private void SelectAllCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            SetAllTaskCheckboxes(false);
        }

        // select or de-select all Tasks
        private void SetAllTaskCheckboxes(bool isChecked)
        {
            foreach (var child in TaskPanel.Children)
            {
                if (child is Grid row)
                {
                    foreach (var element in row.Children)
                    {
                        if (element is CheckBox cb && cb.Tag is TaskEntry)
                        {
                            cb.IsChecked = isChecked;
                        }
                    }
                }
            }
        }

        // turns Selection Mode off
        private void SelectionModeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            _isSelectionMode = false;
            SaveButton.Visibility = Visibility.Collapsed;
            DeleteButton.Visibility = Visibility.Collapsed;
            StatusButton.Visibility = Visibility.Collapsed;
            RefreshButton.Visibility = Visibility.Visible;
            SelectAllCheckbox.Visibility = Visibility.Collapsed;
            SelectAllCheckbox.IsChecked = false;                // Reset on entry
            DisplayTasks();
        }

        // helper to disable Selection Mode
        private void ExitSelectionMode()
        {
            _isSelectionMode = false;
            SelectionModeToggle.IsChecked = false;
            SaveButton.Visibility = Visibility.Collapsed;
            DeleteButton.Visibility = Visibility.Collapsed;
            StatusButton.Visibility = Visibility.Collapsed;
            RefreshButton.Visibility = Visibility.Visible;
        }

        // flips Task status (✅ and ❌) for selected Tasks
        private void ToggleStatus_Click(object sender, RoutedEventArgs e)
        {
            bool updated = false;

            foreach (var child in TaskPanel.Children)
            {
                if (child is Grid row)
                {
                    foreach (var element in row.Children)
                    {
                        if (element is CheckBox cb && cb.Tag is TaskEntry task && cb.IsChecked == true)
                        {
                            task.IsComplete = !task.IsComplete;
                            updated = true;
                        }
                    }
                }
            }

            if (updated)
            {
                try
                {
                    CsvHelper.WriteLogFile(_currentFilePath, _tasks);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to write log file:\n{ex.Message}", "Write Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                DisplayTasks();
                ExitSelectionMode();
            }
            else
            {
                MessageBox.Show("No tasks selected to toggle status.", "Status", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // deletes selected Tasks (after confirm)
        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            var toRemove = new List<TaskEntry>();

            foreach (var child in TaskPanel.Children)
            {
                if (child is Grid row)
                {
                    foreach (var element in row.Children)
                    {
                        if (element is CheckBox cb && cb.Tag is TaskEntry task && cb.IsChecked == true)
                        {
                            toRemove.Add(task);
                        }
                    }
                }
            }

            if (toRemove.Count == 0)
            {
                MessageBox.Show("No tasks selected for deletion.", "Delete Tasks", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete {toRemove.Count} task(s)?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                foreach (var task in toRemove)
                    _tasks.Remove(task);

                try
                {
                    CsvHelper.WriteLogFile(_currentFilePath, _tasks);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to write log file:\n{ex.Message}", "Write Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                DisplayTasks();
                ExitSelectionMode();
            }
        }

        // helpers to get folders
        private string GetLogsFolder() => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "Captains_Log", "Logs");

        private string GetSummariesFolder() => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "Captains_Log", "Summaries");

        // get unique file path
        private string GetUniqueFilePath(string basePath)
        {
            if (!File.Exists(basePath)) return basePath;

            string directory = Path.GetDirectoryName(basePath)!;
            string filename = Path.GetFileNameWithoutExtension(basePath);
            string extension = Path.GetExtension(basePath);
            int counter = 1;

            string newPath;
            do
            {
                newPath = Path.Combine(directory, $"{filename} ({counter}){extension}");
                counter++;
            } while (File.Exists(newPath));

            return newPath;
        }

        // generate weekly Summary file
        private void ViewWeeklySummary_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(GetLogsFolder()) || !Directory.Exists(GetSummariesFolder()))
                EnsureRequiredFolders();

            var logDir = GetLogsFolder();
            var summaryDir = GetSummariesFolder();
            Directory.CreateDirectory(summaryDir);

            var oneWeekAgo = DateTime.Now.Date.AddDays(-6);
            var today = DateTime.Now.Date;
            var summaryPath = GetUniqueFilePath(
                Path.Combine(summaryDir, $"WeeklySummary_{DateTime.Now:yyyyMMdd}.txt"));


            var allTasks = new List<(DateTime, TaskEntry)>();

            for (var date = oneWeekAgo; date <= today; date = date.AddDays(1))
            {
                var file = Path.Combine(logDir, $"Log{date:yyyyMMdd}.csv");
                if (File.Exists(file))
                {
                    var tasks = CsvHelper.ReadLogFile(file);
                    allTasks.AddRange(tasks.Select(t => (date, t)));
                }
            }

            var grouped = allTasks
                .GroupBy(t => t.Item1)
                .OrderBy(g => g.Key)
                .ToList();

            using var writer = new StreamWriter(summaryPath);
            writer.WriteLine($"Weekly Summary ({oneWeekAgo:MMM dd} – {today:MMM dd, yyyy})\n");

            double totalHours = 0;

            foreach (var group in grouped)
            {
                writer.WriteLine($"{group.Key:dddd, MMM dd yyyy}");
                foreach (var (date, task) in group)
                {
                    if (double.TryParse(task.Time, out double hours))
                        totalHours += hours;

                    writer.WriteLine($"  - [{(task.IsComplete ? "✔" : " ")}] {task.Title} ({task.Account}) — {task.Time}h");
                }
                writer.WriteLine();
            }

            // ✅ Add total hours to the bottom
            writer.WriteLine($"TOTAL HOURS THIS WEEK: {totalHours:0.##}h");

            MessageBox.Show($"Weekly summary saved:\n{summaryPath}", "Summary Created", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // generate account Summary file (this or last month only)
        private void ViewAccountSummary_Click(object sender, RoutedEventArgs e)
        {
            // prompt user for which month to summarize
            var result = MessageBox.Show(
                "Generate summary for this month?\nClick 'No' for last month.",
                "Choose Month",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel)
                return;

            var today = DateTime.Now.Date;
            DateTime monthStart, monthEnd;

            // figure out date range based on user choice
            if (result == MessageBoxResult.Yes)
            {
                monthStart = new DateTime(today.Year, today.Month, 1);
                monthEnd = monthStart.AddMonths(1).AddDays(-1);
            }
            else
            {
                var lastMonth = today.AddMonths(-1);
                monthStart = new DateTime(lastMonth.Year, lastMonth.Month, 1);
                monthEnd = monthStart.AddMonths(1).AddDays(-1);
            }

            // get folders and file path
            if (!Directory.Exists(GetLogsFolder()) || !Directory.Exists(GetSummariesFolder()))
                EnsureRequiredFolders();

            var logDir = GetLogsFolder();
            var summaryDir = GetSummariesFolder();
            Directory.CreateDirectory(summaryDir);
            var summaryPath = GetUniqueFilePath(
                Path.Combine(summaryDir, $"AccountSummary_{DateTime.Now:yyyyMMdd}.txt"));

            // grab all Tasks from the selected month
            var allTasks = new List<TaskEntry>();
            for (var date = monthStart; date <= monthEnd; date = date.AddDays(1))
            {
                var file = Path.Combine(logDir, $"Log{date:yyyyMMdd}.csv");
                if (File.Exists(file))
                {
                    var tasks = CsvHelper.ReadLogFile(file);
                    allTasks.AddRange(tasks);
                }
            }

            // group Tasks by Account name (uppercase)
            var grouped = allTasks
                .GroupBy(t => t.Account.ToUpper())
                .OrderBy(g => g.Key)
                .ToList();

            // write the summary file
            using var writer = new StreamWriter(summaryPath);
            writer.WriteLine($"Account Summary ({monthStart:MMM yyyy})\n");

            double grandTotal = 0;

            foreach (var group in grouped)
            {
                // sum up hours for this Account
                double accountTotal = group
                    .Select(t => double.TryParse(t.Time, out var hrs) ? hrs : 0)
                    .Sum();

                grandTotal += accountTotal;

                writer.WriteLine($"[{group.Key}] — {accountTotal:0.##}h");

                foreach (var task in group)
                {
                    writer.WriteLine($"  - [{(task.IsComplete ? "✔" : " ")}] {task.Title} — {task.Time}h");
                }

                writer.WriteLine();
            }

            // show total across all Accounts
            writer.WriteLine($"TOTAL HOURS THIS MONTH: {grandTotal:0.##}h");

            MessageBox.Show($"Account summary saved:\n{summaryPath}", "Summary Created", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // create folders if missing
        private void EnsureRequiredFolders()
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string rootFolder = Path.Combine(desktop, "Captains_Log");
            string logsFolder = Path.Combine(rootFolder, "Logs");
            string summariesFolder = Path.Combine(rootFolder, "Summaries");

            bool isFirstTimeSetup = !Directory.Exists(rootFolder);

            Directory.CreateDirectory(rootFolder);
            Directory.CreateDirectory(logsFolder);
            Directory.CreateDirectory(summariesFolder);

            if (isFirstTimeSetup)
            {
                var welcomeWindow = new CaptainsLog.Views.WelcomeDialog();
                welcomeWindow.ShowDialog();
            }
        }

    }
}