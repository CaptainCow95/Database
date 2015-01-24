﻿using Database.Common;
using Database.Common.Messages;
using System;
using System.Collections.Generic;

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

        /// <inheritdoc />
        public override NodeDefinition Self
        {
            get { throw new NotImplementedException(); }
        }

        /// <inheritdoc />
        public override void Run()
        {
            // Disable Logging
            Logger.Disable();
            BeforeStart();

            CommandParser parser = new CommandParser();
            parser.AddCommand(new CommandSyntax(new CommandPartLiteral("connect "), new CommandPartString()), CommandConnect);
            parser.AddCommand(new CommandSyntax(new CommandPartLiteral("disconnect")), CommandDisconnect);
            parser.AddCommand(new CommandSyntax(new CommandPartLiteral("help")), CommandHelp);

            System.Console.WriteLine("Type \"help\" for a list of commands.");

            while (Running)
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
        protected override void ConnectionLost(NodeDefinition node)
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

            string targetString = ((CommandPartString)command[1]).Value;
            string[] targetStringParts = targetString.Split(':');
            if (targetStringParts.Length != 2)
            {
                System.Console.WriteLine("Failed to connect, error while parsing target");
                return;
            }

            int port;
            if (!int.TryParse(targetStringParts[1], out port))
            {
                System.Console.WriteLine("Failed to connect, error while parsing port");
            }

            NodeDefinition def = new NodeDefinition(targetStringParts[0], port);

            Message message = new Message(def, new JoinAttempt(), true);
            message.SendWithoutConfirmation = true;
            SendMessage(message);
            message.BlockUntilDone();

            if (message.Success)
            {
                if (message.Response.Data is JoinFailure)
                {
                    System.Console.WriteLine("Failed to connect, " + ((JoinFailure)message.Response.Data).Reason);
                }
                else
                {
                    System.Console.WriteLine("Connected to " + def.ConnectionName);
                    _connected = true;
                    Connections[def].ConnectionEstablished(NodeType.Controller);
                }
            }
            else
            {
                System.Console.WriteLine("Failed to connect, could not reach target");
            }
        }

        /// <summary>
        /// The disconnect command.
        /// </summary>
        /// <param name="command">The command data.</param>
        private void CommandDisconnect(List<CommandPart> command)
        {
            foreach (var con in Connections)
            {
                con.Value.Disconnect();
            }

            System.Console.WriteLine("Disconnected");

            _connected = false;
        }

        /// <summary>
        /// The help command.
        /// </summary>
        /// <param name="command">The command data.</param>
        private void CommandHelp(List<CommandPart> command)
        {
            System.Console.WriteLine("connect STRING:\tconnects to the specified node");
            System.Console.WriteLine("disconnect:\tdisconnects from the node");
            System.Console.WriteLine("help:\t\tprints this help text");
            System.Console.WriteLine("status:\t\tprints the status of the network");
        }
    }
}