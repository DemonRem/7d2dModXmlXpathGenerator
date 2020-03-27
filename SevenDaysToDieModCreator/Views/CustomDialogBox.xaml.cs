﻿using SevenDaysToDieModCreator.Extensions;
using SevenDaysToDieModCreator.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace SevenDaysToDieModCreator.Views
{
    /// <summary>
    /// Interaction logic for CustomDialogBox.xaml
    /// </summary>
    public partial class CustomDialogBox : Window
    {
        private ComboBox AllTagsComboBox = new ComboBox();
        public CustomDialogBox(bool doLoadModPath, string textBoxBody = "")
        {
            InitializeComponent();
            string defaultText = "Thank you for downloading the 7 days to die Mod Creator! " +
                "Please input your custom tag now! You can think of this as the Mod Name. " +
                "This will be the top tag used in the xml output files. " +
                "It is also the name used in the Output folder when saving and moving the mod files.\n\n" +
                "IMPORTANT: If you lose work check the log.txt in the Output folder. " +
                "Any time you close the app or reset the object view, the xml that could be generated is output in that log. " +
                "If you like the mod don't forget to drop an endorsment and tell your friends!\n\n\n\n\n\n";
            if (!String.IsNullOrEmpty(textBoxBody)) defaultText = textBoxBody + "\n\n\n\n\n\n";
            if (doLoadModPath)
            {
                List<string> allCustomModsInPath =  XmlFileManager.GetCustomModFoldersInOutput();
                AllTagsComboBox = allCustomModsInPath.CreateComboBoxList();
                AllTagsComboBox.AddToolTip("Input a new tag or select a tag from the list of existing tags");
                AllTagsComboBox.IsEditable = true;
                CustomDialogPanel.Children.Add(AllTagsComboBox);
            }
            LabelTextBlock.Text = defaultText;
        }
        public string ResponseText
        {
            get { return AllTagsComboBox.Text; }
            set { AllTagsComboBox.Text = value; }
        }

        private void OKButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
