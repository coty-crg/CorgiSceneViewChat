using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using Unity.Collections;
using UnityEngine;

namespace CorgiSceneChat
{
    [System.Serializable]
    public enum NetworkMessageId
    {
        None = 0,
        ChatMessage = 1,
        ChangeChannel = 2,
        SetUsername = 3, 
    }

    public static class NetworkMessageLookup
    {
        public static Dictionary<NetworkMessageId, System.Type> table = new Dictionary<NetworkMessageId, System.Type>()
        {
            { NetworkMessageId.ChatMessage, typeof(NetworkMessageChatMessage) },
            { NetworkMessageId.ChangeChannel, typeof(NetworkMessageChangeChannel) },
            { NetworkMessageId.SetUsername, typeof(NetworkMessageSetUsername) },
        };
    }

    [System.Serializable]
    public struct NetworkMessageHeader
    {
        public int NextMessageSize;
        public NetworkMessageId NextMessageId; 
    }

    public interface NetworkMessage
    {
        public void WriteBuffer(byte[] buffer, ref int index);
        public void ReadBuffer(byte[] buffer, ref int index);
        public NetworkMessageId GetNetworkMessageId(); 
    }

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
        }

        public void WriteBuffer(byte[] buffer, ref int index)
        {
            Serialization.WriteBuffer_Int64(buffer, ref index, chatMessage.timestamp);
            Serialization.WriteBuffer_String(buffer, ref index, chatMessage.username);
            Serialization.WriteBuffer_String(buffer, ref index, chatMessage.message);
        }

        public NetworkMessageId GetNetworkMessageId() { return NetworkMessageId.ChatMessage; }
    }

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

    public static class Serialization
    {
        public const int HeaderSize = sizeof(int) * 2;

        public static void WriteBuffer_Int32(byte[] buffer, ref int index, int value)
        {
            buffer[index++] = (byte)(value >> 00);
            buffer[index++] = (byte)(value >> 08);
            buffer[index++] = (byte)(value >> 16);
            buffer[index++] = (byte)(value >> 24);
        }

        public static int ReadBuffer_Int32(byte[] buffer, ref int index)
        {
            int byte0 = (int)buffer[index++] << 0;
            int byte1 = (int)buffer[index++] << 8;
            int byte2 = (int)buffer[index++] << 16;
            int byte3 = (int)buffer[index++] << 24;

            int result = byte0 | byte1 | byte2 | byte3;
            return result;
        }

        public static void WriteBuffer_String(byte[] buffer, ref int index, string str)
        {
            if (str == null) str = "";

            var length = str.Length;
            WriteBuffer_Int32(buffer, ref index, length);

            for (var i = 0; i < length; ++i)
            {
                var value = (int)str[i];
                WriteBuffer_Int32(buffer, ref index, value);
            }
        }

        public static string ReadBuffer_String(byte[] buffer, ref int index)
        {
            var sb = new System.Text.StringBuilder();

            var length = ReadBuffer_Int32(buffer, ref index);
            for (var i = 0; i < length; ++i)
            {
                var value = (char)ReadBuffer_Int32(buffer, ref index);
                sb.Append(value);
            }

            return sb.ToString();
        }

        public static long ReadBuffer_Int64(byte[] buffer, ref int index)
        {
            long byte0 = (long)buffer[index++] << 0;
            long byte1 = (long)buffer[index++] << 8;
            long byte2 = (long)buffer[index++] << 16;
            long byte3 = (long)buffer[index++] << 24;
            long byte4 = (long)buffer[index++] << 32;
            long byte5 = (long)buffer[index++] << 40;
            long byte6 = (long)buffer[index++] << 48;
            long byte7 = (long)buffer[index++] << 56;

            long result = byte0 | byte1 | byte2 | byte3 | byte4 | byte5 | byte6 | byte7;
            return result;
        }

        public static void WriteBuffer_Int64(byte[] buffer, ref int index, long value)
        {
            buffer[index++] = (byte)(value >> 00);
            buffer[index++] = (byte)(value >> 08);
            buffer[index++] = (byte)(value >> 16);
            buffer[index++] = (byte)(value >> 24);
            buffer[index++] = (byte)(value >> 32);
            buffer[index++] = (byte)(value >> 40);
            buffer[index++] = (byte)(value >> 48);
            buffer[index++] = (byte)(value >> 56);
        }

        public static void WriteBuffer_NetworkMessage(byte[] buffer, ref int index, NetworkMessage networkMessage)
        {
            var sizeIndex = index;

            WriteBuffer_Int32(buffer, ref index, 0);
            WriteBuffer_Int32(buffer, ref index, (int) networkMessage.GetNetworkMessageId());

            var messageIndex = index;
            networkMessage.WriteBuffer(buffer, ref index);
            var messageSize = index - messageIndex;

            WriteBuffer_Int32(buffer, ref sizeIndex, messageSize);
        }

        public static NetworkMessageHeader PeekBuffer_NetworkMessageHeader(byte[] buffer, int index)
        {
            var header = new NetworkMessageHeader();
            header.NextMessageSize = ReadBuffer_Int32(buffer, ref index);
            header.NextMessageId = (NetworkMessageId)ReadBuffer_Int32(buffer, ref index);

            return header;
        }

        public static NetworkMessageHeader ReadBuffer_NetworkMessageHeader(byte[] buffer, ref int index)
        {
            var header = new NetworkMessageHeader();
                header.NextMessageSize = ReadBuffer_Int32(buffer, ref index);
                header.NextMessageId = (NetworkMessageId)ReadBuffer_Int32(buffer, ref index);

            return header;
        }

        public static void WriteBuffer_NetworkMessageHeader(byte[] buffer, ref int index, NetworkMessageHeader header)
        {
            WriteBuffer_Int32(buffer, ref index, header.NextMessageSize);
            WriteBuffer_Int32(buffer, ref index, (int) header.NextMessageId);
        }

        public static NetworkMessage ReadBuffer_NetworkMessage(byte[] buffer, ref int index)
        {
            var header = ReadBuffer_NetworkMessageHeader(buffer, ref index);

            if(NetworkMessageLookup.table.TryGetValue(header.NextMessageId, out var type))
            {
                var instance = System.Activator.CreateInstance(type) as NetworkMessage;
                    instance.ReadBuffer(buffer, ref index);

                return instance; 
            }
            else
            {
                Debug.LogError($"Received unknown data type {header.NextMessageId}.");
                return default; 
            }
        }
    }
}
