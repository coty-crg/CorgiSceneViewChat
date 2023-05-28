using System.Collections;
using System.Collections.Generic;

namespace CorgiSceneChat
{
    [System.Serializable]
    public struct NetworkMessageOpenedScene : NetworkMessage
    {
        public string sceneName;

        public void ReadBuffer(byte[] buffer, ref int index)
        {
            sceneName = Serialization.ReadBuffer_String(buffer, ref index);
        }

        public void WriteBuffer(byte[] buffer, ref int index)
        {
            Serialization.WriteBuffer_String(buffer, ref index, sceneName);
        }

        public NetworkMessageId GetNetworkMessageId() { return NetworkMessageId.OpenedScene; }
    }
}
