using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Database.Common
{
    /// <summary>
    /// Represents a JSON document.
    /// </summary>
    public class Document
    {
        /// <summary>
        /// The entries in this document.
        /// </summary>
        private Dictionary<string, DocumentEntry> _data = new Dictionary<string, DocumentEntry>();

        /// <summary>
        /// A value indicating whether the document is valid.
        /// </summary>
        private bool _valid = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="Document"/> class.
        /// </summary>
        /// <param name="json">The JSON to initialize the document with.</param>
        public Document(string json)
        {
            try
            {
                using (StringReader strReader = new StringReader(json))
                using (JsonTextReader reader = new JsonTextReader(strReader))
                {
                    reader.Read();
                    if (reader.TokenType != JsonToken.StartObject)
                    {
                        throw new InvalidDocumentException();
                    }

                    while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                    {
                        DocumentEntry entry = new DocumentEntry(reader, false);
                        _data.Add(entry.Key, entry);
                    }

                    if (reader.TokenType != JsonToken.EndObject)
                    {
                        throw new InvalidDocumentException();
                    }
                }
            }
            catch (InvalidDocumentException)
            {
                _valid = false;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Document"/> class.
        /// </summary>
        /// <param name="reader">The JSON reader to initialize the document with.</param>
        internal Document(JsonTextReader reader)
        {
            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                DocumentEntry entry = new DocumentEntry(reader, false);
                _data.Add(entry.Key, entry);
            }

            if (reader.TokenType != JsonToken.EndObject)
            {
                throw new InvalidDocumentException();
            }
        }

        /// <summary>
        /// Gets a value indicating whether this document is valid.
        /// </summary>
        public bool Valid
        {
            get { return _valid; }
        }

        /// <summary>
        /// Converts a document to JSON.
        /// </summary>
        /// <returns>The JSON representing the document.</returns>
        public string ToJson()
        {
            StringBuilder builder = new StringBuilder();

            using (StringWriter strWriter = new StringWriter(builder))
            using (JsonTextWriter writer = new JsonTextWriter(strWriter))
            {
                Write(writer);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Writes this document to the specified writer.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        internal void Write(JsonTextWriter writer)
        {
            writer.WriteStartObject();

            foreach (var item in _data)
            {
                writer.WritePropertyName(item.Key);
                item.Value.Write(writer);
            }

            writer.WriteEndObject();
        }
    }
}