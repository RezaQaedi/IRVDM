using CitizenFX.Core;
using CitizenFX.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;
using static IRVDM.CommonFunctions;

namespace IRVDM
{
    class SpectorMode : BaseScript
    {
        private static bool PlayerIsChangingSpector = false;
        private readonly static string ScaleName = "breaking_news";
        private static Scaleform Wazel;
        private static Scaleform InstructionalButtonsScaleform;
        private static Scaleform CameraScaleform;
        private static int Index = 0;
        private static int NowSpectingPlayerId = -1;
        private static List<BasePlayerData> InMatchUsersList;

        public enum Spect
        {
            Next,
            back
        }

        public SpectorMode()
        {
            Tick += SpectorMode_Tick;
            SyncData.OnPlayerDropped += new Action<int>(p =>
            {
                if (SyncData.PlayerInfo.PlayerStatus == PlayerStatuses.IsInSpectorMode)
                {
                    if (SyncData.UsersDataDic.Values.Where(u => u.PlayerStatus == PlayerStatuses.IsInMatch).ToList().Count() == 0)
                    {
                        Stop();
                        TriggerEvent("IRV:Menu:start", false);
                    }
                    else if (p == NowSpectingPlayerId)
                        Go(Spect.back);
                }
            });
        }

        private async Task SpectorMode_Tick()
        {
            if (CameraScaleform == null)
            {
                CameraScaleform = new Scaleform("security_camera");

                while (!CameraScaleform.IsLoaded)
                {
                    await Delay(0);
                    RequestScaleformMovie("security_camera");
                }
            }

            if (SyncData.PlayerInfo.PlayerStatus == PlayerStatuses.IsInSpectorMode)
            {
                if (!IsInKhalse && !IsInTeleportState)
                {
                    if (CameraScaleform != null)
                        CameraScaleform.Render2D();


                    if (Wazel != null)
                        Wazel.Render2D();


                    if (InstructionalButtonsScaleform != null)
                        DrawScaleformMovieFullscreen(InstructionalButtonsScaleform.Handle, 255, 255, 255, 255, 0);


                    if (Game.IsControlJustPressed(0, Control.PhoneLeft)) // arrow left
                        Go(Spect.back);

                    if (Game.IsControlJustPressed(0, Control.PhoneRight)) //arrow right
                        Go(Spect.Next);

                    if (Game.IsControlJustPressed(0, Control.FrontendRright)) //backspace 
                    {
                        Stop();
                        TriggerEvent("IRV:Menu:start", false);
                    }
                }

                DisableControllsForMenuThisFrame();
            }
        }

        public static async void Start(int serverId = -1, bool loading = true)
        {
            InMatchUsersList = SyncData.UsersDataDic.Values.Where(p => p.PlayerStatus == PlayerStatuses.IsInMatch).ToList();
            if (InMatchUsersList.Count() == 0)
                return;

            if (loading)
            {
                ShowLoading();

                //while (!Screen.Fading.IsFadedOut)
                //{
                //    await Delay(1000);
                //}
            }

            if (MatchManger.NextMatch != null)
            {
                MatchManger.ToggleIpls(true);
            }

            int playerServerId;

            if (serverId != -1)
            {
                playerServerId = serverId;
                Index = InMatchUsersList.IndexOf(InMatchUsersList.Find(p => p.Id == serverId));
                NowSpectingPlayerId = serverId;
            }
            else
            {
                Index = 0;
                playerServerId = InMatchUsersList[Index].Id;
                NowSpectingPlayerId = InMatchUsersList[Index].Id;
            }
            int player = GetPlayerFromServerId(playerServerId);

            NetworkSetInSpectatorMode(true, GetPlayerPed(player));

            await SetScaleSubtitle(GetPlayerName(player).ToString());

            InstructionalButtonsScaleform = new Scaleform("instructional_buttons");
            while (!InstructionalButtonsScaleform.IsLoaded)
            {
                RequestScaleformMovie("instructional_buttons");
                await Delay(0);
            }

            InstructionalButtonsScaleform.CallFunction("CLEAR_ALL");
            InstructionalButtonsScaleform.CallFunction("TOGGLE_MOUSE_BUTTONS", 0);
            InstructionalButtonsScaleform.CallFunction("CREATE_CONTAINER");
            InstructionalButtonsScaleform.CallFunction("SET_DATA_SLOT", 0, GetControlInstructionalButton(2, (int)Control.PhoneRight, 0), "Next");
            InstructionalButtonsScaleform.CallFunction("SET_DATA_SLOT", 1, GetControlInstructionalButton(2, (int)Control.PhoneLeft, 0), "Back");
            InstructionalButtonsScaleform.CallFunction("SET_DATA_SLOT", 2, GetControlInstructionalButton(2, (int)Control.FrontendRright, 0), "Menu");
            InstructionalButtonsScaleform.CallFunction("DRAW_INSTRUCTIONAL_BUTTONS", -1);

            await Delay(1000);

            if (loading)
                HideLoading();

            Screen.Effects.Start(ScreenEffect.CamPushInFranklin, 2000);
            Screen.Effects.Start(ScreenEffect.ChopVision, 0, true);
            GameplayCamera.Shake(CameraShake.Hand, 1f);
            GameplayCamera.RelativeHeading = GetEntityHeading(GetPlayerPed(player));
            SyncData.UpdatePlayerStatus(PlayerStatuses.IsInSpectorMode);

            //SetAllPlayersVisible(true);
        }

        private static async Task SetScaleSubtitle(string sub)
        {
            DisposeScale();

            Wazel = new Scaleform(ScaleName);

            if (!Wazel.IsLoaded)
                RequestScaleformMovie(ScaleName);

            while (!Wazel.IsLoaded)
            {
                await Delay(1);
            }

            Wazel.CallFunction("SET_TEXT", "SPECTING", sub);
        }

        public static async void Stop()
        {
            while (PlayerIsChangingSpector)
            {
                await Delay(0);
            }

            DisposeScale();
            NetworkSetInSpectatorMode(false, GetPlayerPed(PlayerId()));
            GameplayCamera.StopShaking();
            Screen.Effects.Stop();

            if (MatchManger.NextMatch != null)
            {
                MatchManger.ToggleIpls(false);
            }
        }

        private async void Go(Spect spect)
        {
            if (InMatchUsersList.Count() == 0)
                return;


            ShowLoading();

            PlayerIsChangingSpector = true;

            InMatchUsersList = SyncData.UsersDataDic.Values.Where(p => p.PlayerStatus == PlayerStatuses.IsInMatch).ToList();


            int maxIndex = InMatchUsersList.Count() - 1;
            if (maxIndex >= 1)
            {
                switch (spect)
                {
                    case Spect.Next:
                        if (Index < maxIndex)
                            Index++;
                        else if (Index == maxIndex)
                            Index = 0;
                        break;
                    case Spect.back:
                        if (Index > 0)
                            Index--;
                        else if (Index == 0)
                            Index = maxIndex;
                        break;
                }
            }

            int player = GetPlayerFromServerId(InMatchUsersList[Index].Id);
            NowSpectingPlayerId = InMatchUsersList[Index].Id;
            NetworkSetInSpectatorMode(true, GetPlayerPed(player));
            await SetScaleSubtitle(GetPlayerName(player).ToString());
            await Delay(2000);
            PlayerIsChangingSpector = false;


            HideLoading();
        }

        private static void DisposeScale()
        {
            if (Wazel != null)
            {
                Wazel.Dispose();
                Wazel = null;
            }
        }
    }
}
