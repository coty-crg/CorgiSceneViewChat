namespace CorgiSceneChat
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    public class ChatResources : ScriptableObject
    {
        public Texture2D ChatOptionsIcon;

        [Tooltip("Leave this on it's default value (corgichat.coty.tips) unless you are running your own chat server.")] 
        public string ChatServerAddress = "127.0.0.1";

        [Tooltip("Leave this on it's default value (8008) unless you are running your own chat server.")] 
        public int ChatServerPort = 8008;
        
        [Tooltip("Set this to your project's name, or some other unique value so you do not get messages from unknown users.")] 
        public string ChatChannel = "default"; 

        public static ChatResources FindConfig()
        {
            var guids = AssetDatabase.FindAssets("t:ChatResources");
            foreach (var guid in guids)
            {
                if (string.IsNullOrEmpty(guid)) continue;

                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath)) continue;

                var result = AssetDatabase.LoadAssetAtPath<ChatResources>(assetPath);
                if (result == null) continue;

                return result;
            }

            var newEditorConfig = ChatResources.CreateInstance<ChatResources>();

            var newAssetPath = "Assets/ChatResources.asset";
            AssetDatabase.CreateAsset(newEditorConfig, newAssetPath);
            AssetDatabase.SaveAssets();
            var newAsset = AssetDatabase.LoadAssetAtPath<ChatResources>(newAssetPath);

            Debug.Log("[CorgiSceneViewChat] ChatResources was not found, so one has been created.", newAsset);

            return newAsset;
        }

        public static string GetLocalUsername()
        {
            return EditorPrefs.GetString(Constants.EditorPrefStrings.Username, Environment.UserName);
        }

        public static void SetLocalUsername(string username)
        {
            EditorPrefs.SetString(Constants.EditorPrefStrings.Username, username);
        }
    }

    [CustomEditor(typeof(ChatResources))]
    public class ChatResourcesEditor : Editor
    {
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
                if(GUILayout.Button("reconnect"))
                {
                    ChatOverlay.Log($"Resetting connection to chat server.");

                    var networkClient = NetworkClient.GetNetworkClient();
                        networkClient.Shutdown();
                        networkClient.Initialize(); 
                }
            }
            GUILayout.EndVertical();
        }
    }
}