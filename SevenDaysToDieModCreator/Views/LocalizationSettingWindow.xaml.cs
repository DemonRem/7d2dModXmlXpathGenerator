﻿using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Search;
using SevenDaysToDieModCreator.Extensions;
using SevenDaysToDieModCreator.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SevenDaysToDieModCreator.Views
{
    public partial class LocalizationSettingWindow : Window
    {
        private LocalizationGridUserControl ModLocalizationGridUserControl { get; set; }

        private LocalizationGridUserControl GameRecordGridUserControl { get; set; }


        private LocalizationFileObject GameLocalizationFile { get; set; }

        //Reference to the loaded list warppers in the main window
        private Dictionary<string, XmlObjectsListWrapper> LoadedListWrappers { get; set; }

        private string GridAsCSVAfterUpdate { get; set; }
        private const string TITLE_POSTPEND_MESSAGE = "_" + LocalizationFileObject.LOCALIZATION_FILE_NAME;
        private string WindowTitle { get; set; }
        private string GetTitleForWindow() 
        {
            return WindowTitle + TITLE_POSTPEND_MESSAGE;
        }
        private string StartingMod { get; set; }
        public LocalizationSettingWindow(Dictionary<string, XmlObjectsListWrapper> loadedListWrappers)
        {
            InitializeComponent();

            Closing += new CancelEventHandler(LocalizatonSettingWindow_Closing);

            AddTooltips();

            StartingMod = Properties.Settings.Default.ModTagSetting;
            WindowTitle = StartingMod.ToString();
            this.Title = GetTitleForWindow();
            this.LoadedListWrappers = loadedListWrappers;

            string pathToModLocalizationFile = XmlFileManager._ModConfigOutputPath + LocalizationFileObject.LOCALIZATION_FILE_NAME;
            ModLocalizationGridUserControl = new LocalizationGridUserControl(pathToModLocalizationFile);
            GridAsCSVAfterUpdate = ModLocalizationGridUserControl.Maingrid.GridAsCSV(); 

            List<string> allCustomTagDirectories = XmlFileManager.GetCustomModFoldersInOutput();
            foreach (string nextModTag in allCustomTagDirectories)
            {
                ModSelectionComboBox.AddUniqueValueTo(nextModTag);
            }
            ModSelectionComboBox.SelectedItem = Properties.Settings.Default.ModTagSetting;
            
            ModSelectionComboBox.DropDownClosed += ModSelectionComboBox_DropDownClosed;

            ModLocalizationScrollViewer.Content = ModLocalizationGridUserControl;

            string pathToGameLocalizationFile = XmlFileManager._LoadedFilesPath + LocalizationFileObject.LOCALIZATION_FILE_NAME;
            GameLocalizationFile = new LocalizationFileObject(pathToGameLocalizationFile);

            TextEditorOptions newOptions = new TextEditorOptions
            {
                EnableRectangularSelection = true,
                EnableTextDragDrop = true,
                HighlightCurrentLine = true,
                ShowTabs = true
            };
            LocalizationPreviewBox.ShowLineNumbers = true;
            LocalizationPreviewBox.TextArea.Options = newOptions;
            LocalizationPreviewBox.Text = ModLocalizationGridUserControl.Maingrid.GridAsCSV();
            LocalizationPreviewBox.LostFocus += LocalizationPreviewBox_LostFocus;
            SearchPanel.Install(LocalizationPreviewBox);
            ModLocalizationScrollViewer.GotFocus += Maingrid_GotOrLostFocus;
            ModLocalizationScrollViewer.LostFocus += Maingrid_GotOrLostFocus;

            List<string> gameFileKeys = GameLocalizationFile.HeaderValuesMap.GetValueOrDefault(GameLocalizationFile.KeyColumn);
            GameKeySelectionComboBox.SetComboBox(gameFileKeys);
            GameKeySelectionComboBox.IsEditable = true;
            GameKeySelectionComboBox.DropDownClosed += GameKeySelectionComboBox_DropDownClosed;
            GameKeySelectionComboBox.PreviewKeyDown += GameKeySelectionComboBox_PreviewKeyDown;
        }

        private void AddTooltips()
        {
            LocalizationOutputLabel.AddToolTip("Just below is a direct visualiztion of the selected mod's Localization.txt additions\n" +
                "This text box can be used to directy edit the contents of the file");
            GameLocalizationRecordOutputLabel.AddToolTip("Just below is a Game record visualization tool\n" +
                "Here you can view, and copy existing game Localization records");
            GameKeySelectionComboBox.AddToolTip("Select a value in the box or type out the record and hit enter\n" +
                " to get an instant Localization snapshot above ");
            CopyGameRecordButton.AddToolTip("Copies the game record above directly into the grid below\n\n" +
                "Any language headers included in your mod's\n" +
                "localization headers will be included automatically");
            ModSelectionComboBox.AddToolTip("Use this box to switch between mods and modify the Localization of different Mods easily");
            SaveLocalizationTableButton.AddToolTip("Click here to write out all Localization changes");
            AddEmptyRowButton.AddToolTip("Click here to add an empty row to te grid\n\n" +
                "The first column in each row is a combobox that contains\n" +
                "any added items/blocks from the curret selected mod.");
        }
        private void LocalizatonSettingWindow_Closing(object sender, CancelEventArgs e)
        {
            if (HasGridChanged()) 
            {
                string message = "You have unsaved changes in the grid below, for the localization of " + StartingMod + ".\n" +
                    "Are you sure you want to close the window, and LOSE any work made on the current localization?";
                string caption = "Lose Unsaved Changes";
                MessageBoxResult results = MessageBox.Show(message, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
                switch (results)
                {
                    case MessageBoxResult.Yes:
                        break;
                    case MessageBoxResult.No:
                        e.Cancel =  true;
                        break;
                }
            }
        }
        private void GameKeySelectionComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Return)
            {
                if (sender is ComboBox gameKeySelectionBox)
                {
                    string gameKeyRecord = GetGameRecordUsingKey(gameKeySelectionBox);
                    AddNewGridWithRecord(gameKeyRecord);
                }
            }
        }
        private void GameKeySelectionComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (sender is ComboBox gameKeySelectionBox)
            {
                string gameKeyRecord = GetGameRecordUsingKey(gameKeySelectionBox);
                AddNewGridWithRecord(gameKeyRecord);
            }
        }
        private void AddNewGridWithRecord(string gameKeyRecord)
        {
            string pathToTempFile = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "TMP_" + LocalizationFileObject.LOCALIZATION_FILE_NAME);
            XmlFileManager.WriteStringToFile(Directory.GetCurrentDirectory(), "TMP_" + LocalizationFileObject.LOCALIZATION_FILE_NAME, gameKeyRecord);
            GameRecordGridUserControl = new LocalizationGridUserControl(pathToTempFile, true);
            GameRecordScrollViewer.Content = GameRecordGridUserControl;
        }
        private string GetGameRecordUsingKey(ComboBox gameKeySelectionBox, bool addHeaders = true)
        {
            List<string> record = GameLocalizationFile.KeyValueToRecordMap.GetValueOrDefault(gameKeySelectionBox.Text);
            StringBuilder recordOutputBuilder = new StringBuilder();
            StringBuilder headerBuilder = new StringBuilder();
            if (record != null && record.Count > 0)
            {
                int count = 0;
                //This uses the keys of the header map t determine which column we have for the record.
                foreach (string key in GameLocalizationFile.HeaderValuesMap.Keys) 
                {
                    if(addHeaders)headerBuilder.Append(key + ",");
                    string nextFieldInRecord = record[count];
                    if (nextFieldInRecord.Contains(",")) recordOutputBuilder.Append("\""+nextFieldInRecord +"\",");
                    else recordOutputBuilder.Append(nextFieldInRecord + ",");
                    count++;
                }
                //Remove the trailing comma
                recordOutputBuilder.Remove(recordOutputBuilder.Length - 1, 1);
                if (addHeaders) 
                {
                    //Remove the trailing comma
                    headerBuilder.Remove(headerBuilder.Length - 1, 1);
                    headerBuilder.Append("\n");
                }
                headerBuilder.Append(recordOutputBuilder.ToString());
            }
            return headerBuilder.ToString();
        }
        private void LocalizationPreviewBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (HasLocalizatioWindowChanged())
            {
                string message = "You have modified the localization directly in the output window.\n" +
                     "Would you like to update the grid below with those changes?\n\n" +
                     "if you don't these changes will be lost.";
                string caption = "Update Grid";

                MessageBoxResult results = MessageBox.Show(message, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
                switch (results)
                {
                    case MessageBoxResult.Yes:
                        ReloadGridWithExternalChanges(LocalizationPreviewBox.Text);
                        break;
                }
            }
        }

        //This function will use a temp file to update the grid, used for adding new rows.
        private void ReloadGridWithExternalChanges(string csvStringUsedToUpdateGrid)
        {
            string pathToTempFile = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "TMP_" + LocalizationFileObject.LOCALIZATION_FILE_NAME);
            XmlFileManager.WriteStringToFile(Directory.GetCurrentDirectory(), "TMP_" + LocalizationFileObject.LOCALIZATION_FILE_NAME, csvStringUsedToUpdateGrid);
            ReloadModLocalizationGrid(pathToTempFile);
            GridAsCSVAfterUpdate = LocalizationPreviewBox.Text;
            ModLocalizationGridUserControl.SetGridChangedToTrue();
        }

        private void SaveLocalization()
        {
            XmlFileManager.WriteStringToFile(XmlFileManager.Get_ModOutputPath(ModSelectionComboBox.SelectedItem.ToString()), LocalizationFileObject.LOCALIZATION_FILE_NAME, LocalizationPreviewBox.Text);
            ModLocalizationGridUserControl.SetGridChangedToFalse();
        }
        public bool HasGridChanged() 
        {
            return ModLocalizationGridUserControl.GridHasChanged;
        }
        public bool HasLocalizatioWindowChanged()
        {
            return !LocalizationPreviewBox.Text.Equals(GridAsCSVAfterUpdate);
        }
        private void ModSelectionComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (sender is ComboBox modSelectionComboBox) 
            {
                string message = "You have unsaved changes below, for the localization of " + StartingMod + ".\n" +
                    "Are you sure you want to change mods, and LOSE any work made on the current localization?";
                string caption = "Lose Unsaved Changes";

                //If the grid has changed and is unsaved, make sure they are okay losing changes.
                if (HasGridChanged())
                {
                    MessageBoxResult results = MessageBox.Show(message, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
                    switch (results)
                    {
                        case MessageBoxResult.Yes:
                            ReloadModLocalizationGrid(modSelectionComboBox);
                            break;
                        case MessageBoxResult.No:
                            modSelectionComboBox.SelectedItem = StartingMod;
                            break;
                    }
                }
                else 
                {
                    ReloadModLocalizationGrid(modSelectionComboBox);
                }
            }
        }
        private void ReloadModLocalizationGrid(string pathToLocalizatioFile, bool deleteFileAfterLoadingGrid = true)
        {
            ModLocalizationGridUserControl = new LocalizationGridUserControl(pathToLocalizatioFile);
            ModLocalizationScrollViewer.Content = ModLocalizationGridUserControl;
            string currentGridAsCSV = ModLocalizationGridUserControl.Maingrid.GridAsCSV();
            LocalizationPreviewBox.Text = currentGridAsCSV;
            if (deleteFileAfterLoadingGrid) File.Delete(pathToLocalizatioFile);
        }
        private void ReloadModLocalizationGrid(ComboBox modSelectionComboBox)
        {
            string modOutptPath = XmlFileManager.Get_ModOutputPath(modSelectionComboBox.SelectedItem.ToString());
            string pathToModLocalizationFile = modOutptPath + LocalizationFileObject.LOCALIZATION_FILE_NAME;

            ModLocalizationGridUserControl = new LocalizationGridUserControl(pathToModLocalizationFile);
            ModLocalizationScrollViewer.Content = ModLocalizationGridUserControl;
            string currentGridAsCSV = ModLocalizationGridUserControl.Maingrid.GridAsCSV();
            LocalizationPreviewBox.Text = currentGridAsCSV;
            StartingMod = ModSelectionComboBox.SelectedItem.ToString();
            WindowTitle = StartingMod.ToString();
            this.Title = GetTitleForWindow();
        }
        private void Maingrid_GotOrLostFocus(object sender, RoutedEventArgs e)
        {
            if (HasGridChanged()) 
            {
                LocalizationPreviewBox.Text = ModLocalizationGridUserControl.Maingrid.GridAsCSV();
                GridAsCSVAfterUpdate = LocalizationPreviewBox.Text;            
            }
        }
        private void CopyGameRecord_Click(object sender, RoutedEventArgs e)
        {
            List<string> record = GameLocalizationFile.KeyValueToRecordMap.GetValueOrDefault(GameKeySelectionComboBox.Text);
            if (record != null && record.Count > 0) 
            {
                StringBuilder recordToCopyBuilder = new StringBuilder();
                if (record.Count > 3) 
                {
                    string key = record[0];
                    string source = record[1];
                    string context = record[2];
                    recordToCopyBuilder.Append(LocalizationPreviewBox.Text + "\n");
                    recordToCopyBuilder.Append(key);
                    recordToCopyBuilder.Append(",");
                    recordToCopyBuilder.Append(source);
                    recordToCopyBuilder.Append(",");
                    recordToCopyBuilder.Append(context);
                    recordToCopyBuilder.Append(",");
                    recordToCopyBuilder.Append("New");
                    recordToCopyBuilder.Append(",");
                }

                List<string> allGameRecordHeaders = new List<string>( GameRecordGridUserControl.LocalizationFileObject.HeaderValues);
                List<string> allLocalModHeaders = new List<string>(ModLocalizationGridUserControl.LocalizationFileObject.HeaderValues);
                //Go through game record headers
                foreach (string nextModHeader in allLocalModHeaders) 
                {
                    if (!nextModHeader.Contains("key") && allGameRecordHeaders.Contains(nextModHeader)) 
                    {
                        int indexToUse = allGameRecordHeaders.IndexOf(nextModHeader);
                        if(record[indexToUse].Contains(",")) recordToCopyBuilder.Append("\"" + record[indexToUse] + "\",");
                        else recordToCopyBuilder.Append(record[indexToUse] + ",");
                    }
                }
                //REmove trailing comma
                recordToCopyBuilder.Remove(recordToCopyBuilder.Length - 1, 1);
                ReloadGridWithExternalChanges(recordToCopyBuilder.ToString());

            }

        }
        private void SaveLocalizationTableButton_Click(object sender, RoutedEventArgs e)
        {
            SaveLocalization();
            string message = "Successfully saved changes to: \n\n " + XmlFileManager.Get_ModOutputPath(ModSelectionComboBox.SelectedItem.ToString()) + LocalizationFileObject.LOCALIZATION_FILE_NAME;
            string caption = "Save Grid to Localization.txt";
            MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void AddEmptyRow_Click(object sender, RoutedEventArgs e)
        {
            //Hard code the wrapper to the items.xml for the mod
            XmlObjectsListWrapper selectedModItemsWrapper = LoadedListWrappers.GetValueOrDefault(ModSelectionComboBox.SelectedItem.ToString() + "_items");
            //Hard code the wrapper to the blocks.xml for the mod
            XmlObjectsListWrapper selectedModBlocksWrapper = LoadedListWrappers.GetValueOrDefault(ModSelectionComboBox.SelectedItem.ToString() + "_blocks");

            ModLocalizationGridUserControl.AddEmptyRow(selectedModItemsWrapper, selectedModBlocksWrapper);
        }
    }
}
