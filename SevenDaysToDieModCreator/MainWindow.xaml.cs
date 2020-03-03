﻿using Microsoft.Win32;
using SevenDaysToDieModCreator.Controllers;
using SevenDaysToDieModCreator.Extensions;
using SevenDaysToDieModCreator.Models;
using SevenDaysToDieModCreator.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Xml;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace SevenDaysToDieModCreator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewController MainWindowViewController { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            Properties.Settings.Default.GameFolderModDirectory = "";
            //Properties.Settings.Default.AutoMoveDecisionMade = false;
            this.WindowState = WindowState.Maximized;
            this.MainWindowViewController = new MainWindowViewController(NewObjectFormsPanel, XmlOutputBox, RemoveChildContextMenu_Click);
            Loaded += MyWindow_Loaded;
            Closing += new CancelEventHandler(MainWindow_Closing);
        }
        private void SetOnHoverMessages() 
        {
            //Menu Item
            HelpMenuItem.AddOnHoverMessage("Click to see more information about the app");
            ChangeCustomTagMenuItem.AddOnHoverMessage("Click here to change the custom tag.\nChanging the tag also changes the name of the output folder for the mod.");
            ChangeModGameDirectoryMenu.AddOnHoverMessage("This will be the directory used when Moving the application generated xml files.\n" +
                "Ex: GameDirectory\\7 days to die\\Mods");
            MoveFileMenuItem.AddOnHoverMessage("Click here to move the local app generated mod files to the Game Directory.");
            SaveFileMenuItem.AddOnHoverMessage("Click here to save the xml files.\n" +
                "If auto move is activated this will also move the geerated fles to the game directory.");
            LoadFileMenuItem.AddOnHoverMessage("Click to load an xml file or multiple xml files\nLoaded files will persist on application close");
            //Buttons
            SaveXmlViewButton.AddOnHoverMessage("This will save the XML into the appropriate files found at:\n" + XmlFileManager._ModOutputPath+"\n");
            OpenDirectEditViewButton.AddOnHoverMessage("Click to open a window for direct edits to the selected file above");
            AddObjectViewButton.AddOnHoverMessage("Click to add a new object edit view using the object above\nWARNING: This could take awhile");
            AddNewTreeViewButton.AddOnHoverMessage("Click to add a new searchable tree view using the object above." +
                "\nWith this tree you can also insert items into the existing items." +
                " \nWARNING: This could take awhile");
            ClearAllObjectsViewButton.AddOnHoverMessage("Click to remove all objects from the view above");
            ClearTreesViewButton.AddOnHoverMessage("Click to remove all trees from the view above");
            //Combo Boxes
            OpenDirectEditViewComboBox.AddOnHoverMessage("The selected file for direct edits");
            AllLoadedFilesComboBox.AddOnHoverMessage("The selected object here is used to create the tree view below\nAdd objects to the list by loading an xml file from the game folder");
            AllLoadedNewObjectViewsComboBox.AddOnHoverMessage("The selected object here is used to create the new object view below\nAdd objects to the list by loading an xml file from the game folder.");
        }
        private void MyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SetOnHoverMessages();
            MainWindowViewController.LoadStartingDirectory(AllLoadedFilesComboBox, AllLoadedNewObjectViewsComboBox, OpenDirectEditViewComboBox);
            if (Properties.Settings.Default.CustomTagName.Equals("ThisNeedsToBeSet"))
            {
                CustomTagDialogPopUp();
            }
            //Need to reload all events when loading state like this.
            //bool didLoad = LoadExternalXaml();
        }
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            string xmltoWrite = XmlXpathGenerator.GenerateXmlForObjectView(NewObjectFormsPanel, MainWindowViewController.LeftNewObjectViewController.loadedListWrappers);
            if(!String.IsNullOrEmpty(xmltoWrite)) XmlFileManager.WriteXmlToLog(xmltoWrite, true);
            //SaveExternalXaml();
        }
        private void LoadFile_Click(object sender, RoutedEventArgs e)
        {
            MainWindowViewController.LoadFilesViewControl(AllLoadedFilesComboBox, AllLoadedNewObjectViewsComboBox, OpenDirectEditViewComboBox);
        }
        private void SaveXmlFile_Click(object sender, RoutedEventArgs e)
        {
            if (!Properties.Settings.Default.AutoMoveDecisionMade) CheckAutoMoveProperty("You can change this setting later using the File menu.");
            MessageBoxResult result = MessageBox.Show(
                "This will write all current generated xml to the appropriate files in the output location. Are you sure?", 
                "Save Generated XML", 
                MessageBoxButton.OKCancel, 
                MessageBoxImage.Warning);
            switch (result)
            {
                case MessageBoxResult.OK:
                    XmlXpathGenerator.SaveAllGeneratedXmlToPath(NewObjectFormsPanel, MainWindowViewController.LeftNewObjectViewController.loadedListWrappers, XmlFileManager._ModOutputPath, true);
                    if(Properties.Settings.Default.AutoMoveMod) XmlFileManager.CopyAllOutputFiles();
                    break;
            }
        }
        private void CheckAutoMoveProperty(string appendMessage = "") 
        {
            string currentStatus = Properties.Settings.Default.AutoMoveMod ? "Activated" : "Deactived";
            MessageBoxResult innerResult = MessageBox.Show("Would you like to change the status of the Auto Move feature?\n\n" +
                "Current status " + currentStatus +"\n\n" +
                "When activated, on saving, the application automatically moves all files to the Games Mod Folder chosen.\n" +
                appendMessage,
                "Auto Move Game Files",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) ;
            switch (innerResult)
            {
                case MessageBoxResult.Yes:
                    Properties.Settings.Default.AutoMoveMod = !Properties.Settings.Default.AutoMoveMod;
                    break;
            }
            //If the AutoMoveMod was turned on and the mod directory is not set
            if (String.IsNullOrEmpty(Properties.Settings.Default.GameFolderModDirectory)
                && Properties.Settings.Default.AutoMoveMod)
            {
                HandleMissingGameModDirectory();
                if (String.IsNullOrEmpty(Properties.Settings.Default.GameFolderModDirectory))
                {
                    MessageBox.Show("You need to set the mod directory for this feature to work!");
                    HandleMissingGameModDirectory();
                }
            }
            if (!Properties.Settings.Default.AutoMoveDecisionMade) Properties.Settings.Default.AutoMoveDecisionMade = true;
            Properties.Settings.Default.Save();
        }
        private void HandleMissingGameModDirectory()
        {
            MessageBoxResult result = MessageBox.Show(
             "For the Auto Move function to work you must set the Game Folder Directory. Please do that now.\n\n" +
             "HELP: This is usually a \"Mods\" folder located directly in your 7 Days to Die game folder installation.\n" +
             "Example: \"7 Days To Die\\Mods \"If that folder does not exist please create it first.",
             "Set Game Mod Folder Location",
             MessageBoxButton.OK,
             MessageBoxImage.Warning);
            switch (result) 
            {
                case MessageBoxResult.OK:
                    OpenGameFolderSelectDialog();
                    break;
            }

        }
        private void OpenGameFolderSelectDialog() 
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = Directory.GetCurrentDirectory();
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                if (!String.IsNullOrEmpty(dialog.FileName))
                {
                    MessageBox.Show("You selected: " + dialog.FileName);
                    Properties.Settings.Default.GameFolderModDirectory = dialog.FileName +"/";
                    Properties.Settings.Default.AutoMoveDecisionMade = true;
                    Properties.Settings.Default.Save();
                }
            }
        }
        private void AddObjectView_Click(object sender, RoutedEventArgs e)
        {
            string selectedObject = AllLoadedNewObjectViewsComboBox.Text;
            if (String.IsNullOrEmpty(selectedObject)) return;
            XmlObjectsListWrapper selectedWrapper = MainWindowViewController.LoadedListWrappers.GetWrapperFromDictionary(selectedObject);
            MainWindowViewController.LeftNewObjectViewController.CreateNewObjectFormTree(selectedWrapper);
            if (!MainWindowViewController.LeftNewObjectViewController.loadedListWrappers.ContainsValue(selectedWrapper) && selectedWrapper != null) 
            {
                MainWindowViewController.LeftNewObjectViewController.loadedListWrappers.Add(selectedWrapper.xmlFile.GetFileNameWithoutExtension(), selectedWrapper);
            }
        }
        private void AddNewTreeView_Click(object sender, RoutedEventArgs e)
        {
            string selectedObject = AllLoadedFilesComboBox.Text;
            if (String.IsNullOrEmpty(selectedObject)) return;
            XmlObjectsListWrapper selectedWrapper = MainWindowViewController.LoadedListWrappers.GetWrapperFromDictionary(selectedObject);
            TreeViewItem nextTreeView = MainWindowViewController.RightSearchTreeViewController.GetObjectTreeViewRecursive(selectedWrapper, MakeObjectATargetButton_Click);
            ViewSp.Children.Add(nextTreeView);
            if (!MainWindowViewController.RightSearchTreeViewController.LoadedListWrappers.ContainsValue(selectedWrapper) && selectedWrapper != null)
            {
                MainWindowViewController.RightSearchTreeViewController.LoadedListWrappers.Add(selectedWrapper.xmlFile.GetFileNameWithoutExtension(), selectedWrapper);
            }
        }
        private void MakeObjectATargetButton_Click(object sender, RoutedEventArgs e)
        {
            Button senderAsButton = (Button)sender;
            XmlObjectsListWrapper wrapperToUse = this.MainWindowViewController.LoadedListWrappers.GetValueOrDefault(senderAsButton.Name);
            if (!MainWindowViewController.LeftNewObjectViewController.loadedListWrappers.ContainsValue(wrapperToUse) && wrapperToUse != null)
            {
                MainWindowViewController.LeftNewObjectViewController.loadedListWrappers.Add(wrapperToUse.xmlFile.GetFileNameWithoutExtension(), wrapperToUse);
            }
            string[] contentSplit = senderAsButton.Content.ToString().Split(":");

            TreeViewItem newObjectFormTree = this.MainWindowViewController.LeftNewObjectViewController.GenerateNewObjectFormTreeAddButton(wrapperToUse, contentSplit[0], true);
            //Set the name to the wrapper so we can find the wrapper later
            newObjectFormTree.Name = senderAsButton.Name.ToString();
            //set the xmlNode that was included with the object into the top tree view
            newObjectFormTree.Tag = senderAsButton.Tag;
            //The button should be in the form "TagName:AttribiuteNameVaue"
            if (contentSplit.Length > 1)
            {
                newObjectFormTree.Header = senderAsButton.Content.ToString();
            }
            //There is the edge case where the object did not have a name value to use
            else
            {
                newObjectFormTree.Header = ((Button)newObjectFormTree.Header).Content;
            }
            newObjectFormTree.AddOnHoverMessage("Using this form you can add new objects into the " + newObjectFormTree.Header + " object\n" +
                "For Example: You want to add an ingredient into a certain, existing, recipe.");
            newObjectFormTree.AddContextMenu(RemoveChildContextMenu_Click);
            NewObjectFormsPanel.Children.Add(newObjectFormTree);
            //OpenTargetDialogWindow(newObjectFormTree, wrapperToUse, senderAsButton.Content.ToString().Split(":")[1]);
        }
        private void RemoveChildContextMenu_Click(object sender, RoutedEventArgs e)
        {
            Control myObjectControl = (Control)((MenuItem)sender).Tag;
            NewObjectFormsPanel.Children.Remove(myObjectControl);
        }
        private void ClearAllObjectView_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to clear the view?\nYou will lose any unsaved work!\nTo save you must click the \"Save All Xml\" button.", 
                "Clear Object View", 
                MessageBoxButton.OKCancel, 
                MessageBoxImage.Exclamation);
            switch (result)
            {
                case MessageBoxResult.OK:
                    this.ResetNewObjectView();
                    break;
            }
        }
        private void ResetNewObjectView() 
        {
            string xmltoWrite = XmlXpathGenerator.GenerateXmlForObjectView(NewObjectFormsPanel, MainWindowViewController.LeftNewObjectViewController.loadedListWrappers);
            if (!String.IsNullOrEmpty(xmltoWrite)) XmlFileManager.WriteXmlToLog(xmltoWrite, true);
            NewObjectFormsPanel.Children.Clear();
            MainWindowViewController.LeftNewObjectViewController.loadedListWrappers.Clear();
            XmlXpathGenerator.GenerateXmlViewOutput(NewObjectFormsPanel, MainWindowViewController.LeftNewObjectViewController.loadedListWrappers, XmlOutputBox);
        }
        private void ClearAllTreesView_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to remove all trees?", "Clear Tree View", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            switch (result)
            {
                case MessageBoxResult.OK:
                    ViewSp.Children.Clear();
                    MainWindowViewController.RightSearchTreeViewController.LoadedListWrappers.Clear();
                    break;
            }
        }
        public bool LoadExternalXaml()
        {
            bool didLoad = false;
            string path = Path.Combine(Directory.GetCurrentDirectory(), "..\\..\\..\\Output\\state.xml");
            if (File.Exists(path))
            {
                using FileStream stream = new FileStream(@path, FileMode.Open);
                this.Content = XamlReader.Load(stream);
                didLoad = true;
            }
            return didLoad;
        }

        public void SaveExternalXaml()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "..\\..\\..\\Output\\state.xml");
            using FileStream stream = new FileStream(@path, FileMode.Create);
            XamlWriter.Save(this.Content, stream);
        }
        private void CustomTagDialogPopUp(string dialogText = "")
        {
            var dialog = new CustomDialogBox(dialogText);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string name = XmlConvert.VerifyName(dialog.ResponseText);
                    Properties.Settings.Default.CustomTagName = name;
                    Properties.Settings.Default.Save();
                }
                catch (XmlException)
                {
                    MessageBox.Show("The format was incorrect, the name must follow xml naming rules!", "Format Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (ArgumentNullException)
                {
                    MessageBox.Show("The format was incorrect, you must include something!", "Format Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void OpenDirectEditViewButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedObject = OpenDirectEditViewComboBox.Text;
            if (String.IsNullOrEmpty(selectedObject)) return;
            XmlObjectsListWrapper selectedWrapper = MainWindowViewController.LoadedListWrappers.GetWrapperFromDictionary(selectedObject);
            DirectEditView directEdit = new DirectEditView(selectedWrapper);
            directEdit.Show();
        }

        private void HelpMenu_Click(object sender, RoutedEventArgs e)
        {
            string readmeFileContents = XmlFileManager.GetFileContents(XmlFileManager.LOCAL_DIR, "README.txt");
            MessageBox.Show(readmeFileContents, "Help", MessageBoxButton.OK, MessageBoxImage.Information);   
        }

        private void ChangeModGameDirectoryMenu_Click(object sender, RoutedEventArgs e)
        {
            OpenGameFolderSelectDialog();
        }

        private void MoveFileMenuHeader_Click(object sender, RoutedEventArgs e)
        {
            string gameModDirectory = Properties.Settings.Default.GameFolderModDirectory;
            if (String.IsNullOrEmpty(gameModDirectory))
            {
                HandleMissingGameModDirectory();
                XmlFileManager.CopyAllOutputFiles();
            }
            else
            {
                MessageBoxResult result = MessageBox.Show(
                    "This will copy all local generated xmls files at " +
                     XmlFileManager._ModOutputPath + "\n"+
                    " and replace the files at \n" +
                    gameModDirectory + XmlFileManager._ModPath +"\n"+
                    "Are you sure?",
                    "Save Generated XML",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Warning);
                switch (result)
                {
                    case MessageBoxResult.OK:
                        XmlFileManager.CopyAllOutputFiles();
                        break;
                }
            }
        }

        private void ChangeCustomTagMenuItem_Click(object sender, RoutedEventArgs e)
        {
            CustomTagDialogPopUp("Please input your new custom tag now.\n" +
                "This will also be used in the File Generation Folder and the Game Output folder with the Auto Move feature.\n\n" +
                "Your current tag is: " + Properties.Settings.Default.CustomTagName);
        }

        private void ChangeAutoMoveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            CheckAutoMoveProperty();
        }
    }
}