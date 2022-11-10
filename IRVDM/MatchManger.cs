using CitizenFX.Core;
using CitizenFX.Core.UI;
using IRVDMShared;
using NativeUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;
using static IRVDM.CommonFunctions;

namespace IRVDM
{
    internal class MatchManger : BaseScript
    {
        //TODO **Invalid** players must not count in players [server]
        #region Varibles 
        public enum MatchStatuses
        {
            IsRunnig = 1,
            IsWatting = 0,
            HasEnded = 3,
            IsGoingToStart = 4,
        }

        /// <summary>
        /// will updated from server 
        /// </summary>
        public static MatchStatuses MatchStatusInTotal { get; private set; }
        /// <summary>
        /// will updated from client dont have <see cref="MatchStatuses.IsGoingToStart"/> 
        /// </summary>
        public static MatchStatuses MatchStatus { get; private set; }
        public int MaxTimeOutZone { get; private set; } = 10;
        public bool MusicInMatch { get; set; } = true;

        private readonly TextTimerBar MatchGoingToStartTextTimer = new TextTimerBar("~g~STARTING IN", "");

        private readonly string HeadShotParitcleFxAssest = "scr_family1";//
        private readonly string HeadShotFxName = "scr_fam1_blood_headshot";//

        private int TimerAlpha = 100;
        private int TimerUiShowTimeEnd = -1;
        private bool TimerUiSatyOnScreen = false;
        private readonly Dictionary<int, KeyValuePair<string, int>> OnGoingTimerUiKillerNames = new Dictionary<int, KeyValuePair<string, int>>();
        private readonly int TimerUiTimePerEachKillNotif = 6000;
        private int DeathNotifGameTime = -1;

        private Dictionary<int, string> MatchMessages = new Dictionary<int, string>();
        private int LastMessageTime = -1;
        private int MsgsCount = 0;

        private readonly WeaponData WeaponData = new WeaponData();

        public readonly string ContDown_30 = "AW_COUNTDOWN_30S";
        public readonly string StopMusicCommand = "AW_LOBBY_MUSIC_KILL";
        private bool IsInMatchZone = true;
        private int NotInMatchZoneTime = 10;
        private readonly TimerMain MatchTimer = new TimerMain();
        public static TimerMain BeforMatchCountdown = new TimerMain();
        public static Dictionary<string, DeathMatch> DeathMatchDic;
        public static DeathMatch NextMatch { get; private set; }

        public event Action OnMatchStarts;
        public event Action OnPlayerPrepared;

        public List<string> MusicEventsList = new List<string>()
        {
            "MC_AW_MUSIC_1",
            "MC_AW_MUSIC_2",
            "MC_AW_MUSIC_3",
            "MC_AW_MUSIC_4",
            "MC_AW_MUSIC_5",
            "MC_AW_MUSIC_6",
            "MC_AW_MUSIC_7",
            "MC_AW_MUSIC_8"
        };

        private readonly List<Pickup> InMatchPickups = new List<Pickup>();
        #endregion

        #region Constractor
        public MatchManger()
        {
            DeathMatchDic = DataController.GetDeathMatches();
            Tick += MainMatchTick;
            Tick += MainMatchTick1000;
            Tick += TimerUiTick;
            Tick += PlayerBlipsControl;

            GameEventManager.OnPlayerKilled += new Action<int, uint>(PlayerKilled);
            GameEventManager.OnPlayerKillPlayer += new Action<int, uint, bool, bool>(PlayerKilledPlayer);
            GameEventManager.OnPlayerCollectedPickup += new Action<int, int>(PlayerCollectPickup);
            GameEventManager.OnPlayerHeadShotPlayer += new Action<int, uint>(PlayerHeadShotPlayer);
            #region IRVDMEvents
            EventHandlers["IRV:Match:reciveMatchData"] += new Action<int>(ReciveMatchData);
            EventHandlers["IRV:Match:preparePlayer"] += new Action<float, float, float, float>(PreparePlayer);
            EventHandlers["IRV:Match:reciveBeforeMatchCommand"] += new Action<int, bool>((i, b) =>
            {
                BeforMatchCountdown.StartTimerNow(i);
                Screen.ShowNotification("~g~Match is going to start in ~r~" + i / 1000 + "~g~ Second");
            });
            EventHandlers["IRV:Match:setNexMatch"] += new Action<string>(s => NextMatch = DeathMatchDic.Values.FirstOrDefault(d => d.Name == s));
            EventHandlers["IRV:Match:stopMatchNow"] += new Action(StopMatchNow);
            EventHandlers["IRV:Match:startMatchNow"] += new Action(StartMatchNow);
            EventHandlers["IRV:Match:shutDownTheMatch"] += new Action(ShutDownMatch);
            EventHandlers["IRV:Match:removePickup"] += new Action<int>(p => { if (InMatchPickups[p] != null) RemovePickupByIndex(p); });

            //to do [KDNotif]
            EventHandlers["IRV:Match:notifKD"] += new Action<int, int, uint>((v, k, w) =>
            {
                //killer ids
                //-2= ped ,-1= victim ,-3= not found ,else players
                string victimName = GetPlayerName(GetPlayerFromServerId(v));
                string wepName = WeaponData.GetWeaponLableName(w);
                string Msg;
                if (k == -1) //by them selve
                {
                    string reason = WeaponData.GetNotifSPForWep(w, WeaponData.KilledBySuicideNotifSuffixes);
                    if (!string.IsNullOrEmpty(reason))
                        Msg = $"{victimName} {reason}";
                    else
                    {
                        if (!string.IsNullOrEmpty(wepName))
                            Msg = $"{victimName} committed ~b~suicide~w~ with ~r~{wepName}";
                        else
                            Msg = $"{victimName} committed ~b~suicide";
                    }
                }
                else if (k == -2) //by ped
                {
                    string reason = WeaponData.GetNotifSPForWep(w, WeaponData.KilledByPedNotifSuffixes);
                    if (!string.IsNullOrEmpty(reason))
                        Msg = $"Npc {reason} {victimName}";
                    else
                    {

                        if (!string.IsNullOrEmpty(wepName))
                            Msg = $"Npc ~b~killed ~w~ {victimName} with ~r~{wepName}";
                        else
                            Msg = $"Npc ~b~killed ~w~ {victimName}";
                    }
                }
                else if (k == -3)
                {
                    Msg = $"{victimName} has ~b~killed";
                }
                else
                {
                    string reason = WeaponData.GetNotifSPForWep(w, WeaponData.KilledByPlayerNotifPrefixes);
                    if (!string.IsNullOrEmpty(reason))
                        Msg = $"{GetPlayerName(GetPlayerFromServerId(k))} {reason} {victimName}";
                    else
                    {

                        if (!string.IsNullOrEmpty(wepName))
                            Msg = $"{GetPlayerName(GetPlayerFromServerId(k))} ~b~killed ~w~ {victimName} with ~r~{wepName}";
                        else
                            Msg = $"{GetPlayerName(GetPlayerFromServerId(k))} ~b~killed ~w~ {victimName}";
                    }
                }

                ShowMatchMessage(Msg);
            });

            #endregion

            #region TimersEvents
            MatchTimer.OnTimerEnd += async () =>
            {
                if (MatchStatus == MatchStatuses.IsRunnig)
                    await StartKhalse();
            };
            BeforMatchCountdown.OnTimerEnd += () => Screen.Effects.Start(ScreenEffect.FocusOut, 2000);
            #endregion
            OnPlayerPrepared += () => TriggerServerEvent("IRV:SV:onPlayerPrepared");
        }



