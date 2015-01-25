using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Database.Common.DataOperation
{
    /// <summary>
    /// Represents an entry in a document.
    /// </summary>
    public class DocumentEntry
    {
        /// <summary>
        /// The entry's key.
        /// </summary>
        private readonly string _key;

        /// <summary>
        /// The entry's value.
        /// </summary>
        private readonly object _value;

        /// <summary>
        /// The type of the entry's value.
        /// </summary>
        private readonly DocumentEntryType _valueType;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentEntry"/> class.
        /// </summary>
        /// <param name="key">The key to use.</param>
        /// <param name="valueType">The type of the value.</param>
        /// <param name="value">The value to use.</param>
        public DocumentEntry(string key, DocumentEntryType valueType, object value)
        {
            _key = key;
            _valueType = valueType;
            _value = value;
        }

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
                        _value = ReadArray(reader);
                        _valueType = DocumentEntryType.Array;
                        break;

                    case JsonToken.StartObject:
                        _value = ReadDocument(reader);
                        _valueType = DocumentEntryType.Document;
                        break;

                    default:
                        var result = ReadValue(reader);
                        _value = result.Item1;
                        _valueType = result.Item2;
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
        /// Gets the value as an array.
        /// </summary>
        public List<DocumentEntry> ValueAsArray
        {
            get { return _value as List<DocumentEntry>; }
        }

        /// <summary>
        /// Gets the value as a boolean.
        /// </summary>
        public bool? ValueAsBoolean
        {
            get { return _value as bool?; }
        }

        /// <summary>
        /// Gets the value as a document.
        /// </summary>
        public Document ValueAsDocument
        {
            get { return _value as Document; }
        }

        /// <summary>
        /// Gets the value as a float.
        /// </summary>
        public float? ValueAsFloat
        {
            get { return _value as float?; }
        }

        /// <summary>
        /// Gets the value as an integer.
        /// </summary>
        public int? ValueAsInteger
        {
            get { return _value as int?; }
        }

        /// <summary>
        /// Gets the value as a string.
        /// </summary>
        public string ValueAsString
        {
            get { return _value as string; }
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

                case DocumentEntryType.Document:
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
        /// <param name="reader">The reader to read from.</param>]
        /// <returns>The result of the reading the array.</returns>
        private List<DocumentEntry> ReadArray(JsonTextReader reader)
        {
            List<DocumentEntry> entries = new List<DocumentEntry>();
            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                entries.Add(new DocumentEntry(reader, true));
            }

            return entries;
        }

        /// <summary>
        /// Reads a document entry.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <returns>The result of reading the document.</returns>
        private Document ReadDocument(JsonTextReader reader)
        {
            return new Document(reader);
        }

        /// <summary>
        /// Reads the entry's value.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <returns>The result of reading the value.</returns>
        private Tuple<object, DocumentEntryType> ReadValue(JsonTextReader reader)
        {
            DocumentEntryType type;
            switch (reader.TokenType)
            {
                case JsonToken.Boolean:
                    type = DocumentEntryType.Boolean;
                    break;

                case JsonToken.Float:
                    type = DocumentEntryType.Float;
                    break;

                case JsonToken.Integer:
                    type = DocumentEntryType.Integer;
                    break;

                case JsonToken.String:
                    type = DocumentEntryType.String;
                    break;

                default:
                    throw new InvalidDocumentException("Could not read the specified json token type.");
            }

            return new Tuple<object, DocumentEntryType>(reader.Value, type);
        }
    }
}