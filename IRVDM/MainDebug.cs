using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;

namespace IRVDM
{
    internal class MainDebug : BaseScript
    {
        //should rewrite this calss later [just for now I left it here] 

        #region values
        public enum Prefixes
        {
            none,
            info,
            error,
            warning,
        }

        public enum DebugSettigs
        {
            Playerinfo,
            TempDebugDic,
            DebugWrite
        }

        public static Dictionary<DebugSettigs, bool> DebugSettingsList = new Dictionary<DebugSettigs, bool>()
        {
            [DebugSettigs.Playerinfo] = false,
            [DebugSettigs.TempDebugDic] = false,
            [DebugSettigs.DebugWrite] = true
        };

        public static string Prefix = "[IRVDM]";
        private readonly CommonFunctions Cf = new CommonFunctions();
        private readonly Dictionary<string, string> PlayerInfoList = new Dictionary<string, string>()
        {
            ["PlayerStatues"] = SyncData.PlayerInfo.PlayerStatus.ToString(),
            ["Kills"] = SyncData.PlayerInfo.UserKills.ToString(),
            ["Deaths"] = SyncData.PlayerInfo.UserDeaths.ToString(),
            ["regester"] = SyncData.PlayerInfo.HaveRegisterd.ToString(),
            ["isready"] = SyncData.PlayerInfo.IsReady.ToString(),
            ["xp"] = SyncData.PlayerInfo.Xp.ToString()
        };

        public static Dictionary<string, string> TempDebugDic = new Dictionary<string, string>();

        #endregion
        #region Main Functions
        public MainDebug()
        {

            Tick += MainDebug_Tick;
            RegisterCommand("irvdmdebug", new Action(() =>
            {
                if (DebugSettingsList[DebugSettigs.Playerinfo])
                    DebugSettingsList[DebugSettigs.Playerinfo] = false;
                else
                    DebugSettingsList[DebugSettigs.Playerinfo] = true;

            }), false /*This command is also not restricted, anyone can use it.*/ );
            RegisterCommand("irvdmTempShutDownMainMenu", new Action(() =>
            {
                TriggerEvent("IRV:Menu:shutdown");
                SyncData.PlayerInfo.PlayerStatus = PlayerStatuses.none;

            }), false /*This command is also not restricted, anyone can use it.*/ );
        }

        public static void AddToDebugDrawing(string _key, string _value) => TempDebugDic.Add(_key, _value);
        public static void TurnDebugOff()
        {
            foreach (var item in DebugSettingsList)
            {
                DebugSettingsList[item.Key] = false;
            }
        }

        private async Task MainDebug_Tick() => UpdatePlayerInfo();

        public static void WriteDebug(dynamic s, Prefixes pr = Prefixes.none)
        {
            string prefix = "";

            switch (pr)
            {
                case Prefixes.none:
                    prefix = Prefix + " :";
                    break;
                case Prefixes.info:
                    prefix = Prefix + "[INFO] :";
                    break;
                case Prefixes.error:
                    prefix = Prefix + "[ERROR] :";
                    break;
                case Prefixes.warning:
                    prefix = Prefix + "[WARNING] :";
                    break;
            }

            if (DebugSettingsList[DebugSettigs.DebugWrite])
            {
                Debug.WriteLine(prefix + s);
            }
        }

        private void UpdatePlayerInfo()
        {
            if (DebugSettingsList[DebugSettigs.Playerinfo])
            {
                PlayerInfoList["PlayerStatues"] = SyncData.PlayerInfo.PlayerStatus.ToString();
                PlayerInfoList["Kills"] = SyncData.PlayerInfo.UserKills.ToString();
                PlayerInfoList["Deaths"] = SyncData.PlayerInfo.UserDeaths.ToString();
                PlayerInfoList["regester"] = SyncData.PlayerInfo.HaveRegisterd.ToString();
                PlayerInfoList["isready"] = SyncData.PlayerInfo.IsReady.ToString();
                PlayerInfoList["xp"] = SyncData.PlayerInfo.Xp.ToString();
                ShowScoreboard(1000f, PlayerInfoList);
            }
            if (DebugSettingsList[DebugSettigs.TempDebugDic])
            {
                foreach (var item in TempDebugDic)
                {
                    TempDebugDic[item.Key] = item.Value;
                    ShowScoreboard(250f, TempDebugDic);
                }
            }
        }

        #endregion
        #region Scale functions

        private struct PlayerRow
        {
            public string name;
            public string rightText;
            public int color;

            public int rightIcon;
            public string textureString;
        }


        private Scaleform UpdateScale(Scaleform scale)
        {
            List<PlayerRow> rows = new List<PlayerRow>();

            var amount = 0;
            foreach (var item in PlayerInfoList)
            {


                PlayerRow row = new PlayerRow()
                {
                    name = item.Key + " ===> " + item.Value,
                    color = 25,
                    rightIcon = 65,
                    rightText = "",
                    textureString = ""
                };

                rows.Add(row);

                amount++;
            }

            for (var i = 0; i < PlayerInfoList.Count * 2; i++)
            {
                scale.CallFunction("SET_DATA_SLOT_EMPTY", i);
            }
            var index = 0;
            foreach (PlayerRow row in rows)
            {
                scale.CallFunction("SET_DATA_SLOT", index, row.rightText, row.name, row.color, row.rightIcon,
                    "", row.textureString, row.textureString);
                index++;
            }

            return scale;
        }

        /// <summary>
        /// Loads the scaleform.
        /// </summary>
        /// <returns></returns>
        private Scaleform LoadScale(Dictionary<string, string> setting)
        {
            Scaleform scale = new Scaleform("MP_MM_CARD_FREEMODE");
            if (scale != null)
            {
                for (var i = 0; i < PlayerInfoList.Count * 2; i++)
                {
                    scale.CallFunction("SET_DATA_SLOT_EMPTY", i);
                }
                scale.Dispose();
            }
            scale = null;
            scale = new Scaleform("MP_MM_CARD_FREEMODE");
            var titleIcon = "5";
            var titleLeftText = "Debug";
            var titleRightText = $"INFO: {setting.Count}";
            scale.CallFunction("SET_TITLE", titleLeftText, titleRightText, titleIcon);
            scale = UpdateScale(scale);
            scale.CallFunction("DISPLAY_VIEW");

            return scale;
        }



        /// <summary>
        /// Shows the scoreboard.
        /// </summary>
        /// <returns></returns>
        private void ShowScoreboard(float x, Dictionary<string, string> setting, float y = 50f)
        {
            Scaleform scale = LoadScale(setting);
            float safezone = GetSafeZoneSize();
            float change = (safezone - 0.89f) / 0.11f;
            x -= change * 78f;
            y -= change * 50f;

            var width = 250f;
            var height = 300f;
            if (scale != null)
            {
                if (scale.IsLoaded)
                {
                    scale.Render2DScreenSpace(new System.Drawing.PointF(x, y), new System.Drawing.PointF(width, height));
                }
            }
        }
        #endregion  
    }

}
