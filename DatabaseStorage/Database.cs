using Database.Common.Messages;
using System;
using System.Collections.Generic;

namespace Database.Storage
{
    public class Database
    {
        private SortedDictionary<int, Document> _data = new SortedDictionary<int, Document>();

        public Database()
        {
        }

        public DataOperationResult ProcessOperation(DataOperation operation)
        {
            throw new NotImplementedException();
        }

        private int GetNewDatabaseId()
        {
            int returnValue = 0;
            foreach (var entry in _data)
            {
                if (entry.Key != returnValue)
                {
                    break;
                }

                returnValue = entry.Key + 1;
            }

            return returnValue;
        }
    }
}