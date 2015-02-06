using System;
using System.Collections.Generic;
using System.Linq;

namespace Database.Common.DataOperation
{
    /// <summary>
    /// Represents a query against a single item.
    /// </summary>
    public class QueryItem
    {
        /// <summary>
        /// The key this query is against.
        /// </summary>
        private readonly string _key;

        /// <summary>
        /// The different parts of the query.
        /// </summary>
        private readonly List<QueryItemPart> _parts = new List<QueryItemPart>();

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryItem"/> class.
        /// </summary>
        /// <param name="entry">The entry to generate the query from.</param>
        public QueryItem(DocumentEntry entry)
        {
            _key = entry.Key;

            if (entry.ValueType != DocumentEntryType.Document)
            {
                _parts.Add(new QueryItemPart(QueryItemPartType.Equal, entry));
                return;
            }

            foreach (var item in entry.ValueAsDocument)
            {
                GenerateQueryItem(item.Key, item.Value);
            }
        }

        /// <summary>
        /// The different types of queries.
        /// </summary>
        private enum QueryItemPartType
        {
            /// <summary>
            /// A less then query.
            /// </summary>
            LessThen,

            /// <summary>
            /// A less then or equal to query.
            /// </summary>
            LessThenEqualTo,

            /// <summary>
            /// An equal to query.
            /// </summary>
            Equal,

            /// <summary>
            /// A not equal to query.
            /// </summary>
            NotEqual,

            /// <summary>
            /// A greater then query.
            /// </summary>
            GreaterThen,

            /// <summary>
            /// A greater then or equal to query.
            /// </summary>
            GreaterThenEqualTo,

            /// <summary>
            /// A contains query.
            /// </summary>
            Contains,

            /// <summary>
            /// A does not contain query.
            /// </summary>
            NotContains,
        }

        /// <summary>
        /// Checks to see if the document matches the current query.
        /// </summary>
        /// <param name="doc">The document to test.</param>
        /// <returns>True if the document matches the query, otherwise false.</returns>
        public bool Match(Document doc)
        {
            return doc.ContainsKey(_key) && _parts.All(e => MatchItem(doc, e));
        }

