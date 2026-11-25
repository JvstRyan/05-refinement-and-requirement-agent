using Thumper.Core;

namespace Kantilever.WarehouseService.IntegrationTest
{
    internal class TestListener
    {
        public BasicReceiver Receiver { get; internal set; }
        public EventMessage? ActualMessage { get; internal set; }
        public AutoResetEvent ReceiveFlag { get; internal set; }
    }
}