        private async Task TimerUiTick()
        {
            foreach (var item in OnGoingTimerUiKillerNames)
            {
                ShowKillerNameTimerUi(item.Value.Key, item.Key, item.Value.Value);
            }

            if (MatchStatus == MatchStatuses.IsRunnig)
            {
                DrawRect(0.471f + 0.031f, 0.1f, 0.024f * 2f, 0.041f, 174, 174, 174, TimerAlpha);
                DrawRect(0.444f + 0.022f, 0.1f, 0.012f * 2f, 0.041f, 0, 0, 0, TimerAlpha); //kill
                DrawRect(0.502f + 0.036f, 0.1f, 0.012f * 2f, 0.041f, 0, 0, 0, TimerAlpha); //death
                DrawText(0, 0.607f + 0.025f, 0.1f, 0.3f, 0.045f, 0.47f, MatchTimer.CurrentTimer.ToString(), 0, 0, 0, TimerAlpha);
                DrawText(0, 0.646f + 0.036f, 0.1f, 0.3f, 0.045f, 0.47f, SyncData.PlayerInfo.UserDeaths.ToString(), 255, 0, 0, TimerAlpha); //death
                DrawText(0, 0.589f + 0.022f, 0.1f, 0.3f, 0.045f, 0.47f, SyncData.PlayerInfo.UserKills.ToString() + " ", 0, 255, 0, TimerAlpha); //kill
                ShowDeathNotifTimerUi();
            }
        }

        private void PlayerHeadShotPlayer(int victim, uint weaponHash)
        {
            if (SyncData.PlayerInfo.PlayerStatus == PlayerStatuses.IsInMatch)
            {
                //make sure this PedPlayer exist plase
                int id = Players.First(p => GetPlayerPed(p.Handle) == victim).ServerId;
                StartParticleFxOnEntityBone(HeadShotParitcleFxAssest, HeadShotFxName, victim, Bone.SKEL_Head, 2.5f);
            }
        }

        private void PlayerCollectPickup(int handel, int ammo)
        {
            int index = GetPickupIndex(handel);

            if (index != -1)
                TriggerServerEvent("IRV:SV:onPlayerCollectPickup", index);
            else
                MainDebug.WriteDebug("cant find index of pickup", MainDebug.Prefixes.warning);
        }

        private void PlayerKilledPlayer(int victim, uint wepHash, bool isMele, bool isHead)
        {
            if (SyncData.PlayerInfo.PlayerStatus == PlayerStatuses.IsInMatch)
            {
                //make sure this PedPlayer exist plase
                Player deadPlayer = Players.First(p => GetPlayerPed(p.Handle) == victim);
                TimerUiShowKiller(deadPlayer.Name);
                Screen.Effects.Start(ScreenEffect.CamPushInTrevor, 800, false);
                StartParticleFxOnPlayer(SyncData.PlayerInfo.Loadout.ParticleFx.AssestName, SyncData.PlayerInfo.Loadout.ParticleFx.OnPlayerKillParticleName, deadPlayer.ServerId);
            }

            if (isHead)
            {
                StartParticleFxOnEntityBone(HeadShotParitcleFxAssest, HeadShotFxName, victim, Bone.SKEL_Head, 2.0f);
            }
        }

        private void PlayerKilled(int k, uint w)
        {
            TimerUiShowDeathNotif();
        }
        #region MatchManger Main Ticks

