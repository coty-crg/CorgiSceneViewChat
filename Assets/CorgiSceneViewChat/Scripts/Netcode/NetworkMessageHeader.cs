using System.Collections;
using System.Collections.Generic;

namespace CorgiSceneChat
{
    [System.Serializable]
    public struct NetworkMessageHeader
    {
        public int NextMessageSize;
        public NetworkMessageId NextMessageId;
    }
}
