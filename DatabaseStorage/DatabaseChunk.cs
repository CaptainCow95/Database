using Database.Common.DataOperation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Database.Storage
{
    /// <summary>
    /// Represents a chunk of the database.
    /// </summary>
    public class DatabaseChunk
    {
        /// <summary>
        /// The data contained by the chunk.
        /// </summary>
        private readonly SortedDictionary<ObjectId, Document> _data = new SortedDictionary<ObjectId, Document>();

        /// <summary>
        /// The start of the chunk.
        /// </summary>
        private readonly ChunkMarker _start;

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
        private DatabaseChunk(ChunkMarker start, ChunkMarker end, SortedDictionary<ObjectId, Document> data)
        {
            _start = start;
            _end = end;
            _data = new SortedDictionary<ObjectId, Document>(data);
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

        /// <summary>
        /// Gets or sets an object at the specified id.
        /// </summary>
        /// <param name="id">The id to get or set at.</param>
        /// <returns>The <see cref="Document"/> contained at the specified id.</returns>
        public Document this[ObjectId id]
        {
            get { return _data[id]; }
            set { _data[id] = value; }
        }

        /// <summary>
        /// Adds an object to the chunk.
        /// </summary>
        /// <param name="id">The id of the object.</param>
        /// <param name="document">The document of the object.</param>
        /// <returns>A value indicating whether the add was successful or if the key already existed.</returns>
        public bool Add(ObjectId id, Document document)
        {
            if (_data.ContainsKey(id))
            {
                return false;
            }

            _data.Add(id, document);
            return true;
        }

        /// <summary>
        /// Combines this current chunk with the one passed in.
        /// </summary>
        /// <param name="c">The chunk to combine.</param>
        /// <remarks>Make sure the chunks are sequential and that the chunk passed in comes after the current chunk.</remarks>
        public void Combine(DatabaseChunk c)
        {
            if (!Equals(_end, c._start))
            {
                throw new ArgumentException("The chunks are either not next to each other or you are trying to combine with the last chunk instead of the first.");
            }

            // Copy the data over.
            foreach (var item in c._data)
            {
                _data.Add(item.Key, item.Value);
            }

            _end = c._end;
        }

        /// <summary>
        /// Checks whether this chunks contains an object.
        /// </summary>
        /// <param name="id">The id to check for.</param>
        /// <returns>A value indicating whether the object is in this chunk.</returns>
        public bool ContainsKey(ObjectId id)
        {
            return _data.ContainsKey(id);
        }

        /// <summary>
        /// Queries the chunk.
        /// </summary>
        /// <param name="queryItems">The query to run against the chunk.</param>
        /// <returns>A list of the matching documents in the chunk.</returns>
        public List<Document> Query(List<QueryItem> queryItems)
        {
            List<Document> results = new List<Document>();
            foreach (var item in _data)
            {
                bool matches = queryItems.All(query => query.Match(item.Value));

                if (matches)
                {
                    results.Add(item.Value);
                }
            }

            return results;
        }

        /// <summary>
        /// Removes an object from the chunk.
        /// </summary>
        /// <param name="id">The object to remove.</param>
        public void Remove(ObjectId id)
        {
            _data.Remove(id);
        }

        /// <summary>
        /// Splits a chunk into two chunks.
        /// </summary>
        /// <returns>The new chunk created from the split.</returns>
        public DatabaseChunk Split()
        {
            var newData = new SortedDictionary<ObjectId, Document>(_data.Skip(_data.Count / 2).ToDictionary(e => e.Key, e => e.Value));

            foreach (var item in newData)
            {
                _data.Remove(item.Key);
            }

            var oldEnd = _end;
            _end = new ChunkMarker(newData.First().Key.ToString());

            return new DatabaseChunk(_end, oldEnd, newData);
        }
    }
}