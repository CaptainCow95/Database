using System;

namespace Database.Common.DataOperation
{
    /// <summary>
    /// Thrown when trying to get a data type out of a <see cref="DocumentEntry"/> and the data type requested does not match the actual data type.
    /// </summary>
    public class DataTypeException : Exception
    {
    }
}