        private async Task MainMatchTick1000()
        {
            await Delay(1000);
            if (MatchStatus == MatchStatuses.IsRunnig)
            {
                if (IsInKhalse == false)
                {
                    if (Game.PlayerPed.IsAlive)
                    {
                        if (IsInMatchZone == false)
                        {
                            if (NotInMatchZoneTime == 0)
                            {
                                Game.PlayerPed.Kill();
                                Screen.ShowNotification("you have been killed!", true);
                                NotInMatchZoneTime = MaxTimeOutZone;
                                IsInMatchZone = true;
                            }

                            Screen.ShowSubtitle($"~g~Please get back In or you will be ~r~KILLED!~g~[~r~{NotInMatchZoneTime}~g~]", 1000);
                            Screen.Effects.Start(ScreenEffect.DeathFailOut, 500);
                            PlaySoundFrontend(-1, "Lose_1st", "GTAO_FM_Events_Soundset", false);
                            NotInMatchZoneTime--;
                        }
                    }

                    if (Math.Sqrt(Game.PlayerPed.Position.DistanceToSquared(NextMatch.MiddlePosition)) >= NextMatch.RangeNumber)
                        IsInMatchZone = false;
                    else
                    {
                        IsInMatchZone = true;
                        NotInMatchZoneTime = MaxTimeOutZone;
                    }
                }

                if (MatchTimer.CurrentTimer.Minutes == 0)
                {
                    if (MatchTimer.CurrentTimer.Second == 30)
                    {
                        StopMatchMusic();
                        Game.PlayMusic(ContDown_30);
                    }

                    if (MatchTimer.CurrentTimer.Second <= 10 && MatchTimer.CurrentTimer.Second != 0)
                    {
                        ShowTimerUi();
                        ShowMatchMessagesUi();
                        StartScreenEffect("RaceTurbo", 10000, false);
                        PlaySoundFrontend(-1, "10_SEC_WARNING", "HUD_MINI_GAME_SOUNDSET", false);
                    }
                }
            }

            if (MatchStatus == MatchStatuses.IsGoingToStart)
            {
                if (BeforMatchCountdown.CurrentTimer.Second == 5)
                    PlaySoundFrontend(-1, "5s_To_Event_Start_Countdown", "GTAO_FM_Events_Soundset", false);
            }
        }

        private async Task MainMatchTick()
        {
            if (MatchStatus == MatchStatuses.IsRunnig)
            {
                SetMatchOptionsThisFream();

                if (NextMatch != null)
                    if (NextMatch.Shape)
                        BaseUI.DrawMarker(28, NextMatch.MiddlePosition, new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(NextMatch.RangeNumber, NextMatch.RangeNumber, 30.0f), 255, 0, 0, 75);

                if (!Game.PlayerPed.IsVisible)
                    Game.PlayerPed.IsVisible = true;

                if (Game.GameTime < LastMessageTime)
                {
                    foreach (var item in MatchMessages)
                    {
                        DrawMatchMessage(item.Key, item.Value);
                    }

                    if (MatchMessages.Count != 0)
                        DrawRect(0.33f, 0.895f, 0.3f, 0.178f, 0, 0, 0, 100);
                }

                if (!IsInKhalse)
                {
                    if (!Screen.Hud.IsRadarVisible)
                        Screen.Hud.IsRadarVisible = true;

                    if (Game.IsControlJustPressed(0, Control.MultiplayerInfo))
                    {
                        ShowTimerUi();
                        ShowMatchMessagesUi();
                    }
                }
            }

            if (MatchStatusInTotal == MatchStatuses.IsGoingToStart)
            {
                if (!IsInKhalse)
                {
                    MatchGoingToStartTextTimer.Text = $"{BeforMatchCountdown.CurrentTimer.Minutes}:{BeforMatchCountdown.CurrentTimer.Second}";
                    MatchGoingToStartTextTimer.Draw(1);
                }
            }
        }
        #endregion 
        #endregion

        #region EventHandelers

        /// <summary>
        /// reciveing match data from server
        /// </summary>
        /// <param name="status"></param>
        private void ReciveMatchData(int status) => MatchStatusInTotal = (MatchStatuses)status;

        #region Controlling MatchMethods

        /// <summary>
        /// Main Function to start the match
        /// </summary>
        public void StartMatchNow()
        {
            MainDebug.WriteDebug("starting the match", MainDebug.Prefixes.info);
            MatchStatus = MatchStatuses.IsRunnig;
            SyncData.UpdatePlayerStatus(PlayerStatuses.IsInMatch);
            if (MusicInMatch)
                StartMatchMusic();

            HideLoading();

            Screen.Effects.Start(ScreenEffect.CamPushInTrevor, 6000);
            //and were back to player hopfuly 1s delay for switching in 
            Game.PlayerPed.IsPositionFrozen = false;
            Game.PlayerPed.IsVisible = true;

            MatchTimer.StartTimerNow(NextMatch.TimeInMiliSecond);
            OnMatchStarts?.Invoke();
        }

