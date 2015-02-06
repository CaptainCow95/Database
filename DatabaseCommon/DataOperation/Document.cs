using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Database.Common.DataOperation
{
    /// <summary>
    /// Represents a JSON document.
    /// </summary>
    public class Document : IEnumerable<KeyValuePair<string, DocumentEntry>>
    {
        /// <summary>
        /// The entries in this document.
        /// </summary>
        private readonly Dictionary<string, DocumentEntry> _data = new Dictionary<string, DocumentEntry>();

        /// <summary>
        /// A value indicating whether the document is valid.
        /// </summary>
        private readonly bool _valid = true;

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
        public Document()
        {
            _valid = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Document"/> class.
        /// </summary>
        /// <param name="doc">The document to make a shallow copy of.</param>
        public Document(Document doc)
        {
            if (!doc.Valid)
            {
                _valid = false;
                return;
            }

            foreach (var item in doc)
            {
                _data.Add(item.Key, CopyItem(item.Value));
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
        /// Gets the count of items in the document.
        /// </summary>
        public int Count
        {
            get { return _data.Count; }
        }

        /// <summary>
        /// Gets a value indicating whether this document is valid.
        /// </summary>
        public bool Valid
        {
            get { return _valid; }
        }

        /// <inheritdoc />
        public DocumentEntry this[string key]
        {
            get
            {
                if (key.Contains("."))
                {
                    string subfield = key.Substring(0, key.IndexOf(".", StringComparison.InvariantCulture));
                    if (_data.ContainsKey(subfield) && _data[subfield].ValueType == DocumentEntryType.Document)
                    {
                        return _data[subfield].ValueAsDocument[key.Substring(subfield.Length + 1)];
                    }
                }
                else
                {
                    return _data[key];
                }

                throw new KeyNotFoundException();
            }

            set
            {
                if (key.Contains("."))
                {
                    string subfield = key.Substring(0, key.IndexOf(".", StringComparison.InvariantCulture));
                    if (_data.ContainsKey(subfield) && _data[subfield].ValueType == DocumentEntryType.Document)
                    {
                        _data[subfield].ValueAsDocument[key.Substring(subfield.Length + 1)] = value;
                    }
                }
                else
                {
                    _data[key] = value;
                }
            }
        }

        /// <summary>
        /// Checks for a document that contains sub-keys (A key that contains a period).
        /// </summary>
        /// <returns>True if the document contains sub-keys, otherwise false.</returns>
        public bool CheckForSubkeys()
        {
            foreach (var item in _data)
            {
                if (item.Key.Contains("."))
                {
                    return true;
                }

                if (item.Value.ValueType == DocumentEntryType.Document)
                {
                    return item.Value.ValueAsDocument.CheckForSubkeys();
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the document contains the specified key.
        /// </summary>
        /// <param name="key">The key to search for.</param>
        /// <returns>True if the key was found, otherwise false.</returns>
        public bool ContainsKey(string key)
        {
            if (key.Contains("."))
            {
                string subfield = key.Substring(0, key.IndexOf(".", StringComparison.InvariantCulture));
                if (_data.ContainsKey(subfield) && _data[subfield].ValueType == DocumentEntryType.Document)
                {
                    return _data[subfield].ValueAsDocument.ContainsKey(key.Substring(subfield.Length + 1));
                }
            }
            else
            {
                return _data.ContainsKey(key);
            }

            return false;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var doc = obj as Document;
            return doc != null && Equals(_data, doc._data);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _data.GetHashCode();
        }

        /// <inheritdoc />
        IEnumerator<KeyValuePair<string, DocumentEntry>> IEnumerable<KeyValuePair<string, DocumentEntry>>.GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        /// <summary>
        /// Merges another document into this one.
        /// </summary>
        /// <param name="doc">The document to merge.</param>
        public void Merge(Document doc)
        {
            foreach (var field in doc)
            {
                if (!_data.ContainsKey(field.Key))
                {
                    // field does not exist, create it.
                    _data.Add(field.Key, field.Value);
                }
                else
                {
                    if (field.Value.ValueType == DocumentEntryType.Document && _data[field.Key].ValueType == DocumentEntryType.Document)
                    {
                        // the incoming and current fields are both documents, merge them.
                        _data[field.Key].ValueAsDocument.Merge(field.Value.ValueAsDocument);
                    }
                    else if (_data[field.Key].ValueType == DocumentEntryType.Array && field.Value.ValueType == DocumentEntryType.Array)
                    {
                        // TODO: Add ability to both erase arrays and merge arrays.
                        _data[field.Key] = field.Value;
                    }
                    else
                    {
                        // Overwrite the current data.
                        _data[field.Key] = field.Value;
                    }
                }
            }
        }

        /// <summary>
        /// Removes the specified field from the document.
        /// </summary>
        /// <param name="field">The field to remove.</param>
        public void RemoveField(string field)
        {
            if (field.Contains("."))
            {
                // Referencing a subfield, call RemoveField on the sub document.
                string subfield = field.Substring(0, field.IndexOf(".", StringComparison.InvariantCulture));
                if (_data.ContainsKey(subfield) && _data[subfield].ValueType == DocumentEntryType.Document)
                {
                    _data[subfield].ValueAsDocument.RemoveField(field.Substring(subfield.Length + 1));
                }
            }
            else
            {
                // remove the field.
                if (_data.ContainsKey(field))
                {
                    _data.Remove(field);
                }
            }
        }

        /// <inheritdoc />
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _data.GetEnumerator();
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

        /// <summary>
        /// Shallow copies one item into another.
        /// </summary>
        /// <param name="item">The item to copy.</param>
        /// <returns>A shallow copy of the item.</returns>
        private DocumentEntry CopyItem(DocumentEntry item)
        {
            if (item.ValueType == DocumentEntryType.Document)
            {
                return new DocumentEntry(item.Key, DocumentEntryType.Document, new Document(item.ValueAsDocument));
            }

            if (item.ValueType == DocumentEntryType.Array)
            {
                List<DocumentEntry> copy = new List<DocumentEntry>();
                item.ValueAsArray.ForEach(e => copy.Add(CopyItem(e)));
                return new DocumentEntry(item.Key, DocumentEntryType.Array, copy);
            }

            return item;
        }
    }
}