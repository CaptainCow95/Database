using System.Collections.Generic;

namespace Database.Console
{
    /// <summary>
    /// Represents the syntax parser for a command.
    /// </summary>
    public class CommandSyntax
    {
        /// <summary>
        /// The parts that make up the syntax.
        /// </summary>
        private readonly List<CommandPart> _parts = new List<CommandPart>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandSyntax"/> class.
        /// </summary>
        /// <param name="parts">The parts that make up the syntax.</param>
        public CommandSyntax(params CommandPart[] parts)
        {
            _parts.AddRange(parts);
        }

        /// <summary>
        /// Parses a string to figure out if the syntax matches.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="parsedCommand">The list of the <see cref="CommandPart"/>s that make up the command.</param>
        /// <returns>True if the the parsing was successful, false if there was a syntax error.</returns>
        public bool Parse(string s, out List<CommandPart> parsedCommand)
        {
            parsedCommand = new List<CommandPart>();
            foreach (var item in _parts)
            {
                s = s.Trim();
                CommandPart part;
                if (!item.Parse(ref s, out part))
                {
                    return false;
                }

                parsedCommand.Add(part);
            }

            s = s.Trim();
            if (s.Length > 0)
            {
                return false;
            }

            return true;
        }
    }
}