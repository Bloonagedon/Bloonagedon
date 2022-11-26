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
    }
}