namespace Database.Common
{
    public class Message
    {
        private string _address;
        private byte[] _data;

        public Message(string address, byte[] data)
        {
            _address = address;
            _data = data;
        }

        public string Address { get { return _address; } }

        public byte[] Data { get { return _data; } }
    }
}