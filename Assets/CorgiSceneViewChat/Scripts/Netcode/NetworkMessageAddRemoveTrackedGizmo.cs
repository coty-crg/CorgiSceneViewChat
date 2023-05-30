using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CorgiSceneChat
{
    [System.Serializable]
    public struct NetworkMessageAddRemoveTrackedGizmo : NetworkMessage
    {
        public int ClientId;
        public bool adding;
        public bool removing;

        public void ReadBuffer(byte[] buffer, ref int index)
        {
            ClientId = Serialization.ReadBuffer_Int32(buffer, ref index);
            adding = Serialization.ReadBuffer_Bool(buffer, ref index);
            removing = Serialization.ReadBuffer_Bool(buffer, ref index);
        }

        public void WriteBuffer(byte[] buffer, ref int index)
        {
            Serialization.WriteBuffer_Int32(buffer, ref index, ClientId);
            Serialization.WriteBuffer_Bool(buffer, ref index, adding);
            Serialization.WriteBuffer_Bool(buffer, ref index, removing);
        }

        public NetworkMessageId GetNetworkMessageId() { return NetworkMessageId.AddRemoveTrackedGizmo; }
    }
}
