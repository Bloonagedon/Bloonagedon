using UnityEngine;
using UnityEngine.UI;

public class InscryptionMultiplayerMenuUI : MonoBehaviour
{
    #region JoinMode
    public GameObject TabGroup_JoinMode;
    public GenericUIButtonProxy HostButton;
    public GenericUIButtonProxy JoinButton;
    public Text QuickplayText;
    #endregion
    
    #region Quickplay
    public GameObject TabGroup_Quickplay;
    public GenericUIButtonProxy QuickplayCancelButton;
    #endregion
    
    #region Settings
    public GameObject TabGroup_Settings;
    public GenericUIButtonProxy SettingsStartButton;
    public GenericUIButtonProxy LobbyAccessButton;
    public Text LobbyAccessText;
    public Text SaveSettingsText;
    #endregion
    
    #region LobbyTab
    public GameObject TabGroup_Lobby;
    public Text OtherPlayerText;
    public GenericUIButtonProxy ChangeSettingsButton;
    public GenericUIButtonProxy StartGameButton;
    public GenericUIButtonProxy LeaveLobbyButton;
    public Text StartGameText;
    #endregion

    public void ResetMenu()
    {
        TabGroup_Quickplay.SetActive(false);
        TabGroup_Settings.SetActive(false);
        TabGroup_Lobby.SetActive(false);
        TabGroup_JoinMode.SetActive(true);
    }
}