        /// <summary>
        /// prepare player for match 
        /// </summary>
        /// <param name="data"></param>
        private async void PreparePlayer(float x, float y, float z, float h)
        {
            if (MatchStatus != MatchStatuses.IsRunnig)
            {
                await StartKhalse();

                if (MenuAPI.MenuController.IsAnyMenuOpen())
                    MenuAPI.MenuController.CloseAllMenus();

                ShowLoading("Wating for players");
                MainDebug.WriteDebug("preparing player for match", MainDebug.Prefixes.info);

                if (!PrepareMusicEvent(ContDown_30))
                    PrepareMusicEvent(ContDown_30);


                MatchSpawnPoint _spawnpos = new MatchSpawnPoint(new Vector3(x, y, z), h);

                SetMatchOptions(true);
                await Delay(4000);
                await TeleportPlayerPedToThisPosition(_spawnpos.Position, _spawnpos.Heading, false, NextMatch.WorldWeather, NextMatch.WorldTime, true);
                MainDebug.WriteDebug("player teleported");

                if (NextMatch.PickupWeapons != null)
                {
                    foreach (MatchPickupSpawnPoint spawnPoint in NextMatch.PickupWeapons)
                    {
                        try
                        {
                            PickupType pickup = (PickupType)spawnPoint.PickupHash;

                            Pickup matchPickup = CreateWeaponPickup(pickup, spawnPoint.Position, spawnPoint.MaxAmmo);

                            if (matchPickup != null)
                                InMatchPickups.Add(matchPickup);
                            else
                                MainDebug.WriteDebug($"tried to create pickup for {spawnPoint.PickupHash} but hash was not valid or at least handeld", MainDebug.Prefixes.error);

                        }
                        catch (Exception e)
                        {
                            MainDebug.WriteDebug($"well we have a not handeled or wrong hash pickup type orginal error is : {e}", MainDebug.Prefixes.error);
                        }
                    }
                }

                RemoveAllPedWeapons(Game.PlayerPed.Handle, true);

                if (IsInKhalse)
                {
                    await GetOutOfKhalse();
                    await Delay(3000);
                    MainDebug.WriteDebug("player just Prepared!");
                }

                LoadPlayerLoadout(true);

                RenderScriptCams(false, false, 0, true, true);
                OnPlayerPrepared?.Invoke();

                Game.PlayerPed.IsInvincible = false;
            }
        }

        private void ShutDownMatch()
        {
            MatchStatus = MatchStatuses.HasEnded;
            SetMatchOptions(false);
        }

        private void StopMatchNow()
        {
            MatchTimer.StopTimerNow();
            MatchStatus = MatchStatuses.HasEnded;
            SetMatchOptions(false);
        }

        private void SetMatchOptions(bool toggle)
        {
            ToggleIpls(toggle);
            NetworkSetFriendlyFireOption(toggle);
            SetCanAttackFriendly(Game.PlayerPed.Handle, toggle, true);
            Screen.Hud.IsRadarVisible = true;

            if (!toggle)
            {
                RemoveAllPickups();
                StopMatchMusic();
            }
            else
                TimerUiStayOnScreen(false);
        }

        private void SetMatchOptionsThisFream()
        {
            SetTickHideNpc();
            SetRadarZoomLevelThisFrame(10);
            SetPlayerInvincible(PlayerId(), false);
            SetPlayerWantedLevel(PlayerId(), 0, false);
            SetPlayerWantedLevelNow(PlayerId(), false);
            //RestorePlayerStamina(PlayerId(), 1.0f);
        }
        #endregion

        private static void SetPlayerMaxHealth(int i)
        {
            SetEntityMaxHealth(PlayerId(), i);
            SetPedMaxHealth(PlayerId(), i);
        }

        private void SetTickHideNpc()
        {
            SetParkedVehicleDensityMultiplierThisFrame(0);
            SetVehicleDensityMultiplierThisFrame(0);
            SetParkedVehicleDensityMultiplierThisFrame(0);
            SetPedDensityMultiplierThisFrame(0);
            SetRandomVehicleDensityMultiplierThisFrame(0);
            SetScenarioPedDensityMultiplierThisFrame(0, 0);
            SetSomethingMultiplierThisFrame(false);
            SetSomeVehicleDensityMultiplierThisFrame(0);
            SetVehicleDensityMultiplierThisFrame(0);
        }

        #region Respawn func

        public static async void Respawn(Vector3 pos, float heding, bool loadout = true, bool isFreeze = true, bool isShowDisplay = true)
        {
            if (!Game.PlayerPed.IsAlive)
            {
                Game.PlayerPed.Weapons.RemoveAll();
                RequestCollisionAtCoord(pos.X, pos.Y, pos.Z);

                if (isShowDisplay)
                    await BaseUI.ShowLoadDisplay();

                if (isFreeze)
                    Game.PlayerPed.IsPositionFrozen = true;

                SetEntityCoords(GetPlayerPed(-1), pos.X, pos.Y, pos.Z, true, false, false, true);
                NetworkResurrectLocalPlayer(pos.X, pos.Y, pos.Z, heding, true, false);

                GameplayCamera.RelativeHeading = Game.PlayerPed.Heading;

                if (loadout)
                    LoadPlayerLoadout(true);

                ClearPedBloodDamage(GetPlayerPed(-1));
                StopAllScreenEffects();

                await Delay(2000);
                if (isFreeze)
                    Game.PlayerPed.IsPositionFrozen = false;

                int gameTime = GetGameTimer();
                while (!Game.PlayerPed.IsAlive)
                {
                    if (GetGameTimer() > gameTime + 3000)
                    {
                        break;
                    }

                    await Delay(0);
                }

                if (isShowDisplay)
                {
                    await BaseUI.HideLoadDisplay();

                    gameTime = GetGameTimer();
                    while (!Screen.Fading.IsFadedIn)
                    {
                        if (GetGameTimer() > gameTime + 3000)
                        {
                            break;
                        }

                        await Delay(100);
                    }
                }

                await StartParticleFxLocalyOnEntity(SyncData.PlayerInfo.Loadout.ParticleFx.AssestName, SyncData.PlayerInfo.Loadout.ParticleFx.OnPlayerSawnParticleName, Game.PlayerPed.Handle, 1.5f);
            }
        }
        #endregion
        #region TimerUi

        private async void ShowTimerUi()
        {
            if (TimerUiShowTimeEnd == -1)
            {
                TimerAlpha = 255;
                TimerUiShowTimeEnd = Game.GameTime + TimerUiTimePerEachKillNotif;

                while (Game.GameTime <= TimerUiShowTimeEnd)
                {
                    await Delay(1);
                }

                if (!TimerUiSatyOnScreen)
                    TimerAlpha = 100;
                TimerUiShowTimeEnd = -1;
            }
            else
            {
                TimerUiShowTimeEnd = Game.GameTime + TimerUiTimePerEachKillNotif;
            }
        }

