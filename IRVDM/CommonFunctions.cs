using CitizenFX.Core;
using CitizenFX.Core.UI;
using IRVDMShared;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;

namespace IRVDM
{
    internal class CommonFunctions
    {
        #region varibles

        public static bool IsInTeleportState { get; private set; } = false;
        public static bool IsInSwitchState
        {
            get
            {
                if (IsInTeleportState || GetPlayerSwitchState() != 12)
                    return true;
                else
                    return false;
            }
        }

        public enum PlayerSwitchState
        {
            SwitchedOut = 5,
            IsSwitchingOut = 3,
            IsSwitchedIn = 12,
            IsGoingToSwitchIn = 7,
            IsSwitchingIn = 8
        }

        public static bool IsInKhalse { get; private set; } = false;

        #endregion
        #region some misc functions copied from base script
        /// <summary>
        /// Copy of <see cref="BaseScript.TriggerServerEvent(string, object[])"/>
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="args"></param>
        public static void TriggerServerEvent(string eventName, params object[] args)
        {
            BaseScript.TriggerServerEvent(eventName, args);
        }

        /// <summary>
        /// Copy of <see cref="BaseScript.TriggerEvent(string, object[])"/>
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="args"></param>
        public static void TriggerEvent(string eventName, params object[] args)
        {
            BaseScript.TriggerEvent(eventName, args);
        }

        /// <summary>
        /// Copy of <see cref="BaseScript.Delay(int)"/>
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static async Task Delay(int time)
        {
            await BaseScript.Delay(time);
        }
        #endregion
        #region Usefull functions

        public static void DisableControllsForMenuThisFrame()
        {
            Game.DisableControlThisFrame(0, Control.MoveDown);
            Game.DisableControlThisFrame(0, Control.MoveLeft);
            Game.DisableControlThisFrame(0, Control.MoveRight);
            Game.DisableControlThisFrame(0, Control.MoveUp);
            Game.DisableControlThisFrame(0, Control.Cover);
            Game.DisableControlThisFrame(0, Control.MoveUpDown);
            Game.DisableControlThisFrame(0, Control.MoveRightOnly);
            Game.DisableControlThisFrame(0, Control.MoveLeftOnly);
            Game.DisableControlThisFrame(0, Control.MoveUpOnly);
            Game.DisableControlThisFrame(0, Control.MoveDownOnly);
            Game.DisableControlThisFrame(0, Control.MoveLeftRight);
            Game.DisableControlThisFrame(0, Control.Duck);
            Game.DisableControlThisFrame(0, Control.Aim);
            Game.DisableControlThisFrame(0, Control.NextCamera);
            Game.DisableControlThisFrame(0, Control.SelectWeapon);
            //Game.DisableAllControlsThisFrame(2);
        }

        /// <summary></summary>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <param name="heading"></param>
        /// <param name="style"> Using this will add  ~3000ms delay before completing task </param>
        /// <param name="weather"> change weather during </param>
        /// <param name="time"> change time during </param>
        /// <returns></returns>
        public static async Task TeleportPlayerPedToThisPosition(Vector3 pos, float heading, bool style, string weather = null, TimeFormat time = null, bool freez = false)
        {
            IsInTeleportState = true;
            DisplayRadar(false);
            if (style)
            {
                SwitchOutPlayer(PlayerPedId(), 0, 1);
                await Delay(3000);
            }

            Game.PlayerPed.Task.ClearAll();
            //Game.PlayerPed.IsVisible = false;
            //using vMenu TeleportFunctions hopfully we can teleport more safely !
            RequestCollisionAtCoord(pos.X, pos.Y, pos.Z);
            NewLoadSceneStart(pos.X, pos.Y, pos.Z, pos.X, pos.Y, pos.Z, 50f, 0);

            if (weather != null)
                SetWeather(weather);

            if (time != null)
                NetworkOverrideClockTime(time.Hour, time.Minutes, time.MilliSecond);

            // Timer to make sure things don't get out of hand (player having to wait forever to get teleported if something fails).
            int tempTimer = GetGameTimer();

            // Wait for the new scene to be loaded.
            while (IsNetworkLoadingScene())
            {
                // If this takes longer than 1 second, just abort. It's not worth waiting that long.
                if (GetGameTimer() - tempTimer > 2000)
                {
                    break;
                }

                await Delay(0);
            }


            ClearAreaOfPeds(pos.X, pos.Y, pos.Z, 100f, 1);
            SetEntityCoordsNoOffset(Game.PlayerPed.Handle, pos.X, pos.Y, pos.Z, false, false, false);
            //Game.PlayerPed.Heading = heading;
            //Game.PlayerPed.Rotation = rot;
            Game.PlayerPed.IsPositionFrozen = true;
            await Delay(500);

            // Reset the timer.
            tempTimer = GetGameTimer();

            // Wait for the collision to be loaded around the entity in this new location.
            while (HasCollisionLoadedAroundEntity(Game.PlayerPed.Handle) == false)
            {
                // If this takes too long, then just abort, it's not worth waiting that long since we haven't found the real ground coord yet anyway.
                if (GetGameTimer() - tempTimer > 3000)
                {
                    break;
                }
                await Delay(0);
            }

            if (style)
            {
                SwitchInPlayer(PlayerPedId());
                while (GetPlayerSwitchState() != 12)
                {
                    await Delay(10);
                }
            }

            if (!freez)
                Game.PlayerPed.IsPositionFrozen = false;
            else
                Game.PlayerPed.IsPositionFrozen = true;

            SetEntityHeading(Game.PlayerPed.Handle, heading);
            DisplayRadar(true);
            IsInTeleportState = false;
        }

