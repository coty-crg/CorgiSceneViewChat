using System.Collections;
using System.Collections.Generic;

namespace CorgiSceneChat
{
    [System.Serializable]
    public enum NetworkMessageId
    {
        None = 0,
        ChatMessage = 1,
        ChangeChannel = 2,
        SetUsername = 3,
        OpenedScene = 4,
    }

    public static class NetworkMessageLookup
    {
        public static Dictionary<NetworkMessageId, System.Type> table = new Dictionary<NetworkMessageId, System.Type>()
        {
            { NetworkMessageId.ChatMessage, typeof(NetworkMessageChatMessage) },
            { NetworkMessageId.ChangeChannel, typeof(NetworkMessageChangeChannel) },
            { NetworkMessageId.SetUsername, typeof(NetworkMessageSetUsername) },
            { NetworkMessageId.OpenedScene, typeof(NetworkMessageOpenedScene) },
        };
    }
}