        private void TimerUiStayOnScreen(bool toggel)
        {
            if (toggel)
            {
                TimerUiSatyOnScreen = true;
                TimerAlpha = 255;
            }
            else
            {
                TimerUiSatyOnScreen = false;
                TimerAlpha = 100;
            }
        }

        public void TimerUiShowKiller(string Name)
        {
            int index = -1;
            foreach (var item in OnGoingTimerUiKillerNames)
            {
                if (item.Value.Value + TimerUiTimePerEachKillNotif <= Game.GameTime)
                {
                    index = item.Key;
                    break;
                }
            }

            ShowTimerUi();
            if (index != -1)
            {
                OnGoingTimerUiKillerNames[index] = new KeyValuePair<string, int>(Name, Game.GameTime);
            }
            else
                OnGoingTimerUiKillerNames.Add(OnGoingTimerUiKillerNames.Count, new KeyValuePair<string, int>(Name, Game.GameTime));
        }

        public void TimerUiShowDeathNotif()
        {
            DeathNotifGameTime = Game.GameTime;
            ShowTimerUi();
        }

        private void ShowDeathNotifTimerUi()
        {
            if (DeathNotifGameTime != -1)
            {
                if (Game.GameTime <= DeathNotifGameTime + TimerUiTimePerEachKillNotif)
                {
                    int rectR = 175;
                    int rectG = 175;
                    int rectB = 175;

                    if (Game.GameTime <= DeathNotifGameTime + TimerUiTimePerEachKillNotif - 1000)
                    {
                        rectR = 0;
                        rectG = 0;
                        rectB = 0;
                    }

                    DrawRect(0.568f + 0.014f, 0.1f, 0.028f * 2, 0.041f, rectR, rectG, rectB, 225);
                    DrawText(7, 0.669f + 0.036f, 0.102f, 0.3f, 0.045f, 0.51f, "~r~+DEATH", 198, 46, 35, 255);
                }
            }
        }

        private void ShowKillerNameTimerUi(string Name, int index, int gameTime)
        {
            float offset = 0;
            float rectWith = 0.028f * 2;
            float rectHeight = 0.041f;
            float rectY = 0.12f + ((rectHeight + 0.002f) * index);
            float textX = 0.530f + 0.022f;
            float textHeight = 0.045f;
            float textY = 0.1f + (textHeight * index) - ((textHeight * index) / (45 - index));
            float killRectY = 0.1f + ((rectHeight + 0.002f) * index);
            int killRectR = 175;
            int killRectG = 175;
            int killRectB = 175;
            int rectR = 175;
            int rectG = 175;
            int rectB = 175;


            if (Name.Count() > 6)
            {
                int multiply = Name.Count() - 6;
                offset = (0.007f * multiply) - ((0.007f * multiply) / (multiply * multiply));

                rectWith += offset;
                textX -= offset;
            }

            if (Game.GameTime <= gameTime + TimerUiTimePerEachKillNotif)
            {
                if (Game.GameTime <= gameTime + TimerUiTimePerEachKillNotif / 2)
                {
                    if (Game.GameTime <= gameTime + (TimerUiTimePerEachKillNotif / 2) - 2000)
                    {
                        killRectR = 0;
                        killRectG = 0;
                        killRectB = 0;
                    }

                    DrawRect(0.401f + 0.022f, killRectY, 0.028f * 2, 0.041f, killRectR, killRectG, killRectB, 225);
                    DrawText(7, 0.532f + 0.025f, textY, 0.3f, textHeight, 0.51f, "~g~+KILL", 255, 255, 255, 255);
                }
                else
                {
                    if (Game.GameTime <= gameTime + TimerUiTimePerEachKillNotif - 1000)
                    {
                        rectR = 0;
                        rectG = 0;
                        rectB = 0;
                    }


                    DrawRct(0.429f + 0.022f, rectY, rectWith, rectHeight, rectR, rectB, rectG, 225);
                    DrawText(1, textX, textY, 0.3f, textHeight, 0.51f, "~r~" + Name.ToUpper(), 255, 255, 255, 255);
                }
            }
        }
        #endregion
        #region Create WeaponPickup stuff

