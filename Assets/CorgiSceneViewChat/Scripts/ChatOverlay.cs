namespace CorgiSceneChat
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.Overlays;
    using UnityEngine.UIElements;
    using UnityEditor.EditorTools;
    using System;
    using UnityEngine.SceneManagement;
    using UnityEditor.VersionControl;

    [Overlay(typeof(SceneView), "Corgi SceneView Chat")]
    public class ChatOverlay : Overlay
    {
        private static List<ChatMessage> _messages = new List<ChatMessage>();
        private static List<ChatOverlay> _overlays = new List<ChatOverlay>();

        private bool _dirty;
        private bool _needsScrollDown;
        private bool _automaticallyOpened;
        private DateTime _autoOpenedAtTime;

        private ScrollView _scrollView;
        private TextField _chatInput;

        private DateTime _previousGizmoUpdateAtTime;

        public override void OnCreated()
        {
            base.OnCreated();
            _overlays.Add(this);

            UnityEditor.EditorApplication.update += EditorUpdate;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnSceneOpened;

            SceneView.duringSceneGui += OnSceneViewGUI;
        }

        public override void OnWillBeDestroyed()
        {
            base.OnWillBeDestroyed();
            _overlays.Remove(this); 

            UnityEditor.EditorApplication.update -= EditorUpdate;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= OnSceneOpened;
            SceneView.duringSceneGui -= OnSceneViewGUI;
        }

        private void OnSceneOpened(Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            var networkClient = NetworkClient.GetNetworkClient();
            networkClient.SendMessage(new NetworkMessageOpenedScene()
            {
                sceneName = scene.name,
            });
        }

        private static void OnSceneViewGUI(SceneView sceneView)
        {
            // Handles.FreeMoveHandle()


            var networkClient = NetworkClient.GetNetworkClient();
            if(networkClient == null || !networkClient.GetIsConnected())
            {
                return;
            }

            foreach(var otherClient in networkClient.TrackedClients)
            {
                var center = otherClient.GizmoPosition;
                var up = otherClient.GizmoRotation * Vector3.up;
                var right = otherClient.GizmoRotation * Vector3.right;
                var forward = otherClient.GizmoRotation * Vector3.forward;

                Handles.color = Color.blue;
                Handles.DrawLine(center, center + forward);

                Handles.color = Color.red;
                Handles.DrawLine(center, center + right);

                Handles.color = Color.green;
                Handles.DrawLine(center, center + up);

                if (!string.IsNullOrEmpty(otherClient.SelectedTransformStr))
                {
                    var unityObject = GetObjectFromGlobalId(otherClient.SelectedTransformStr);

                    if (unityObject != null)
                    {
                        var transformObject = unityObject as Transform;
                        if (transformObject != null)
                        {
                            transformObject.position = otherClient.GizmoPosition;
                            transformObject.rotation = otherClient.GizmoRotation;
                            transformObject.localScale = otherClient.GizmoScale;

                            EditorUtility.SetDirty(transformObject); 
                        }
                    }
                }
            }
        }

        private static Dictionary<int, UnityEngine.Object> _objectLookupCache = new Dictionary<int, UnityEngine.Object>();

        private static UnityEngine.Object GetObjectFromGlobalId(string idString)
        {
            var idHash = idString.GetHashCode();

            if (_objectLookupCache.TryGetValue(idHash, out var obj))
            {
                return obj;
            }

            if (GlobalObjectId.TryParse(idString, out var transformObjectId))
            {
                var unityObject = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(transformObjectId);
                if(unityObject != null)
                {

                    _objectLookupCache.Add(idHash, unityObject);
                    return unityObject; 
                }
            }

            _objectLookupCache.Add(idHash, null);

            return null; 
        }

        private void EditorUpdate()
        {
            if(_needsScrollDown)
            {
                _needsScrollDown = false;
                _scrollView.verticalScroller.ScrollPageDown(100);

            }

            if(_dirty)
            {
                _dirty = false;
                // collapsed = false;
                
                RebuildScrollViewContent();

                if(!_focused)
                {
                    OnInputFocusEvent(null);
                    _automaticallyOpened = true;
                    _autoOpenedAtTime = System.DateTime.UtcNow;
                }
            }

            if(_automaticallyOpened && System.DateTime.UtcNow > _autoOpenedAtTime + new TimeSpan(0, 0, 0, 5, 0))
            {
                OnInputFocusLostEvent(null); 
            }

            if(System.DateTime.UtcNow > _previousGizmoUpdateAtTime + Constants.Network.GizmoSendRate)
            {
                _previousGizmoUpdateAtTime = System.DateTime.UtcNow;

                if(Selection.activeTransform != null)
                {
                    var position = Selection.activeTransform.position;
                    var rotation = Selection.activeTransform.rotation;
                    var scale = Selection.activeTransform.localScale;

                    var networkClient = NetworkClient.GetNetworkClient();
                    if(networkClient != null && networkClient.GetIsConnected())
                    {
                        var transformObjectId = GlobalObjectId.GetGlobalObjectIdSlow(Selection.activeTransform);
                        var transformObjectIdStr = transformObjectId.ToString();

                        networkClient.SendMessage(new NetworkMessageUpdateGizmo()
                        {
                            ClientId = networkClient.GetOurNetId(),
                            gizmoMode = 0,
                            Position_x = position.x,
                            Position_y = position.y,
                            Position_z = position.z,
                            Rotation_x = rotation.x,
                            Rotation_y = rotation.y,
                            Rotation_z = rotation.z,
                            Rotation_w = rotation.w,
                            Scale_x = scale.x,
                            Scale_y = scale.y,
                            Scale_z = scale.z,
                            SelectedGlobalObjectId = transformObjectIdStr,
                        });
                    }
                }
            }
        }

        public static void OnChatMessageReceived(ChatMessage message) 
        {
            _messages.Add(message);

            foreach(var overlay in _overlays)
            {
                if(overlay != null)
                {
                    overlay._dirty = true;
                }
            }
        } 

        public static void Log(string message)
        {
            OnChatMessageReceived(new ChatMessage()
            {
                message = message,
                systemMessage = true,
                username = "syste",
                timestamp = System.DateTime.UtcNow.Ticks,
            });
        }

        private void RebuildScrollViewContent()
        {
            if (_scrollView == null) return;

            _scrollView.contentContainer.Clear();

            for (var i = 0; i < _messages.Count; ++i)
            {
                var messageData = _messages[i];

                var formatted = $"(<b>{messageData.username}</b>): {messageData.message}";

                if(messageData.systemMessage)
                {
                    formatted = $"<i>{messageData.message}</i>";
                }

                var label = new Label(formatted);
                label.style.whiteSpace = WhiteSpace.Normal;
                label.style.maxWidth = 512; 

                _scrollView.Add(label);
            }

            // auto scroll 
            _needsScrollDown = true;
        }

        private void OnChatSubmit(KeyDownEvent e)
        {
            if (e.keyCode != KeyCode.Return)
            {
                return;
            }

            if (string.IsNullOrEmpty(_chatInput.text))
            {
                return;
            }

            var newMessage = new ChatMessage()
            {
                username = ChatResources.GetLocalUsername(),
                message = _chatInput.text,
                timestamp = System.DateTime.UtcNow.Ticks,
            };

            // _messages.Add(newMessage);

            var networkClient = NetworkClient.GetNetworkClient();
            networkClient.SendMessage(new NetworkMessageChatMessage()
            {
                chatMessage = newMessage,
            });

            _chatInput.SetValueWithoutNotify(string.Empty);

            RebuildScrollViewContent();
        }

        private void OnOptionsButton(ClickEvent e)
        {
            var chatResources = ChatResources.FindConfig();
            Selection.SetActiveObjectWithContext(chatResources, null);
        }

        private bool _focused;
        private VisualElement _root;
        private Button _optionsButton;
        private Image _optionsIcon;

        private void OnInputFocusEvent(FocusEvent e)
        {
            _focused = true; 
            _automaticallyOpened = false;

            if (_scrollView == null) return; 

            _scrollView.style.minHeight = 32;
            _scrollView.style.maxHeight = 128;

            _root.style.color = new Color(0.90f, 0.90f, 0.90f, 1.00f);
            _optionsIcon.tintColor = new Color(0.90f, 0.90f, 0.90f, 1.00f);
        }

        private void OnInputFocusLostEvent(FocusOutEvent e)
        {
            _focused = false;
            _automaticallyOpened = false;

            if (_scrollView == null) return;

            _scrollView.style.minHeight = 0;
            _scrollView.style.maxHeight = 0;

            _root.style.color = Color.clear;
            _optionsIcon.tintColor = Color.clear;
        }

        public override VisualElement CreatePanelContent()
        {
            var chatResources = ChatResources.FindConfig();

            _root = new VisualElement();

            _root.RegisterCallback<FocusEvent>(OnInputFocusEvent);
            _root.RegisterCallback<FocusOutEvent>(OnInputFocusLostEvent);
            
            _scrollView = new ScrollView(ScrollViewMode.Vertical);
            _scrollView.style.minWidth = 512;
            _scrollView.style.minHeight = 32;
            _scrollView.style.maxHeight = 128;

            _scrollView.RegisterCallback<FocusEvent>(OnInputFocusEvent);
            _scrollView.RegisterCallback<FocusOutEvent>(OnInputFocusLostEvent);

            _chatInput = new TextField(256, false, false, '*');
            _chatInput.RegisterCallback<KeyDownEvent>(OnChatSubmit);
            _chatInput.style.minWidth = 432;
            _chatInput.style.maxWidth = 432;

            _chatInput.RegisterCallback<FocusEvent>(OnInputFocusEvent);
            _chatInput.RegisterCallback<FocusOutEvent>(OnInputFocusLostEvent);

            var submitChatGroup = new VisualElement();
            submitChatGroup.style.flexDirection = FlexDirection.Row;

            _optionsButton = new Button();
            _optionsButton.RegisterCallback<ClickEvent>(OnOptionsButton);
            _optionsButton.style.backgroundColor = Color.clear;

            _optionsIcon = new Image();
            _optionsIcon.image = chatResources.ChatOptionsIcon;
            _optionsIcon.style.minWidth = 20;
            _optionsIcon.style.maxWidth = 20;
            _optionsIcon.style.minHeight = 20;
            _optionsIcon.style.maxHeight = 20;
            
            _optionsButton.Add(_optionsIcon);

            submitChatGroup.Add(new Label($"{ChatResources.GetLocalUsername()}: "));
            submitChatGroup.Add(_chatInput);
            submitChatGroup.Add(_optionsButton);

            _root.Add(_scrollView);
            _root.Add(submitChatGroup);

            RebuildScrollViewContent();  

            // initializes connection 
            NetworkClient.GetNetworkClient();

            return _root;
        }
    }
}