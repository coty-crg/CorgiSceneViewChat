using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEditor;
using UnityEditor.MemoryProfiler;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CorgiSceneChat
{
    public class NetworkClient
    {
        public List<OtherClient> TrackedClients = new List<OtherClient>(); 

        private static NetworkClient editorClient;
        private Socket clientSocket;

        private ConcurrentQueue<NetworkMessage> _sendQueue = new ConcurrentQueue<NetworkMessage>();
        private Thread _clientThread;
        private bool _running;
        private bool _connected;
        private byte[] _receiveBuffer = new byte[1024 * 16];
        private byte[] _sendBuffer = new byte[1024 * 16];

        private int _ourNetId;

        private static Dictionary<NetworkMessageId, System.Action<NetworkMessage>> networkCallbacks = new Dictionary<NetworkMessageId, Action<NetworkMessage>>()
        {
            { NetworkMessageId.ChatMessage, OnNetworkMessage_ChatMessage },
            { NetworkMessageId.UpdateGizmo, OnNetworkMessage_UpdatedGizmo },
            { NetworkMessageId.SetNetId, OnNetworkMessage_SetNetId },
            { NetworkMessageId.AddRemoveTrackedGizmo, OnNetworkMessage_AddRemoveTrackedGizmo },
        };

        private static void OnNetworkMessage_ChatMessage(NetworkMessage networkMessage)
        {
            var chatMessage = (NetworkMessageChatMessage) networkMessage;
            ChatOverlay.OnChatMessageReceived(chatMessage.chatMessage); 
        }

        private static void OnNetworkMessage_UpdatedGizmo(NetworkMessage networkMessage)
        {
            if (editorClient == null) return;

            var message = (NetworkMessageUpdateGizmo) networkMessage;
            foreach(var trackedClient in editorClient.TrackedClients)
            {
                if (trackedClient.ClientId != message.ClientId) continue;

                trackedClient.GizmoMode = message.gizmoMode;
                trackedClient.GizmoPosition = new Vector3(message.Position_x, message.Position_y, message.Position_z);
                trackedClient.GizmoRotation = new Quaternion(message.Rotation_x, message.Rotation_y, message.Rotation_z, message.Rotation_w);
                trackedClient.GizmoScale = new Vector3(message.Scale_x, message.Scale_y, message.Scale_z);
                trackedClient.SelectedTransformStr = message.SelectedGlobalObjectId; 
            }
        }
        
        private static void OnNetworkMessage_SetNetId(NetworkMessage networkMessage)
        {
            if (editorClient == null) return;

            var message = (NetworkMessageSetNetId) networkMessage;
            editorClient._ourNetId = message.ClientId;
        }

        private static void OnNetworkMessage_AddRemoveTrackedGizmo(NetworkMessage networkMessage)
        {
            if (editorClient == null) return;

            var message = (NetworkMessageAddRemoveTrackedGizmo) networkMessage;

            if(editorClient._ourNetId == message.ClientId)
            {
                return; 
            }

            if(message.adding)
            {
                editorClient.TrackedClients.Add(new OtherClient()
                {
                     ClientId = message.ClientId,
                });
            }

            if(message.removing)
            {
                for(var i = editorClient.TrackedClients.Count - 1; i >= 0; --i)
                {
                    if (editorClient.TrackedClients[i].ClientId == message.ClientId)
                    {
                        editorClient.TrackedClients.RemoveAt(i); 
                    }
                }
            }
        }

        public void SendMessage(NetworkMessage message)
        {
            _sendQueue.Enqueue(message); 
        }

        public static NetworkClient GetNetworkClient()
        {
            if(editorClient != null)
            {
                if(!editorClient._running)
                {
                    editorClient.Initialize();
                }

                return editorClient;
            }

            editorClient = new NetworkClient(); 
            editorClient.Initialize();

            return editorClient; 
        }

        public void Initialize()
        {
            _running = true;
            _connected = false;

            var chatResources = ChatResources.FindConfig();
            var chatAddress = chatResources.ChatServerAddress;

            if(!IPAddress.TryParse(chatAddress, out var chatIpAddress))
            {
                var dnsEntry = Dns.GetHostEntry(chatAddress);
                if(dnsEntry.AddressList.Length == 0)
                {
                    ChatOverlay.Log($"Couldn't find DNS entry for {chatAddress}");
                    return; 
                }

                chatIpAddress = dnsEntry.AddressList[0];

                // debug 
                // ChatOverlay.Log($"{chatAddress} resolved to {chatIpAddress}");
            }

            var clientRemoteEndpoint = new IPEndPoint(chatIpAddress, chatResources.ChatServerPort);
            var clientLocalEndpoint = new IPEndPoint(GetLocalIpAddress(AddressFamily.InterNetwork), chatResources.ChatServerPort + 1); 

            clientSocket = new Socket(clientLocalEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.BeginConnect(clientRemoteEndpoint, OnBeginConnect, clientSocket);

            SendMessage(new NetworkMessageChangeChannel()
            {
                channel = chatResources.ChatChannel
            });

            SendMessage(new NetworkMessageSetUsername()
            {
                username = ChatResources.GetLocalUsername(),
            });

            SendMessage(new NetworkMessageOpenedScene()
            {
                sceneName = SceneManager.GetActiveScene().name
            });
        }

        private void OnBeginConnect(IAsyncResult result)
        {
            try
            {
                clientSocket.EndConnect(result);
                ChatOverlay.Log("Connected to chat server.");
            }
            catch (System.Exception e)
            {
                ChatOverlay.Log($"<color=red>Failed to connect to the chat server.</color>");
                ChatOverlay.Log($"{e.Message}");
                return; 
            }  

            _connected = true;
            _clientThread = new Thread(() => NetworkLoop());
            _clientThread.Name = "CorgiNetThread";
            _clientThread.IsBackground = true; 
            _clientThread.Start();
        }

        private void NetworkLoop()
        {
            while (_running)
            {
                Thread.Sleep(16);

                try
                {
                    // receive messages 
                    if(clientSocket.Available >= Serialization.HeaderSize)
                    {
                        var receivedBytes = clientSocket.Receive(_receiveBuffer, 0, Serialization.HeaderSize, SocketFlags.Peek);
                        if(receivedBytes >= Serialization.HeaderSize)
                        {
                            var header = Serialization.PeekBuffer_NetworkMessageHeader(_receiveBuffer, 0);

                            if(clientSocket.Available >= header.NextMessageSize)
                            {
                                receivedBytes = clientSocket.Receive(_receiveBuffer, 0, Serialization.HeaderSize + header.NextMessageSize, SocketFlags.None);

                                var readIndex = 0;
                                var networkMessage = Serialization.ReadBuffer_NetworkMessage(_receiveBuffer, ref readIndex);

                                if(networkCallbacks.TryGetValue(header.NextMessageId, out var callback))
                                {
                                    callback.Invoke(networkMessage); 
                                }
                                else
                                {
                                    Debug.LogError($"Received unexpected message? {header.NextMessageId}");
                                }
                            }
                        }
                    } 

                    // send messages 
                    while (_sendQueue.TryDequeue(out var sendMessage))
                    {
                        var writeIndex = 0;

                        Serialization.WriteBuffer_NetworkMessage(_sendBuffer, ref writeIndex, sendMessage);
                        clientSocket.Send(_sendBuffer, 0, writeIndex, SocketFlags.None); 
                    }
                }
                catch (System.Exception e)
                {
                    ChatOverlay.Log("You have been disconnected from the server.");
                    ChatOverlay.Log(e.Message);
                    ChatOverlay.Log(e.StackTrace);

                    Debug.LogException(e);

                    Shutdown();  
                    break; 
                }
            }
        }

        public static IPAddress GetLocalIpAddress(AddressFamily family)
        {
            var ourHostname = Dns.GetHostName();
            var ourAddresses = Dns.GetHostAddresses(ourHostname);

            for (var i = 0; i < ourAddresses.Length; ++i)
            {
                var address = ourAddresses[i];

                if (address.AddressFamily == family)
                {
                    return address;
                }
            }

            return null;
        }

        public void Shutdown()
        {
            _running = false;
            _connected = false;

            if (clientSocket != null)
            {
                clientSocket.Dispose();
                clientSocket = null;   
            }

            if(_clientThread != null)
            {
                _clientThread.Abort();
                _clientThread = null; 
            }
        }

        public bool GetIsConnected()
        {
            return _connected;
        }

        public int GetOurNetId()
        {
            return _ourNetId;
        }
    }
}
