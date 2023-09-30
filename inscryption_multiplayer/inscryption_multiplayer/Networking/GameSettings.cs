using Newtonsoft.Json;

namespace inscryption_multiplayer.Networking
{
    public enum LobbyAccess
    {
        InviteOnly = 0,
        FriendsOnly = 1
    }

    public class GameSettings
    {
        [JsonIgnore] public static GameSettings Current = new();

        public LobbyAccess LobbyType = LobbyAccess.FriendsOnly;
        public int MapsUsed = 1;
        public bool AllowTotems;
        public bool AllowItems = true;
        public int ScaleSize = 5;
        public int NodeWidth = 3;
        public int NodeLength = 13;
        public bool AllowBackrows = true;
        public bool AllowSacrificeOnFrontrows;
        [JsonIgnore] public bool PreviousAllowSecrificeOnFrontrows;
    }
}