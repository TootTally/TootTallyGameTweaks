using HarmonyLib;
using System;
using System.Collections.Generic;
using TootTallyCore.Graphics;
using TootTallyCore.Utils.Assets;
using UnityEngine.UI;
using UnityEngine;
using TootTallyLeaderboard.Replays;
using TootTallySpectator;

namespace TootTallyGameTweaks
{
    public static class GameTweaksPatches
    {
        public static bool _hasSyncedOnce;

        [HarmonyPatch(typeof(GameController), nameof(GameController.Start))]
        [HarmonyPostfix]
        public static void FixChampMeterSize(GameController __instance)
        {
            if (Plugin.Instance.ChampMeterSize.Value == 1f) return;
            //0.29f is the default localScale size
            __instance.champcontroller.letters[0].transform.parent.localScale = Vector2.one * 0.29f * Plugin.Instance.ChampMeterSize.Value; // :skull: that's how the base game gets that object...
            __instance.healthmask.transform.parent.SetParent(__instance.champcontroller.letters[0].transform.parent, true);
            __instance.healthmask.transform.parent.localScale = Vector2.one * Plugin.Instance.ChampMeterSize.Value;
        }

        [HarmonyPatch(typeof(GameController), nameof(GameController.Start))]
        [HarmonyPrefix]
        public static void RemoveMouseSmoothing()
        {
            if (Plugin.Instance.EnableMouseSmoothing.Value) return;
            GlobalVariables.localsettings.mouse_smoothing = 0;
        }

        [HarmonyPatch(typeof(GameController), nameof(GameController.Start))]
        [HarmonyPostfix]
        public static void TouchScreenPatch(GameController __instance)
        {
            if (!Plugin.Instance.TouchScreenMode.Value) return;

            var gameplayCanvas = GameObject.Find("GameplayCanvas").gameObject;
            gameplayCanvas.GetComponent<GraphicRaycaster>().enabled = true;
            gameplayCanvas.transform.Find("GameSpace").transform.localScale = new Vector2(1, -1);
            var button = GameObjectFactory.CreateCustomButton(gameplayCanvas.transform, Vector2.zero, new Vector2(32, 32), AssetManager.GetSprite("Block64.png"), "PauseButton", delegate { OnPauseButtonPress(__instance); });
            button.transform.position = new Vector3(-7.95f, 4.75f, 1f);
        }
        [HarmonyPatch(typeof(GameController), nameof(GameController.Start))]
        [HarmonyPostfix]
        public static void HideTromboner(GameController __instance)
        {
            if (Plugin.Instance.ShowTromboner.Value) return;
            __instance.puppet_human.SetActive(false);
        }

        [HarmonyPatch(typeof(GameController), nameof(GameController.buildNotes))]
        [HarmonyPrefix]
        public static void OverwriteNoteSpacing(GameController __instance)
        {
            if (!Plugin.Instance.OverwriteNoteSpacing.Value || ReplaySystemManager.wasPlayingReplay) return;
            if (int.TryParse(Plugin.Instance.NoteSpacing.Value, out var num) && num > 0)
                __instance.defaultnotelength = (int)(100f / (__instance.tempo * ReplaySystemManager.gameSpeedMultiplier) * num * GlobalVariables.gamescrollspeed);

        }

        [HarmonyPatch(typeof(MuteBtn), nameof(MuteBtn.Start))]
        [HarmonyPostfix]
        public static void SetMuteButtonAlphaOnStart(MuteBtn __instance)
        {
            __instance.cg.alpha = Plugin.Instance.MuteButtonTransparency.Value;
        }

        [HarmonyPatch(typeof(MuteBtn), nameof(MuteBtn.hoverOut))]
        [HarmonyPostfix]
        public static void UnMuteButtonHoverOut(MuteBtn __instance)
        {
            __instance.cg.alpha = Plugin.Instance.MuteButtonTransparency.Value;
        }

        //Yoinked from DNSpy 
        //Token: 0x06000274 RID: 628 RVA: 0x000266D0 File Offset: 0x000248D0
        private static void OnPauseButtonPress(GameController __instance)
        {
            if (!__instance.quitting && __instance.musictrack.time > 0.5f && !__instance.level_finished && __instance.pausecontroller.done_animating && !__instance.freeplay)
            {
                __instance.notebuttonpressed = false;
                __instance.musictrack.Pause();
                __instance.sfxrefs.backfromfreeplay.Play();
                __instance.puppet_humanc.shaking = false;
                __instance.puppet_humanc.stopParticleEffects();
                __instance.puppet_humanc.playCameraRotationTween(false);
                __instance.paused = true;
                __instance.quitting = true;
                __instance.pausecanvas.SetActive(true);
                __instance.pausecontroller.showPausePanel();
                Cursor.visible = true;
            }
        }

