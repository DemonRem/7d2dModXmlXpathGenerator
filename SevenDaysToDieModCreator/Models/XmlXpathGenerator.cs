﻿using SevenDaysToDieModCreator.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Xml;

namespace SevenDaysToDieModCreator.Models
{
    static class XmlXpathGenerator
    {
        public const string XPATH_ACTION_SET = "set";
        public const string XPATH_ACTION_REMOVE_ATTRIBUTE = "removeattribute";
        public const string XPATH_ACTION_SET_ATTRIBUTE = "setattribute";
        public const string XPATH_ACTION_APPEND = "append";
        public const string XPATH_ACTION_REMOVE = "remove";
        public const string XPATH_ACTION_INSERT_BEFORE = "insertBefore";
        public const string XPATH_ACTION_INSERT_AFTER = "insertAfter";
        public const string ATTRIBUTE_NAME = "Attribute";
        public const string ATTRIBUTE_VALUE = "AttributeValue";
        public const string IGNORE_STRING = "IGNORE";
        public const string HEADER_APPEND_STRING = "(*)";


        public static string GenerateXmlForObjectView(MyStackPanel newObjectFormsPanel)
        {
            string topTag = "\n<" + Properties.Settings.Default.ModTagSetting + ">\n";
            string topTagEnd = "</" + Properties.Settings.Default.ModTagSetting + ">\n";
            string xmlOut = "";
            foreach (Control nextChild in newObjectFormsPanel.Children)
            {
                //It is a top object in the view
                if (nextChild.GetType() == typeof(TreeViewItem))
                {
                    TreeViewItem nextChildAsTree = (TreeViewItem)nextChild;
                    XmlObjectsListWrapper xmlObjectsListWrapper = newObjectFormsPanel.LoadedListWrappers.GetValueOrDefault(nextChildAsTree.Uid);
                    xmlOut += xmlObjectsListWrapper == null ? "" : GenerateXmlWithWrapper(nextChildAsTree, xmlObjectsListWrapper);
                }
            }
            return topTag + xmlOut + topTagEnd;
        }
        public static void SaveAllGeneratedXmlToPath(MyStackPanel newObjectFormsPanel, string path, bool writeToLog = false)
        {
            string topTag = "<" + Properties.Settings.Default.ModTagSetting + ">\n";
            string topTagEnd = "</" + Properties.Settings.Default.ModTagSetting + ">\n";

            foreach (Control nextChild in newObjectFormsPanel.Children)
            {
                //It is a top object in the view
                if (nextChild.GetType() == typeof(TreeViewItem))
                {
                    TreeViewItem nextChildAsTree = (TreeViewItem)nextChild;
                    XmlObjectsListWrapper xmlObjectsListWrapper = newObjectFormsPanel.LoadedListWrappers.GetValueOrDefault(nextChildAsTree.Uid);
                    string parentPath = xmlObjectsListWrapper.xmlFile.ParentPath == null ? "" : xmlObjectsListWrapper.xmlFile.ParentPath;
                    string xmlOut = xmlObjectsListWrapper == null ? "" : GenerateXmlWithWrapper(nextChildAsTree, xmlObjectsListWrapper, true);
                    if (!String.IsNullOrEmpty(xmlOut)) XmlFileManager.WriteStringToFile(Path.Combine(path, parentPath), xmlObjectsListWrapper.xmlFile.FileName, topTag + xmlOut.TrimEnd() + "\n" + topTagEnd, Properties.Settings.Default.DoLogTimestampOnSave);
                    if (writeToLog && !String.IsNullOrEmpty(xmlOut)) XmlFileManager.WriteStringToLog(xmlOut, true);
                }
            }
        }
        private static string GenerateXmlWithWrapper(Control parentControl, XmlObjectsListWrapper xmlObjectsListWrapper, bool includeExistingData = false)
        {
            string xmlOut = "";
            string existingWrapperFileData = "";
            TreeViewItem nextChildAsTree = (TreeViewItem)parentControl;
            //We have a target type tree view
            if (nextChildAsTree.Header.GetType() == typeof(string) && nextChildAsTree.Tag != null)
            {
                //The header is in the form nodename:targetattributename
                string[] treeTagSplit = nextChildAsTree.Header.ToString().Split(":");
                if (!treeTagSplit[0].Equals(IGNORE_STRING)) xmlOut += GenerateAppendXmlForTargetObject(xmlObjectsListWrapper, nextChildAsTree, (XmlNode)nextChildAsTree.Tag, nextChildAsTree.Uid);
            }
            //We have a normal object creation tree view 
            else
            {
                Button nextChildTreeButton = (Button)nextChildAsTree.Header;

                foreach (string nodeName in xmlObjectsListWrapper.allTopLevelTags)
                {
                    if (nodeName.Contains(nextChildTreeButton.Uid))
                    {
                        xmlOut += GenerateAppendXmlForObject(xmlObjectsListWrapper, nextChildAsTree, nodeName);
                    }
                }
                if (nextChildAsTree.Tag != null)
                {
                    bool doIgnore = (bool)nextChildAsTree.Tag;
                    if (doIgnore) xmlOut = "";
                }
            }
            string parentPath = xmlObjectsListWrapper.xmlFile.ParentPath == null ? "" : xmlObjectsListWrapper.xmlFile.ParentPath;
            if (includeExistingData) existingWrapperFileData = XmlFileManager.ReadExistingFile(Path.Combine(parentPath, xmlObjectsListWrapper.xmlFile.FileName), Properties.Settings.Default.ModTagSetting);
            if (!String.IsNullOrEmpty(xmlOut) && !String.IsNullOrEmpty(existingWrapperFileData)) xmlOut += existingWrapperFileData;
            return xmlOut;
        }
        private static string GenerateAppendXmlForTargetObject(XmlObjectsListWrapper xmlObjectsListWrapper, TreeViewItem topTree, XmlNode currentXmlNode, string nodeName)
        {
            string[] actionSplit = topTree.Name.Split("_");
            string xPathAction = actionSplit[0];
            string attributeInAction = "";
            if (actionSplit.Length > 1)
            {
                foreach (string nextString in actionSplit)
                {
                    if (!nextString.Equals(xPathAction)) attributeInAction += nextString + "_";
                }
                //Trim trailing _
                attributeInAction = attributeInAction.Substring(0, attributeInAction.Length - 1);
            }

            string attributeName = "";
            string nodeToSkip;
            //If it is insert before or insert after and a top tag we don't want to skip the first tag.
            if (((xPathAction.Equals(XPATH_ACTION_INSERT_AFTER) || xPathAction.Equals(XPATH_ACTION_INSERT_BEFORE)) && String.IsNullOrEmpty(attributeInAction)) && xmlObjectsListWrapper.allTopLevelTags.Contains(nodeName)) nodeToSkip = "";
            else nodeToSkip = nodeName;

            string generatedXml = GenerateXmlForObject(topTree, "", nodeToSkip, xPathAction, nodeName, 1);
            
            if (actionSplit[0].Equals(XPATH_ACTION_SET_ATTRIBUTE) 
                || ((actionSplit[0].Equals(XPATH_ACTION_INSERT_AFTER ) || actionSplit[0].Equals(XPATH_ACTION_INSERT_BEFORE)) 
                    && !String.IsNullOrEmpty(attributeInAction))) attributeName = ((topTree.Items.GetItemAt(0) as TreeViewItem).Header as TextBox).Text;

            string xmlOut = GenerateXpathTagetPath(currentXmlNode, xmlObjectsListWrapper.TopTagName, generatedXml, xPathAction, attributeInAction, attributeName);
            return xmlOut;
        }
        private static string GenerateXpathTagetPath(XmlNode currentXmlNode, string topTagName, string generatedXml, string xpathAction, string attributeInAction, string attributeName)
        {
            if (String.IsNullOrEmpty(generatedXml) && !xpathAction.Equals(XPATH_ACTION_REMOVE)) return "";

            string startingXml = "\t<" + xpathAction + " xpath=\"";

            XmlNode nextParentNode = currentXmlNode;
            //
            string pathToParent = "";
            do
            {
                XmlAttribute attributeToUse = nextParentNode.GetAvailableAttribute();
                if (attributeToUse != null)
                {
                    string targetString = attributeToUse != null ? "[@" + attributeToUse.Name + "='" + attributeToUse.Value + "']" : "";
                    pathToParent = "/" + nextParentNode.Name + targetString + pathToParent;
                }
                else pathToParent = "/" + nextParentNode.Name + pathToParent;
                if (nextParentNode.ParentNode != null) nextParentNode = nextParentNode.ParentNode;
                else break;
            } while (!nextParentNode.Name.Equals(topTagName));
            if (!String.IsNullOrEmpty(attributeInAction)) attributeInAction = "/@" + attributeInAction;
            if (!String.IsNullOrEmpty(attributeName) && xpathAction.Equals(XPATH_ACTION_SET_ATTRIBUTE)) attributeInAction = "\" name=\"" + attributeName + "\"";
            else if(!String.IsNullOrEmpty(attributeName)) attributeInAction += "\" name=\"" + attributeName;

            pathToParent = "/" + topTagName + pathToParent + attributeInAction;
            //                  if the action is not a one line type action and attribute in action is null
            string endingXml;
            if (!xpathAction.Equals(XPATH_ACTION_APPEND) && !xpathAction.Equals(XPATH_ACTION_INSERT_AFTER) && !xpathAction.Equals(XPATH_ACTION_INSERT_BEFORE))
            {
                endingXml = "\">" + generatedXml.Trim() + "</" + xpathAction + ">\n";
            }
            else if (!String.IsNullOrEmpty(attributeInAction))
            {
                endingXml = "\">" + generatedXml.Trim() + "</" + xpathAction + ">\n";
            }
            else endingXml = "\">\n" + generatedXml + "\t</" + xpathAction + ">\n";
            if (xpathAction.Equals(XPATH_ACTION_SET_ATTRIBUTE)) endingXml = ">" + generatedXml.Trim() + "</" + xpathAction + ">\n";
            if (xpathAction.Equals(XPATH_ACTION_REMOVE)) endingXml = "\"/>\n";

            return startingXml + pathToParent + endingXml;
        }
        private static string GenerateAppendXmlForObject(XmlObjectsListWrapper xmlObjectsListWrapper, TreeViewItem nextChildAsTree, string nodeName)
        {
            string generatedXml = GenerateXmlForObject(nextChildAsTree, targetNode: nodeName, level: 1);
            string xmlOut = "";
            if (generatedXml.Length > 0)
            {
                xmlOut = "\t<append xpath=\"/" + xmlObjectsListWrapper.TopTagName + "\">\n" + generatedXml + "\t</append>\n";
            }
            if (xmlObjectsListWrapper.TopTagName == StringConstants.PROGRESSION_TAG_NAME)
            {
                generatedXml = GenerateXmlForObject(nextChildAsTree, nodeToSkip: nodeName, level: 1);
                if (generatedXml.Length > 0) xmlOut = "\t<append xpath=\"/" + xmlObjectsListWrapper.TopTagName + "/" + nodeName + "\">\n" + generatedXml + "\t</append>\n";
            }
            return xmlOut;
        }
        //This has to return an empty string if the xml is invalid
        private static string GenerateXmlForObject(TreeViewItem nextTreeItem, string xmlOut = "", string nodeToSkip = "", string xPathAction = "", string targetNode = null, int level = 0)
        {
            if (nextTreeItem == null) return "";
            level++;
            string tabs = "";
            for (int i = 0; i < level; i++) tabs += "\t";
            if (nextTreeItem.Name.Equals(ATTRIBUTE_VALUE))
            {
                if (nextTreeItem.Header.GetType() == typeof(TextBox)) xmlOut = (nextTreeItem.Header as TextBox).Text;
                else if (nextTreeItem.Header.GetType() == typeof(ComboBox)) xmlOut = (nextTreeItem.Header as ComboBox).Text;
                if (!String.IsNullOrEmpty(xmlOut)) return tabs + xmlOut + "\n";
                else return "";
            }
            if (nextTreeItem.Name.Equals(ATTRIBUTE_NAME)) return "";


            Button headerAsButton = nextTreeItem.Header as Button;
            //If the target node is null use the treeitem header 
            string targetNodeContent = targetNode ?? headerAsButton.Uid;
            if (nextTreeItem.ChildIsCheckBox())
            {
                CheckBox treeViewChildCheckBox = nextTreeItem.Items[0] as CheckBox;
                string valueToReturn = null;
                if (treeViewChildCheckBox.IsChecked.Value)
                {
                    valueToReturn = tabs + "<" + targetNodeContent + "/>\n";
                }
                return valueToReturn;
            }
            bool didAddAttributes = AddTagWithAttributes(nextTreeItem, ref xmlOut, targetNodeContent);
            if (didAddAttributes) xmlOut = tabs + xmlOut;

            List<TreeViewItem> tVChildren = nextTreeItem.GetTreeViewChildren();
            //There are children trees to check
            if (tVChildren != null && tVChildren.Count > 0)
            {
                string childXml = "";
                foreach (TreeViewItem childTreeView in tVChildren)
                {
                    childXml += GenerateXmlForObject(childTreeView, "", "", xPathAction, null, level);
                }
                //There is child xml
                if (!String.IsNullOrEmpty(childXml))
                {
                    //if there aren't attributes print top tag
                    if (!didAddAttributes && targetNodeContent != nodeToSkip ) xmlOut += tabs + "<" + targetNodeContent + ">\n";
                    if (targetNodeContent != nodeToSkip) xmlOut += childXml + tabs + "</" + targetNodeContent + ">\n";
                    else xmlOut += childXml;
                }
                //there are child trees and attributes for the tag but no children xml 
                //Need to remove the previous closing tag and add the xml closing tag
                else if (!String.IsNullOrEmpty(xmlOut) && didAddAttributes)
                {
                    xmlOut = xmlOut.Substring(0, xmlOut.Length - 3) + "/>\n";
                }
            }
            //There were attributes but no children trees, add closing tag.
            else if ((didAddAttributes && targetNodeContent != nodeToSkip))
            {
                xmlOut += "/>\n";
            }
            return xmlOut;
        }
        private static bool AddTagWithAttributes(TreeViewItem nextTreeItem, ref string xmlOut, string headerContent)
        {
            bool hasFoundItem = false, didWriteStart = false;
            foreach (Control nextControl in nextTreeItem.Items)
            {
                if (nextControl.GetType() == typeof(ComboBox))
                {
                    ComboBox nextControlAsBox = (ComboBox)nextControl;
                    if (nextControlAsBox.Text.Trim().Length > 0)
                    {
                        hasFoundItem = true;
                        if (!didWriteStart) xmlOut += "<" + headerContent;
                        xmlOut += " " + nextControlAsBox.Tag + "=\"" + nextControlAsBox.Text.Trim() + "\" ";
                        didWriteStart = true;
                    }
                }
            }
            List<TreeViewItem> tVChildren = nextTreeItem.GetTreeViewChildren();
            //If there are children trees and attributes were added to the tag.
            if (tVChildren != null && tVChildren.Count > 0 && hasFoundItem) xmlOut += ">\n";
            return hasFoundItem;
        }
        public static string GenerateXmlViewOutput(MyStackPanel newObjectFormsPanel)
        {
            string addedViewTextStart = " <!--WARNING: Direct text edits made here will NOT be saved.-->\n\n" +
             "<!--To make direct file edits you can select a file below and open the direct editor window for the file.-->\n\n" +
             "<!--You can also make direct changes to the file(s) at the current output location: \n" + XmlFileManager._ModConfigOutputPath + "-->\n";
            string unsavedGeneratedXmlStart = "<!-- -------------------------------------- Current Unsaved XML ----------------------------------- -->\n\n";

            string unsavedGeneratedXmlEnd = "\n\n<!-- --------------------------------------------------------------------------------------------------------- -->\n\n";

            //foreach (XmlObjectsListWrapper xmlObjectsListWrapper in newObjectFormsPanel.LoadedListWrappers.Values)
            //{
            //    string parentPath = xmlObjectsListWrapper.xmlFile.ParentPath == null ? "" : xmlObjectsListWrapper.xmlFile.ParentPath;
            //    existingWrapperFileData += XmlFileManager.ReadExistingFile(Path.Combine(parentPath, xmlObjectsListWrapper.xmlFile.FileName), Properties.Settings.Default.ModTagSetting);
            //}

            string allGeneratedXml = GenerateXmlForObjectView(newObjectFormsPanel);
            return addedViewTextStart + unsavedGeneratedXmlStart + allGeneratedXml + unsavedGeneratedXmlEnd ;
        }
    }
}
