using inscryption_multiplayer.Networking;
using System;
using System.Collections.Generic;
using System.Text;

namespace inscryption_multiplayer
{
    public class API
    {
        public static bool MultiplayerActive => InscryptionNetworking.Connection.Connected;

        public static Dictionary<string, Action<string, string>> messageList = new Dictionary<string, Action<string, string>>();
        public static void SendJson(string message, object serializedClass)
        {
            InscryptionNetworking.Connection.SendJson(message, serializedClass);
        }

        public static void SendMessage(string message)
        {
            InscryptionNetworking.Connection.Send(message);
        }

        public static void ReceiveMessage(string message, Action<string, string> receivedMessage)
        {
            messageList.Add(message, receivedMessage);
        }
    }
}
