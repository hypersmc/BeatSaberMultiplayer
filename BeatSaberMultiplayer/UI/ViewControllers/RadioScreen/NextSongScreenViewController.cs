﻿using BeatSaberMultiplayer.Data;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BeatSaberMultiplayer.Misc;
using CustomUI.BeatSaber;
using System.Threading;
using HMUI;
using Image = UnityEngine.UI.Image;

namespace BeatSaberMultiplayer.UI.ViewControllers.RadioScreen
{
    class NextSongScreenViewController : ViewController
    {
        public event Action skipPressedEvent;

        TextMeshProUGUI _timerText;

        TextMeshProUGUI _nextSongText;

        SongInfo _currentSongInfo;
        LevelListTableCell _currentSongCell;

        RectTransform _progressBarRect;

        Image _progressBackground;
        Image _progressBarImage;
        TextMeshProUGUI _progressText;

        Button _skipButton;
        TextMeshProUGUI _skipText;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation)
            {
                _timerText = BeatSaberUI.CreateText(rectTransform, "0:30", new Vector2(0f, 35f));
                _timerText.alignment = TextAlignmentOptions.Top;
                _timerText.fontSize = 8f;

                _nextSongText = BeatSaberUI.CreateText(rectTransform, "Next song:", new Vector2(0f, 15.5f));
                _nextSongText.alignment = TextAlignmentOptions.Top;
                _nextSongText.fontSize = 6f;

                _currentSongCell = Instantiate(Resources.FindObjectsOfTypeAll<LevelListTableCell>().First(x => (x.name == "LevelListTableCell")), rectTransform, false);
                (_currentSongCell.transform as RectTransform).anchoredPosition = new Vector2(55f, -27f);
                //_currentSongCell.SetPrivateField("_beatmapCharacteristicAlphas", new float[0]);
                _currentSongCell.SetPrivateField("_beatmapCharacteristicImages", new UnityEngine.UI.Image[0]);
                _currentSongCell.SetPrivateField("_bought", true);
                foreach (var icon in _currentSongCell.GetComponentsInChildren<UnityEngine.UI.Image>().Where(x => x.name.StartsWith("LevelTypeIcon")))
                {
                    Destroy(icon.gameObject);
                }

                _progressBarRect = new GameObject("ProgressBar", typeof(RectTransform)).GetComponent<RectTransform>();

                _progressBarRect.SetParent(rectTransform, false);
                _progressBarRect.anchorMin = new Vector2(0.5f, 0.5f);
                _progressBarRect.anchorMax = new Vector2(0.5f, 0.5f);
                _progressBarRect.anchoredPosition = new Vector2(0f, -7.5f);
                _progressBarRect.sizeDelta = new Vector2(46f, 5f);

                _progressBackground = new GameObject("Background", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                _progressBackground.rectTransform.SetParent(_progressBarRect, false);
                _progressBackground.rectTransform.anchorMin = new Vector2(0f, 0f);
                _progressBackground.rectTransform.anchorMax = new Vector2(1f, 1f);
                _progressBackground.rectTransform.anchoredPosition = new Vector2(0f, 0f);
                _progressBackground.rectTransform.sizeDelta = new Vector2(0f, 0f);

                _progressBackground.sprite = Sprites.whitePixel;
                _progressBackground.material = Sprites.NoGlowMat;
                _progressBackground.color = new Color(1f, 1f, 1f, 0.075f);

                _progressBarImage = new GameObject("ProgressImage", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                _progressBarImage.rectTransform.SetParent(_progressBarRect, false);
                _progressBarImage.rectTransform.anchorMin = new Vector2(0f, 0f);
                _progressBarImage.rectTransform.anchorMax = new Vector2(1f, 1f);
                _progressBarImage.rectTransform.anchoredPosition = new Vector2(0f, 0f);
                _progressBarImage.rectTransform.sizeDelta = new Vector2(0f, 0f);

                _progressBarImage.sprite = Sprites.whitePixel;
                _progressBarImage.material = Sprites.NoGlowMat;
                _progressBarImage.type = Image.Type.Filled;
                _progressBarImage.fillMethod = Image.FillMethod.Horizontal;
                _progressBarImage.fillAmount = 0.5f;

                _progressText = BeatSaberUI.CreateText(rectTransform, "0.0%", new Vector2(55f, -10f));
                _progressText.rectTransform.SetParent(_progressBarRect, true);

                _progressBarRect.gameObject.SetActive(false);

                _skipButton = BeatSaberUI.CreateUIButton(rectTransform, "CancelButton", new Vector2(-12.5f, -25f), new Vector2(20f, 8.8f), () => { skipPressedEvent?.Invoke(); }, "Skip");
                _skipButton.ToggleWordWrapping(false);

                _skipText = BeatSaberUI.CreateText(rectTransform, " this song", new Vector2(8.5f, -21f));
                _skipText.alignment = TextAlignmentOptions.Top;
                _skipText.fontSize = 6f;
                _skipText.rectTransform.sizeDelta = new Vector2(0f, 0f);
                _skipText.overflowMode = TextOverflowModes.Overflow;
                _skipText.enableWordWrapping = false;
            }
            _progressBarRect.gameObject.SetActive(false);
            SetSkipState(false);
        }

        public void SetSongInfo(SongInfo songInfo)
        {
            _currentSongInfo = songInfo;

            if (_currentSongCell != null)
            {
                IPreviewBeatmapLevel level = SongCore.Loader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks.SelectMany(x => x.beatmapLevelCollection.beatmapLevels).FirstOrDefault(x => x.levelID.StartsWith(songInfo.levelId));
                if (level == null)
                {
                    _currentSongCell.SetText(_currentSongInfo.songName);
                    _currentSongCell.SetSubText("Loading info...");
                    SongDownloader.Instance.RequestSongByLevelID(_currentSongInfo.hash, (song) =>
                    {
                        _currentSongCell.SetText($"{song.songName} <size=80%>{song.songSubName}</size>");
                        _currentSongCell.SetSubText(song.songAuthorName + " <size=80%>[" + song.levelAuthorName + "]</size>");
                        StartCoroutine(LoadScripts.LoadSpriteCoroutine(song.coverURL, (cover) => { _currentSongCell.SetIcon(cover); }));
                    }
                    );
                }
                else
                {
                    _currentSongCell.SetText($"{level.songName} <size=80%>{level.songSubName}</size>");
                    _currentSongCell.SetSubText(level.songAuthorName + " <size=80%>[" + level.levelAuthorName + "]</size>");

                    level.GetCoverImageTexture2DAsync(new CancellationTokenSource().Token).ContinueWith((tex) =>
                    {
                        if (!tex.IsFaulted)
                            _currentSongCell.SetIcon(tex.Result);
                    }).ConfigureAwait(false);
                }

            }
        }

        public void SetProgressBarState(bool enabled, float progress)
        {
            _progressBarRect.gameObject.SetActive(enabled);
            _progressBarImage.fillAmount = progress;
            _progressText.text = progress.ToString("P");
        }

        public void SetSkipState(bool skipped)
        {
            _skipButton.SetButtonText((skipped ? "Play" : "Skip"));
        }

        public void SetTimer(float currentTime, float totalTime)
        {
            if (_timerText != null)
            {
                _timerText.text = SecondsToString(totalTime - currentTime);
            }

        }

        public string SecondsToString(float time)
        {
            int minutes = (int)(time / 60f);
            int seconds = (int)(time - minutes * 60);
            return minutes.ToString() + ":" + string.Format("{0:00}", seconds);
        }
    }
}