        /// <summary>
        /// Checks if a equals b.
        /// </summary>
        /// <param name="a">The left side of the operator.</param>
        /// <param name="b">The right side of the operator.</param>
        /// <returns>The result of the operator.</returns>
        private bool AreEqual(DocumentEntry a, DocumentEntry b)
        {
            if (a.ValueType != b.ValueType)
            {
                return false;
            }

            switch (a.ValueType)
            {
                case DocumentEntryType.Array:
                    return Equals(a.ValueAsArray, b.ValueAsArray);

                case DocumentEntryType.Boolean:
                    return a.ValueAsBoolean == b.ValueAsBoolean;

                case DocumentEntryType.Document:
                    return Equals(a.ValueAsDocument, b.ValueAsDocument);

                case DocumentEntryType.Float:
                    return Math.Abs(a.ValueAsFloat - b.ValueAsFloat) < float.Epsilon;

                case DocumentEntryType.Integer:
                    return a.ValueAsInteger == b.ValueAsInteger;

                case DocumentEntryType.String:
                    return a.ValueAsString == b.ValueAsString;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks if a contains b.
        /// </summary>
        /// <param name="a">The left side of the operator.</param>
        /// <param name="b">The right side of the operator.</param>
        /// <returns>The result of the operator.</returns>
        private bool Contains(DocumentEntry a, DocumentEntry b)
        {
            if (a.ValueType != DocumentEntryType.Array)
            {
                return false;
            }

            return a.ValueAsArray.Any(item => Equals(item.Value, b.Value));
        }

        /// <summary>
        /// Generates a query item from a document entry and adds it to the parts list.
        /// </summary>
        /// <param name="key">The entry's key.</param>
        /// <param name="value">The entry's value.</param>
        private void GenerateQueryItem(string key, DocumentEntry value)
        {
            switch (key)
            {
                case "lt":
                    if (value.ValueType == DocumentEntryType.Array ||
                        value.ValueType == DocumentEntryType.Boolean ||
                        value.ValueType == DocumentEntryType.Document)
                    {
                        throw new QueryException("DocumentEntryType \"" +
                                                 Enum.GetName(typeof(DocumentEntryType), value.ValueType) +
                                                 "\" is not supported on the \"lt\" query.");
                    }

                    _parts.Add(new QueryItemPart(QueryItemPartType.LessThen, value));
                    break;

                case "lte":
                    if (value.ValueType == DocumentEntryType.Array ||
                        value.ValueType == DocumentEntryType.Boolean ||
                        value.ValueType == DocumentEntryType.Document)
                    {
                        throw new QueryException("DocumentEntryType \"" +
                                                 Enum.GetName(typeof(DocumentEntryType), value.ValueType) +
                                                 "\" is not supported on the \"lte\" query.");
                    }

                    _parts.Add(new QueryItemPart(QueryItemPartType.LessThenEqualTo, value));
                    break;

                case "eq":
                    _parts.Add(new QueryItemPart(QueryItemPartType.Equal, value));
                    break;

                case "neq":
                    _parts.Add(new QueryItemPart(QueryItemPartType.NotEqual, value));
                    break;

                case "gt":
                    if (value.ValueType == DocumentEntryType.Array ||
                        value.ValueType == DocumentEntryType.Boolean ||
                        value.ValueType == DocumentEntryType.Document)
                    {
                        throw new QueryException("DocumentEntryType \"" +
                                                 Enum.GetName(typeof(DocumentEntryType), value.ValueType) +
                                                 "\" is not supported on the \"gt\" query.");
                    }

                    _parts.Add(new QueryItemPart(QueryItemPartType.GreaterThen, value));
                    break;

                case "gte":
                    if (value.ValueType == DocumentEntryType.Array ||
                        value.ValueType == DocumentEntryType.Boolean ||
                        value.ValueType == DocumentEntryType.Document)
                    {
                        throw new QueryException("DocumentEntryType \"" +
                                                 Enum.GetName(typeof(DocumentEntryType), value.ValueType) +
                                                 "\" is not supported on the \"gte\" query.");
                    }

                    _parts.Add(new QueryItemPart(QueryItemPartType.GreaterThenEqualTo, value));
                    break;

                case "in":
                    if (value.ValueType == DocumentEntryType.Array ||
                        value.ValueType == DocumentEntryType.Document)
                    {
                        throw new QueryException("DocumentEntryType \"" +
                                                 Enum.GetName(typeof(DocumentEntryType), value.ValueType) +
                                                 "\" is not supported on the \"in\" query.");
                    }

                    _parts.Add(new QueryItemPart(QueryItemPartType.Contains, value));
                    break;

                case "nin":
                    if (value.ValueType == DocumentEntryType.Array ||
                        value.ValueType == DocumentEntryType.Document)
                    {
                        throw new QueryException("DocumentEntryType \"" +
                                                 Enum.GetName(typeof(DocumentEntryType), value.ValueType) +
                                                 "\" is not supported on the \"nin\" query.");
                    }

                    _parts.Add(new QueryItemPart(QueryItemPartType.NotContains, value));
                    break;

                default:
                    throw new QueryException("Invalid query entry.");
            }
        }

        /// <summary>
        /// Checks if a is greater then b.
        /// </summary>
        /// <param name="a">The left side of the operator.</param>
        /// <param name="b">The right side of the operator.</param>
        /// <returns>The result of the operator.</returns>
        private bool GreaterThen(DocumentEntry a, DocumentEntry b)
        {
            if (a.ValueType != b.ValueType)
            {
                return false;
            }

            switch (a.ValueType)
            {
                case DocumentEntryType.Float:
                    return a.ValueAsFloat > b.ValueAsFloat;

                case DocumentEntryType.Integer:
                    return a.ValueAsInteger > b.ValueAsInteger;

                case DocumentEntryType.String:
                    return string.Compare(a.ValueAsString, b.ValueAsString, StringComparison.Ordinal) > 0;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks if a is greater then or equal to b.
        /// </summary>
        /// <param name="a">The left side of the operator.</param>
        /// <param name="b">The right side of the operator.</param>
        /// <returns>The result of the operator.</returns>
        private bool GreaterThenEqualTo(DocumentEntry a, DocumentEntry b)
        {
            if (a.ValueType != b.ValueType)
            {
                return false;
            }

            switch (a.ValueType)
            {
                case DocumentEntryType.Float:
                    return a.ValueAsFloat >= b.ValueAsFloat;

                case DocumentEntryType.Integer:
                    return a.ValueAsInteger >= b.ValueAsInteger;

                case DocumentEntryType.String:
                    return string.Compare(a.ValueAsString, b.ValueAsString, StringComparison.Ordinal) >= 0;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks if a is less then b.
        /// </summary>
        /// <param name="a">The left side of the operator.</param>
        /// <param name="b">The right side of the operator.</param>
        /// <returns>The result of the operator.</returns>
        private bool LessThen(DocumentEntry a, DocumentEntry b)
        {
            if (a.ValueType != b.ValueType)
            {
                return false;
            }

            switch (a.ValueType)
            {
                case DocumentEntryType.Float:
                    return a.ValueAsFloat < b.ValueAsFloat;

                case DocumentEntryType.Integer:
                    return a.ValueAsInteger < b.ValueAsInteger;

                case DocumentEntryType.String:
                    return string.Compare(a.ValueAsString, b.ValueAsString, StringComparison.Ordinal) < 0;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks if a is less then or equal to b.
        /// </summary>
        /// <param name="a">The left side of the operator.</param>
        /// <param name="b">The right side of the operator.</param>
        /// <returns>The result of the operator.</returns>
        private bool LessThenEqualTo(DocumentEntry a, DocumentEntry b)
        {
            if (a.ValueType != b.ValueType)
            {
                return false;
            }

            switch (a.ValueType)
            {
                case DocumentEntryType.Float:
                    return a.ValueAsFloat <= b.ValueAsFloat;

                case DocumentEntryType.Integer:
                    return a.ValueAsInteger <= b.ValueAsInteger;

                case DocumentEntryType.String:
                    return string.Compare(a.ValueAsString, b.ValueAsString, StringComparison.Ordinal) <= 0;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks to see if item at the key matches the query item.
        /// </summary>
        /// <param name="doc">The document to look in.</param>
        /// <param name="part">The query item to match against.</param>
        /// <returns>True if the document matches the query item, otherwise false.</returns>
        private bool MatchItem(Document doc, QueryItemPart part)
        {
            switch (part.Type)
            {
                case QueryItemPartType.LessThen:
                    return LessThen(doc[_key], part.Value);

                case QueryItemPartType.LessThenEqualTo:
                    return LessThenEqualTo(doc[_key], part.Value);

                case QueryItemPartType.Equal:
                    return AreEqual(doc[_key], part.Value);

                case QueryItemPartType.NotEqual:
                    return !AreEqual(doc[_key], part.Value);

                case QueryItemPartType.GreaterThen:
                    return GreaterThen(doc[_key], part.Value);

                case QueryItemPartType.GreaterThenEqualTo:
                    return GreaterThenEqualTo(doc[_key], part.Value);

                case QueryItemPartType.Contains:
                    return Contains(doc[_key], part.Value);

                case QueryItemPartType.NotContains:
                    return !Contains(doc[_key], part.Value);

                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Represents part of a single query.
        /// </summary>
        private struct QueryItemPart
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="QueryItemPart"/> struct.
            /// </summary>
            /// <param name="type">The type of the value.</param>
            /// <param name="value">The value to be compared against.</param>
            public QueryItemPart(QueryItemPartType type, DocumentEntry value)
                : this()
            {
                Type = type;
                Value = value;
            }

            /// <summary>
            /// Gets the type.
            /// </summary>
            public QueryItemPartType Type { get; private set; }

            /// <summary>
            /// Gets the value.
            /// </summary>
            public DocumentEntry Value { get; private set; }
        }
    }
}