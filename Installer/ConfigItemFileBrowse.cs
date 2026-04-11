using CFIT.Installer.Product;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Installer
{
    public class ConfigItemFileBrowse : CFIT.Installer.UI.Config.ConfigItem
    {
        public virtual string Text { get; set; } = "";
        public virtual string Filter { get; set; } = "All files (*.*)|*.*";
        public virtual string DefaultFileName { get; set; } = "";
        public virtual string InitialDirectory { get; set; } = "";
        public virtual Func<string, string> ValidationFunc { get; set; }

        protected virtual StackPanel Panel { get; set; }
        protected virtual TextBox PathTextBox { get; set; }
        protected virtual Button BrowseButton { get; set; }
        protected virtual TextBlock StatusText { get; set; }

        public ConfigItemFileBrowse(string name, string text, string filter, string key, ConfigBase config)
            : base(name, key, config)
        {
            Text = text;
            Filter = filter;
        }

        public override UIElement CreateElement()
        {
            if (Element != null)
                return Element;

            PathTextBox = new TextBox
            {
                IsReadOnly = true,
                MinWidth = 350,
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0),
                Padding = new Thickness(4, 2, 4, 2)
            };

            BrowseButton = new Button
            {
                Content = "Browse...",
                Padding = new Thickness(12, 4, 12, 4),
                VerticalAlignment = VerticalAlignment.Center
            };
            BrowseButton.Click += BrowseButton_Click;

            StatusText = new TextBlock
            {
                Margin = new Thickness(0, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                Visibility = Visibility.Collapsed
            };

            var browsePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };
            browsePanel.Children.Add(PathTextBox);
            browsePanel.Children.Add(BrowseButton);

            Panel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            if (!string.IsNullOrEmpty(Text))
            {
                Panel.Children.Add(new TextBlock
                {
                    Text = Text,
                    Margin = new Thickness(0, 0, 0, 4),
                    TextWrapping = TextWrapping.Wrap
                });
            }

            Panel.Children.Add(browsePanel);
            Panel.Children.Add(StatusText);

            Element = Panel;
            SetValueElement();
            return Element;
        }

        protected virtual void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = $"Select {DefaultFileName}",
                Filter = Filter,
                DefaultExt = ".dll",
                CheckFileExists = true,
                CheckPathExists = true
            };

            // Set initial directory from current value or configured default
            string currentPath = Config.GetOption<string>(Key);
            if (!string.IsNullOrEmpty(currentPath) && File.Exists(currentPath))
            {
                dialog.InitialDirectory = Path.GetDirectoryName(currentPath);
            }
            else if (!string.IsNullOrEmpty(InitialDirectory) && Directory.Exists(InitialDirectory))
            {
                dialog.InitialDirectory = InitialDirectory;
            }

            if (dialog.ShowDialog() == true)
            {
                Config.SetOption(Key, dialog.FileName);
                SetValueElement();
            }
        }

        protected override void SetValueConfig(object sender, RoutedEventArgs e)
        {
            Config.SetOption(Key, PathTextBox.Text);
        }

        protected override void SetValueElement()
        {
            string path = Config.GetOption<string>(Key) ?? "";
            PathTextBox.Text = path;
            UpdateStatus(path);
        }

        protected virtual void UpdateStatus(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                StatusText.Text = "No file selected. Please browse to locate the file.";
                StatusText.Foreground = Brushes.Orange;
                StatusText.Visibility = Visibility.Visible;
                return;
            }

            if (!File.Exists(path))
            {
                StatusText.Text = "The selected file does not exist.";
                StatusText.Foreground = Brushes.Red;
                StatusText.Visibility = Visibility.Visible;
                return;
            }

            // Run custom validation if provided
            if (ValidationFunc != null)
            {
                string validationMessage = ValidationFunc(path);
                if (!string.IsNullOrEmpty(validationMessage))
                {
                    StatusText.Text = validationMessage;
                    StatusText.Foreground = Brushes.Orange;
                    StatusText.Visibility = Visibility.Visible;
                    return;
                }
            }

            StatusText.Text = $"Valid file selected: {Path.GetFileName(path)}";
            StatusText.Foreground = Brushes.Green;
            StatusText.Visibility = Visibility.Visible;
        }
    }
}
