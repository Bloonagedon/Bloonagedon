using System;
using System.Linq;
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
    public GameObject[] SettingsPages;
    public GenericUIButtonProxy SettingsStartButton;
    public GenericUIButtonProxy LobbyAccessButton;
    public Text LobbyAccessText;
    public Text SaveSettingsText;
    public GenericUIButtonProxy PreviousPageButton;
    public GenericUIButtonProxy NextPageButton;
    public Text PageText;
    public int CurrentPage { get; private set; }

    //Page 1
    public GenericUIButtonProxy MapsPlusButton;
    public GenericUIButtonProxy MapsMinusButton;
    public Text MapsNumberText;
    public GenericUIButtonProxy ToggleTotemsButton;
    public Text TotemSettingText;
    public GenericUIButtonProxy ToggleItemsButton;
    public Text ItemSettingText;
    //
    
    //Page 2
    public GenericUIButtonProxy ToggleBackrowsButton;
    public Text BackrowsSettingText;
    public GenericUIButtonProxy ToggleFrontrowSacrificesButton;
    public Text SacrificeSettingText;
    //
    
    //Page 3
    public GenericUIButtonProxy ScaleSizeMinusButton;
    public GenericUIButtonProxy ScaleSizePlusButton;
    public Text ScaleSizeText;
    public GenericUIButtonProxy NodeWidthMinusButton;
    public GenericUIButtonProxy NodeWidthPlusButton;
    public Text NodeWidthText;
    public GenericUIButtonProxy NodeLengthMinusButton;
    public GenericUIButtonProxy NodeLengthPlusButton;
    public Text NodeLengthText;
    //

    public void AdvanceSettingsPage(int offset)
    {
        var newIndex = Mathf.Max(Mathf.Min(CurrentPage + offset, SettingsPages.Length - 1), 0);
        if (newIndex != CurrentPage)
        {
            SettingsPages[CurrentPage].SetActive(false);
            SettingsPages[newIndex].SetActive(true);
            CurrentPage = newIndex;
            PageText.text = $"PAGE {newIndex+1}/{SettingsPages.Length}";
        }
    }

    public void ResetSettingsPages()
    {
        foreach(var page in SettingsPages.Skip(1))
            page.SetActive(false);
        SettingsPages[0].SetActive(true);
        CurrentPage = 0;
        PageText.text = $"PAGE 1/{SettingsPages.Length}";
    }
    #endregion
    
    #region LobbyTab
    public GameObject TabGroup_Lobby;
    public Text OtherPlayerText;
    public GenericUIButtonProxy ChangeSettingsButton;
    public GenericUIButtonProxy StartGameButton;
    public GenericUIButtonProxy LeaveLobbyButton;
    public GenericUIButtonProxy PlayWithBotButton;
    public Text StartGameText;
    #endregion

    public void ResetMenu()
    {
        TabGroup_Quickplay.SetActive(false);
        TabGroup_Settings.SetActive(false);
        TabGroup_Lobby.SetActive(false);
        TabGroup_JoinMode.SetActive(true);
        ResetSettingsPages();
    }
}
