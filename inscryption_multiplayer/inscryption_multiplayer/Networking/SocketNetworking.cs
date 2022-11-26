#if NETWORKING_SOCKETS

using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace inscryption_multiplayer.Networking
{
    internal class SocketNetworking : InscryptionNetworking
    {
        private class SocketServer : WebSocketBehavior
        {
            internal void Broadcast(byte[] message)
            {
                Sessions.Broadcast(message);
            }

            protected override void OnOpen()
            {
                base.OnOpen();
                Debug.Log("New connection");
                handler = this;
                Connection.Send("start_game");
            }

            protected override void OnMessage(MessageEventArgs e)
            {
                base.OnMessage(e);
                lock (DispatchQueue)
                    DispatchQueue.Enqueue(() => Connection.Receive(false, e.RawData));
            }
        }
        
        private const int PORT = 2600;
        private readonly string Address;

        private static Queue<Action> DispatchQueue = new();

        private static volatile SocketServer handler;
        private WebSocketServer server;
        private WebSocket client;

        internal override bool Connected => IsHost || (client is not null && client.IsAlive);
        internal override bool IsHost => server is not null && server.IsListening;

        internal override void Host()
        {
            server = new WebSocketServer(IPAddress.Parse(Address), PORT, false);
            server.AddWebSocketService<SocketServer>("/Inscryption");
            server.Start();
            Debug.Log($"Started server on {server.Address}:{server.Port}");
            if(START_ALONE)
                Send("start_game");
        }

        internal override void Join()
        {
            client = new WebSocket($"ws://{Address}:{PORT}/Inscryption");
            client.OnOpen += (sender, e) =>
            {
                Debug.Log("Connection successful");
            };
            client.OnMessage += (sender, e) =>
            {
                lock (DispatchQueue)
                    DispatchQueue.Enqueue(() => Receive(false, e.RawData));
            };
            Debug.Log("Connecting to server");
            client.Connect();
        }

        internal override void Leave()
        {
            if (Connected)
            {
                if(IsHost)
                    server.Stop();
                else client.Close();
            }
        }

        internal override void Update()
        {
            lock (DispatchQueue)
            {
                if(DispatchQueue.Count > 0)
                    DispatchQueue.Dequeue().Invoke();
            }
        }

        internal override void Send(byte[] message)
        {
            Receive(true, message);
            if(IsHost)
                handler.Broadcast(message);
            else client.Send(message);
        }

        public override void Dispose()
        {
            base.Dispose();
            server?.Stop();
            client?.Close();
            (client as IDisposable)?.Dispose();
        }

        internal SocketNetworking()
        {
            Address = File.ReadAllText(Path.Combine(Application.persistentDataPath, "WSHost.txt")).Trim();
        }
    }
}

#endif