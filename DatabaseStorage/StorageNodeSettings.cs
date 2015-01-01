using Database.Common;
using System.Xml;

namespace Database.Storage
{
    public class StorageNodeSettings : Settings
    {
        private bool _canBecomePrimary = true;
        private string _connectionString;

        private string _location = "";

        private string _nodeName;

        private int _port = 12345;

        private int _weight = 1;

        public StorageNodeSettings(string xml)
            : base(xml)
        {
        }

        public StorageNodeSettings()
            : base()
        {
        }

        public bool CanBecomePrimary { get { return _canBecomePrimary; } }

        public string ConnectionString { get { return _connectionString; } }

        public string Location { get { return _location; } }

        public string NodeName { get { return _nodeName; } }

        public int Port { get { return _port; } }

        public int Weight { get { return _weight; } }

        protected override void Load(XmlNode settings)
        {
            _connectionString = ReadString(settings, "ConnectionString", _connectionString);
            _nodeName = ReadString(settings, "NodeName", _nodeName);
            _port = ReadInt32(settings, "Port", _port);
            _location = ReadString(settings, "Location", _location);
            _canBecomePrimary = ReadBoolean(settings, "CanBecomePrimary", _canBecomePrimary);
            _weight = ReadInt32(settings, "Weight", _weight);
        }

        protected override void Save(XmlDocument document, XmlNode root)
        {
            WriteString(document, "ConnectionString", _connectionString, root);
            WriteString(document, "NodeName", _nodeName, root);
            WriteInt32(document, "Port", _port, root);
            WriteString(document, "Location", _location, root);
            WriteBoolean(document, "CanBecomePrimary", _canBecomePrimary, root);
            WriteInt32(document, "Weight", _weight, root);
        }
    }
}