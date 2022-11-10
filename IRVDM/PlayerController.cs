using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using CitizenFX.Core.UI;
using NativeUI;
using IRVDMShared;

namespace IRVDM
{
    public class PlayerController : BaseScript
    {
        #region Varibles
        private static bool _spawnLock = false;
        private bool FirstSpawn = false;
        private bool IsDead = false;
        private bool hasBeenDead = false;
        private int KilledAt = -1;
        private int KilledAtGameTime = -1;
        public static int ReSpawnPlayerTime { get; private set; } = 10000;
        public static int ShowWastedTime { get; private set; } = 3000;

        private readonly BarTimerBar RespwnTimerBar = new BarTimerBar("");

        #endregion

        #region MainDefualt constractor
        public PlayerController()
        {
            Tick += PlayerController_Tick;
            Tick += SpawnManagerTick;
            #region FiveMEvents 
            RespwnTimerBar.ForegroundColor = UnknownColors.Purple;
            //EventHandlers["onClientResourceStart"] += new Action<string>(async (n) =>
            //{
            //    if (GetCurrentResourceName() == n)
            //    {
            //        if (!FirstSpawn)
            //        {
            //            await Delay(10000);
            //            ShutDownLoading();
            //            CommonFunctions.ShowLoading("Geting things ready");
            //            await Delay(3000);
            //            Game.PlayerPed.IsVisible = false;
            //            TriggerServerEvent("IRV:SV:clientJustJoiend");
            //            //Exports["spawnmanager"].setAutoSpawn(false);
            //            //Exports["spawnmanager"].forceRespawn();
            //            FirstSpawn = true;
            //        }
            //    }
            //});

            EventHandlers["onClientResourceStart"] += new Action<string>(async (n) =>
            {
                if (GetCurrentResourceName() == n)
                {
                    try
                    {
                        Exports["spawnmanager"].setAutoSpawn(true);
                    }
                    catch (Exception s)
                    { 
                        MainDebug.WriteDebug("temp");
                    }
                }
            });

            EventHandlers["playerSpawned"] += new Action(async () =>
            {
                if (!FirstSpawn)
                {
                    //await Delay(10000);
                    //ShutDownLoading();
                    await CommonFunctions.StartKhalse();
                    await Delay(1000);
                    Screen.LoadingPrompt.Show("Getting things ready");
                    await Delay(3000);
                    Game.PlayerPed.IsVisible = false;
                    TriggerServerEvent("IRV:SV:clientJustJoiend");
                    FirstSpawn = true;
                    Exports["spawnmanager"].setAutoSpawn(false);
                }
            });

            #endregion
            #region IRVDMEvents
            #region Match Events
            EventHandlers["IRV:PL:preparePlayer"] += new Action<float, float, float, float>((x, y, z, h) =>
            {
                TriggerEvent("IRV:Menu:shutdown");
                TriggerEvent("IRV:Match:preparePlayer", x, y, z, h);
            });

            EventHandlers["IRV:PL:startTheMatch"] += new Action(() => TriggerEvent("IRV:Match:startMatchNow"));

            EventHandlers["IRV:PL:reciveEndMatchCommand"] += new Action(async () =>
            {
                TriggerEvent("IRV:Match:shutDownTheMatch");
                await CommonFunctions.StartKhalse();
                BaseUI.ShowLeaderBoard(8000, true, false, false);
                await Delay(9000);
                TriggerEvent("IRV:Menu:start", true);
            });

            EventHandlers["IRV:Pl:reciveFailCommand"] += new Action(() =>
            {
                TriggerEvent("IRV:Match:stopMatchNow");
                TriggerEvent("IRV:Menu:start", true);
            });
            #endregion

            EventHandlers["IRV:Pl:SendToMainMenu"] += new Action(() =>
            {
                if (SyncData.PlayerInfo.PlayerStatus == PlayerStatuses.IsInSpectorMode)
                    SpectorMode.Stop();

                MainDebug.WriteDebug("send to main menu recived from server forcing player to menu", MainDebug.Prefixes.info);

                TriggerEvent("IRV:Menu:start", true);
            });

            EventHandlers["IRV:Pl:SpawnPlayer"] += new Action<float, float, float, float>(Spawn);

            EventHandlers["IRV:Pl:StartParticleOnEntity"] += new Action<string, string, int>(async (a, f, p) => await CommonFunctions.StartParticleFxLocalyOnEntity(a, f, GetPlayerPed(GetPlayerFromServerId(p))));

            #endregion
        }

