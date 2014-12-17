namespace Database.Master
{
    public class MasterNodeDefinition
    {
        private string _hostname;
        private string _name;
        private int _port;

        public MasterNodeDefinition(string name, string hostname, int port)
        {
            _name = name;
            _hostname = hostname;
            _port = port;
        }

        public string Hostname { get { return _hostname; } }

        public string Name { get { return _name; } }

        public int Port { get { return _port; } }
    }
}