        public static Pickup CreateWeaponPickup(PickupType type, Vector3 pos, int ammo = 200, bool blip = true, bool respawn = false, bool onGround = false)
        {
            Pickup temp;
            BlipSprite tempblip = BlipSprite.Blimp;
            int handel;

            switch (type)
            {
                //assault                
                case PickupType.WeaponMG:
                case PickupType.WeaponCombatMG:
                case PickupType.WeaponAdvancedRifle:
                case PickupType.WeaponAssaultRifle:
                case PickupType.WeaponBullpupRifle:
                case PickupType.WeaponCarbineRifle:
                case PickupType.WeaponSpecialCarbine:
                    tempblip = BlipSprite.AssaultRifle;
                    break;

                //handgun
                case PickupType.WeaponAPPistol:
                case PickupType.WeaponHeavyPistol:
                case PickupType.WeaponPistol:
                case PickupType.WeaponSNSPistol:
                case PickupType.WeaponCombatPistol:
                    tempblip = BlipSprite.Pistol;
                    break;

                //shotguns
                case PickupType.WeaponAssaultShotgun:
                case PickupType.WeaponPumpShotgun:
                case PickupType.WeaponSawnoffShotgun:
                    tempblip = BlipSprite.Shotgun;
                    break;

                //mellee
                case PickupType.WeaponBat:
                case PickupType.WeaponBottle:
                case PickupType.WeaponCrowbar:
                case PickupType.WeaponGolfclub:
                case PickupType.WeaponKnife:
                case PickupType.WeaponNightstick:
                case PickupType.WeaponPetrolCan:
                    tempblip = BlipSprite.Bat;
                    break;


                case PickupType.WeaponGrenadeLauncher:
                    tempblip = BlipSprite.GrenadeLauncher;
                    break;


                case PickupType.WeaponMinigun:
                    tempblip = BlipSprite.Minigun;
                    break;
                case PickupType.WeaponRPG:
                    tempblip = BlipSprite.RPG;
                    break;


                case PickupType.WeaponSmokeGrenade:
                case PickupType.WeaponGrenade:
                case PickupType.WeaponStickyBomb:
                    tempblip = BlipSprite.Grenade;
                    break;

                case PickupType.WeaponMolotov:
                    tempblip = BlipSprite.Molotov;
                    break;


                case PickupType.WeaponHeavySniper:
                case PickupType.WeaponSniperRifle:
                    tempblip = BlipSprite.Sniper;
                    break;

                case PickupType.WeaponMicroSMG:
                case PickupType.WeaponSMG:
                    tempblip = BlipSprite.SMG;
                    break;
                case PickupType.Armour:
                    tempblip = BlipSprite.Armor;
                    break;
                case PickupType.Health:
                    tempblip = BlipSprite.Health;
                    break;

            }

            int flag = onGround ? 8 : 512;


            handel = CreatePickupRotate((uint)type, pos.X, pos.Y, pos.Z, 0.0f, 0.0f, 0.0f, flag, ammo, 5, false, 0);
            int bliphandel;
            if (blip)
            {
                bliphandel = AddBlipForPickup(handel);
                SetBlipDisplay(bliphandel, 2);
                SetBlipSprite(bliphandel, (int)tempblip);
                SetBlipAsShortRange(bliphandel, true);
            }

            if (respawn)
                SetPickupRegenerationTime(handel, 10000);

            temp = new Pickup(handel);
            return temp;
        }

        private void RemovePickupByIndex(int index)
        {
            RemovePickup(InMatchPickups[index].Handle);
            InMatchPickups.RemoveAt(index);
        }

        private void RemoveAllPickups()
        {
            foreach (Pickup pickup in InMatchPickups)
            {
                RemovePickup(pickup.Handle);
            }
            InMatchPickups.Clear();
        }

        private int GetPickupIndex(int handel)
        {
            foreach (Pickup pickup in InMatchPickups)
            {
                if (pickup.Handle == handel)
                    return InMatchPickups.IndexOf(pickup);
            }
            return -1;
        }
        #endregion
        #region Loading loadoutMethods

        public static void LoadPlayerLoadout(bool useMatchWepsSetting = false)
        {
            PlayerLoadout loadout = SyncData.PlayerInfo.Loadout;

            if (useMatchWepsSetting)
            {
                if (NextMatch.StartWithWeapons != null)
                {
                    foreach (var wep in NextMatch.StartWithWeapons)
                    {
                        if (IsWeaponValid((uint)GetHashKey(wep.Key)))
                        {
                            GiveWeaponToPed(Game.PlayerPed.Handle, (uint)GetHashKey(wep.Key), wep.Value, false, false);
                        }
                    }
                }

                if (!NextMatch.LockOnWeapons)
                {
                    GiveLoadoutWeaponSlotsToPlayerPed(loadout);
                }
            }
            else
                GiveLoadoutWeaponSlotsToPlayerPed(loadout);

            Game.PlayerPed.DropsWeaponsOnDeath = true;
            SetPlayerMaxHealth(loadout.Skin.MaxHealth);
            Game.PlayerPed.Armor = loadout.Skin.MaxArmour;
            SetPlayerHealthRechargeMultiplier(Game.Player.Handle, loadout.Skin.Strength / 100f);
            Game.PlayerPed.Accuracy = loadout.Skin.Accuracy;
            PlayerController.SetPlayerStates(PlayerController.PlayerHudState.Stamina, loadout.Skin.Stamina);
            Game.PlayerPed.Weapons.Select(Game.PlayerPed.Weapons.BestWeapon);
        }

        private static void GiveLoadoutWeaponSlotsToPlayerPed(PlayerLoadout loadout)
        {
            GiveWeaponToPed(Game.PlayerPed.Handle, (uint)GetHashKey(loadout.PrimaryWeapon.WeaponName.ToUpper()), loadout.PrimaryWeapon.MaxAmount, false, false);
            GiveWeaponToPed(Game.PlayerPed.Handle, (uint)GetHashKey(loadout.SecondaryWeapon.WeaponName.ToUpper()), loadout.SecondaryWeapon.MaxAmount, false, false);
            GiveWeaponToPed(Game.PlayerPed.Handle, (uint)GetHashKey(loadout.Equipment.WeaponName.ToUpper()), loadout.Equipment.MaxAmount, false, false);

            GiveWeaponComponent((uint)GetHashKey(loadout.PrimaryWeapon.WeaponName.ToUpper()), loadout.PrimaryWeapon.WeaponComponent.ToArray());
            GiveWeaponComponent((uint)GetHashKey(loadout.SecondaryWeapon.WeaponName.ToUpper()), loadout.SecondaryWeapon.WeaponComponent.ToArray());
            GiveWeaponComponent((uint)GetHashKey(loadout.Equipment.WeaponName.ToUpper()), loadout.Equipment.WeaponComponent.ToArray());
            SetPedWeaponTintIndex(Game.PlayerPed.Handle, (uint)GetHashKey(loadout.PrimaryWeapon.WeaponName.ToUpper()), loadout.PrimaryWeapon.TintIndex);
            SetPedWeaponTintIndex(Game.PlayerPed.Handle, (uint)GetHashKey(loadout.SecondaryWeapon.WeaponName.ToUpper()), loadout.SecondaryWeapon.TintIndex);
            SetPedWeaponTintIndex(Game.PlayerPed.Handle, (uint)GetHashKey(loadout.Equipment.WeaponName.ToUpper()), loadout.Equipment.TintIndex);
        }

