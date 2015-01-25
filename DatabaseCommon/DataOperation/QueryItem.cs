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

            if (entry.ValueType == DocumentEntryType.Document)
            {
                Document doc = entry.ValueAsDocument;

                foreach (var item in doc)
                {
                    switch (item.Key)
                    {
                        case "lt":
                            if (item.Value.ValueType == DocumentEntryType.Array || item.Value.ValueType == DocumentEntryType.Boolean || item.Value.ValueType == DocumentEntryType.Document)
                            {
                                throw new QueryException("DocumentEntryType \"" + Enum.GetName(typeof(DocumentEntryType), item.Value.ValueType) + "\" is not supported on the \"lt\" query.");
                            }

                            _parts.Add(new QueryItemPart(QueryItemPartType.LessThen, item.Value));
                            break;

                        case "lte":
                            if (item.Value.ValueType == DocumentEntryType.Array || item.Value.ValueType == DocumentEntryType.Boolean || item.Value.ValueType == DocumentEntryType.Document)
                            {
                                throw new QueryException("DocumentEntryType \"" + Enum.GetName(typeof(DocumentEntryType), item.Value.ValueType) + "\" is not supported on the \"lte\" query.");
                            }

                            _parts.Add(new QueryItemPart(QueryItemPartType.LessThenEqualTo, item.Value));
                            break;

                        case "eq":
                            _parts.Add(new QueryItemPart(QueryItemPartType.Equal, item.Value));
                            break;

                        case "neq":
                            _parts.Add(new QueryItemPart(QueryItemPartType.NotEqual, item.Value));
                            break;

                        case "gt":
                            if (item.Value.ValueType == DocumentEntryType.Array || item.Value.ValueType == DocumentEntryType.Boolean || item.Value.ValueType == DocumentEntryType.Document)
                            {
                                throw new QueryException("DocumentEntryType \"" + Enum.GetName(typeof(DocumentEntryType), item.Value.ValueType) + "\" is not supported on the \"gt\" query.");
                            }

                            _parts.Add(new QueryItemPart(QueryItemPartType.GreaterThen, item.Value));
                            break;

                        case "gte":
                            if (item.Value.ValueType == DocumentEntryType.Array || item.Value.ValueType == DocumentEntryType.Boolean || item.Value.ValueType == DocumentEntryType.Document)
                            {
                                throw new QueryException("DocumentEntryType \"" + Enum.GetName(typeof(DocumentEntryType), item.Value.ValueType) + "\" is not supported on the \"gte\" query.");
                            }

                            _parts.Add(new QueryItemPart(QueryItemPartType.GreaterThenEqualTo, item.Value));
                            break;

                        case "in":
                            if (item.Value.ValueType == DocumentEntryType.Array || item.Value.ValueType == DocumentEntryType.Document)
                            {
                                throw new QueryException("DocumentEntryType \"" + Enum.GetName(typeof(DocumentEntryType), item.Value.ValueType) + "\" is not supported on the \"in\" query.");
                            }

                            _parts.Add(new QueryItemPart(QueryItemPartType.Contains, item.Value));
                            break;

                        case "nin":
                            if (item.Value.ValueType == DocumentEntryType.Array || item.Value.ValueType == DocumentEntryType.Document)
                            {
                                throw new QueryException("DocumentEntryType \"" + Enum.GetName(typeof(DocumentEntryType), item.Value.ValueType) + "\" is not supported on the \"nin\" query.");
                            }

                            _parts.Add(new QueryItemPart(QueryItemPartType.NotContains, item.Value));
                            break;

                        default:
                            throw new QueryException("Invalid query entry.");
                    }
                }
            }
            else
            {
                _parts.Add(new QueryItemPart(QueryItemPartType.Equal, entry));
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
            if (!doc.ContainsKey(_key))
            {
                return false;
            }

            foreach (var part in _parts)
            {
                switch (part.Type)
                {
                    case QueryItemPartType.LessThen:
                        if (!LessThen(doc[_key], part.Value))
                        {
                            return false;
                        }

                        break;

                    case QueryItemPartType.LessThenEqualTo:
                        if (!LessThenEqualTo(doc[_key], part.Value))
                        {
                            return false;
                        }

                        break;

                    case QueryItemPartType.Equal:
                        if (!Equals(doc[_key], part.Value))
                        {
                            return false;
                        }

                        break;

                    case QueryItemPartType.NotEqual:
                        if (Equals(doc[_key], part.Value))
                        {
                            return false;
                        }

                        break;

                    case QueryItemPartType.GreaterThen:
                        if (!GreaterThen(doc[_key], part.Value))
                        {
                            return false;
                        }

                        break;

                    case QueryItemPartType.GreaterThenEqualTo:
                        if (!GreaterThenEqualTo(doc[_key], part.Value))
                        {
                            return false;
                        }

                        break;

                    case QueryItemPartType.Contains:
                        if (!Contains(doc[_key], part.Value))
                        {
                            return false;
                        }

                        break;

                    case QueryItemPartType.NotContains:
                        if (Contains(doc[_key], part.Value))
                        {
                            return false;
                        }

                        break;
                }
            }

            return true;
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