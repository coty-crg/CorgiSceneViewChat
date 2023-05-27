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

#if UNITY_EDITOR
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
#endif
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
                var username = EditorPrefs.GetString("corgichat_username", Environment.UserName);

                var newUsername = EditorGUILayout.TextField("Username", username);

                if (username != newUsername)
                {
                    EditorPrefs.SetString("corgichat_username", newUsername);
                }
            }

            GUILayout.EndVertical();
        }
    }
}