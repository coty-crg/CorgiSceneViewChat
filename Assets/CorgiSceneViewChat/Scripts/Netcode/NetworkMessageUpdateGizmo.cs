using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CorgiSceneChat
{
    [System.Serializable]
    public struct NetworkMessageUpdateGizmo : NetworkMessage
    {
        public int ClientId;
        public int gizmoMode;
        public float Position_x;
        public float Position_y;
        public float Position_z;
        public float Rotation_x;
        public float Rotation_y;
        public float Rotation_z;
        public float Rotation_w;
        public float Scale_x;
        public float Scale_y;
        public float Scale_z;
        public string SelectedGlobalObjectId;

        public void ReadBuffer(byte[] buffer, ref int index)
        {
            ClientId = Serialization.ReadBuffer_Int32(buffer, ref index);
            gizmoMode = Serialization.ReadBuffer_Int32(buffer, ref index);
            Position_x = Serialization.ReadBuffer_Float(buffer, ref index);
            Position_y = Serialization.ReadBuffer_Float(buffer, ref index);
            Position_z = Serialization.ReadBuffer_Float(buffer, ref index);
            Rotation_x = Serialization.ReadBuffer_Float(buffer, ref index);
            Rotation_y = Serialization.ReadBuffer_Float(buffer, ref index);
            Rotation_z = Serialization.ReadBuffer_Float(buffer, ref index);
            Rotation_w = Serialization.ReadBuffer_Float(buffer, ref index);
            Scale_x = Serialization.ReadBuffer_Float(buffer, ref index);
            Scale_y = Serialization.ReadBuffer_Float(buffer, ref index);
            Scale_z = Serialization.ReadBuffer_Float(buffer, ref index);
            SelectedGlobalObjectId = Serialization.ReadBuffer_String(buffer, ref index);
        }

        public void WriteBuffer(byte[] buffer, ref int index)
        {
            Serialization.WriteBuffer_Int32(buffer, ref index, ClientId);
            Serialization.WriteBuffer_Int32(buffer, ref index, gizmoMode);
            Serialization.WriteBuffer_Float(buffer, ref index, Position_x);
            Serialization.WriteBuffer_Float(buffer, ref index, Position_y);
            Serialization.WriteBuffer_Float(buffer, ref index, Position_z);
            Serialization.WriteBuffer_Float(buffer, ref index, Rotation_x);
            Serialization.WriteBuffer_Float(buffer, ref index, Rotation_y);
            Serialization.WriteBuffer_Float(buffer, ref index, Rotation_z);
            Serialization.WriteBuffer_Float(buffer, ref index, Rotation_w);
            Serialization.WriteBuffer_Float(buffer, ref index, Scale_x);
            Serialization.WriteBuffer_Float(buffer, ref index, Scale_y);
            Serialization.WriteBuffer_Float(buffer, ref index, Scale_z);
            Serialization.WriteBuffer_String(buffer, ref index, SelectedGlobalObjectId);
        }

        public NetworkMessageId GetNetworkMessageId() { return NetworkMessageId.UpdateGizmo; }
    }
}
