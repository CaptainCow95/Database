using Database.Common;
using System.Threading;

namespace Database.Controller
{
    public class ControllerNode : Node
    {
        private bool _primary = false;

        public ControllerNode(ControllerNodeSettings settings)
            : base(NodeType.Controller, settings.Port, settings.ConnectionString, settings.ConnectionList)
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
    }
}