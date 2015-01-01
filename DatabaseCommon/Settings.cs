using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace Database.Common
{
    public abstract class Settings
    {
        public Settings(string xml)
        {
            using (StringReader xmlStream = new StringReader(xml))
            {
                XmlDocument document = new XmlDocument();
                document.Load(xmlStream);

                Load(document.SelectSingleNode("Settings"));
            }
        }

        public Settings()
        {
        }

        public override string ToString()
        {
            XmlDocument document = new XmlDocument();

            XmlNode root = document.CreateElement("Settings");
            document.AppendChild(root);

            Save(document, root);

            var writerSettings = new XmlWriterSettings();
            writerSettings.Indent = true;
            writerSettings.IndentChars = " ";
            writerSettings.Encoding = Encoding.UTF8;

            using (var stringWriter = new StringWriter())
            using (var xmlTextWriter = XmlWriter.Create(stringWriter, writerSettings))
            {
                document.WriteTo(xmlTextWriter);
                xmlTextWriter.Close();
                return stringWriter.GetStringBuilder().ToString();
            }
        }

        protected abstract void Load(XmlNode settings);

        protected bool ReadBoolean(XmlNode parent, string name, bool defaultValue)
        {
            var value = ReadString(parent, name, string.Empty);
            if (!string.IsNullOrEmpty(value))
            {
                bool result;
                if (bool.TryParse(value, out result))
                {
                    return result;
                }

                Logger.Log("Could not convert the value of " + name + " to a boolean, using the default value.");
            }

            return defaultValue;
        }

        protected int ReadInt32(XmlNode parent, string name, int defaultValue)
        {
            var value = ReadString(parent, name, string.Empty);
            if (!string.IsNullOrEmpty(value))
            {
                int result;
                if (int.TryParse(value, out result))
                {
                    return result;
                }

                Logger.Log("Could not convert the value of " + name + " to an int, using the default value.");
            }

            return defaultValue;
        }

        protected string ReadString(XmlNode parent, string name, string defaultValue)
        {
            var node = parent.SelectSingleNode(name);
            if (node != null)
            {
                return node.InnerText;
            }

            Logger.Log("Could not load setting " + name + ", using the default value.");
            return defaultValue;
        }

        protected abstract void Save(XmlDocument document, XmlNode root);

        protected XmlNode WriteBoolean(XmlDocument document, string name, bool data, XmlNode parent)
        {
            return WriteString(document, name, data.ToString(CultureInfo.InvariantCulture), parent);
        }

        protected XmlNode WriteInt32(XmlDocument document, string name, int data, XmlNode parent)
        {
            return WriteString(document, name, data.ToString(CultureInfo.InvariantCulture), parent);
        }

        protected XmlNode WriteString(XmlDocument document, string name, string data, XmlNode parent)
        {
            var node = document.CreateElement(name);
            node.InnerText = data;
            parent.AppendChild(node);

            return node;
        }
    }
}