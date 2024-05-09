using DiskCardGame;
using System.Collections.Generic;
using static inscryption_multiplayer.Utils;

namespace inscryption_multiplayer
{
    public class MultiplayerRunState
    {
        public static MultiplayerRunState Run = new();

        public Dictionary<string, SigilCommunicatorBase> SigilCommunicators = new();
        public TotemDefinition OpponentTotem;
        public SelectableItemSlot OpponentItemSlot;
        public bool OpponentItemUsed;
        public bool SkipNextTurn;   //TODO: Actually implement this
        internal SigilCommunicator<bool> EggCommunicator;
        internal SigilCommunicator<int> SniperCommunicator;
        internal SigilCommunicator<CardSlotMultiplayer> LatchCommunicator;
        internal void InitCommunicators()
        {
            EggCommunicator = API.InitSigilCommunicator<bool>("Vanilla_Egg");
            SniperCommunicator = API.InitSigilCommunicator<int>("Vanilla_Sniper");
            LatchCommunicator = API.InitSigilCommunicator<CardSlotMultiplayer>("Vanilla_Latch");
        }
    }
}
