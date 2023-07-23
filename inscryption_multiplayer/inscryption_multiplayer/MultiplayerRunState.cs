using System.Collections.Generic;
using DiskCardGame;

namespace inscryption_multiplayer
{
    public class MultiplayerRunState
    {
        public static MultiplayerRunState Run = new();

        public Queue<bool> EggQueue = new();    //false -> broken egg, true -> raven egg
        public TotemDefinition OpponentTotem;
        public SelectableItemSlot OpponentItemSlot;
        public bool OpponentItemUsed;
        public bool SkipNextTurn;   //TODO: Actually implement this
    }
}