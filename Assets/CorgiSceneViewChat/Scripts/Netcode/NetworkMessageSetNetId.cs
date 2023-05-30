using System.Collections;
using System.Collections.Generic;

namespace CorgiSceneChat
{
    [System.Serializable]
    public struct NetworkMessageSetNetId : NetworkMessage
    {
        public int ClientId;

        public void ReadBuffer(byte[] buffer, ref int index)
        {
            ClientId = Serialization.ReadBuffer_Int32(buffer, ref index);
        }

        public void WriteBuffer(byte[] buffer, ref int index)
        {
            Serialization.WriteBuffer_Int32(buffer, ref index, ClientId);
        }

        public NetworkMessageId GetNetworkMessageId() { return NetworkMessageId.SetNetId; }
    }
}
