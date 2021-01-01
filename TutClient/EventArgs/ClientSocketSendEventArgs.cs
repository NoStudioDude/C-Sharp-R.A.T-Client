using System.Net.Sockets;

namespace TutClient.EventArgs
{
    public class ClientSocketSendEventArgs : System.EventArgs
    {
        public byte[] Data;
        public SocketFlags Flags;
    }
}
