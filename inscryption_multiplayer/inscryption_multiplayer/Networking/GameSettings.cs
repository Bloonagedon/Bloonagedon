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
        public int MapsUsed = 1; //done
        public bool AllowTotems = true; //done
        public bool AllowItems = true; //done
        public int ScaleSize = 5; //done
        public int NodeWidth = 3; //done
        public int NodeLength = 13; //done
        public bool AllowBackrows = true; //done
        public bool AllowSacrificeOnFrontrows; //done
        [JsonIgnore] public bool PreviousAllowSecrificeOnFrontrows;
    }
}