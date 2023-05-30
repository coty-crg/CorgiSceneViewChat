using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CorgiSceneChat
{
    public static class Constants
    {
        public static class EditorPrefStrings
        {
            public const string Username = "corgichat_username"; 
            public const string IgnoredGithubMessage = "corgichat_ignored_github"; 
        }

        public static class Network
        {
            public static readonly TimeSpan GizmoSendRate = new TimeSpan(0, 0, 0, 0, 100); 
        }
    }
}
