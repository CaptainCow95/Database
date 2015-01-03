namespace Database.Common
{
    /// <summary>
    /// Represents the different node types.
    /// </summary>
    public enum NodeType
    {
        /// <summary>
        /// Represents a controller node.
        /// </summary>
        Controller,

        /// <summary>
        /// Represents a storage node.
        /// </summary>
        Storage,

        /// <summary>
        /// Represents a query node.
        /// </summary>
        Query,

        /// <summary>
        /// Represents a currently unknown node.
        /// </summary>
        Unknown
    }
}