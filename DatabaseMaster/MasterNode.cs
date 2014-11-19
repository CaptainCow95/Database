namespace Database.Master
{
    public static class MasterNode
    {
        private static int _port;

        public static void Start(int port)
        {
            _port = port;
        }

        public static void Stop()
        {
        }
    }
}