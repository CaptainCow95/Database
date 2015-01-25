using System;
using System.Globalization;

namespace Database.Common.DataOperation
{
    /// <summary>
    /// Represents a unique object id.
    /// </summary>
    public class ObjectId : IComparable
    {
        /// <summary>
        /// The first part of the id.
        /// </summary>
        private readonly int _int0;

        /// <summary>
        /// The second part of the id.
        /// </summary>
        private readonly int _int1;

        /// <summary>
        /// The third part of the id.
        /// </summary>
        private readonly int _int2;

        /// <summary>
        /// The fourth part of the id.
        /// </summary>
        private readonly int _int3;

        /// <summary>
        /// The fifth part of the id.
        /// </summary>
        private readonly int _int4;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectId"/> class.
        /// </summary>
        /// <param name="systemId">The id of the system.</param>
        /// <param name="counterValue">The current counter value.</param>
        public ObjectId(Guid systemId, int counterValue)
        {
            byte[] bytes = systemId.ToByteArray();
            _int0 = (bytes[0] << 24) + (bytes[1] << 16) + (bytes[2] << 8) + bytes[3];
            _int1 = (bytes[4] << 24) + (bytes[5] << 16) + (bytes[6] << 8) + bytes[7];
            _int2 = (bytes[8] << 24) + (bytes[9] << 16) + (bytes[10] << 8) + bytes[11];
            _int3 = (bytes[12] << 24) + (bytes[13] << 16) + (bytes[14] << 8) + bytes[15];
            _int4 = counterValue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectId"/> class.
        /// </summary>
        /// <param name="s">The string to initialize from.</param>
        public ObjectId(string s)
        {
            if (s.Length != 40)
            {
                throw new ArgumentException("The string is of the wrong length, it should be a 20 character hex string.");
            }

            _int0 = int.Parse(s.Substring(0, 8), NumberStyles.AllowHexSpecifier);
            _int1 = int.Parse(s.Substring(8, 8), NumberStyles.AllowHexSpecifier);
            _int2 = int.Parse(s.Substring(16, 8), NumberStyles.AllowHexSpecifier);
            _int3 = int.Parse(s.Substring(24, 8), NumberStyles.AllowHexSpecifier);
            _int4 = int.Parse(s.Substring(32, 8), NumberStyles.AllowHexSpecifier);
        }

        /// <inheritdoc />
        public int CompareTo(object obj)
        {
            return string.Compare(ToString(), obj.ToString(), StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            ObjectId id = obj as ObjectId;
            if (id == null)
            {
                return false;
            }

            return id._int0 == _int0 && id._int1 == _int1 && id._int2 == _int2 && id._int3 == _int3 && id._int4 == _int4;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _int4;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return _int0.ToString("X8") + _int1.ToString("X8") + _int2.ToString("X8") + _int3.ToString("X8") + _int4.ToString("X8");
        }
    }
}