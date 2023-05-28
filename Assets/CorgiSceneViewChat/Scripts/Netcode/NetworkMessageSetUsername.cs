using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CorgiSceneChat
{
    [System.Serializable]
    public struct NetworkMessageSetUsername : NetworkMessage
    {
        public string username;

        public void ReadBuffer(byte[] buffer, ref int index)
        {
            username = Serialization.ReadBuffer_String(buffer, ref index);
        }

        public void WriteBuffer(byte[] buffer, ref int index)
        {
            Serialization.WriteBuffer_String(buffer, ref index, username);
        }

        public NetworkMessageId GetNetworkMessageId() { return NetworkMessageId.SetUsername; }
    }
}
