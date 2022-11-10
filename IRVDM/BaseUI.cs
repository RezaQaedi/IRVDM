using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using NativeUI;
using System;
using System.Drawing;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;

namespace IRVDM
{
    class BaseUI : BaseScript
    {
        public static SizeF Res = GetScreenResolutionMaintainRatio();
        private static readonly float Height = Res.Height;
        private static readonly float Width = Res.Width;
        public static int CurrentOnline = 0;
        static readonly LeaderBoard Board = new LeaderBoard();
        private static int LeaderBoradGameTime = -1;
        private static bool ShowInMatchPlayers = true;
        private static bool ShowSpectorPlayers = true;
        private static bool ShowInMenuPlayers = true;

        public enum HudColors
        {
            Purpule = 30,
            Blue = 40,
            Gold = 50,
            SalmonGreen = 60,
            Gray = 70,
            DarkGreen = 80,
            DarkRed = 90,
            LightGreen = 100,
            Orange = 130,
            DarkBlue = 200,
            Black = 140,
            Green = 210,
            Yellow = 180,
            White = 220,
        }

        public BaseUI()
        {
            Tick += BaseUI_Tick;
            EventHandlers["IRV:UI:showNotif"] += new Action<string>(t => ShowNotfi(t));
            EventHandlers["IRV:UI:showSub"] += new Action<string>(s => Screen.ShowSubtitle(s));
            EventHandlers["IRV:UI:showPicNotif"] += new Action<int, string, string, string>((i, p, s, d) => ShowPlayerPedPicNotif(i, p, s, d, true));
        }

        public async Task BaseUI_Tick()
        {
            if (Game.GameTime < LeaderBoradGameTime)
            {
                if (Board.IsLoaded())
                {
                    Board.Draw(ShowInMatchPlayers, ShowInMenuPlayers, ShowSpectorPlayers);
                }
            }
            else
            {
                await Delay(500);
                ShowInMatchPlayers = true;
                ShowInMenuPlayers = true;
                ShowSpectorPlayers = true;
            }
        }

        /// <summary>
        /// show leaderboard for amount of time in millisec
        /// </summary>
        /// <param name="inMatchPl"></param>
        /// <param name="inMenuPl"></param>
        /// <param name="SpecorPl"></param>
        public static void ShowLeaderBoard(int time = 1000, bool inMatchPl = true, bool inMenuPl = true, bool SpecorPl = true)
        {
            LeaderBoradGameTime = Game.GameTime + time;

            ShowInMatchPlayers = inMatchPl;
            ShowInMenuPlayers = inMenuPl;
            ShowSpectorPlayers = SpecorPl;
        }

        public static void DrawText3D(Vector3 pos, string text, int font)
        {
            pos.Z += .25f;
            SetDrawOrigin(pos.X, pos.Y, pos.Z, 0);
            var camPos = GetGameplayCamCoords();
            var dist = World.GetDistance(pos, camPos);
            float scale = 1 / dist * 2f;
            float fov = 1 / GetGameplayCamFov() * 100f;
            scale *= fov;
            if (scale < 0.4)
                scale = 0.4f;

            SetTextScale(0.1f * scale, 0.55f * scale);
            SetTextFont(font);
            SetTextProportional(true);
            SetTextColour(255, 255, 255, 255);
            SetTextDropshadow(0, 0, 0, 0, 255);
            SetTextOutline();
            SetTextEdge(2, 0, 0, 0, 150);
            SetTextDropShadow();
            SetTextEntry("STRING");
            SetTextCentre(true);
            AddTextComponentString(text);
            DrawText(0, 0);
            ClearDrawOrigin();
        }

        public static void DrawRect3D(Vector3 pos, float wSize, float hSize, int r, int g, int b, int alpha)
        {
            SetDrawOrigin(pos.X, pos.Y, pos.Z, 0);
            PointF worldToScreen = Screen.WorldToScreen(pos);
            DrawRect(worldToScreen.X, worldToScreen.Y, wSize, hSize, r, g, b, alpha);
            ClearDrawOrigin();
        }

        public static void DrawMarker(int type, Vector3 pos, Vector3 dir, Vector3 rot, Vector3 scale, int r, int g, int b, int a, bool upDown = false)
            => API.DrawMarker(type, pos.X, pos.Y, pos.Z, dir.X, dir.Y, dir.Z, rot.X, rot.Y, rot.Z, scale.X, scale.Y, scale.Z, r, g, b, a, upDown, true, 1, false, null, null, false);

        public static void DrawSprite(string dict, string txtName, float xPos, float yPos, float width, float height, float heading, int r, int g, int b, int alpha, int vAlig = 0, int hAlig = 0)
        {
            if (!IsHudPreferenceSwitchedOn() || !Screen.Hud.IsVisible) return;

            if (!HasStreamedTextureDictLoaded(dict))
                RequestStreamedTextureDict(dict, true);

            if (hAlig == 2)
                xPos = Res.Width - xPos;
            else if (hAlig == 1)
                xPos = Res.Width / 2 + xPos;

            if (vAlig == 2)
                yPos = Res.Height - yPos;
            else if (vAlig == 1)
                yPos = Res.Height / 2 + yPos;

            float w = width / Width;
            float h = height / Height;
            float x = xPos / Width + w * 0.5f;
            float y = yPos / Height + h * 0.5f;

            API.DrawSprite(dict, txtName, x, y, w, h, heading, r, g, b, alpha);
        }

