using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CorgiSceneChat
{
    [CustomEditor(typeof(ChatResources))]
    public class ChatResourcesEditor : Editor
    {
        public const string ChatServerGithubUrl = "https://github.com/coty-crg/CorgiSceneViewChatServer";

        public override void OnInspectorGUI()
        {
            // draw default inspector (project settings)
            GUILayout.BeginVertical("GroupBox");
            {
                GUILayout.Label("Project Settings (shared)");
                base.OnInspectorGUI();
            }
            GUILayout.EndVertical();

            // draw custom inspector (editor prefs settings) 
            GUILayout.BeginVertical("GroupBox");
            {
                GUILayout.Label("Local Settings (this PC only)");
                var username = ChatResources.GetLocalUsername();

                var newUsername = EditorGUILayout.TextField("Username", username);

                if (username != newUsername)
                {
                    ChatResources.SetLocalUsername(newUsername);
                }
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical("GroupBox");
            {
                GUILayout.BeginHorizontal();

                GUILayout.Label("Chat Network Status: ", GUILayout.Width(128f)); 

                var instance = (ChatResources) target;
                var networkClient = NetworkClient.GetNetworkClient();
                var icon = networkClient.GetIsConnected() ? instance.IconStatusOnline : instance.IconStatusOffline;
                if(icon != null)
                {
                    GUILayout.Box(icon, GUILayout.Width(16f), GUILayout.Height(16f)); 
                }

                if (GUILayout.Button("reconnect", GUILayout.Width(128f)))
                {
                    ChatOverlay.Log($"Resetting connection to chat server.");

                    networkClient.Shutdown();
                    networkClient.Initialize();
                }

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                var ignoredGithubMessage = EditorPrefs.GetBool(Constants.EditorPrefStrings.IgnoredGithubMessage, false); 
                if(!ignoredGithubMessage)
                {
                    EditorGUILayout.HelpBox($"Would you like to run your own chat server? Feel free to give it a shot, at: {ChatServerGithubUrl}", MessageType.Info);

                    GUILayout.BeginVertical();
                    if (GUILayout.Button("show me!"))
                    {
                        Application.OpenURL(ChatServerGithubUrl);
                    }

                    if (GUILayout.Button("ignore"))
                    {
                        EditorPrefs.SetBool(Constants.EditorPrefStrings.IgnoredGithubMessage, true); 
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }
    }
}
