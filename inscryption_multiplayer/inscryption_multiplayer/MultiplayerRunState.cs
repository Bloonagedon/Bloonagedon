using System.Collections.Generic;
using DiskCardGame;

namespace inscryption_multiplayer
{
    public class MultiplayerRunState
    {
        public static MultiplayerRunState Run = new();

        public Queue<bool> EggQueue = new();
        public TotemDefinition OpponentTotem;
    }
}