        public async Task SpawnManagerTick()
        {

            //if (Game.IsControlJustPressed(0, Control.Aim))
            //{
            //               Exports["spawnmanager"].setAutoSpawn(true);
            //              Exports["spawnmanager"].forceRespawn();
            //}

            if (NetworkIsPlayerActive(Game.Player.Handle))
            {
                if (!CommonFunctions.IsInKhalse && !CommonFunctions.IsInTeleportState)
                {
                    if (SyncData.PlayerInfo.PlayerStatus == PlayerStatuses.IsInMatch)
                    {
                        if (IsPedFatallyInjured(Game.PlayerPed.Handle) && !IsDead)
                        {
                            IsDead = true;

                            if (KilledAt == -1)
                                KilledAt = Game.GameTime;

                            GameplayCamera.Shake(CameraShake.DeathFail, 2f);
                            PlaySoundFrontend(-1, "Bed", "WastedSounds", true);

                            KilledAtGameTime = Game.GameTime;

                            uint killerweapon = 0;
                            int killer = NetworkGetEntityKillerOfPlayer(Game.Player.Handle, ref killerweapon);

                            int killerentitytype = GetEntityType(killer);

                            int killertype = -1;

                            string killervehiclename = "";
                            //int killervehicleseat = 0;

                            if (killerentitytype == 1)
                            {
                                killertype = GetPedType(killer);

                                if (IsPedInAnyVehicle(killer, false))
                                {

                                    killervehiclename = GetDisplayNameFromVehicleModel((uint)GetEntityModel(GetVehiclePedIsUsing(killer)));
                                    //killervehicleseat = getpedve(killer);
                                }
                            }

                            int killerid = -2;
                            string killerpl = "";
                            int killerId = 0;
                            foreach (var item in Players)
                            {
                                if (NetworkIsPlayerActive(item.Handle))
                                {
                                    if (GetPlayerPed(item.Handle) == killer)
                                    {
                                        killerid = item.ServerId;
                                        killerpl = item.Name;
                                        killerId = item.ServerId;
                                    }
                                }
                            }

                            int ped = Game.PlayerPed.Handle;


                            //if (killer != ped && killerid != -2 && NetworkIsPlayerActive(killerid)) { killerid = GetPlayerServerId(killerid); }
                            //else killerid = -1; //?? set to this ? ***invalid***

                            if (killer == ped || killer == -1)
                            {
                                await Delay(1500);
                                Screen.Effects.Start(ScreenEffect.DeathFailMpIn, looped: true);
                                BigMessageThread.MessageInstance.ShowMpWastedMessage("~g~YOU HAVE BEEN ~r~DIED", "", 2000);
                                hasBeenDead = true;
                            }
                            else
                            {
                                BigMessageThread.MessageInstance.ShowMpWastedMessage("~g~YOU HAVE BEEN ~r~KILLED", $"Killed by ~r~{killerpl}");

                                hasBeenDead = true;

                                await Delay(500);
                                NetworkSetInSpectatorMode(true, GetPlayerPed(GetPlayerFromServerId(killerId)));
                            }
                        }
                        else if (!IsPedFatallyInjured(Game.PlayerPed.Handle))
                        {
                            IsDead = false;
                            KilledAt = -1;
                        }

                        //check if the player has to respawn in order to trigger an event
                        if (!hasBeenDead && KilledAt != -1 && KilledAt > 0)
                        {
                            //wasted
                            MainDebug.WriteDebug("(((Player wasted)))");
                            hasBeenDead = true;
                        }
                        else if (hasBeenDead && KilledAt != -1 && KilledAt <= 0)
                        {
                            hasBeenDead = false;
                        }


                        if (!Game.PlayerPed.IsAlive)
                        {
                            if (Game.GameTime < (KilledAtGameTime + ReSpawnPlayerTime + ShowWastedTime))
                            {
                                RespwnTimerBar.Draw(1);

                                float diffrence = Game.GameTime - (KilledAtGameTime + ReSpawnPlayerTime + ShowWastedTime);
                                RespwnTimerBar.Percentage = (diffrence / (ReSpawnPlayerTime + ShowWastedTime)) * -1;

                                if (Game.IsControlJustPressed(0, Control.Attack))
                                {
                                    KilledAtGameTime -= 500;
                                    PlaySoundFrontend(-1, "10_SEC_WARNING", "HUD_MINI_GAME_SOUNDSET", false);
                                }
                            }
                            else
                            {
                                await Delay(500);
                                var sp = GetBestMatchSpawnPoint(MatchManger.NextMatch.SpawnPoints);

                                MatchManger.Respawn(sp.Position, sp.Heading, true, false, true);
                                if (NetworkIsInSpectatorMode())
                                {
                                    NetworkSetInSpectatorMode(false, Game.PlayerPed.Handle);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static async Task SetSkin(string modelName)
        {
            uint spawnModel = (uint)GetHashKey(modelName);
            if (IsModelInCdimage(spawnModel))
            {
                RequestModel(spawnModel);
                while (!HasModelLoaded(spawnModel))
                {
                    await Delay(1);
                }

                SetPlayerModel(PlayerId(), spawnModel);
                await Delay(500);
                SetModelAsNoLongerNeeded(spawnModel);
            }
        }

        public static async Task SetSkin(string modelName, PedInfo info)
        {
            uint spawnModel = (uint)GetHashKey(modelName);
            if (IsModelInCdimage(spawnModel))
            {
                RequestModel(spawnModel);
                while (!HasModelLoaded(spawnModel))
                {
                    await Delay(1);
                }

                SetPlayerModel(PlayerId(), spawnModel);

                await Delay(500);

                SetPedDefaultComponentVariation(Game.PlayerPed.Handle);
                ClearAllPedProps(Game.PlayerPed.Handle);
                ClearPedDecorations(Game.PlayerPed.Handle);
                ClearPedFacialDecorations(Game.PlayerPed.Handle);

                var ped = Game.PlayerPed.Handle;
                for (var drawable = 0; drawable < 21; drawable++)
                {
                    SetPedComponentVariation(ped, drawable, info.DrawableVariations[drawable],
                        info.DrawableVariationTextures[drawable], 1);
                }

                for (var i = 0; i < 21; i++)
                {
                    int prop = info.Props[i];
                    int propTexture = info.PropTextures[i];
                    if (prop == -1 || propTexture == -1)
                    {
                        ClearPedProp(ped, i);
                    }
                    else
                    {
                        SetPedPropIndex(ped, i, prop, propTexture, true);
                    }
                }

                SetModelAsNoLongerNeeded(spawnModel);
            }
        }

        /// <summary>
        /// spawn player for the first time
        /// </summary>
        /// <param name="skin"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="heading"></param>
        public async void Spawn(float x, float y, float z, float heading)
        {
            if (_spawnLock)
                return;

            _spawnLock = true;

            //await BaseUI.ShowLoadDisplay();
            CommonFunctions.SetLoadingScreenSound(false);
            await SyncData.LoadPlayerLoadOut(true);
            List<Unlockable> addons = new List<Unlockable>();
            foreach (var unlockble in Unlockables.Data.GetUnlockables)
            {
                if (unlockble.IsAddon == true)
                {
                    addons.Add(unlockble);
                }
            }


            if (addons.Count != 0)
            {
                Screen.LoadingPrompt.Show($"Downloading Items 0/{addons.Count}");

                foreach (var item in addons)
                {
                    Model model = new Model(item.ModelName);
                    if (model.IsValid && model.IsInCdImage)
                    {
                        model.Request();

                        while (!model.IsLoaded)
                        {
                            await Delay(100);
                        }

                        model.MarkAsNoLongerNeeded();
                    }
                    else
                    {
                        Debug.WriteLine($"Model {item.ModelName} was not valid or not in cdimage!");
                    }
                    Screen.LoadingPrompt.Show($"Downloading Items {addons.IndexOf(item) + 1}/{addons.Count}");
                }
            }

            Screen.LoadingPrompt.Show($"Getting player ready");
            RequestCollisionAtCoord(x, y, z);

            var ped = GetPlayerPed(-1);
            //NetworkResurrectLocalPlayer(x, y, z, heading, true, true);
            SetEntityCoords(ped, x, y, z, false, false, false, true);
            SetEntityCoordsNoOffset(ped, x, y, z, false, false, false);
            ClearPedTasksImmediately(ped);
            RemoveAllPedWeapons(ped, false);
            ClearPlayerWantedLevel(PlayerId());

            int gameTime = GetGameTimer();
            while (!HasCollisionLoadedAroundEntity(ped) && GetGameTimer() < gameTime + 3000)
                await Delay(1);

            SetHudColour(157, 125, 50, 50, 110);
            Game.PlayerPed.IsPositionFrozen = false;
            Game.PlayerPed.IsVisible = true;
            CommonFunctions.SetLoadingScreenSound(true);
            //await CommonFunctions.StartKhalse();
            TriggerEvent("IRV:Pl:SendToMainMenu");
            _spawnLock = false;
        }

        private async Task PlayerController_Tick()
        {
            if (!CommonFunctions.IsInKhalse)
            {
                if (SyncData.PlayerInfo.PlayerStatus == PlayerStatuses.IsInMainMenu)
                {
                    //players need to dont have collsion togeter so we call this in loop
                    //not sure how this functions work but anyway were happy for now 
                    SetAllPlayerCollision(true);
                }
                else if (SyncData.PlayerInfo.PlayerStatus == PlayerStatuses.IsInMatch)
                {
                    //some time player getting invincible ?
                    if (Game.PlayerPed.IsInvincible)
                        Game.PlayerPed.IsInvincible = false;
                }
            }

            if (Game.IsControlPressed(0, Control.Duck))
            {
                BaseUI.ShowLeaderBoard(500);
            }

            if (SyncData.PlayerInfo.PlayerStatus == PlayerStatuses.IsInMainMenu)
            {
                if (Screen.Hud.IsRadarVisible)
                    Screen.Hud.IsRadarVisible = false;
            }

            if (CommonFunctions.IsInKhalse)
            {
                Game.DisableAllControlsThisFrame(0);
                Game.DisableAllControlsThisFrame(2);
            }
        }

        /// <summary>
        /// somehow by setting this functions to true IT WILL destroy collision (Must call in loop)
        /// </summary>
        /// <param name="IsDisable"></param>
        public void SetAllPlayerCollision(bool IsDisable)
        {
            foreach (var player in Players)
            {
                if (player.Handle != Game.Player.Handle)
                {
                    SetEntityCollision_2(GetPlayerPed(player.Handle), IsDisable, IsDisable);
                    SetEntityLoadCollisionFlag(GetPlayerPed(player.Handle), IsDisable);
                    SetEntityNoCollisionEntity(Game.PlayerPed.Handle, GetPlayerPed(player.Handle), IsDisable);
                    //SetEntityCollision(GetPlayerPed(player.Handle), toggle, toggle); no need for this
                }
            }
        }

        public enum PlayerHudState
        {
            Stamina,
            Shooting,
            Strength,
            StealthAblility,
            LungCapacity,
        }

        internal static void SetPlayerStates(PlayerHudState hudState, int amount)
        {
            switch (hudState)
            {
                case PlayerHudState.Stamina:
                    StatSetInt((uint)GetHashKey("MP0_STAMINA"), amount, true);
                    break;
                case PlayerHudState.Shooting:
                    StatSetInt((uint)GetHashKey("MP0_SHOOTING_ABILITY"), amount, true);        // Shooting
                    break;
                case PlayerHudState.Strength:
                    StatSetInt((uint)GetHashKey("MP0_STRENGTH"), amount, true);                // Strength
                    break;
                case PlayerHudState.StealthAblility:
                    StatSetInt((uint)GetHashKey("MP0_STEALTH_ABILITY"), amount, true);         // Stealth
                    break;
                case PlayerHudState.LungCapacity:
                    StatSetInt((uint)GetHashKey("MP0_LUNG_CAPACITY"), amount, true);           // Lung Capacity
                    break;
                default:
                    break;
            }
        }

        public async void ShutDownLoading()
        {
            while (GetIsLoadingScreenActive())
            {
                ShutdownLoadingScreen();
                ShutdownLoadingScreenNui();
                await Delay(400);
            }
        }

        public MatchSpawnPoint GetBestMatchSpawnPoint(List<MatchSpawnPoint> spawnPoints)
        {
            MatchSpawnPoint spawnPoint = spawnPoints[new Random().Next(0, spawnPoints.Count - 1)];

            foreach (var player in Players)
            {
                if (player.Character.IsAlive && NetworkIsPlayerActive(player.Handle))
                {
                    //TODO need more shit and i think its not working ?
                    if (player.Character.Position.DistanceToSquared(spawnPoint.Position) < 8)
                    {
                        MainDebug.WriteDebug("this spawn point is taken moving to next one!");
                        return GetBestMatchSpawnPoint(spawnPoints);
                    }
                }
            }

            return spawnPoint;
        }

        #endregion
    }
}