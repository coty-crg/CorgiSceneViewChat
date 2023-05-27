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

    [Overlay(typeof(SceneView), "Corgi SceneView Chat", defaultLayout: true)]
    public class ChatOverlay : Overlay
    {
        private List<ChatMessage> _messages = new List<ChatMessage>()
        {
            new ChatMessage() { username = "test", message = "message "},
            new ChatMessage() { username = "test", message = "message "},
            new ChatMessage() { username = "test", message = "message "},
            new ChatMessage() { username = "test", message = "message "},
            new ChatMessage() { username = "test", message = "message "},
            new ChatMessage() { username = "test", message = "message "},
            new ChatMessage() { username = "test", message = "message "},
            new ChatMessage() { username = "test", message = "message "},
            new ChatMessage() { username = "test", message = "message "},
            new ChatMessage() { username = "test", message = "message "},
            new ChatMessage() { username = "test", message = "message "},
            new ChatMessage() { username = "test", message = "message "},
            new ChatMessage() { username = "test", message = "message "},
            new ChatMessage() { username = "test", message = "message "},
            new ChatMessage() { username = "test", message = "message "},
        };

        private void RebuildScrollViewContent()
        {
            _scrollView.contentContainer.Clear();

            for (var i = 0; i < _messages.Count; ++i)
            {
                var messageData = _messages[i];
                var formatted = $"(<b>{messageData.username}</b>): {messageData.message}";
                var label = new Label(formatted);
                label.style.whiteSpace = WhiteSpace.Normal;

                _scrollView.Add(label);
            }

            // always add a blank one to the end 
            // this is a dumb hack so the auto scroll sees the final message 
            var finalElement = new Label("");
            _scrollView.Add(finalElement);

            // auto scroll 
            _scrollView.verticalScroller.ScrollPageDown(100);
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

            _messages.Add(newMessage);
            _chatInput.SetValueWithoutNotify(string.Empty);

            RebuildScrollViewContent();
        }

        private void OnOptionsButton(ClickEvent e)
        {
            var chatResources = ChatResources.FindConfig();
            Selection.SetActiveObjectWithContext(chatResources, null);
        }

        private ScrollView _scrollView;
        private TextField _chatInput;

        public override VisualElement CreatePanelContent()
        {
            var chatResources = ChatResources.FindConfig();

            var root = new VisualElement();

            _scrollView = new ScrollView(ScrollViewMode.Vertical);
            _scrollView.style.minWidth = 512;
            _scrollView.style.minHeight = 32;
            _scrollView.style.maxHeight = 128;

            _chatInput = new TextField(256, false, false, '*');
            _chatInput.RegisterCallback<KeyDownEvent>(OnChatSubmit);
            _chatInput.style.minWidth = 432;
            _chatInput.style.maxWidth = 432;

            var submitChatGroup = new VisualElement();
            submitChatGroup.style.flexDirection = FlexDirection.Row;

            var chatOptionsButton = new Button();
            chatOptionsButton.RegisterCallback<ClickEvent>(OnOptionsButton);

            var chatOptionsIcon = new Image();
            chatOptionsIcon.image = chatResources.ChatOptionsIcon;
            chatOptionsIcon.style.minWidth = 20;
            chatOptionsIcon.style.maxWidth = 20;
            chatOptionsIcon.style.minHeight = 20;
            chatOptionsIcon.style.maxHeight = 20;

            chatOptionsButton.Add(chatOptionsIcon);

            submitChatGroup.Add(new Label("user: "));
            submitChatGroup.Add(_chatInput);
            submitChatGroup.Add(chatOptionsButton);

            root.Add(_scrollView);
            root.Add(submitChatGroup);

            RebuildScrollViewContent();

            return root;
        }
    }
}