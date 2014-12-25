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

        protected abstract void Save(XmlDocument document, XmlNode root);
    }
}