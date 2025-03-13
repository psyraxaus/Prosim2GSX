﻿﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using Prosim2GSX.Services;
using System;
using System.Windows;

namespace Prosim2GSX
{
    public partial class NotifyIconViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ShowWindowCommand))]
        public bool canExecuteShowWindow = true;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(HideWindowCommand))]
        public bool canExecuteHideWindow;

        [RelayCommand(CanExecute = nameof(CanExecuteShowWindow))]
        public void ShowWindow()
        {
            try
            {
                // Check if MainWindow is null before attempting to show it
                if (Application.Current.MainWindow != null)
                {
                    Application.Current.MainWindow.Show(disableEfficiencyMode: true);
                    CanExecuteShowWindow = false;
                    CanExecuteHideWindow = true;
                }
                else
                {
                    // Log the issue
                    Logger.Log(LogLevel.Warning, "NotifyIconViewModel:ShowWindow", 
                        "MainWindow is null. Cannot show window.");
                    
                    // Show error message to the user
                    MessageBox.Show(
                        "Unable to show application window. The main window is not available.\n\n" +
                        "This may occur if the application is still initializing or if there was an error during startup.\n\n" +
                        "Please wait a moment and try again, or restart the application if the issue persists.",
                        "Window Not Available", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Logger.Log(LogLevel.Error, "NotifyIconViewModel:ShowWindow", ex, 
                    "Exception occurred while showing window");
                
                // Show error message to the user
                MessageBox.Show(
                    $"Error showing application window: {ex.Message}\n\n" +
                    "Please restart the application.",
                    "Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteHideWindow))]
        public void HideWindow()
        {
            try
            {
                // Check if MainWindow is null before attempting to hide it
                if (Application.Current.MainWindow != null)
                {
                    Application.Current.MainWindow.Hide(enableEfficiencyMode: false);
                    CanExecuteShowWindow = true;
                    CanExecuteHideWindow = false;
                }
                else
                {
                    // Log the issue
                    Logger.Log(LogLevel.Warning, "NotifyIconViewModel:HideWindow", 
                        "MainWindow is null. Cannot hide window.");
                    
                    // Reset button states
                    CanExecuteShowWindow = true;
                    CanExecuteHideWindow = false;
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Logger.Log(LogLevel.Error, "NotifyIconViewModel:HideWindow", ex, 
                    "Exception occurred while hiding window");
                
                // Reset button states
                CanExecuteShowWindow = true;
                CanExecuteHideWindow = false;
            }
        }

        [RelayCommand]
        public void ExitApplication()
        {
            Application.Current.Shutdown();
        }
    }
}
