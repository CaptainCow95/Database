using Database.Common;
using System.Threading;

namespace Database.Master
{
    public class MasterNode : Node
    {
        public MasterNode(int port)
            : base(port)
        {
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