using System;

namespace Database.Common.DataOperation
{
    /// <summary>
    /// Represents a marker between chunks in the database.
    /// </summary>
    public class ChunkMarker : IComparable<ChunkMarker>
    {
        /// <summary>
        /// The type of the marker.
        /// </summary>
        private readonly ChunkMarkerType _type;

        /// <summary>
        /// The value of the marker.
        /// </summary>
        private readonly string _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkMarker"/> class.
        /// </summary>
        /// <param name="type">The type of the marker this is.</param>
        public ChunkMarker(ChunkMarkerType type)
        {
            if (type == ChunkMarkerType.Value)
            {
                throw new ArgumentException("Use the other constructor when instantiating a value marker.");
            }

            _type = type;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkMarker"/> class.
        /// </summary>
        /// <param name="value">The value this marker represents.</param>
        public ChunkMarker(string value)
        {
            _type = ChunkMarkerType.Value;
            _value = value;
        }

        /// <summary>
        /// Converts a string to a <see cref="ChunkMarker"/>.
        /// </summary>
        /// <param name="s">The value to convert.</param>
        /// <returns>The <see cref="ChunkMarker"/> represented by the string.</returns>
        public static ChunkMarker ConvertFromString(string s)
        {
            if (s == "start")
            {
                return new ChunkMarker(ChunkMarkerType.Start);
            }

            if (s == "end")
            {
                return new ChunkMarker(ChunkMarkerType.End);
            }

            return new ChunkMarker(s);
        }

        /// <summary>
        /// Checks to see whether value is between a and b.
        /// </summary>
        /// <param name="a">The start of the range.</param>
        /// <param name="b">The end of the range.</param>
        /// <param name="value">The value to check.</param>
        /// <returns>A value indicating whether value is between a and b.</returns>
        public static bool IsBetween(ChunkMarker a, ChunkMarker b, string value)
        {
            if (a._type == ChunkMarkerType.End || b._type == ChunkMarkerType.Start)
            {
                return false;
            }

            bool afterA = a._type == ChunkMarkerType.Start || string.Compare(a._value, value, StringComparison.Ordinal) <= 0;
            bool beforeB = b._type == ChunkMarkerType.End || string.Compare(value, b._value, StringComparison.Ordinal) < 0;

            return afterA && beforeB;
        }

        /// <inheritdoc />
        public int CompareTo(ChunkMarker other)
        {
            if (other == null)
            {
                return -1;
            }

            if (_type == ChunkMarkerType.Start)
            {
                return other._type == ChunkMarkerType.Start ? 0 : -1;
            }

            if (_type == ChunkMarkerType.End)
            {
                return other._type == ChunkMarkerType.End ? 0 : 1;
            }

            if (other._type == ChunkMarkerType.Start)
            {
                return 1;
            }

            if (other._type == ChunkMarkerType.End)
            {
                return -1;
            }

            return string.Compare(_value, other._value, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            ChunkMarker marker = obj as ChunkMarker;
            if (marker == null)
            {
                return false;
            }

            if (_type == ChunkMarkerType.Value && marker._type == ChunkMarkerType.Value)
            {
                return _value == marker._value;
            }

            return _type == marker._type;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            switch (_type)
            {
                case ChunkMarkerType.Start:
                    return "start";

                case ChunkMarkerType.End:
                    return "end";

                default:
                    return _value;
            }
        }
    }
}