        public static void DrawText(int f, float x, float y, float width, float height, float scale, string text, int r, int g, int b, int a)
        {
            SetTextFont(f);
            SetTextScale(scale, scale);
            SetTextColour(r, g, b, a);
            SetTextDropShadow();
            SetTextEdge(1, 0, 0, 0, 255);
            SetTextDropShadow();
            SetTextOutline();
            BeginTextCommandDisplayText("STRING");
            AddTextComponentSubstringPlayerName(text);
            EndTextCommandDisplayText(x - width / 2f, y - height / 2f + 0.005f);
        }

        public static void DrawRct(float x, float y, float width, float height, int r, int g, int b, int a) => DrawRect(x - width / 2f, y - height / 2f, width, height, r, g, b, a);

        /// <summary>
        /// Get a user input text string.
        /// </summary>
        /// <param name="windowTitle"></param>
        /// <param name="defaultText"></param>
        /// <param name="maxInputLength"></param>
        /// <returns></returns>
        public static async Task<string> GetUserInput(string windowTitle, string defaultText, int maxInputLength)
        {
            // Create the window title string.
            var spacer = "\t";
            AddTextEntry($"{GetCurrentResourceName().ToUpper()}_WINDOW_TITLE", $"{windowTitle ?? "Enter"}:{spacer}(MAX {maxInputLength.ToString()} Characters)");

            // Display the input box.
            DisplayOnscreenKeyboard(1, $"{GetCurrentResourceName().ToUpper()}_WINDOW_TITLE", "", defaultText ?? "", "", "", "", maxInputLength);
            await Delay(0);
            // Wait for a result.
            while (true)
            {
                int keyboardStatus = UpdateOnscreenKeyboard();

                switch (keyboardStatus)
                {
                    case 3: // not displaying input field anymore somehow
                    case 2: // cancelled
                        return null;
                    case 1: // finished editing
                        return GetOnscreenKeyboardResult();
                    default:
                        await Delay(0);
                        break;
                }
            }
        }

        public static Camera CreateCameraAtThisPos(Vector3 pos, Vector3 rot, string camera = null)
        {
            string _cam = "DEFAULT_SCRIPTED_CAMERA";

            if (camera != null)
            {
                _cam = camera;
            }

            Camera temp = new Camera(CreateCam(_cam, true))
            {
                //temp.IsActive = true;
                Position = pos,
                Rotation = rot
            };

            return temp;
        }

        public static async Task RenderScriptCamera(bool render, bool ease, int easetime)
        {
            RenderScriptCams(render, ease, easetime, true, true);
            await Delay(easetime);
        }

        public static void SetLoadingScreenSound(bool toggle)
        {
            if (!toggle)
                StartAudioScene("MP_LEADERBOARD_SCENE");
            else
                StopAudioScene("MP_LEADERBOARD_SCENE");
        }

        /// <summary>
        /// copy of <see cref="SwitchOutPlayer(int, int, int)"/> with default parameters
        /// </summary>
        public static void SwitchPlayerOut() => SwitchOutPlayer(PlayerPedId(), 0, 1);

        /// <summary>
        /// copy of <see cref="SwitchInPlayer(int)"/> with default parameters
        /// </summary>
        public static void SwitchPlayerIn() => SwitchInPlayer(PlayerPedId());

        #region KhalseState Functions

