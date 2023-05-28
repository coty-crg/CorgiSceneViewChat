using System.Collections;
using System.Collections.Generic;

namespace CorgiSceneChat
{
    [System.Serializable]
    public struct NetworkMessageChangeChannel : NetworkMessage
    {
        public string channel;

        public void ReadBuffer(byte[] buffer, ref int index)
        {
            channel = Serialization.ReadBuffer_String(buffer, ref index);
        }

        public void WriteBuffer(byte[] buffer, ref int index)
        {
            Serialization.WriteBuffer_String(buffer, ref index, channel);
        }

        public NetworkMessageId GetNetworkMessageId() { return NetworkMessageId.ChangeChannel; }
    }
}