        [HarmonyPatch(typeof(GameController), nameof(GameController.startSong))]
        [HarmonyPostfix]
        public static void ResetSyncFlag()
        {
            _hasSyncedOnce = false;
        }

        [HarmonyPatch(typeof(GameController), nameof(GameController.syncTrackPositions))]
        [HarmonyPrefix]
        public static bool SyncOnlyOnce()
        {
            if (Input.GetKey(KeyCode.Space)) return true; //Sync if holding down spacebar
            if (Plugin.Instance.SyncDuringSong.Value) return true; //always sync if enabled
            if (ReplaySystemManager.wasPlayingReplay) return true; //always sync if watching replay
            if (SpectatingManager.IsSpectating) return true; //always sync if spectating someone

            var previousSync = _hasSyncedOnce;
            _hasSyncedOnce = true;
            return !previousSync;
        }

        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.Update))]
        [HarmonyPostfix]
        public static void DetectKeyPressInLevelSelectController(LevelSelectController __instance)
        {
            if (Input.GetKeyDown(Plugin.Instance.RandomizeKey.Value) && !__instance.randomizing)
                __instance.clickRandomTrack();
        }

        [HarmonyPatch(typeof(CardSceneController), nameof(CardSceneController.showMultiPurchaseCanvas))]
        [HarmonyPostfix]
        public static void OverwriteMaximumOpeningCard(CardSceneController __instance)
        {
            if (Plugin.Instance.ShowCardAnimation.Value) return;
            __instance.multipurchase_maxpacks = (int)Mathf.Clamp(__instance.currency_toots / 499f, 1, 999);
        }

        [HarmonyPatch(typeof(CardSceneController), nameof(CardSceneController.clickedContinue))]
        [HarmonyPrefix]
        public static bool OverwriteOpeningCardAnimation(CardSceneController __instance)
        {
            if (Plugin.Instance.ShowCardAnimation.Value) return true;

            __instance.moveAwayOpenedCards();
            return __instance.multipurchase_opened_sacks >= __instance.multipurchase_chosenpacks;
        }

        [HarmonyPatch(typeof(GameController), nameof(GameController.buildAllLyrics))]
        [HarmonyPrefix]
        public static bool OverwriteBuildAllLyrics() => Plugin.Instance.ShowLyrics.Value;

        private static NoteStructure[] _noteArray;

        [HarmonyPatch(typeof(GameController), nameof(GameController.buildNotes))]
        [HarmonyPrefix]
        public static bool OverwriteBuildNotes(GameController __instance)
        {
            if (!Plugin.Instance.OptimizeGame.Value || ReplaySystemManager.wasPlayingReplay) return true;

            BuildNoteArray(__instance, TrombLoader.Plugin.Instance.beatsToShow.Value);
            BuildNotes(__instance);

            return false;
        }

        private static Queue<Coroutine> _currentCoroutines;

        [HarmonyPatch(typeof(GameController), nameof(GameController.animateOutNote))]
        [HarmonyPostfix]
        public static void OnGrabNoteRefsInstantiateNote(GameController __instance)
        {
            if (!Plugin.Instance.OptimizeGame.Value || ReplaySystemManager.wasPlayingReplay) return;

            if (__instance.beatstoshow < __instance.leveldata.Count)
            {
                _currentCoroutines.Enqueue(Plugin.Instance.StartCoroutine(WaitForSecondsCallback(.1f,
                    delegate
                    {
                        _currentCoroutines.Dequeue();
                        BuildSingleNote(__instance, __instance.beatstoshow);
                    })));
            }

        }

        public static IEnumerator<WaitForSeconds> WaitForSecondsCallback(float seconds, Action callback)
        {
            yield return new WaitForSeconds(seconds);
            callback();
        }


        [HarmonyPatch(typeof(PointSceneController), nameof(PointSceneController.Start))]
        [HarmonyPostfix]
        public static void StopAllNoteCoroutine()
        {
            while (_currentCoroutines.TryDequeue(out Coroutine c))
            {
                Plugin.Instance.StopCoroutine(c);
            };
        }


        [HarmonyPatch(typeof(GameController), nameof(GameController.Start))]
        [HarmonyPostfix]

        public static void OnGameControllerStartInitRoutineQueue()
        {
            _currentCoroutines = new Queue<Coroutine>();
        }

        [HarmonyPatch(typeof(GameController), nameof(GameController.activateNextNote))]
        [HarmonyPrefix]
        public static bool RemoveActivateNextNote() => !Plugin.Instance.OptimizeGame.Value || ReplaySystemManager.wasPlayingReplay;

        private static void BuildNotes(GameController __instance)
        {
            __instance.beatstoshow = 0;
            for (int i = 0; i < _noteArray.Length && __instance.beatstoshow < __instance.leveldata.Count; i++)
            {
                BuildSingleNote(__instance, i);
            }
        }

        private static void BuildNoteArray(GameController __instance, int size)
        {
            _noteArray = new NoteStructure[size];
            for (int i = 0; i < size; i++)
            {
                _noteArray[i] = new NoteStructure(GameObject.Instantiate<GameObject>(__instance.singlenote, new Vector3(0f, 0f, 0f), Quaternion.identity, __instance.noteholder.transform));
            }
        }


        private static void BuildSingleNote(GameController __instance, int index)
        {
            if (index > __instance.leveldata.Count - 1)
                return;

            float[] previousNoteData = new float[]
            {
                    9999f,
                    9999f,
                    9999f,
                    0f,
                    9999f
            };
            if (index > 0)
                previousNoteData = __instance.leveldata[index - 1];

            float[] noteData = __instance.leveldata[index];
            bool previousNoteIsSlider = Mathf.Abs(previousNoteData[0] + previousNoteData[1] - noteData[0]) <= 0.02f;
            bool isTapNote = noteData[1] <= 0.0625f && __instance.tempo > 50f && noteData[3] == 0f && !previousNoteIsSlider;
            if (noteData[1] <= 0f)
            {
                noteData[1] = 0.015f;
                __instance.leveldata[index][1] = 0.015f;
            }
            NoteStructure currentNote = _noteArray[index % _noteArray.Length];
            currentNote.CancelLeanTweens();
            currentNote.root.transform.localScale = Vector3.one;
            __instance.allnotes.Add(currentNote.root);
            __instance.flipscheme = previousNoteIsSlider && !__instance.flipscheme;

            currentNote.SetColorScheme(__instance.note_c_start, __instance.note_c_end, __instance.flipscheme);
            currentNote.noteDesigner.enabled = false;

            if (index > 0)
                __instance.allnotes[index - 1].transform.GetChild(1).gameObject.SetActive(!previousNoteIsSlider || index - 1 >= __instance.leveldata.Count); //End of previous note
            currentNote.noteEnd.SetActive(!isTapNote);
            currentNote.noteStart.SetActive(!previousNoteIsSlider);

            currentNote.noteRect.anchoredPosition3D = new Vector3(noteData[0] * __instance.defaultnotelength, noteData[2], 0f);
            currentNote.noteEndRect.localScale = isTapNote ? Vector2.zero : Vector3.one;

            currentNote.noteEndRect.anchoredPosition3D = new Vector3(__instance.defaultnotelength * noteData[1] - __instance.levelnotesize + 11.5f, noteData[3], 0f);
            if (!isTapNote)
            {
                if (index >= TrombLoader.Plugin.Instance.beatsToShow.Value)
                {
                    currentNote.noteEndRect.anchorMin = currentNote.noteEndRect.anchorMax = new Vector2(1, .5f);
                    currentNote.noteEndRect.pivot = new Vector2(0.34f, 0.5f);
                }
            }
            float[] noteVal = new float[]
            {
                    noteData[0] * __instance.defaultnotelength,
                    noteData[0] * __instance.defaultnotelength + __instance.defaultnotelength * noteData[1],
                    noteData[2],
                    noteData[3],
                    noteData[4]
            };
            __instance.allnotevals.Add(noteVal);
            float noteLength = __instance.defaultnotelength * noteData[1];
            float pitchDelta = noteData[3];
            foreach (LineRenderer lineRenderer in currentNote.lineRenderers)
            {
                lineRenderer.gameObject.SetActive(!isTapNote);
                if (isTapNote) continue;
                if (pitchDelta == 0f)
                {
                    lineRenderer.positionCount = 2;
                    lineRenderer.SetPosition(0, new Vector3(-3f, 0f, 0f));
                    lineRenderer.SetPosition(1, new Vector3(noteLength, 0f, 0f));
                }
                else
                {
                    int sliderSampleCount = (int)Plugin.Instance.SliderSamplePoints.Value;
                    lineRenderer.positionCount = sliderSampleCount;
                    lineRenderer.SetPosition(0, new Vector3(-3f, 0f, 0f));
                    for (int k = 1; k < sliderSampleCount; k++)
                    {
                        lineRenderer.SetPosition(k,
                        new Vector3(
                        noteLength / (sliderSampleCount - 1) * k,
                        __instance.easeInOutVal(k, 0f, pitchDelta, sliderSampleCount - 1),
                        0f));
                    }
                }
            }
            __instance.beatstoshow++;
        }

        #region CharSelectPatches
        [HarmonyPatch(typeof(CharSelectController_new), nameof(CharSelectController_new.Start))]
        [HarmonyPrefix]
        public static void OnCharSelectStart()
        {
            if (!Plugin.Instance.RememberMyBoner.Value) return;
            GlobalVariables.chosen_character = Plugin.Instance.CharacterID.Value;
            GlobalVariables.chosen_trombone = Plugin.Instance.TromboneID.Value;
            GlobalVariables.chosen_soundset = Math.Min(Plugin.Instance.SoundID.Value, 5);
            GlobalVariables.chosen_vibe = Plugin.Instance.VibeID.Value;
            GlobalVariables.show_toot_rainbow = Plugin.Instance.TootRainbow.Value;
            GlobalVariables.show_long_trombone = Plugin.Instance.LongTrombone.Value;

        }

        [HarmonyPatch(typeof(CharSelectController_new), nameof(CharSelectController_new.chooseChar))]
        [HarmonyPostfix]
        public static void OnCharSelect(int puppet_choice)
        {
            if (!Plugin.Instance.RememberMyBoner.Value) return;
            Plugin.Instance.CharacterID.Value = puppet_choice;
        }
        [HarmonyPatch(typeof(CharSelectController_new), nameof(CharSelectController_new.chooseTromb))]
        [HarmonyPostfix]
        public static void OnColorSelect(int tromb_choice)
        {
            if (!Plugin.Instance.RememberMyBoner.Value) return;
            Plugin.Instance.TromboneID.Value = tromb_choice;
        }
        [HarmonyPatch(typeof(CharSelectController_new), nameof(CharSelectController_new.chooseSoundPack))]
        [HarmonyPostfix]
        public static void OnSoundSelect(int sfx_choice)
        {
            if (!Plugin.Instance.RememberMyBoner.Value) return;
            Plugin.Instance.SoundID.Value = sfx_choice;
        }
        [HarmonyPatch(typeof(CharSelectController_new), nameof(CharSelectController_new.clickVibeButton))]
        [HarmonyPostfix]
        public static void OnTromboneSelect(int vibe_index)
        {
            if (!Plugin.Instance.RememberMyBoner.Value) return;
            Plugin.Instance.VibeID.Value = vibe_index;
        }
        [HarmonyPatch(typeof(CharSelectController_new), nameof(CharSelectController_new.clickExtraButton))]
        [HarmonyPostfix]
        public static void OnTogRainbowSelect()
        {
            if (!Plugin.Instance.RememberMyBoner.Value) return;
            Plugin.Instance.TootRainbow.Value = GlobalVariables.show_toot_rainbow;
            Plugin.Instance.LongTrombone.Value = GlobalVariables.show_long_trombone;
        }
        #endregion

        [HarmonyPatch(typeof(GameController), nameof(GameController.buildNotes))]
        [HarmonyPrefix]
        public static void FixAudioLatency(GameController __instance)
        {
            if (!Plugin.Instance.AudioLatencyFix.Value) return;

            if (GlobalVariables.practicemode == 1 && !GlobalVariables.turbomode)
                __instance.latency_offset = GlobalVariables.localsettings.latencyadjust * 0.001f * ReplaySystemManager.gameSpeedMultiplier;
        }

        [HarmonyPatch(typeof(ConfettiMaker), nameof(ConfettiMaker.startConfetti))]
        [HarmonyPrefix]
        public static bool RemoveAllConfetti() => Plugin.Instance.ShowConfetti.Value;
    }
}
