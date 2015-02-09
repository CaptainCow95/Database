using Database.Common.DataOperation;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Database.Storage
{
    /// <summary>
    /// Represents a chunk of the database.
    /// </summary>
    public class DatabaseChunk : IComparable<DatabaseChunk>
    {
        /// <summary>
        /// The start of the chunk.
        /// </summary>
        private readonly ChunkMarker _start;

        /// <summary>
        /// The data contained by the chunk.
        /// </summary>
        private ConcurrentDictionary<ObjectId, Document> _data = new ConcurrentDictionary<ObjectId, Document>();

        /// <summary>
        /// The end of the chunk.
        /// </summary>
        private ChunkMarker _end;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseChunk"/> class.
        /// </summary>
        /// <param name="start">The start of the chunk.</param>
        /// <param name="end">The end of the chunk.</param>
        public DatabaseChunk(ChunkMarker start, ChunkMarker end)
        {
            _start = start;
            _end = end;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseChunk"/> class.
        /// </summary>
        /// <param name="start">The start of the chunk.</param>
        /// <param name="end">The end of the chunk.</param>
        /// <param name="data">The data to load the chunk with.</param>
        private DatabaseChunk(ChunkMarker start, ChunkMarker end, ConcurrentDictionary<ObjectId, Document> data)
        {
            _start = start;
            _end = end;
            _data = data;
        }

        /// <summary>
        /// Gets a count of the items in the chunk.
        /// </summary>
        public int Count
        {
            get { return _data.Count; }
        }

        /// <summary>
        /// Gets the end marker of the chunk.
        /// </summary>
        public ChunkMarker End
        {
            get { return _end; }
        }

        /// <summary>
        /// Gets the start marker of the chunk.
        /// </summary>
        public ChunkMarker Start
        {
            get { return _start; }
        }

        /// <inheritdoc />
        public int CompareTo(DatabaseChunk other)
        {
            if (other == null)
            {
                return 1;
            }

            return _start.CompareTo(other._start);
        }

        /// <summary>
        /// Merges this chunk with the one passed in.
        /// </summary>
        /// <param name="c">The chunk to merge.</param>
        /// <remarks>Make sure the chunks are sequential and that the chunk passed in comes after the current chunk.</remarks>
        public void Merge(DatabaseChunk c)
        {
            if (!Equals(_end, c._start))
            {
                throw new ArgumentException("The chunks are either not next to each other or you are trying to merge with the last chunk instead of the first.");
            }

            // Copy the data over.
            foreach (var item in c._data)
            {
                _data.TryAdd(item.Key, item.Value);
            }

            _end = c._end;
        }

        /// <summary>
        /// Queries the chunk.
        /// </summary>
        /// <param name="queryItems">The query to run against the chunk.</param>
        /// <returns>A list of the matching documents in the chunk.</returns>
        public List<Document> Query(List<QueryItem> queryItems)
        {
            return _data.Where(e => queryItems.All(query => query.Match(e.Value))).Select(e => e.Value).ToList();
        }

        /// <summary>
        /// Splits a chunk into two chunks.
        /// </summary>
        /// <returns>The new chunk created from the split.</returns>
        public DatabaseChunk Split()
        {
            var newData = new ConcurrentDictionary<ObjectId, Document>(_data.OrderBy(e => e.Key).Skip(_data.Count / 2).ToDictionary(e => e.Key, e => e.Value));
            _data = new ConcurrentDictionary<ObjectId, Document>(_data.OrderBy(e => e.Key).Take(_data.Count / 2));

            var oldEnd = _end;
            _end = new ChunkMarker(newData.Keys.Min().ToString());

            return new DatabaseChunk(_end, oldEnd, newData);
        }

        /// <summary>
        /// Tries to add an object to the chunk.
        /// </summary>
        /// <param name="id">The id of the object.</param>
        /// <param name="document">The document of the object.</param>
        /// <returns>A value indicating whether the add was successful or if the key already existed.</returns>
        public bool TryAdd(ObjectId id, Document document)
        {
            return _data.TryAdd(id, document);
        }

        /// <summary>
        /// Tries to get an object from the chunk.
        /// </summary>
        /// <param name="id">The id of the object.</param>
        /// <param name="value">The document that was retrieved.</param>
        /// <returns>True if the get was successful, otherwise false.</returns>
        public bool TryGet(ObjectId id, out Document value)
        {
            return _data.TryGetValue(id, out value);
        }

        /// <summary>
        /// Tries to remove an object from the chunk.
        /// </summary>
        /// <param name="id">The object to remove.</param>
        /// <param name="removed">The object that was removed.</param>
        /// <returns>True if the remove was successful, otherwise false.</returns>
        public bool TryRemove(ObjectId id, out Document removed)
        {
            return _data.TryRemove(id, out removed);
        }

        /// <summary>
        /// Tries to update a value in the chunk.
        /// </summary>
        /// <param name="id">The id to update at.</param>
        /// <param name="newValue">The new value to use.</param>
        /// <param name="oldValue">The old value to make sure it wasn't updated in the mean time.</param>
        /// <returns>True if the update was successful, otherwise false.</returns>
        public bool TryUpdate(ObjectId id, Document newValue, Document oldValue)
        {
            return _data.TryUpdate(id, newValue, oldValue);
        }
    }
}