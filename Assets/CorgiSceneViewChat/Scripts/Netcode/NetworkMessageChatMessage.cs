using System.Collections;
using System.Collections.Generic;

namespace CorgiSceneChat
{
    [System.Serializable]
    public struct NetworkMessageChatMessage : NetworkMessage
    {
        public ChatMessage chatMessage;

        public void ReadBuffer(byte[] buffer, ref int index)
        {
            chatMessage = new ChatMessage();
            chatMessage.timestamp = Serialization.ReadBuffer_Int64(buffer, ref index);
            chatMessage.username = Serialization.ReadBuffer_String(buffer, ref index);
            chatMessage.message = Serialization.ReadBuffer_String(buffer, ref index);
            chatMessage.systemMessage = Serialization.ReadBuffer_Bool(buffer, ref index);
        }

        public void WriteBuffer(byte[] buffer, ref int index)
        {
            Serialization.WriteBuffer_Int64(buffer, ref index, chatMessage.timestamp);
            Serialization.WriteBuffer_String(buffer, ref index, chatMessage.username);
            Serialization.WriteBuffer_String(buffer, ref index, chatMessage.message);
            Serialization.WriteBuffer_Bool(buffer, ref index, chatMessage.systemMessage);
        }

        public NetworkMessageId GetNetworkMessageId() { return NetworkMessageId.ChatMessage; }
    }
}
