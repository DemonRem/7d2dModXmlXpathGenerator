﻿using System;
using System.IO;
using System.Xml;

namespace SevenDaysToDieModCreator.Models
{
    public class XmlFileObject
    {
        public string FileName { get; private set; }
        public XmlDocument xmlDocument { get; private set; }
        public XmlFileObject(string directory)
        {
            LoadFile(directory);
            FileName = ParseFileName(directory);
        }
        public void LoadFile(string path)
        {
            xmlDocument = new XmlDocument();
            xmlDocument.PreserveWhitespace = true;
            try
            {
                xmlDocument.Load(path);
            }
            catch (System.IO.FileNotFoundException)
            {
                Console.Error.Write("File Not Found!");
            }
        }
        public string GetFileNameWithoutExtension() 
        {
            //Expected fileName Before: "recipes.xml" After: "recipes"
            return FileName.Substring(0, FileName.Length - 4);
        }
        private string ParseFileName(string directory)
        {
            return Path.GetFileName(directory);
        }
    }
}