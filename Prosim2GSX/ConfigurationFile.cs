﻿using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Prosim2GSX
{
    public class ConfigurationFile
    {
        private Dictionary<string, string> appSettings = new();
        private XmlDocument xmlDoc = new();

        public string this[string key]
        {
            get => GetSetting(key);
            set => SetSetting(key, value);
        }

        public void LoadConfiguration()
        {
            xmlDoc = new();
            
            // Use XmlReaderSettings for better security and performance in .NET 8.0
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit, // Prevent DTD processing for security
                MaxCharactersFromEntities = 1024 * 1024, // Limit entity expansion
                XmlResolver = null // Prevent external entity resolution
            };
            
            using (var reader = XmlReader.Create(App.ConfigFile, settings))
            {
                xmlDoc.Load(reader);
            }

            XmlNode xmlSettings = xmlDoc.ChildNodes[1];
            appSettings.Clear();
            foreach(XmlNode child in xmlSettings.ChildNodes)
                appSettings.Add(child.Attributes["key"].Value, child.Attributes["value"].Value);
        }

        public void SaveConfiguration()
        {
            foreach (XmlNode child in xmlDoc.ChildNodes[1])
                child.Attributes["value"].Value = appSettings[child.Attributes["key"].Value];

            // Use XmlWriterSettings for better control in .NET 8.0
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\n",
                NewLineHandling = NewLineHandling.Replace
            };
            
            using (var writer = XmlWriter.Create(App.ConfigFile, settings))
            {
                xmlDoc.Save(writer);
            }
        }

        public string GetSetting(string key, string defaultValue = "")
        {
            if (appSettings.TryGetValue(key, out string value))
                return value;
            else
            {
                XmlNode newNode = xmlDoc.CreateElement("add");

                XmlAttribute attribute = xmlDoc.CreateAttribute("key");
                attribute.Value = key;
                newNode.Attributes.Append(attribute);

                attribute = xmlDoc.CreateAttribute("value");
                attribute.Value = defaultValue;
                newNode.Attributes.Append(attribute);

                xmlDoc.ChildNodes[1].AppendChild(newNode);
                appSettings.Add(key, defaultValue);
                SaveConfiguration();

                return defaultValue;
            }
        }

        public void SetSetting(string key, string value)
        {
            if (appSettings.ContainsKey(key))
            {
                appSettings[key] = value;
                SaveConfiguration();
            }
        }
    }
}
