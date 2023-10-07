using System.Collections.Generic;
using DiskCardGame;
using inscryption_multiplayer.Networking;
using Newtonsoft.Json;
using UnityEngine;

namespace inscryption_multiplayer
{
    internal class SigilEvent
    {
        public string ID;
        public string Data;
    }

    internal class SigilData<TData>
    {
        public TData Data;
    }
    
    public abstract class SigilCommunicatorBase
    {
        public readonly string ID;
        
        internal abstract void Process(string data);

        internal SigilCommunicatorBase(string id)
        {
            ID = id;
        }
    }
    
    public class SigilCommunicator<TData> : SigilCommunicatorBase
    {
        public class SigilReceiver : WaitForSecondsRealtime
        {
            public enum ReceiverState
            {
                TimeWait,
                DataWait,
                Completed
            }
            
            private readonly SigilCommunicator<TData> Communicator;
            private string WaitText;
            
            public TData Data { get; private set; }
            public ReceiverState State { get; private set; } = ReceiverState.TimeWait;
            
            public override bool keepWaiting
            {
                get
                {
                    if (State == ReceiverState.Completed)
                        return false;
                    if (base.keepWaiting)
                        return true;
                    if (State == ReceiverState.TimeWait)
                    {
                        State = ReceiverState.DataWait;
                        if(!string.IsNullOrEmpty(WaitText))
                            WaitText = Singleton<TextDisplayer>.Instance.ShowMessage(WaitText);
                    }
                    if (Communicator.DataQueue.Count == 0)
                        return true;
                    Data = Communicator.DataQueue.Dequeue();
                    State = ReceiverState.Completed;
                    if(!string.IsNullOrEmpty(WaitText) && Singleton<TextDisplayer>.Instance.textMesh.text == WaitText)
                        Singleton<TextDisplayer>.Instance.Clear();
                    return false;
                }
            }

            internal SigilReceiver(SigilCommunicator<TData> communicator, float waitTime, string waitText) : base(waitTime)
            {
                Communicator = communicator;
                WaitText = waitText;
            }
        }
        
        private protected Queue<TData> DataQueue = new();
        
        internal override void Process(string data)
        {
            var sigilData = JsonConvert.DeserializeObject<SigilData<TData>>(data);
            DataQueue.Enqueue(sigilData.Data);
        }

        public SigilReceiver CreateReceiver(float waitTime = .1f, string waitText = null) =>
            new(this, waitTime, waitText);

        public void Send(TData data)
        {
            InscryptionNetworking.Connection.SendJson(NetworkingMessage.SigilData, new SigilEvent
            {
                ID = ID,
                Data = JsonConvert.SerializeObject(new SigilData<TData>
                {
                    Data = data
                })
            });
        }

        internal SigilCommunicator(string id) : base(id)
        {
        }
    }

    internal class SigilCommunicatorDummy : SigilCommunicator<string>
    {
        internal override void Process(string data)
        {
            DataQueue.Enqueue(data);
        }

        internal SigilCommunicator<TData> CreateProper<TData>()
        {
            var comm = new SigilCommunicator<TData>(ID);
            while (DataQueue.Count > 0)
                comm.Process(DataQueue.Dequeue());
            return comm;
        }
        
        internal SigilCommunicatorDummy(string id) : base(id)
        {
        }
    }
}