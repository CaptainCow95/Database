using System;

namespace Database.Common.Messages
{
    /// <summary>
    /// A message to attempt to join a network.
    /// </summary>
    public class JoinAttempt : BaseMessageData
    {
        /// <summary>
        /// The name of the node.
        /// </summary>
        private string _name;

        /// <summary>
        /// The port of the node.
        /// </summary>
        private int _port;

        /// <summary>
        /// A value indicating whether the joining node is the primary controller.
        /// </summary>
        private bool _primary;

        /// <summary>
        /// The settings of the node.
        /// </summary>
        private string _settings;

        /// <summary>
        /// The type of the node.
        /// </summary>
        private NodeType _type;

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinAttempt"/> class.
        /// </summary>
        /// <param name="type">The type of the node.</param>
        /// <param name="name">The name of the node.</param>
        /// <param name="port">The port of the node.</param>
        /// <param name="settings">The settings of the node.</param>
        public JoinAttempt(NodeType type, string name, int port, string settings)
            : this(type, name, port, settings, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinAttempt"/> class.
        /// </summary>
        /// <param name="name">The name of the node.</param>
        /// <param name="port">The port of the node.</param>
        /// <param name="settings">The settings of the node.</param>
        /// <param name="primary">A value indicating whether the joining node is the primary controller.</param>
        public JoinAttempt(string name, int port, string settings, bool primary)
            : this(NodeType.Controller, name, port, settings, primary)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinAttempt"/> class.
        /// </summary>
        /// <param name="data">The data to read from.</param>
        /// <param name="index">The index at which to start reading from.</param>
        internal JoinAttempt(byte[] data, int index)
        {
            _type = (NodeType)Enum.ToObject(typeof(NodeType), ByteArrayHelper.ToInt32(data, ref index));
            _name = ByteArrayHelper.ToString(data, ref index);
            _port = ByteArrayHelper.ToInt32(data, ref index);
            _settings = ByteArrayHelper.ToString(data, ref index);
            _primary = ByteArrayHelper.ToBoolean(data, ref index);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinAttempt"/> class.
        /// </summary>
        /// <param name="type">The type of the node.</param>
        /// <param name="name">The name of the node.</param>
        /// <param name="port">The port of the node.</param>
        /// <param name="settings">The settings of the node.</param>
        /// <param name="primary">A value indicating whether the joining node is the primary controller.</param>
        private JoinAttempt(NodeType type, string name, int port, string settings, bool primary)
        {
            _type = type;
            _name = name;
            _port = port;
            _settings = settings;
            _primary = primary;
        }

        /// <summary>
        /// Gets the name of the node.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Gets the port of the node.
        /// </summary>
        public int Port
        {
            get { return _port; }
        }

        /// <summary>
        /// Gets a value indicating whether the joining node is the primary controller.
        /// </summary>
        public bool Primary
        {
            get { return _primary; }
        }

        /// <summary>
        /// Gets the settings of the node.
        /// </summary>
        public string Settings
        {
            get { return _settings; }
        }

        /// <summary>
        /// Gets the type of the node.
        /// </summary>
        public NodeType Type
        {
            get { return _type; }
        }

        /// <inheritdoc />
        public override byte[] EncodeInternal()
        {
            byte[] typeBytes = ByteArrayHelper.ToBytes((int)_type);
            byte[] nameBytes = ByteArrayHelper.ToBytes(_name);
            byte[] portBytes = ByteArrayHelper.ToBytes(_port);
            byte[] settingsBytes = ByteArrayHelper.ToBytes(_settings);
            byte[] primaryBytes = ByteArrayHelper.ToBytes(_primary);

            return ByteArrayHelper.Combine(typeBytes, nameBytes, portBytes, settingsBytes, primaryBytes);
        }

        /// <inheritdoc />
        protected override int GetMessageTypeId()
        {
            return (int)MessageType.JoinAttempt;
        }
    }
}