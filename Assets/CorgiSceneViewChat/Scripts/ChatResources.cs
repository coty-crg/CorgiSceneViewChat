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
        public Texture2D IconStatusOnline;
        public Texture2D IconStatusOffline;

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
}