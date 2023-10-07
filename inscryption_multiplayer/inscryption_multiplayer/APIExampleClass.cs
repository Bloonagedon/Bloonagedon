using System;
using System.Collections.Generic;
using System.Text;

namespace inscryption_multiplayer
{
    public class APIExampleClass
    {
        public class ASerializedClass
        {
            public string someInfo { get; set; }
        }

        public void Awake()
        {
            API.AddMessageReceiver("TestMessage", ReceivedTestMessage);
        }

        public static void AMethod()
        {
            ASerializedClass jsonClass = new ASerializedClass
            {
                someInfo = "info"
            };

            API.SendMessage("TestMessage");
            API.SendJson("TestMessage", jsonClass);
        }

        public void ReceivedTestMessage(string message, string json)
        {
            Plugin.Log.LogInfo("RECEIVED MESSAGE");
        }
    }
}
