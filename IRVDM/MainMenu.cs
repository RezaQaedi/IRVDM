using CitizenFX.Core;
using CitizenFX.Core.UI;
using IRVDMShared;
using MenuAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;
using static IRVDM.CommonFunctions;
using static IRVDM.SyncData;

namespace IRVDM
{
    internal class MainMenu : BaseScript
    {
        #region Varibles
        private readonly Menu UIMainMenu;
        private MainMenuLocation NextMenu;
        //private Camera MenuCam;
        public bool InMenuMusic = false;
        private readonly string Music = "AW_LOBBY_MUSIC_START";
        private readonly string EndCommandMusicEvent = "AW_LOBBY_MUSIC_KILL";
        private bool IsPedDoingIdleScenario = false;
        private bool IsPlayerIdle = false;
        private readonly int MaxTimeToWaitForPlayerBeforeIdle = 60000;
        private readonly bool DoRandomScenarioOnPlayerIdle = true;
        private readonly string VoteMapeTxd = "DeathMatches";
        private int IdleTime = -1;
        private bool FirstTimeMenuOpens = true;
        private readonly ColorFormat LoudOutMarkerColor = new ColorFormat()
        {
            Alpha = 255,
            Red = 255,
            Blue = 0,
            Green = 0,
        };
        private float MarkerZpos = 0.0f;
        private readonly MainMenuUi Ui = new MainMenuUi();
        private VoteMap Vote;
        private List<DeathMatch> VoteMapPool;

        /// <summary>
        /// default Menu location 
        /// </summary>
        public bool UseDefult { get; private set; } = false;
        public bool IsVoteMapRunning { get; private set; } = false;

        public static event Action<int> OnPlayerGettingIdle;

        private readonly Dictionary<string, MainMenuLocation> MainMenuLocationsDic = new Dictionary<string, MainMenuLocation>();


        #endregion
        #region Contractor

        public MainMenu()
        {
            MainMenuLocationsDic = DataController.GetMainMenus();

            if (FirstTimeMenuOpens)
            {
                NextMenu = GetNextMenu();
                Ui.MainMenuLocation = NextMenu;
            }

            UIMainMenu = Ui.GetMenu();
            MarkerZpos = NextMenu.PrimaryWeaponObjPos.Z;
            MenuController.PreventExitingMenu = true;
            Tick += MainMenu_Tick;
            #region Menu EventHandelers
            EventHandlers["IRV:Menu:shutdown"] += new Action(() => ShutDownMainMenu());
            EventHandlers["IRV:Menu:start"] += new Action<bool>(async p => await StartMainMenu(p));
            EventHandlers["IRV:Menu:StartVoteMap"] += new Action<dynamic, int>(StartVoteMap);
            EventHandlers["IRV:Menu:onPlayerVoted"] += new Action<string, string, int>((plName, dmName, voteCount) =>
            {
                Vote.ShowPlayerVoteOnThisItem(dmName, plName);
                Vote.SetVotesForThisItem(dmName, voteCount);
                MainDebug.WriteDebug($"{plName} voted for {dmName}, that have {voteCount} vote");
            });
            UIMainMenu.OnIndexChange += UIMainMenu_OnIndexChange;
            UIMainMenu.OnMenuClose += m =>
            {
                IdleTime = -1;
                IsPlayerIdle = false;

                if (IsPedDoingIdleScenario)
                {
                    StopPlayerPedTaskNow();
                    IsPedDoingIdleScenario = false;
                }
            };
            UIMainMenu.OnItemSelect += UIMainMenu_OnItemSelect;
            #endregion
        }

        private void UIMainMenu_OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
        {
            IdleTime = -1;
            IsPlayerIdle = false;
        }

        private void UIMainMenu_OnIndexChange(Menu menu, MenuItem oldItem, MenuItem newItem, int oldIndex, int newIndex)
        {
            IsPlayerIdle = false;
            IdleTime = -1;
        }