        public static async Task StartKhalse()
        {
            if (IsInKhalse == false)
            {
                await SetKhalseOptions(true);
            }
        }

        public static async Task GetOutOfKhalse()
        {
            await SetKhalseOptions(false);
        }

        public static async Task SetKhalseOptions(bool toggle)
        {

            DisplayRadar(!toggle);
            //SetAllPlayersVisible(!toggle);
            if (toggle)
            {
                SwitchPlayerOut();

                int gameTime = GetGameTimer();
                while (GetPlayerSwitchState() != (int)PlayerSwitchState.SwitchedOut && GetGameTimer() < gameTime + 6500)
                {
                    await Delay(0);
                }
            }
            else
            {
                SwitchPlayerIn();

                int gameTime = GetGameTimer();
                while (GetPlayerSwitchState() != (int)PlayerSwitchState.IsSwitchedIn && GetGameTimer() < gameTime + 6500)
                {
                    await Delay(0);
                }
            }

            IsInKhalse = toggle;

            #endregion

            #endregion
        }

        public static void ShowLoading(string text = null)
        {
            if (!Screen.Fading.IsFadedOut)
                Screen.Fading.FadeOut(0);

            if (!Screen.LoadingPrompt.IsActive)
                Screen.LoadingPrompt.Show(text);
        }

        public static void HideLoading()
        {
            if (Screen.LoadingPrompt.IsActive)
                Screen.LoadingPrompt.Hide();

            if (Screen.Fading.IsFadedOut)
                Screen.Fading.FadeIn(500);
        }

        public static void SetWeather(string weathertype)
        {
            SetWeatherTypePersist(weathertype);
            SetWeatherTypeNowPersist(weathertype);
            SetWeatherTypeNow(weathertype);
            SetOverrideWeather(weathertype);
        }

        public static void StopPlayerPedTask()
        {
            ClearPedTasks(Game.PlayerPed.Handle);
            ClearPedSecondaryTask(Game.PlayerPed.Handle);
        }

        public static void StopPlayerPedTaskNow()
        {
            ClearPedTasks(Game.PlayerPed.Handle);
            ClearPedTasksImmediately(Game.PlayerPed.Handle);
        }

        public static async Task StartParticleFxLocalyOnEntity(string assestName, string fxName, int entity, float scale = 1.0f)
        {
            if (!HasNamedPtfxAssetLoaded(assestName))
                RequestNamedPtfxAsset(assestName);

            int gameTime = GetGameTimer();
            while (!HasNamedPtfxAssetLoaded(assestName) && GetGameTimer() < gameTime + 4000)
                await Delay(0);

            SetPtfxAssetNextCall(assestName);
            StartParticleFxNonLoopedOnEntity(fxName, entity, 0.0f, 0.0f, -0.5f, 0.0f, 0.0f, 0.0f, scale, false, false, false);
            RemoveNamedPtfxAsset(assestName);
        }

        /// <summary>
        /// it will start on every client
        /// </summary>
        /// <param name="assestName"></param>
        /// <param name="fxName"></param>
        /// <param name="id"> is player server id</param>
        public static void StartParticleFxOnPlayer(string assestName, string fxName, int id)
        {
            TriggerServerEvent("IRV:SV:startParticlefx", assestName, fxName, id);
        }

        public static async void StartParticleFxOnEntityBone(string assestName, string fxName, int entity, Bone bone, float scale = 1.0f)
        {
            if (!HasNamedPtfxAssetLoaded(assestName))
                RequestNamedPtfxAsset(assestName);

            int gameTime = GetGameTimer();
            while (!HasNamedPtfxAssetLoaded(assestName) && GetGameTimer() < gameTime + 4000)
                await Delay(0);

            SetPtfxAssetNextCall(assestName);
            StartParticleFxLoopedOnEntityBone(fxName, entity, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, GetPedBoneIndex(entity, (int)bone), scale, false, false, false);

            RemoveNamedPtfxAsset(assestName);
        }


        /// <summary>
        /// Save json data. Returns true if save was successfull.
        /// </summary>
        /// <param name="saveName">Name to store the data under.</param>
        /// <param name="jsonData">The data to store.</param>
        /// <param name="overrideExistingData">If the saveName is already in use, can we override it?</param>
        /// <returns>Whether or not the data was saved successfully.</returns>
        public static bool SaveJsonData(string saveName, string jsonData, bool overrideExistingData)
        {
            if (!string.IsNullOrEmpty(saveName) && !string.IsNullOrEmpty(jsonData))
            {
                string existingData = GetResourceKvpString(saveName); // check for existing data.

                if (!string.IsNullOrEmpty(existingData)) // data already exists for this save name.
                {
                    if (!overrideExistingData)
                    {
                        return false; // data already exists, and we are not allowed to override it.
                    }
                }

                // write data.
                SetResourceKvp(saveName, jsonData);

                // return true if the data is successfully written, otherwise return false.
                return (GetResourceKvpString(saveName) ?? "") == jsonData;
            }
            return false; // input parameters are invalid.
        }

