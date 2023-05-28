using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace CorgiSceneChat
{
    public interface NetworkMessage
    {
        public void WriteBuffer(byte[] buffer, ref int index);
        public void ReadBuffer(byte[] buffer, ref int index);
        public NetworkMessageId GetNetworkMessageId(); 
    }
}
