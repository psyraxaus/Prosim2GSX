using Microsoft.Win32;
using Prosim2GSX.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Service for GSX menu interaction
    /// </summary>
    public class GSXMenuService : IGSXMenuService
    {
        private readonly string pathMenuFile = @"\MSFS\fsdreamteam-gsx-pro\html_ui\InGamePanels\FSDT_GSX_Panel\menu";
        private readonly string registryPath = @"HKEY_CURRENT_USER\SOFTWARE\FSDreamTeam";
        private readonly string registryValue = @"root";
        private string menuFile = "";
        private readonly MobiSimConnect simConnect;
        private readonly ServiceModel model;
        private bool operatorWasSelected = false;
        
        public bool OperatorWasSelected 
        { 
            get => operatorWasSelected; 
            set => operatorWasSelected = value; 
        }
        
        public GSXMenuService(ServiceModel model, MobiSimConnect simConnect)
        {
            this.model = model;
            this.simConnect = simConnect;
            
            try
            {
                string regPath = (string)Registry.GetValue(registryPath, registryValue, null);
                if (regPath != null)
                {
                    regPath += pathMenuFile;
                    if (Path.Exists(regPath))
                        menuFile = regPath;
                    else
                        Logger.Log(LogLevel.Warning, "GSXMenuService:Constructor", $"Menu file path does not exist: {regPath}");
                }
                else
                {
                    Logger.Log(LogLevel.Warning, "GSXMenuService:Constructor", $"Registry key not found: {registryPath}\\{registryValue}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXMenuService:Constructor", $"Error accessing registry: {ex.Message}");
            }
            
            Logger.Log(LogLevel.Information, "GSXMenuService:Constructor", "GSX Menu Service initialized");
        }
        
        /// <summary>
        /// Opens the GSX menu
        /// </summary>
        public void MenuOpen()
        {
            try
            {
                simConnect.IsGsxMenuReady = false;
                Logger.Log(LogLevel.Debug, "GSXMenuService:MenuOpen", $"Opening GSX Menu");
                simConnect.WriteLvar("FSDT_GSX_MENU_OPEN", 1);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXMenuService:MenuOpen", $"Error opening menu: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Selects a menu item by index
        /// </summary>
        public void MenuItem(int index, bool waitForMenu = true)
        {
            try
            {
                if (waitForMenu)
                    MenuWaitReady();
                simConnect.IsGsxMenuReady = false;
                Logger.Log(LogLevel.Debug, "GSXMenuService:MenuItem", $"Selecting Menu Option {index} (L-Var Value {index - 1})");
                simConnect.WriteLvar("FSDT_GSX_MENU_CHOICE", index - 1);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXMenuService:MenuItem", $"Error selecting menu item: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Waits for the GSX menu to be ready
        /// </summary>
        public void MenuWaitReady()
        {
            try
            {
                int counter = 0;
                while (!simConnect.IsGsxMenuReady && counter < 1000) 
                { 
                    Thread.Sleep(100); 
                    counter++; 
                }
                Logger.Log(LogLevel.Debug, "GSXMenuService:MenuWaitReady", $"Wait ended after {counter * 100}ms");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXMenuService:MenuWaitReady", $"Error waiting for menu: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Checks if operator selection is active
        /// </summary>
        public int IsOperatorSelectionActive()
        {
            int result = -1;

            try
            {
                if (!string.IsNullOrEmpty(menuFile))
                {
                    string[] lines = File.ReadLines(menuFile).ToArray();
                    if (lines.Length > 1)
                    {
                        if (!string.IsNullOrEmpty(lines[0]) && (lines[0] == "Select handling operator" || lines[0] == "Select catering operator"))
                        {
                            Logger.Log(LogLevel.Debug, "GSXMenuService:IsOperatorSelectionActive", $"Match found for operator Selection: '{lines[0]}'");
                            result = 1;
                        }
                        else if (string.IsNullOrEmpty(lines[0]))
                        {
                            Logger.Log(LogLevel.Debug, "GSXMenuService:IsOperatorSelectionActive", $"Line is empty! Lines total: {lines.Length}");
                            result = -1;
                        }
                        else
                        {
                            Logger.Log(LogLevel.Debug, "GSXMenuService:IsOperatorSelectionActive", $"No Match found for operator Selection: '{lines[0]}'");
                            result = 0;
                        }
                    }
                    else
                    {
                        Logger.Log(LogLevel.Debug, "GSXMenuService:IsOperatorSelectionActive", $"Menu Lines not above 1 ({lines.Length})");
                    }
                }
                else
                {
                    Logger.Log(LogLevel.Debug, "GSXMenuService:IsOperatorSelectionActive", $"Menu File was empty");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXMenuService:IsOperatorSelectionActive", $"Error checking operator selection: {ex.Message}");
                result = -1;
            }

            return result;
        }
        
        /// <summary>
        /// Handles operator selection
        /// </summary>
        public void OperatorSelection()
        {
            try
            {
                Thread.Sleep(2000);

                int result = IsOperatorSelectionActive();
                if (result == -1)
                {
                    Logger.Log(LogLevel.Information, "GSXMenuService:OperatorSelection", $"Waiting {model.OperatorDelay}s for Operator Selection");
                    Thread.Sleep((int)(model.OperatorDelay * 1000));
                }
                else if (result == 1)
                {
                    Logger.Log(LogLevel.Information, "GSXMenuService:OperatorSelection", $"Operator Selection active, choosing Option 1");
                    MenuItem(1);
                    operatorWasSelected = true;
                }
                else
                    Logger.Log(LogLevel.Information, "GSXMenuService:OperatorSelection", $"No Operator Selection needed");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXMenuService:OperatorSelection", $"Error handling operator selection: {ex.Message}");
            }
        }
    }
}
