namespace Database.Console
{
    /// <summary>
    /// Represents a part of a command.
    /// </summary>
    public abstract class CommandPart
    {
        /// <summary>
        /// Parses a string.
        /// </summary>
        /// <param name="text">The string to parse, and after this is called, the string that is left over after parsing.</param>
        /// <param name="output">The <see cref="CommandPart"/> that this text contained.</param>
        /// <returns>True if the parsing was successful, otherwise false.</returns>
        public abstract bool Parse(ref string text, out CommandPart output);
    }
}