        private static void GiveWeaponComponent(uint wepHash, params string[] comp)
        {
            int ammo = GetAmmoInPedWeapon(Game.PlayerPed.Handle, wepHash);
            int clipAmmo = GetMaxAmmoInClip(Game.PlayerPed.Handle, wepHash, false);
            GetAmmoInClip(Game.PlayerPed.Handle, wepHash, ref clipAmmo);

            foreach (var item in comp)
            {
                GiveWeaponComponentToPed(Game.PlayerPed.Handle, wepHash, (uint)GetHashKey(item));
            }

            SetAmmoInClip(Game.PlayerPed.Handle, wepHash, clipAmmo);
            SetPedAmmo(Game.PlayerPed.Handle, wepHash, ammo);
        }
        #endregion
        #region Match Music Methods

        public void StartMatchMusic()
        {
            if (NextMatch != null)
            {
                if (NextMatch.MusicEventEventId != null && !string.IsNullOrEmpty(NextMatch.MusicEventEventId))
                {
                    if (!PrepareMusicEvent(NextMatch.MusicEventEventId))
                        PrepareMusicEvent(NextMatch.MusicEventEventId);

                    Game.PlayMusic(NextMatch.MusicEventEventId);
                }
                else
                {
                    int random = new Random().Next(MusicEventsList.Count - 1);
                    if (!PrepareMusicEvent(MusicEventsList[random]))
                        PrepareMusicEvent(MusicEventsList[random]);

                    Game.PlayMusic(MusicEventsList[random]);
                }
            }
        }

        public void StopMatchMusic()
        {
            if (NextMatch != null)
            {
                if (NextMatch.MusicEventEventId != null && !string.IsNullOrEmpty(NextMatch.MusicEventEventId))
                {
                    Game.PlayMusic(NextMatch.StopMusicCommandEventId);
                }
                else Game.PlayMusic(StopMusicCommand);
            }
        }
        #endregion

        private void DrawMatchMessage(int index, string Msg)
        {
            float yOffset = index * 0.03f;
            string msg = Msg;
            if (Msg.ToLower().Contains("radi"))
            {
                //for fun :)
                if (Game.GameTime / 1000 % 2 == 0)
                {
                    msg = Msg.ToLower().Replace("radi", "~o~radi~w~");
                }
                else if (Game.GameTime / 1000 % 3 == 0)
                {
                    msg = Msg.ToLower().Replace("radi", "~b~radi~w~");
                }
                else
                {
                    msg = Msg.ToLower().Replace("radi", "~g~radi~w~");
                }
            }

            DrawText(2, 0.335f, 0.83f + yOffset, 0.3f, 0.045f, 0.50f, msg, 255, 255, 255, 255);
        }

        private void ShowMatchMessage(string Msg)
        {
            LastMessageTime = Game.GameTime + 6000;
            if (MatchMessages.Count <= 4)
            {
                MatchMessages.Add(MsgsCount, Msg);
                MsgsCount++;
            }
            else
            {
                Dictionary<int, string> Msgs = new Dictionary<int, string>();
                int i = 0;
                foreach (var item in MatchMessages)
                {
                    if (item.Key != 0)
                    {
                        Msgs.Add(i, item.Value);
                        i++;
                    }
                }
                Msgs.Add(i, Msg);
                MatchMessages = Msgs;
            }
        }

        private void ShowMatchMessagesUi() => LastMessageTime = Game.GameTime + 6000;

        public static void ToggleIpls(bool toggle)
        {
            if (toggle)
            {
                if (NextMatch.IPLs != null)
                {
                    foreach (var item in NextMatch.IPLs)
                    {
                        if (!IsIplActive(item))
                        {
                            RequestIpl(item);
                        }
                    }

                    if (NextMatch.IPLs.Count != 0)
                    {
                        if (NextMatch.InteriorPropNames.Count != 0)
                        {
                            int id = GetInteriorAtCoords(NextMatch.SpawnPoints[0].Position.X, NextMatch.SpawnPoints[0].Position.Y, NextMatch.SpawnPoints[0].Position.Z);

                            if (IsValidInterior(id))
                            {
                                foreach (var item in NextMatch.InteriorPropNames)
                                {
                                    if (!IsInteriorPropEnabled(id, item))
                                    {
                                        EnableInteriorProp(id, item);
                                    }
                                }

                                RefreshInterior(id);
                            }
                            else
                            {
                                MainDebug.WriteDebug("Cant find interior id from first spawn point is that realy inside?", MainDebug.Prefixes.error);
                            }
                        }
                    }
                }
            }
            else
            {
                if (NextMatch.IPLs != null)
                {
                    if (NextMatch.IPLs.Count != 0)
                    {
                        if (NextMatch.InteriorPropNames.Count != 0)
                        {
                            int id = GetInteriorAtCoords(NextMatch.SpawnPoints[0].Position.X, NextMatch.SpawnPoints[0].Position.Y, NextMatch.SpawnPoints[0].Position.Z);

                            if (IsValidInterior(id))
                            {
                                foreach (var item in NextMatch.InteriorPropNames)
                                {
                                    if (IsInteriorPropEnabled(id, item))
                                    {
                                        DisableInteriorProp(id, item);
                                    }
                                }
                            }
                            else
                            {
                                MainDebug.WriteDebug("Cant find interior id from first spawn point is that realy inside?", MainDebug.Prefixes.error);
                            }
                        }
                    }

                    foreach (var item in NextMatch.IPLs)
                    {
                        if (IsIplActive(item))
                        {
                            RemoveIpl(item);
                        }
                    }
                }
            }
        }
        #endregion