        #endregion
        #region Methods
        private async Task MainMenu_Tick()
        {
            DrawText(2, 0.98f, 0.1f, 0.1f, 0.1f, 0.5f, "iranov~r~dm", 255, 255, 255, 120);
            if (PlayerInfo.PlayerStatus == PlayerStatuses.IsInMainMenu)
            {
                if (!IsInKhalse)
                {
                    DisablePlayerFiring(Game.Player.Handle, true);

                    if (!IsVoteMapRunning)
                    {
                        if (Game.IsControlJustPressed(1, Control.MultiplayerInfo))
                        {
                            if (UIMainMenu.Visible)
                            {
                                UIMainMenu.Visible = false;
                                IsPlayerIdle = false;
                            }
                            else
                            {
                                UIMainMenu.Visible = true;
                                IsPlayerIdle = false;
                            }
                        }
                    }
                    else
                    {
                        Game.DisableAllControlsThisFrame(2);
                        if (UIMainMenu.Visible)
                        {
                            UIMainMenu.Visible = false;
                            IsPlayerIdle = false;
                        }

                        if (Vote != null)
                            Vote.Draw();
                    }

                    if (MenuController.DontOpenAnyMenu)
                        MenuController.DontOpenAnyMenu = false;

                    if (Game.PlayerPed.IsVisible)
                        Game.PlayerPed.IsVisible = false;



                    //making sure we can see ourselfs !
                    SetEntityLocallyVisible(Game.PlayerPed.Handle);
                    DisableControllsForMenuThisFrame();
                    if (Ui != null)
                    {
                        if (Ui.MenuStatus == MainMenuUi.PlayerInMenuStatus.Main)
                        {
                            if (MatchManger.MatchStatus != MatchManger.MatchStatuses.IsGoingToStart)
                            {
                                if (!IsPlayerIdle)
                                {
                                    if (!IsPedDoingIdleScenario)
                                    {
                                        if (IdleTime == -1)
                                        {
                                            IdleTime = GetGameTimer();
                                        }
                                        else if (IdleTime != -1)
                                        {
                                            if (Game.GameTime > IdleTime + MaxTimeToWaitForPlayerBeforeIdle)
                                            {
                                                IsPlayerIdle = true;
                                                IdleTime = -1;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        StopPlayerPedTaskNow();
                                        IsPedDoingIdleScenario = false;
                                        IdleTime = -1;
                                    }
                                }
                                else
                                {
                                    if (!IsPedDoingIdleScenario)
                                    {
                                        if (DoRandomScenarioOnPlayerIdle)
                                        {
                                            BaseUI.ShowNotfi($"getting idle after {MaxTimeToWaitForPlayerBeforeIdle / 1000} second (-_-)");
                                            List<string> temp = vMenuClient.PedScenarios.ScenarioNames.Values.ToList();
                                            TaskStartScenarioInPlace(Game.PlayerPed.Handle, temp[new Random().Next(0, temp.Count - 1)], 0, true);
                                            IsPedDoingIdleScenario = true;
                                        }
                                        OnPlayerGettingIdle?.Invoke(MaxTimeToWaitForPlayerBeforeIdle);
                                    }
                                }
                            }
                            if (MatchManger.MatchStatusInTotal == MatchManger.MatchStatuses.IsGoingToStart)
                            {
                                if (IsPlayerIdle)
                                {
                                    IsPlayerIdle = false;
                                    IdleTime = -1;
                                }
                            }
                        }


                        if (Ui.IsShowingLoadOut)
                        {
                            if (Ui.PrimaryProp != null)
                            {
                                SetEntityLocallyVisible(Ui.PrimaryProp.Handle);
                                if (Ui.PrimaryProp.Exists())
                                {
                                    BaseUI.DrawText3D(Ui.PrimaryProp.Position, " ~r~Primary", 1);
                                }
                            }

                            if (Ui.SeconderyProp != null)
                            {
                                SetEntityLocallyVisible(Ui.SeconderyProp.Handle);
                                if (Ui.SeconderyProp.Exists())
                                {
                                    BaseUI.DrawText3D(Ui.SeconderyProp.Position, " ~g~Secondery", 1);
                                }
                            }

                            if (Ui.EquipmentProp != null)
                            {
                                SetEntityLocallyVisible(Ui.EquipmentProp.Handle);
                                if (Ui.EquipmentProp.Exists())
                                {
                                    BaseUI.DrawText3D(Ui.EquipmentProp.Position, " ~b~Equipment", 1);
                                }
                            }

                            //NetworkSetEntityVisibleToNetwork(Ui.PrimaryProp.Handle, true);


                            if (Ui.SelectedUiItem == WepLoadOut.Slot01)
                            {

                                LoudOutMarkerColor.Red = 255;
                                LoudOutMarkerColor.Blue = 0;
                                LoudOutMarkerColor.Green = 0;
                                MarkerZpos = NextMenu.PrimaryWeaponObjPos.Z;
                            }
                            else if (Ui.SelectedUiItem == WepLoadOut.Slot02)
                            {

                                LoudOutMarkerColor.Red = 0;
                                LoudOutMarkerColor.Blue = 0;
                                LoudOutMarkerColor.Green = 255;
                                MarkerZpos = NextMenu.SeconderyWeaponObjPos.Z;
                            }
                            else if (Ui.SelectedUiItem == WepLoadOut.Slot03)
                            {
                                LoudOutMarkerColor.Red = 0;
                                LoudOutMarkerColor.Blue = 255;
                                LoudOutMarkerColor.Green = 0;
                                MarkerZpos = NextMenu.EquipmentWeaponObjPos.Z;
                            }

                            Game.PlayerPed.Task.LookAt(new Vector3(NextMenu.LoadotShowPlayPos.X, NextMenu.LoadotShowPlayPos.Y - 1.5f, MarkerZpos), 500);

                            BaseUI.DrawMarker(2, new Vector3(NextMenu.PrimaryWeaponObjPos.X, NextMenu.PrimaryWeaponObjPos.Y, MarkerZpos + 0.2f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, -180.0f, 0.0f), new Vector3(0.15f, 0.12f, 0.12f), LoudOutMarkerColor.Red, LoudOutMarkerColor.Green,
                                LoudOutMarkerColor.Blue, LoudOutMarkerColor.Alpha);
                        }
                    }
                }
            }

            if (PlayerInfo.PlayerStatus != PlayerStatuses.IsInMainMenu || IsInKhalse)
            {
                if (!MenuController.DontOpenAnyMenu)
                    MenuController.DontOpenAnyMenu = true;
            }
        }

        /// <summary>
        /// Invoke MainMenu Functions to load Menu and tp playerped
        /// </summary>
        /// <param name="style">switchoutplayer</param>
        /// <returns></returns>
        public async Task StartMainMenu(bool style)
        {
            await PreparePlayerPed(style);
            await Delay(2000);
            if (IsInKhalse)
                await GetOutOfKhalse();

            HideLoading();
            while (IsInTeleportState || IsInKhalse || !Screen.Fading.IsFadedIn)
            {
                await Delay(0);
            }

            //loading it for showCase 
            MatchManger.LoadPlayerLoadout();

            Ui.SetCamera(MainMenuUi.MenuCams.MainMenuCamera);

            if (InMenuMusic)
            {
                if (!PrepareMusicEvent(Music))
                    PrepareMusicEvent(Music);

                Game.PlayMusic(Music);
            }

            if (FirstTimeMenuOpens)
            {
                Ui.UnlockItems();
                FirstTimeMenuOpens = false;
            }
            else
                BaseUI.ShowNotfi("You can hide~b~/~w~show menu with ~r~Z", true);

            if (!UIMainMenu.Visible)
                UIMainMenu.Visible = true;
        }

        /// <summary>
        /// Set all Menu opetions off
        /// </summary>
        /// <returns></returns>
        public void ShutDownMainMenu()
        {
            SetMenuOption(false);
            StopVoteMatch();
            UIMainMenu.CloseMenu();
            Ui.DeleteMenuCamera();
        }

        /// <summary>
        /// InvisbleAllPlayers and teleport playerped to menu locations
        /// </summary>
        private async Task PreparePlayerPed(bool style)
        {
            if (!FirstTimeMenuOpens)
            {
                NextMenu = GetNextMenu();
                Ui.MainMenuLocation = NextMenu;
            }

            if (!Game.PlayerPed.IsAlive)
                MatchManger.Respawn(Game.PlayerPed.Position, 100f, false, false);

            NetworkSetInSpectatorMode(false, PlayerPedId());

            Game.PlayerPed.Health = 100;

            Screen.Effects.Start(ScreenEffect.CamPushInFranklin, 5000);
            await TeleportPlayerPedToThisPosition(NextMenu.PlayerPedPos, NextMenu.PlayerHeading, style);
            Screen.Effects.Start(ScreenEffect.CamPushInFranklin, 5000);

            Game.PlayerPed.Task.LookAt(NextMenu.PlayerPedLookPos, 50000);

            UpdatePlayerStatus(PlayerStatuses.IsInMainMenu);
            MenuController.DontOpenAnyMenu = false;
            UIMainMenu.OpenMenu();
            //MenuController.DisableBackButton = true;

            SetMenuOption(true);

            //make sure inMatch set to false
            Game.PlayerPed.IsInvincible = true;

            //added this cuz of first person shit 
            SetFollowPedCamViewMode(0);

            await Delay(1000);
            //must call this after everthing done with teleport
            Game.PlayerPed.IsPositionFrozen = true;
        }

        /// <summary>
        /// option required for MainMenu
        /// </summary>
        /// <param name="toggle"> [on] or [off]</param>
        private async void SetMenuOption(bool toggle)
        {
            //SetAllPlayersVisible(!toggle);
            //DisplayHud(!toggle);
            DisplayRadar(!toggle);

            Game.PlayerPed.IsVisible = !toggle;
            SetMobileRadioEnabledDuringGameplay(toggle);

            if (toggle == false)
            {
                await RenderScriptCamera(false, true, 500);
                Game.PlayerPed.IsPositionFrozen = false;
                DisablePlayerFiring(Game.Player.Handle, false);
                NetworkSetVoiceChannel(1);
                StopMusicNow();
                ClearTimecycleModifier();
            }
            else
            {
                SetTimecycleModifier("NO_fog_alpha");
                SetTimecycleModifierStrength(100f);
                NetworkSetVoiceChannel(999);
            }
        }

        private MainMenuLocation GetNextMenu()
        {
            if (UseDefult == false && MainMenuLocationsDic.Count > 1)
            {
                List<string> temp = new List<string>();

                foreach (string item in MainMenuLocationsDic.Keys)
                {
                    temp.Add(item);

                    if (item == "Shop01")
                        temp.Add(item);
                }

                temp.GroupBy(p => new Random().Next());

                MainMenuLocation nextmenu = MainMenuLocationsDic[temp[new Random().Next(0, temp.Count - 1)]];

                return nextmenu;
            }
            else
            {
                return MainMenuLocationsDic["Shop01"];
            }
        }

        private void StopMusicNow() => Game.PlayMusic(EndCommandMusicEvent);

        public async void StartVoteMap(dynamic data, int time)
        {
            IsVoteMapRunning = true;
            Ui.SetCamera(MainMenuUi.MenuCams.VoteMapCamera);
            await Delay(2000);

            if (Vote != null)
                Vote.Dispose();

            VoteMapPool = new List<DeathMatch>();
            List<VoteMapFormat> voteMaps = new List<VoteMapFormat>();
            foreach (var item in data)
            {
                DeathMatch temp = MatchManger.DeathMatchDic.Values.SingleOrDefault(p => p.Name == item);

                string pickups = "Yes";

                if (temp.PickupWeapons.Count == 0)
                    pickups = "No";

                VoteMapFormat mapFormat = new VoteMapFormat()
                {
                    Title = temp.Name,
                    SubTitle = temp.Name,
                    Txd = VoteMapeTxd,
                    Txn = temp.TextureName,
                    Cheked = false,
                    Description = temp.Description,
                    Details = new Dictionary<string, string>()
                    {
                        //["Match Type"] = temp.Type,
                        ["~b~Maker"] = temp.Author,
                        ["~b~World Time"] = $"{temp.WorldTime.Hour}:{temp.WorldTime.Minutes}:{temp.WorldTime.Second}",
                        ["~b~Weather Type"] = temp.WorldWeather,
                        ["~b~Match Time"] = temp.TimeInMiliSecond.ToString(),
                        ["~b~Pickups"] = pickups,
                    },
                    Icon = 1,
                    IconColor = 1,
                    Money = 0,
                    Rp = 0,
                    TitleAlpha = false,
                    Verifyed = false
                };
                voteMaps.Add(mapFormat);
                VoteMapPool.Add(temp);
            }

            Vote = new VoteMap("Vote", voteMaps);
            Vote.OnVote += new Action<string>(s => TriggerServerEvent("IRV:SV:onPlayerVoted", s));
            MatchManger.BeforMatchCountdown.StartTimerNow(time);
        }

        private void StopVoteMatch()
        {
            if (IsVoteMapRunning)
            {
                IsVoteMapRunning = false;
                Vote.Dispose();
                Vote = null;
            }
        }

        #endregion
    }
}
