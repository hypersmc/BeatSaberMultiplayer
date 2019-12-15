﻿using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Settings;
using BeatSaberMultiplayer.Data;
using BeatSaberMultiplayer.Misc;
using BeatSaberMultiplayer.UI.FlowCoordinators;
using BeatSaberMultiplayer.UI.UIElements;
using BS_Utils.Gameplay;
using BS_Utils.Utilities;
using HMUI;
using Polyglot;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace BeatSaberMultiplayer.UI
{
    class PluginUI : MonoBehaviour
    {
        public static PluginUI instance;
        
        private MainMenuViewController _mainMenuViewController;
        private RectTransform _mainMenuRectTransform;
        private SimpleDialogPromptViewController _noUserInfoWarning;

        public ServerHubFlowCoordinator serverHubFlowCoordinator;
        public RoomCreationFlowCoordinator roomCreationFlowCoordinator;
        public RoomFlowCoordinator roomFlowCoordinator;
        public ModeSelectionFlowCoordinator modeSelectionFlowCoordinator;
        public ChannelSelectionFlowCoordinator channelSelectionFlowCoordinator;
        public RadioFlowCoordinator radioFlowCoordinator;

        private TextMeshProUGUI _newVersionText;
        private Button _multiplayerButton;

        public static void OnLoad()
        {
            if (instance != null)
            {
                instance.CreateUI();
                return;
            }
            new GameObject("Multiplayer Plugin").AddComponent<PluginUI>();
        }

        public void Awake()
        {
            if (instance != this)
            {
                DontDestroyOnLoad(this);
                instance = this;
                GetUserInfo.UpdateUserInfo();
                SongCore.Loader.SongsLoadedEvent += SongsLoaded;
                CreateUI();
            }
        }

        public void SongsLoaded(SongCore.Loader sender, Dictionary<string, CustomPreviewBeatmapLevel> levels)
        {
            if (_multiplayerButton != null)
            {
                _multiplayerButton.interactable = true;
            }
            else
            {
                CreateUI();
            }

            SongInfo.GetOriginalLevelHashes();
        }

        public void CreateUI()
        {
            try
            {
                _mainMenuViewController = Resources.FindObjectsOfTypeAll<MainMenuViewController>().First();
                _mainMenuRectTransform = _mainMenuViewController.transform as RectTransform;

                if (serverHubFlowCoordinator == null)
                {
                    serverHubFlowCoordinator = BeatSaberUI.CreateFlowCoordinator<ServerHubFlowCoordinator>();
                }
                if (roomCreationFlowCoordinator == null)
                {
                    roomCreationFlowCoordinator = BeatSaberUI.CreateFlowCoordinator<RoomCreationFlowCoordinator>();
                }
                if (roomFlowCoordinator == null)
                {
                    roomFlowCoordinator = BeatSaberUI.CreateFlowCoordinator<RoomFlowCoordinator>();
                }
                if (modeSelectionFlowCoordinator == null)
                {
                    modeSelectionFlowCoordinator = BeatSaberUI.CreateFlowCoordinator<ModeSelectionFlowCoordinator>();
                }
                if (channelSelectionFlowCoordinator == null)
                {
                    channelSelectionFlowCoordinator = BeatSaberUI.CreateFlowCoordinator<ChannelSelectionFlowCoordinator>();
                }
                if (radioFlowCoordinator == null)
                {
                    radioFlowCoordinator = BeatSaberUI.CreateFlowCoordinator<RadioFlowCoordinator>();
                }

                CreateOnlineButton();
                SongCore.Loader.SongsLoadedEvent += SongsLoaded;

                StartCoroutine(CheckVersion());

                BSMLSettings.instance.AddSettingsMenu("Multiplayer", "BeatSaberMultiplayer.UI.Settings", Settings.instance);
            }
            catch (Exception e)
            {
                Plugin.log.Critical($"Unable to create UI! Exception: {e}");
            }
        }

        private void CreateOnlineButton()
        {
            _newVersionText = BeatSaberUI.CreateText(_mainMenuRectTransform, "A new version of the mod\nis available!", new Vector2(55.5f, 33f));
            _newVersionText.fontSize = 5f;
            _newVersionText.lineSpacing = -52;
            _newVersionText.gameObject.SetActive(false);

            Button[] mainButtons = Resources.FindObjectsOfTypeAll<RectTransform>().First(x => x.name == "MainButtons" && x.parent.name == "MainMenuViewController").GetComponentsInChildren<Button>();

            foreach(var item in mainButtons)
            {
                (item.transform as RectTransform).sizeDelta = new Vector2(35f, 30f);
            }

            _multiplayerButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().Last(x => (x.name == "SoloFreePlayButton")), _mainMenuRectTransform, false);
            _multiplayerButton.name = "BSMultiplayerButton";
            Destroy(_multiplayerButton.GetComponentInChildren<LocalizedTextMeshProUGUI>());
            Destroy(_multiplayerButton.GetComponentInChildren<HoverHint>());
            _multiplayerButton.transform.SetParent(mainButtons.First(x => x.name == "SoloFreePlayButton").transform.parent);
            _multiplayerButton.transform.SetAsLastSibling();

            _multiplayerButton.SetButtonText("Online");
            _multiplayerButton.SetButtonIcon(Sprites.onlineIcon);
            //BeatSaberUI.AddHintText(_multiplayerButton.transform as RectTransform, "Play with your friends online!");

            _multiplayerButton.onClick = new Button.ButtonClickedEvent();
            _multiplayerButton.onClick.AddListener(delegate ()
            {
                try
                {
                    MainFlowCoordinator mainFlow = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();

                    if (_noUserInfoWarning == null)
                    {
                        var dialogOrig = ReflectionUtil.GetPrivateField<SimpleDialogPromptViewController>(mainFlow, "_simpleDialogPromptViewController");
                        _noUserInfoWarning = Instantiate(dialogOrig.gameObject).GetComponent<SimpleDialogPromptViewController>();
                    }

                    if (GetUserInfo.GetUserID() == 0 && string.IsNullOrWhiteSpace(GetUserInfo.GetUserName()) || GetUserInfo.GetUserID() == 0)
                    {
                        _noUserInfoWarning.Init("Invalid username and ID", $"Your username and ID are invalid\nMake sure you are logged in", "Go back", "Continue anyway",
                           (selectedButton) =>
                           {
                               mainFlow.InvokeMethod("DismissViewController", _noUserInfoWarning, null, selectedButton == 1);
                               if (selectedButton == 1)
                               {
                                   mainFlow.InvokeMethod("PresentFlowCoordinator", modeSelectionFlowCoordinator, null, true, false);
                               }
                           });

                        mainFlow.InvokeMethod("PresentViewController", _noUserInfoWarning, null, false);

                    }
                    else
                    {
                        mainFlow.InvokeMethod("PresentFlowCoordinator", modeSelectionFlowCoordinator, null, false, false);
                    }
                }
                catch (Exception e)
                {
                    Plugin.log.Critical($"Unable to present flow coordinator! Exception: {e}");
                }
            });
        }     

        IEnumerator CheckVersion()
        {

            Plugin.log.Info("Checking for updates...");

            UnityWebRequest www = UnityWebRequest.Get($"https://api.github.com/repos/andruzzzhka/BeatSaberMultiplayer/releases");
            www.timeout = 10;

            yield return www.SendWebRequest();
            
            if(!www.isNetworkError && !www.isHttpError)
            {
                JSONNode releases = JSON.Parse(www.downloadHandler.text);

                JSONNode latestRelease = releases[0];

                SemVer.Version currentVer = IPA.Loader.PluginManager.GetPlugin("Beat Saber Multiplayer").Metadata.Version;
                SemVer.Version githubVer = new SemVer.Version(latestRelease["tag_name"], true);

                bool newTag = new SemVer.Range($">{currentVer}").IsSatisfied(githubVer);

                if (newTag)
                {
                    Plugin.log.Info($"An update for the mod is available!\nNew mod version: {(string)latestRelease["tag_name"]}\nCurrent mod version: {currentVer}");
                    _newVersionText.gameObject.SetActive(true);
                    _newVersionText.text = $"Version {(string)latestRelease["tag_name"]}\n of the mod is available!\nCurrent mod version: {currentVer}";
                    _newVersionText.alignment = TextAlignmentOptions.Center;
                }
            }
        }
    }
}
