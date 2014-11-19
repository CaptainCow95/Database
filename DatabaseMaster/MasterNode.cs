using Database.Common;

namespace Database.Master
{
    public class MasterNode : Node
    {
        public MasterNode(int port)
            : base(port)
        {
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