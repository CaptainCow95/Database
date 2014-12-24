namespace Database.Common
{
    public class NodeDefinition
    {
        private readonly string _connectionName;
        private readonly string _hostname;
        private readonly int _port;

        public NodeDefinition(string hostname, int port)
        {
            _hostname = hostname;
            _port = port;
            _connectionName = hostname + ":" + port;
        }

        public string ConnectionName { get { return _connectionName; } }

        public string Hostname { get { return _hostname; } }

        public int Port { get { return _port; } }
    }
}