        #region player blips tasks
        private async Task PlayerBlipsControl()
        {
            if (DecorIsRegisteredAsType("irvdm_player_blip_sprite_id", 3))
            {
                int sprite = 6;
                if (Game.PlayerPed.IsAlive)
                {
                    if (IsPedInAnyVehicle(Game.PlayerPed.Handle, false))
                    {
                        Vehicle veh = GetVehicle();
                        if (veh != null && veh.Exists())
                        {
                            sprite = vMenuClient.BlipInfo.GetBlipSpriteForVehicle(veh.Handle);
                        }
                    }
                }
                else
                {
                    sprite = 274;
                }

                try
                {
                    DecorSetInt(Game.PlayerPed.Handle, "irvdm_player_blip_sprite_id", sprite);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Error Location: {e.StackTrace}\nError info: {e.Message.ToString()}");
                    await Delay(1000);
                }

                foreach (Player p in Players)
                {
                    // continue only if this player is valid.
                    if (p != null && NetworkIsPlayerActive(p.Handle) && p.Character != null && p.Character.Exists())
                    {

                        //    
                        //else
                        //    SetBlipDisplay(blip, 3);

                        // match is running 
                        if (MatchStatus == MatchStatuses.IsRunnig)
                        {
                            if (p != Game.Player)
                            {
                                int ped = p.Character.Handle;
                                int blip = GetBlipFromEntity(ped);

                                // if blip id is invalid.
                                if (blip < 1)
                                {
                                    blip = AddBlipForEntity(ped);
                                }
                                // only manage the blip for this player if the player is nearby                                
                                if (p.Character.Position.DistanceToSquared2D(Game.PlayerPed.Position) < 250000)
                                {
                                    //IsEntityOnScreen()

                                    if (HasEntityClearLosToEntity(Game.PlayerPed.Handle, ped, 17) || CanPedHearPlayer(p.Handle, Game.PlayerPed.Handle))
                                    {
                                        // (re)set the blip color in case something changed it.
                                        SetBlipColour(blip, 2);

                                        // if the decorator exists on this player, use the decorator value to determine what the blip sprite should be.
                                        if (DecorExistOn(p.Character.Handle, "irvdm_player_blip_sprite_id"))
                                        {
                                            int decorSprite = DecorGetInt(p.Character.Handle, "irvdm_player_blip_sprite_id");
                                            // set the sprite according to the decorator value.
                                            SetBlipSprite(blip, decorSprite);

                                            // show heading on blip only if the player is on foot (blip sprite 1)
                                            ShowHeadingIndicatorOnBlip(blip, decorSprite == 1);

                                            // set the blip rotation if the player is not in a helicopter (sprite 422).
                                            if (decorSprite != 422)
                                            {
                                                SetBlipRotation(blip, (int)GetEntityHeading(ped));
                                            }
                                            SetBlipColour(blip, 1);
                                            SetBlipScale(blip, 0.6f);
                                        }
                                        else // backup method for when the decorator value is not found.
                                        {
                                            // set the blip sprite using the backup method in case decorators failed.
                                            SetCorrectBlipSprite(ped, blip);
                                            SetBlipScale(blip, 0.6f);
                                            SetBlipColour(blip, 1);
                                            SetBlipRotation(blip, (int)GetEntityHeading(ped));

                                            // only show the heading indicator if the player is NOT in a vehicle.
                                            //if (!IsPedInAnyVehicle(ped, false))
                                            //{
                                            //    ShowHeadingIndicatorOnBlip(blip, true);
                                            //}
                                            //else
                                            //{
                                            //    ShowHeadingIndicatorOnBlip(blip, false);

                                            //    // If the player is not in a helicopter, set the blip rotation.
                                            //    if (!p.Character.IsInHeli)
                                            //    {
                                            //        SetBlipRotation(blip, (int)GetEntityHeading(ped));
                                            //    }
                                            //}
                                        }

                                        // set the player name.
                                        SetBlipNameToPlayerName(blip, p.Handle);

                                        // thanks lambda menu for hiding this great feature in their source code!
                                        // sets the blip category to 7, which makes the blips group under "Other Players:"
                                        SetBlipCategory(blip, 7);

                                        //N_0x75a16c3da34f1245(blip, false); // unknown

                                        // display on minimap and main map.
                                        SetBlipDisplay(blip, 6);
                                    }
                                    else
                                    {
                                        // dont show any where if our ped dont have clear los to ped
                                        await Delay(1000);
                                        SetBlipDisplay(blip, 0);
                                    }
                                }
                                else
                                {
                                    // dont show any where if our ped dont have clear los to ped
                                    SetBlipDisplay(blip, 0);
                                }
                            }
                        }
                        else // not in match
                        {
                            if (!(p.Character.AttachedBlip == null || !p.Character.AttachedBlip.Exists()))
                            {
                                p.Character.AttachedBlip.Delete(); // remove player blip if it exists.
                            }
                        }
                    }

                }
            }


            else // decorator does not exist.
            {
                try
                {
                    DecorRegister("irvdm_player_blip_sprite_id", 3);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Error Location: {e.StackTrace}\nError info: {e.Message.ToString()}");
                    await Delay(1000);
                }
                while (!DecorIsRegisteredAsType("irvdm_player_blip_sprite_id", 3))
                {
                    await Delay(0);
                }
            }
        }

        #endregion
    }
}

