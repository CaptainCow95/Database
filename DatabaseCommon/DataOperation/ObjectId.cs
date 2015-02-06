using System;

namespace Database.Common.DataOperation
{
    /// <summary>
    /// Represents a unique object id.
    /// </summary>
    public class ObjectId : IComparable
    {
        /// <summary>
        /// The internal representation of the <see cref="ObjectId"/>.
        /// </summary>
        private readonly Guid _internalId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectId"/> class.
        /// </summary>
        public ObjectId()
        {
            _internalId = Guid.NewGuid();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectId"/> class.
        /// </summary>
        /// <param name="s">The string to initialize from.</param>
        public ObjectId(string s)
        {
            _internalId = Guid.Parse(s);
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

            return Equals(_internalId, id._internalId);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _internalId.GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return _internalId.ToString();
        }
    }
}