        /// <summary>
        /// Returns the saved json data for the provided save name. Returns null if no data exists.
        /// </summary>
        /// <param name="saveName"></param>
        /// <returns></returns>
        public static string GetJsonData(string saveName)
        {
            if (!string.IsNullOrEmpty(saveName))
            {
                //Debug.WriteLine("not null");
                string data = GetResourceKvpString(saveName);
                //Debug.Write(data + "\n");
                if (!string.IsNullOrEmpty(data))
                {
                    return data;
                }
            }
            return null;
        }

        /// <summary>
        /// Delete the specified saved item from local storage.
        /// </summary>
        /// <param name="saveName">The full name of the item to remove.</param>
        public static void DeleteSavedStorageItem(string saveName)
        {
            DeleteResourceKvp(saveName);
        }

        #region Set Correct Blip
        /// <summary>
        /// Sets the correct blip sprite for the specific ped and blip.
        /// This is the (old) backup method for setting the sprite if the decorators version doesn't work.
        /// </summary>
        /// <param name="ped"></param>
        /// <param name="blip"></param>
        public static void SetCorrectBlipSprite(int ped, int blip)
        {
            if (IsPedInAnyVehicle(ped, false))
            {
                int vehicle = GetVehiclePedIsIn(ped, false);
                int blipSprite = vMenuClient.BlipInfo.GetBlipSpriteForVehicle(vehicle);
                if (GetBlipSprite(blip) != blipSprite)
                {
                    SetBlipSprite(blip, blipSprite);
                }
            }
            else
            {
                SetBlipSprite(blip, 6);
            }
        }
        #endregion

        #region GetVehicle from specified player id (if not specified, return the vehicle of the current player)
        /// <summary>
        /// Returns the current or last vehicle of the current player.
        /// </summary>
        /// <param name="lastVehicle"></param>
        /// <returns></returns>
        public static Vehicle GetVehicle(bool lastVehicle = false)
        {
            if (lastVehicle)
            {
                return Game.PlayerPed.LastVehicle;
            }
            else
            {
                if (Game.PlayerPed.IsInVehicle())
                {
                    return Game.PlayerPed.CurrentVehicle;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the current or last vehicle of the selected ped.
        /// </summary>
        /// <param name="ped"></param>
        /// <param name="lastVehicle"></param>
        /// <returns></returns>
        public static Vehicle GetVehicle(Ped ped, bool lastVehicle = false)
        {
            if (lastVehicle)
            {
                return ped.LastVehicle;
            }
            else
            {
                if (ped.IsInVehicle())
                {
                    return ped.CurrentVehicle;
                }
            }
            return null;
        }

        /// <summary>
        /// Moves camera to a new spot
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="newPos"></param>
        /// <param name="newPointAt"></param>
        /// <returns></returns>
        public static async Task<Camera> MoveCamToNewSpot(Camera camera, Vector3 newPos, Vector3 newPointAt)
        {
            var newCam = CreateCam("DEFAULT_SCRIPTED_CAMERA", true);
            Camera newCamera = new Camera(newCam)
            {
                Position = newPos
            };
            newCamera.PointAt(newPointAt);
            camera.InterpTo(newCamera, 800, 4000, 4000);
            int timer = GetGameTimer();
            while (camera.IsInterpolating || GetGameTimer() - timer < 900)
            {
                //FreezeEntityPosition(Game.PlayerPed.Handle, true);
                //DisableMovementControlsThisFrame(true, true);
                await Delay(0);
            }
            camera.Delete();
            return newCamera;
        }

        /// <summary>
        /// Returns the current or last vehicle of the selected player.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="lastVehicle"></param>
        /// <returns></returns>
        public static Vehicle GetVehicle(Player player, bool lastVehicle = false)
        {
            if (lastVehicle)
            {
                return player.Character.LastVehicle;
            }
            else
            {
                if (player.Character.IsInVehicle())
                {
                    return player.Character.CurrentVehicle;
                }
            }
            return null;
        }
        #endregion
    }
}
