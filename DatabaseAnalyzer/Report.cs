using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace DatabaseAnalyzer
{
    abstract class Report
    {
        public XmlDocument XmlDocument;
        public string Path;
        protected XmlElement RootElement;

        public abstract void Create();

        protected void CreateDocument(string rootName)
        {
            XmlDocument = new XmlDocument();
            RootElement = XmlDocument.CreateElement(rootName);
            XmlDocument.AppendChild(RootElement);
        }

        protected XmlElement AddChild(XmlElement parent, string tag)
        {
            XmlElement element = XmlDocument.CreateElement(tag);
            parent.AppendChild(element);
            return element;
        }

        protected XmlElement AddChild(string tag)
        {
            return AddChild(RootElement, tag);
        }

        protected XmlElement AddChild(XmlElement parent, string tag, string name)
        {
            XmlElement child = AddChild(parent, tag);
            child.SetAttribute("name", name);
            return child;
        }

        protected XmlElement AddChild(string tag, string name)
        {
            return AddChild(RootElement, tag, name);
        }

        protected void AddProperty(XmlElement parent, string name, string value)
        {
            XmlElement element = XmlDocument.CreateElement(name);
            parent.AppendChild(element);
            element.AppendChild(XmlDocument.CreateTextNode(value));
        }

        protected void AddProperty(string name, string value)
        {
            AddProperty(RootElement, name, value);
        }

        protected void AddProperty(XmlElement parent, string name, int value)
        {
            AddProperty(parent, name, value.ToString());
        }

        protected void AddProperty(string name, int value)
        {
            AddProperty(name, value.ToString());
        }

        protected void AddProperty(XmlElement parent, string name, double value)
        {
            AddProperty(parent, name, value.ToString("F0"));
        }

        protected void AddProperty(XmlElement parent, string name, double value, int precision)
        {
            AddProperty(parent, name, value.ToString("F" + precision.ToString()));
        }

        protected void AddProperty(string name, double value)
        {
            AddProperty(name, value.ToString("F0"));
        }

        protected void AddProperty(string name, double value, int precision)
        {
            AddProperty(name, value.ToString("F" + precision.ToString()));
        }

        public void Save()
        {
            XmlDocument.Save(Path);
        }

        public string FormatPercent(float value, int precision)
        {
            return (value * 100).ToString("F" + precision.ToString());
        }
    }
}
