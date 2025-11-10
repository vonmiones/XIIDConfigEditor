using Microsoft.Win32;
using System.IO;
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
using Path = System.IO.Path;

namespace XIIDConfigEditor
{
    public partial class MainWindow : Window
    {
        private string currentFilePath;
        private bool isModified = false;

        // INI data
        private Dictionary<string, Dictionary<string, string>> iniData = new();

        // Mapping of section to editable items
        private Dictionary<string, List<IniItem>> tabItems = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        #region File Handling

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "INI files (*.ini)|*.ini" };
            if (dlg.ShowDialog() == true)
                LoadIniFile(dlg.FileName);
        }

        public void LoadIniFile(string path)
        {
            currentFilePath = path;
            iniData = ParseIni(File.ReadAllLines(path));
            RenderTabs();
            isModified = false;
            Title = $"INI Editor - {Path.GetFileName(path)}";
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            if (currentFilePath == null)
            {
                SaveAsFile_Click(sender, e);
                return;
            }

            BackupFile();
            SaveIni(currentFilePath);
            isModified = false;
            MessageBox.Show("Configuration saved successfully!", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveAsFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog { Filter = "INI files (*.ini)|*.ini" };
            if (dlg.ShowDialog() == true)
            {
                currentFilePath = dlg.FileName;
                SaveIni(currentFilePath);
                isModified = false;
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (isModified)
            {
                var res = MessageBox.Show("You have unsaved changes. Save before closing?",
                    "Unsaved Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

                if (res == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
                else if (res == MessageBoxResult.Yes)
                {
                    SaveFile_Click(sender, null);
                }
            }
        }

        private void BackupFile()
        {
            if (currentFilePath == null || !File.Exists(currentFilePath)) return;

            var dir = Path.GetDirectoryName(currentFilePath);
            var backupDir = Path.Combine(dir, "BackupConfigurations");
            Directory.CreateDirectory(backupDir);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFile = Path.Combine(backupDir,
                $"{Path.GetFileNameWithoutExtension(currentFilePath)}_{timestamp}.ini");
            File.Copy(currentFilePath, backupFile, true);
        }

        #endregion

        #region INI Parsing and Saving

        private Dictionary<string, Dictionary<string, string>> ParseIni(string[] lines)
        {
            var dict = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            string currentSection = null;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";")) continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line[1..^1];
                    dict[currentSection] = new Dictionary<string, string>();
                }
                else if (currentSection != null && line.Contains('='))
                {
                    var parts = line.Split('=', 2);
                    dict[currentSection][parts[0].Trim()] = parts[1].Trim();
                }
            }
            return dict;
        }

        private void SaveIni(string path)
        {
            var sb = new StringBuilder();
            foreach (var section in iniData)
            {
                sb.AppendLine($"[{section.Key}]");
                foreach (var kv in section.Value)
                    sb.AppendLine($"{kv.Key}={kv.Value}");
                sb.AppendLine();
            }
            File.WriteAllText(path, sb.ToString());
        }

        #endregion

        #region Rendering Tabs

        private void RenderTabs()
        {
            SectionsTab.Items.Clear();
            tabItems.Clear();

            foreach (var section in iniData)
            {
                var tab = new TabItem();

                // Tab header with close button
                var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
                headerPanel.Children.Add(new TextBlock { Text = section.Key, Margin = new Thickness(0, 0, 5, 0) });

                var closeBtn = new Button
                {
                    Content = "✖",
                    Width = 18,
                    Height = 18,
                    Padding = new Thickness(0),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Foreground = Brushes.Gray,
                    Tag = section.Key
                };
                closeBtn.Click += DeleteSection_Click;
                headerPanel.Children.Add(closeBtn);

                tab.Header = headerPanel;

                // Main panel
                var mainPanel = new DockPanel { Margin = new Thickness(10) };

                // Add key button
                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0, 0, 0, 5)
                };

                var addBtn = new Button
                {
                    Content = "➕ Add Item",
                    Background = Brushes.LightGreen,
                    Padding = new Thickness(8, 4, 8, 4),
                    Margin = new Thickness(0, 0, 10, 0),
                    Tag = section.Key
                };
                addBtn.Click += AddKey_Click;
                buttonPanel.Children.Add(addBtn);
                DockPanel.SetDock(buttonPanel, Dock.Top);
                mainPanel.Children.Add(buttonPanel);

                // DataGrid
                var grid = new DataGrid
                {
                    AutoGenerateColumns = false,
                    CanUserAddRows = false,
                    Margin = new Thickness(0),
                    HeadersVisibility = DataGridHeadersVisibility.Column,
                    IsReadOnly = false
                };

                // Delete column
                var deleteColumn = new DataGridTemplateColumn { Header = "" };
                var deleteTemplate = new DataTemplate();
                var buttonFactory = new FrameworkElementFactory(typeof(Button));
                buttonFactory.SetValue(Button.ContentProperty, "❌");
                buttonFactory.SetValue(Button.BackgroundProperty, Brushes.Transparent);
                buttonFactory.SetValue(Button.BorderThicknessProperty, new Thickness(0));
                buttonFactory.SetValue(Button.ForegroundProperty, Brushes.Red);
                buttonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(DeleteItem_Click));
                deleteTemplate.VisualTree = buttonFactory;
                deleteColumn.CellTemplate = deleteTemplate;
                deleteColumn.Width = 40;
                grid.Columns.Add(deleteColumn);

                // Key column
                grid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Key",
                    Binding = new System.Windows.Data.Binding("Key"),
                    IsReadOnly = true,
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                });

                // Value column
                grid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Value",
                    Binding = new System.Windows.Data.Binding("Value"),
                    Width = new DataGridLength(2, DataGridLengthUnitType.Star)
                });

                // Bind items
                var items = section.Value.Select(kv =>
                {
                    var item = new IniItem { Key = kv.Key, Value = kv.Value };
                    item.OnValueChanged += (s, e) =>
                    {
                        iniData[section.Key][item.Key] = item.Value;
                        AutoSaveIni(); // <-- call auto-save here
                    };
                    return item;
                }).ToList();


                tabItems[section.Key] = items;
                grid.ItemsSource = items;

                mainPanel.Children.Add(grid);
                tab.Content = mainPanel;

                SectionsTab.Items.Add(tab);
            }
        }

        #endregion

        private void AutoSaveIni()
        {
            if (string.IsNullOrEmpty(currentFilePath)) return;

            // Backup latest version
            BackupFile();

            // Save current iniData to file
            SaveIni(currentFilePath);

            isModified = false; // mark as saved
        }


        #region CRUD

        private void AddSection_Click(object sender, RoutedEventArgs e)
        {
            var name = Microsoft.VisualBasic.Interaction.InputBox("Enter new section name:", "Add Section");
            if (string.IsNullOrWhiteSpace(name)) return;
            if (iniData.ContainsKey(name))
            {
                MessageBox.Show("Section already exists!");
                return;
            }

            iniData[name] = new Dictionary<string, string>();
            RenderTabs();
            SectionsTab.SelectedItem = SectionsTab.Items.Cast<TabItem>()
                .FirstOrDefault(t => ((StackPanel)t.Header).Children.OfType<TextBlock>().First().Text == name);
            isModified = true;
            AutoSaveIni();
        }

        private void AddKey_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            var section = btn.Tag.ToString();
            var key = Microsoft.VisualBasic.Interaction.InputBox("Enter key name:", "Add Key");
            if (string.IsNullOrWhiteSpace(key)) return;
            var val = Microsoft.VisualBasic.Interaction.InputBox("Enter value:", "Add Value");

            iniData[section][key] = val;
            RenderTabs();
            isModified = true;
            AutoSaveIni();
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            var grid = FindParent<DataGrid>(btn);
            if (grid == null) return;
            var item = grid.SelectedItem as IniItem;
            if (item == null) return;

            var sectionName = ((StackPanel)((TabItem)SectionsTab.SelectedItem).Header).Children.OfType<TextBlock>().First().Text;
            if (MessageBox.Show($"Delete key '{item.Key}'?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                iniData[sectionName].Remove(item.Key);
                RenderTabs();
                isModified = true;

                AutoSaveIni(); // <-- call auto-save here
            }
        }


        private void DeleteSection_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            var section = btn.Tag.ToString();
            if (MessageBox.Show($"Delete section [{section}]?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                iniData.Remove(section);
                RenderTabs();
                isModified = true;

                AutoSaveIni(); // <-- call auto-save here
            }
        }


        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parentObject = System.Windows.Media.VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindParent<T>(parentObject);
        }

        #endregion
    }
}