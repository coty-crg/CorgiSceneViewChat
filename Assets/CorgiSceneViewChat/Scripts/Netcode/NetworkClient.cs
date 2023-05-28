using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEditor.MemoryProfiler;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CorgiSceneChat
{
    public class NetworkClient
    {
        private static NetworkClient editorClient;
        private Socket clientSocket;

        private ConcurrentQueue<NetworkMessage> _sendQueue = new ConcurrentQueue<NetworkMessage>();
        private Thread _clientThread;
        private bool _running;
        private byte[] _receiveBuffer = new byte[1024 * 16];
        private byte[] _sendBuffer = new byte[1024 * 16];

        private static Dictionary<NetworkMessageId, System.Action<NetworkMessage>> networkCallbacks = new Dictionary<NetworkMessageId, Action<NetworkMessage>>()
        {
            { NetworkMessageId.ChatMessage, OnNetworkMessage_ChatMessage },
        };

        private static void OnNetworkMessage_ChatMessage(NetworkMessage networkMessage)
        {
            var chatMessage = (NetworkMessageChatMessage) networkMessage;
            ChatOverlay.OnChatMessageReceived(chatMessage.chatMessage); 
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

            var chatResources = ChatResources.FindConfig();
            var chatAddress = chatResources.ChatServerAddress;
            var chatIpAddress = IPAddress.Parse(chatAddress);
            
            var clientRemoteEndpoint = new IPEndPoint(chatIpAddress, chatResources.ChatServerPort);
            var clientLocalEndpoint = new IPEndPoint(GetLocalIpAddress(AddressFamily.InterNetwork), chatResources.ChatServerPort + 1); 

            clientSocket = new Socket(clientLocalEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.LingerState = new LingerOption(false, 0);
            clientSocket.NoDelay = true;
            clientSocket.Blocking = false;

            var success_bind = TryBindPortRange(clientSocket, ref clientLocalEndpoint);
            if (!success_bind)
            {
                ChatOverlay.Log($"Failed to connect to the chat server at {clientLocalEndpoint.Address}:{clientLocalEndpoint.Port}"); 
                return;
            }

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
                ChatOverlay.Log($"[client]: Failed to connect to \nremote:{clientSocket.RemoteEndPoint}\nlocal: {clientSocket.LocalEndPoint}");
                Debug.LogException(e);
                return; 
            }

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

        public static bool TryBindPortRange(Socket socket, ref IPEndPoint endpoint)
        {
            var startPort = endpoint.Port;
            var endPort = startPort + 16;
            for (var port = startPort; port < endPort; ++port)
            {
                endpoint = new IPEndPoint(endpoint.Address, port);

                try
                {
                    socket.Bind(endpoint);
                    return true;
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                    Debug.LogWarning($"TryBindPortRange(): Failed to bind {endpoint.Address}:{port}");
                    continue;
                }
            }

            return false;
        }

        public void Shutdown()
        {
            _running = false;

            if(clientSocket != null)
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
    }
}
