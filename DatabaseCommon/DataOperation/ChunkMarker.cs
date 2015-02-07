using System;

namespace Database.Common.DataOperation
{
    /// <summary>
    /// Represents a marker between chunks in the database.
    /// </summary>
    public class ChunkMarker
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

            return new ChunkMarker(s.Substring(5));
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

        /// <summary>
        /// Converts a <see cref="ChunkMarker"/> to a string for easy network transfer.
        /// </summary>
        /// <returns>The current value as a string.</returns>
        public string ConvertToString()
        {
            switch (_type)
            {
                case ChunkMarkerType.Start:
                    return "start";

                case ChunkMarkerType.End:
                    return "end";

                default:
                    return "value" + _value;
            }
        }
    }
}