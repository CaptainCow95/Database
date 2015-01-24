using Newtonsoft.Json;
using System.Collections.Generic;

namespace Database.Common
{
    /// <summary>
    /// Represents an entry in a document.
    /// </summary>
    public class DocumentEntry
    {
        /// <summary>
        /// The entry's key.
        /// </summary>
        private string _key;

        /// <summary>
        /// The entry's value.
        /// </summary>
        private object _value;

        /// <summary>
        /// The type of the entry's value.
        /// </summary>
        private DocumentEntryType _valueType;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentEntry"/> class.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <param name="arrayEntry">A value indicating whether this is an entry in an array.</param>
        internal DocumentEntry(JsonTextReader reader, bool arrayEntry)
        {
            if (arrayEntry)
            {
                // Array entry, so there is no key.
                _key = string.Empty;
                ReadValue(reader);
            }
            else
            {
                _key = (string)reader.Value;
                reader.Read();

                switch (reader.TokenType)
                {
                    case JsonToken.StartArray:
                        ReadArray(reader);
                        _valueType = DocumentEntryType.Array;
                        break;

                    case JsonToken.StartObject:
                        ReadObject(reader);
                        _valueType = DocumentEntryType.Object;
                        break;

                    default:
                        ReadValue(reader);
                        break;
                }
            }
        }

        /// <summary>
        /// Gets the entry's key.
        /// </summary>
        public string Key
        {
            get { return _key; }
        }

        /// <summary>
        /// Gets the entry's value.
        /// </summary>
        public object Value
        {
            get { return _value; }
        }

        /// <summary>
        /// Gets the type of the entry's value.
        /// </summary>
        public DocumentEntryType ValueType
        {
            get { return _valueType; }
        }

        /// <summary>
        /// Writes this entry to the specified writer.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        internal void Write(JsonTextWriter writer)
        {
            switch (_valueType)
            {
                case DocumentEntryType.Array:
                    writer.WriteStartArray();

                    foreach (var item in (List<DocumentEntry>)_value)
                    {
                        item.Write(writer);
                    }

                    writer.WriteEndArray();
                    break;

                case DocumentEntryType.Object:
                    ((Document)_value).Write(writer);
                    break;

                default:
                    writer.WriteValue(_value);
                    break;
            }
        }

        /// <summary>
        /// Reads an array entry.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        private void ReadArray(JsonTextReader reader)
        {
            List<DocumentEntry> entries = new List<DocumentEntry>();
            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                entries.Add(new DocumentEntry(reader, true));
            }

            _value = entries;
        }

        /// <summary>
        /// Reads an object entry.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        private void ReadObject(JsonTextReader reader)
        {
            _value = new Document(reader);
        }

        /// <summary>
        /// Reads the entry's value.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        private void ReadValue(JsonTextReader reader)
        {
            _value = reader.Value;
            switch (reader.TokenType)
            {
                case JsonToken.Boolean:
                    _valueType = DocumentEntryType.Boolean;
                    break;

                case JsonToken.Float:
                    _valueType = DocumentEntryType.Float;
                    break;

                case JsonToken.Integer:
                    _valueType = DocumentEntryType.Integer;
                    break;

                case JsonToken.String:
                    _valueType = DocumentEntryType.String;
                    break;

                default:
                    throw new InvalidDocumentException();
            }
        }
    }
}