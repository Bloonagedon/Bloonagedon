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

        public static SigilCommunicator<TData> InitSigilCommunicator<TData>(string id)
        {
            if (MultiplayerRunState.Run.SigilCommunicators.TryGetValue(id, out var communicatorBase))
            {
                if (communicatorBase is SigilCommunicatorDummy dummy)
                {
                    var comm = dummy.CreateProper<TData>();
                    MultiplayerRunState.Run.SigilCommunicators[id] = comm;
                    return comm;
                }
                if (communicatorBase is SigilCommunicator<TData> communicator)
                    return communicator;
                throw new ArgumentException(
                    $"Communicator type mismatch (Used: {communicatorBase.GetType().FullName}; Given: {typeof(SigilCommunicator<TData>).FullName})");
            }
            var newComm = new SigilCommunicator<TData>(id);
            MultiplayerRunState.Run.SigilCommunicators.Add(id, newComm);
            return newComm;
        }
    }
}