        public static async Task<bool> ShowLoadDisplay()
        {
            DoScreenFadeOut(500);
            //int gameTime = GetGameTimer();
            while (IsScreenFadingOut())
            {
                //if (gameTime > gameTime + 3000)
                //{
                //    break;
                //}

                await Delay(1);
            }
            return true;
        }

        public static async Task<bool> HideLoadDisplay()
        {
            DoScreenFadeIn(500);
            //int gameTime = GetGameTimer();
            while (IsScreenFadingIn())
            {
                //if (gameTime > gameTime + 3000)
                //{
                //    break;
                //}

                await Delay(1);
            }
            return true;
        }

        public static void DrawRectangle(float xPos, float yPos, float wSize, float hSize, int r, int g, int b, int alpha, int vAlig = 0, int hAlig = 0)
        {
            if (!IsHudPreferenceSwitchedOn() || !Screen.Hud.IsVisible) return;

            if (hAlig == 2)
                xPos = Res.Width - xPos;
            else if (hAlig == 1)
                xPos = Res.Width / 2 + xPos;

            if (vAlig == 2)
                yPos = Res.Height - yPos;
            else if (vAlig == 1)
                yPos = Res.Height / 2 + yPos;

            float w = wSize / Width;
            float h = hSize / Height;
            float x = xPos / Width + w * 0.5f;
            float y = yPos / Height + h * 0.5f;

            DrawRect(x, y, w, h, r, g, b, alpha);
        }

        public static SizeF GetScreenResolutionMaintainRatio()
        {
            return new SizeF(Screen.Resolution.Height * ((float)Screen.Resolution.Width / (float)Screen.Resolution.Height), Screen.Resolution.Height);
        }

        /// <summary>
        /// Returns the safezone bounds in pixel, relative to the 1080pixel based system.
        /// </summary>
        /// <returns></returns>
        public static PointF GetSafezoneBounds()
        {
            float t = Function.Call<float>(Hash.GET_SAFE_ZONE_SIZE); // Safezone size.
            double g = Math.Round(Convert.ToDouble(t), 2);
            g = (g * 100) - 90;
            g = 10 - g;

            const float hmp = 5.4f;
            int screenw = Screen.Resolution.Width;
            int screenh = Screen.Resolution.Height;
            float ratio = (float)screenw / screenh;
            float wmp = ratio * hmp;

            return new PointF((int)Math.Round(g * wmp), (int)Math.Round(g * hmp));
        }

        public static int CreatRenderTarget(string name, uint model)
        {
            if (!IsNamedRendertargetRegistered(name))
                RegisterNamedRendertarget(name, false);
            if (!IsNamedRendertargetLinked(model))
                LinkNamedRendertarget(model);
            if (IsNamedRendertargetRegistered(name))
                return GetNamedRendertargetRenderId(name);
            return -1;
        }

        public static void SetBaseHudColors(HudColor hudColor, bool pausMenu = false)
        {
            SetHudColoursSwitch(0, (int)hudColor);
            SetHudColoursSwitch(116, (int)hudColor);

            if (pausMenu)
                SetHudColoursSwitch(117, (int)hudColor);
        }

        private static void SetTickHideHud()
        {
            HideHudComponentThisFrame(1); // Wanted Stars
            HideHudComponentThisFrame(3); // Cash
            HideHudComponentThisFrame(4); // MP Cash
            HideHudComponentThisFrame(6); // Vehicle Name
            HideHudComponentThisFrame(7); // Area Name
            HideHudComponentThisFrame(8);// Vehicle Class
            HideHudComponentThisFrame(9); // Street Name
            HideHudComponentThisFrame(13); // Cash Change
            //HideHudComponentThisFrame(17); // Save Game
        }

        public static void ShowNotfi(string text, bool blink = false, HudColors backColor = HudColors.Black, HudColors textColor = HudColors.Gray, ColorFormat flashColor = null)
        {
            SetNotificationTextEntry("STRING");
            AddTextComponentString(text);
            SetNotificationBackgroundColor((int)backColor);
            SetNotificationColorNext((int)textColor);

            if (flashColor != null)
                SetNotificationFlashColor(flashColor.Red, flashColor.Green, flashColor.Blue, flashColor.Alpha);

            DrawNotification(blink, false);
        }

        public static async void ShowPlayerPedPicNotif(int id, string text, string title, string subtitle, bool saveToBrief = false) //??
        {
            if (NetworkIsPlayerActive(GetPlayerFromServerId(id)))
            {
                int handle = RegisterPedheadshot(GetPlayerPed(GetPlayerFromServerId(id)));
                int gameTime = GetGameTimer();
                while (!IsPedheadshotReady(handle) || !IsPedheadshotValid(handle))
                {
                    if (GetGameTimer() > gameTime + 8000)
                    {
                        break;
                    }

                    await Delay(1);
                }

                string txd = GetPedheadshotTxdString(handle);

                SetNotificationTextEntry("CELL_EMAIL_BCON"); // 10x ~a~
                foreach (string s in Screen.StringToArray(text))
                {
                    AddTextComponentSubstringPlayerName(s);
                }
                SetNotificationMessage(txd, txd, false, 0, title, subtitle);
                DrawNotification(false, saveToBrief);

                UnregisterPedheadshot(handle);
            }
        }
    }

    public class ColorFormat
    {
        public int Red { get; set; }
        public int Green { get; set; }
        public int Blue { get; set; }
        public int Alpha { get; set; }
    }
}
