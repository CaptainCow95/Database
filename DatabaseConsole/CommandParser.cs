using System;
using System.Collections.Generic;

namespace Database.Console
{
    /// <summary>
    /// Helps in parsing and executing commands.
    /// </summary>
    public class CommandParser
    {
        /// <summary>
        /// A list of the valid commands.
        /// </summary>
        private readonly List<Tuple<CommandSyntax, ProcessCommand>> _commands = new List<Tuple<CommandSyntax, ProcessCommand>>();

        /// <summary>
        /// The delegate used when executing a command.
        /// </summary>
        /// <param name="parsedCommand">The <see cref="CommandPart"/>s that were parsed to find the command.</param>
        public delegate void ProcessCommand(List<CommandPart> parsedCommand);

        /// <summary>
        /// Adds a command to the parser.
        /// </summary>
        /// <param name="commandSyntax">The syntax of the command.</param>
        /// <param name="target">The target to call when the command is found.</param>
        public void AddCommand(CommandSyntax commandSyntax, ProcessCommand target)
        {
            _commands.Add(new Tuple<CommandSyntax, ProcessCommand>(commandSyntax, target));
        }

        /// <summary>
        /// Parses a string to find a valid command, and upon finding one, executes it.
        /// </summary>
        /// <param name="command">The string to parse.</param>
        /// <returns>True if a match was found, otherwise false.</returns>
        public bool ParseCommand(string command)
        {
            foreach (var com in _commands)
            {
                List<CommandPart> parsedCommand;
                if (com.Item1.Parse(command, out parsedCommand))
                {
                    com.Item2(parsedCommand);
                    return true;
                }
            }

            return false;
        }
    }
}