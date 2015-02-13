using Database.Common;
using Database.Common.DataOperation;
using System;
using System.Collections.Generic;
using API = Database.Common.API;

namespace Database.Console
{
    /// <summary>
    /// Represents a console node.
    /// </summary>
    public class ConsoleNode : Node
    {
        /// <summary>
        /// A value indicating whether the console is connected to a node.
        /// </summary>
        private bool _connected = false;

        /// <summary>
        /// A value indicating whether the console is running.
        /// </summary>
        private bool _consoleRunning;

        /// <summary>
        /// The API object used to connect to the database.
        /// </summary>
        private API.Database _database;

        /// <inheritdoc />
        public override NodeDefinition Self
        {
            get { throw new NotImplementedException(); }
        }

        /// <inheritdoc />
        public override void Run()
        {
            _consoleRunning = true;

            // Disable Logging
            Logger.Disable();
            BeforeStart();

            CommandParser parser = new CommandParser();
            parser.AddCommand(new CommandSyntax(new CommandPartLiteral("connect "), new CommandPartString()), CommandConnect);
            parser.AddCommand(new CommandSyntax(new CommandPartLiteral("disconnect")), CommandDisconnect);
            parser.AddCommand(new CommandSyntax(new CommandPartLiteral("exit")), CommandExit);
            parser.AddCommand(new CommandSyntax(new CommandPartLiteral("help")), CommandHelp);
            parser.AddCommand(new CommandSyntax(new CommandPartLiteral("help"), new CommandPartString()), CommandHelpDetailed);
            parser.AddCommand(new CommandSyntax(new CommandPartLiteral("add"), new CommandPartString()), CommandAdd);
            parser.AddCommand(new CommandSyntax(new CommandPartLiteral("query"), new CommandPartString()), CommandQuery);
            parser.AddCommand(new CommandSyntax(new CommandPartLiteral("remove"), new CommandPartString()), CommandRemove);
            parser.AddCommand(new CommandSyntax(new CommandPartLiteral("update"), new CommandPartString()), CommandUpdate);

            System.Console.WriteLine("Type \"help\" for a list of commands.");

            while (_consoleRunning)
            {
                System.Console.Write("> ");
                if (!parser.ParseCommand(System.Console.ReadLine()))
                {
                    System.Console.WriteLine("Unrecognized command");
                }
            }

            AfterStop();
        }

        /// <inheritdoc />
        protected override void ConnectionLost(NodeDefinition node, NodeType type)
        {
            _connected = false;
        }

        /// <inheritdoc />
        protected override void MessageReceived(Message message)
        {
        }

        /// <inheritdoc />
        protected override void PrimaryChanged()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The add command.
        /// </summary>
        /// <param name="command">The command data.</param>
        private void CommandAdd(List<CommandPart> command)
        {
            if (!_connected)
            {
                System.Console.WriteLine("Not connected to a database.");
                return;
            }

            System.Console.WriteLine(_database.Add(new Document(((CommandPartString)command[1]).Value)).ToJson());
        }

        /// <summary>
        /// The connect command.
        /// </summary>
        /// <param name="command">The command data.</param>
        private void CommandConnect(List<CommandPart> command)
        {
            if (_connected)
            {
                System.Console.WriteLine("Please disconnect from the existing node before reconnecting to another.");
                return;
            }

            _database = new API.Database(((CommandPartString)command[1]).Value);

            if (_database.Connected)
            {
                _connected = true;
                System.Console.WriteLine("Connected to database.");
            }
            else
            {
                System.Console.WriteLine("Failed to connect to database.");
            }
        }

        /// <summary>
        /// The disconnect command.
        /// </summary>
        /// <param name="command">The command data.</param>
        private void CommandDisconnect(List<CommandPart> command)
        {
            _database.Shutdown();
            _connected = false;
            System.Console.WriteLine("Disconnected");
        }

        /// <summary>
        /// The exit command.
        /// </summary>
        /// <param name="command">The command data.</param>
        private void CommandExit(List<CommandPart> command)
        {
            _consoleRunning = false;
        }

        /// <summary>
        /// The help command.
        /// </summary>
        /// <param name="command">The command data.</param>
        private void CommandHelp(List<CommandPart> command)
        {
            System.Console.WriteLine("connect STRING:\t\tconnects to the specified node");
            System.Console.WriteLine("disconnect:\t\tdisconnects from the node");
            System.Console.WriteLine("exit:\t\t\texits the program");
            System.Console.WriteLine("help:\t\t\tprints this help text");
            System.Console.WriteLine("help STRING:\t\tdisplays detailed help about the specified command");
            System.Console.WriteLine("add STRING:\t\tadds a document to the database");
            System.Console.WriteLine("query STRING:\t\tqueries the database with the supplied fields");
            System.Console.WriteLine("remove STRING:\t\tremoves the specified document id from the database");
            System.Console.WriteLine("update STRING:\t\tupdates a document to the database");
        }

        /// <summary>
        /// The detailed help command.
        /// </summary>
        /// <param name="command">The command data.</param>
        private void CommandHelpDetailed(List<CommandPart> command)
        {
            string helpCommand = ((CommandPartString)command[1]).Value;

            switch (helpCommand)
            {
                case "connect":
                    System.Console.WriteLine("Connects to the supplied node. This is where all database operations will be sent to.");
                    break;

                case "disconnect":
                    System.Console.WriteLine("Disconnects from the connected node.");
                    break;

                case "exit":
                    System.Console.WriteLine("Exits the program.");
                    break;

                case "help":
                    System.Console.WriteLine("Prints out the help text or the help text of a specified command.");
                    break;

                case "add":
                    System.Console.WriteLine("Adds a document to the database. Supply the json of the document that you want to add. If an id is not supplied, one will be created and should a collision occur with the generated id, the operation will be retried with a new one until it succeeds or runs into a different error.");
                    break;

                case "query":
                    System.Console.WriteLine("Queries the database. Supply a document that represents your query operation.");
                    break;

                case "remove":
                    System.Console.WriteLine("Removes a document from the database. Supply the id of the document you want to be removed.");
                    break;

                case "update":
                    System.Console.WriteLine("Updates a document in the database. Supply a document that represents your update operation.");
                    break;

                default:
                    System.Console.WriteLine("Not a valid command.");
                    break;
            }
        }

        /// <summary>
        /// The query command.
        /// </summary>
        /// <param name="command">The command data.</param>
        private void CommandQuery(List<CommandPart> command)
        {
            if (!_connected)
            {
                System.Console.WriteLine("Not connected to a database.");
                return;
            }

            System.Console.WriteLine(_database.Query(new Document(((CommandPartString)command[1]).Value)).ToJson());
        }

        /// <summary>
        /// The remove command.
        /// </summary>
        /// <param name="command">The command data.</param>
        private void CommandRemove(List<CommandPart> command)
        {
            if (!_connected)
            {
                System.Console.WriteLine("Not connected to a database.");
                return;
            }

            System.Console.WriteLine(_database.Remove(((CommandPartString)command[1]).Value).ToJson());
        }

        /// <summary>
        /// The update command.
        /// </summary>
        /// <param name="command">The command data.</param>
        private void CommandUpdate(List<CommandPart> command)
        {
            if (!_connected)
            {
                System.Console.WriteLine("Not connected to a database.");
                return;
            }

            System.Console.WriteLine(_database.Update(new Document(((CommandPartString)command[1]).Value)).ToJson());
        }
    }
}