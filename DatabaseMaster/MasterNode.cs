using Database.Common;
using Database.Common.Messages;
using System;
using System.Net.Sockets;
using System.Threading;

namespace Database.Master
{
    public class MasterNode : Node
    {
        public MasterNode(MasterNodeSettings settings)
            : base(NodeType.Master, settings.Port, settings.Name)
        {
            foreach (var node in settings.MasterList)
            {
                TcpClient client = new TcpClient(node.Hostname, node.Port);
                Connection connection = new Connection(client, DateTime.UtcNow, ConnectionStatus.ConfirmingConnection);
                Connections.Add(node.Name, connection);

                StartConnectionMessageData messageData = new StartConnectionMessageData(NodeType.Master, NodeName, false);
                Message message = new Message(node.Name, messageData, true);
                message.SendWithoutConfirmation = true;

                SendMessage(message);

                message.BlockUntilDone();

                if (message.Success)
                {
                    // TODO: Do more stuff here
                }
            }
        }

        public override void Run()
        {
            BeforeStart();

            while (Running)
            {
                Thread.Sleep(1);
            }

            AfterStop();
        }

        public void Start()
        {
            BeforeStart();
        }

        public void Stop()
        {
            AfterStop();
        }
    }
}