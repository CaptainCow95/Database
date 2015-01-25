using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Database.Common
{
    /// <summary>
    /// A base class for managing and loading settings.
    /// </summary>
    public abstract class Settings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Settings"/> class.
        /// </summary>
        /// <param name="xml">The xml text to load from.</param>
        public Settings(string xml)
        {
            using (StringReader xmlStream = new StringReader(xml))
            {
                XmlDocument document = new XmlDocument();
                document.Load(xmlStream);

                Load(document.SelectSingleNode("Settings"));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Settings"/> class.
        /// </summary>
        public Settings()
        {
        }

        /// <inheritdoc />
        public override string ToString()
        {
            XmlDocument document = new XmlDocument();

            XmlNode root = document.CreateElement("Settings");
            document.AppendChild(root);

            Save(document, root);

            var writerSettings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = " ",
                Encoding = Encoding.UTF8
            };

            using (var stringWriter = new StringWriter())
            using (var xmlTextWriter = XmlWriter.Create(stringWriter, writerSettings))
            {
                document.WriteTo(xmlTextWriter);
                xmlTextWriter.Close();
                return stringWriter.GetStringBuilder().ToString();
            }
        }

        /// <summary>
        /// Loads settings from the root xml node.
        /// </summary>
        /// <param name="settings">The root xml node to read from.</param>
        protected abstract void Load(XmlNode settings);

        /// <summary>
        /// Reads a boolean from a node.
        /// </summary>
        /// <param name="parent">The parent of the node to read from.</param>
        /// <param name="name">The node to read from.</param>
        /// <param name="defaultValue">The default value should an error occur, or it doesn't exist.</param>
        /// <returns>The value read from the node.</returns>
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

                Logger.Log("Could not convert the value of " + name + " to a boolean, using the default value.", LogLevel.Error);
            }

            return defaultValue;
        }

        /// <summary>
        /// Reads an enumeration from a node.
        /// </summary>
        /// <typeparam name="T">The type of enumeration to read.</typeparam>
        /// <param name="parent">The parent of the node to read from.</param>
        /// <param name="name">The node to read from.</param>
        /// <param name="defaultValue">The default value should an error occur, or it doesn't exist.</param>
        /// <returns>The value read from the node.</returns>
        protected T ReadEnum<T>(XmlNode parent, string name, T defaultValue)
        {
            var value = ReadString(parent, name, string.Empty);
            try
            {
                T enumValue = (T)Enum.Parse(typeof(T), value);
                if (Enum.IsDefined(typeof(T), enumValue))
                {
                    return enumValue;
                }

                Logger.Log("\"" + value + "\" is not a valid option. Valid options are as follows: " + Enum.GetNames(typeof(T)).Aggregate((working, next) => working + ", " + next), LogLevel.Error);
            }
            catch
            {
                Logger.Log("Could not convert the value of " + name + " to a valid value, using the default value. Valid options are as follows: " + Enum.GetNames(typeof(T)).Aggregate((working, next) => working + ", " + next), LogLevel.Error);
            }

            return defaultValue;
        }

        /// <summary>
        /// Reads an integer from a node.
        /// </summary>
        /// <param name="parent">The parent of the node to read from.</param>
        /// <param name="name">The node to read from.</param>
        /// <param name="defaultValue">The default value should an error occur, or it doesn't exist.</param>
        /// <returns>The value read from the node.</returns>
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

                Logger.Log("Could not convert the value of " + name + " to an int, using the default value.", LogLevel.Error);
            }

            return defaultValue;
        }

        /// <summary>
        /// Reads a string from a node.
        /// </summary>
        /// <param name="parent">The parent of the node to read from.</param>
        /// <param name="name">The node to read from.</param>
        /// <param name="defaultValue">The default value should an error occur, or it doesn't exist.</param>
        /// <returns>The value read from the node.</returns>
        protected string ReadString(XmlNode parent, string name, string defaultValue)
        {
            var node = parent.SelectSingleNode(name);
            if (node != null)
            {
                return node.InnerText;
            }

            Logger.Log("Could not load setting " + name + ", using the default value.", LogLevel.Error);
            return defaultValue;
        }

        /// <summary>
        /// Saves the settings to a document with the given root node.
        /// </summary>
        /// <param name="document">The document to write to.</param>
        /// <param name="root">The root node of the document.</param>
        protected abstract void Save(XmlDocument document, XmlNode root);

        /// <summary>
        /// Writes a boolean to a node in the document.
        /// </summary>
        /// <param name="document">The document being written to.</param>
        /// <param name="name">The name of the node.</param>
        /// <param name="data">The boolean being written.</param>
        /// <param name="parent">The parent node to attach the written node to.</param>
        /// <returns>The node that was written.</returns>
        protected XmlNode WriteBoolean(XmlDocument document, string name, bool data, XmlNode parent)
        {
            return WriteString(document, name, data.ToString(CultureInfo.InvariantCulture), parent);
        }

        /// <summary>
        /// Writes an enumeration to a node in the document.
        /// </summary>
        /// <typeparam name="T">The type of enumeration to write.</typeparam>
        /// <param name="document">The document being written to.</param>
        /// <param name="name">The name of the node.</param>
        /// <param name="data">The enumeration being written.</param>
        /// <param name="parent">The parent node to attach the written node to.</param>
        /// <returns>The node that was written.</returns>
        protected XmlNode WriteEnum<T>(XmlDocument document, string name, T data, XmlNode parent)
        {
            return WriteString(document, name, Enum.GetName(typeof(T), data), parent);
        }

        /// <summary>
        /// Writes an integer to a node in the document.
        /// </summary>
        /// <param name="document">The document being written to.</param>
        /// <param name="name">The name of the node.</param>
        /// <param name="data">The integer being written.</param>
        /// <param name="parent">The parent node to attach the written node to.</param>
        /// <returns>The node that was written.</returns>
        protected XmlNode WriteInt32(XmlDocument document, string name, int data, XmlNode parent)
        {
            return WriteString(document, name, data.ToString(CultureInfo.InvariantCulture), parent);
        }

        /// <summary>
        /// Writes a string to a node in the document.
        /// </summary>
        /// <param name="document">The document being written to.</param>
        /// <param name="name">The name of the node.</param>
        /// <param name="data">The string being written.</param>
        /// <param name="parent">The parent node to attach the written node to.</param>
        /// <returns>The node that was written.</returns>
        protected XmlNode WriteString(XmlDocument document, string name, string data, XmlNode parent)
        {
            var node = document.CreateElement(name);
            node.InnerText = data;
            parent.AppendChild(node);

            return node;
        }
    }
}