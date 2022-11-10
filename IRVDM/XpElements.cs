using CitizenFX.Core;
using System;
using System.Collections.Generic;
using static CitizenFX.Core.Native.API;

namespace IRVDM
{
    internal class XpElementFormat
    {
        public string Id { get; set; } = "";
        public int Xp { get; set; } = 0;
        public string NotifText { get; set; } = "";
        public string Txd { get; set; } = "";
        public string Txn { get; set; } = "";

        public XpElementFormat(int xp, string id, string text = "", string txd = "", string txn = "")
        {
            Id = id;
            Xp = xp;
            NotifText = text;
            Txd = txd;
            Txn = txn;
        }

        public virtual async void Trigger()
        {
            BaseScript.TriggerServerEvent("IRV:SV:addXpToPlayer", Xp, Id);

            if (!string.IsNullOrEmpty(Txn))
            {
                if (!HasStreamedTextureDictLoaded(Txd))
                    RequestStreamedTextureDict(Txd, true);

                int gameTime = GetGameTimer();
                while (!HasStreamedTextureDictLoaded(Txd))
                {
                    if (GetGameTimer() > gameTime + 3000)
                    {
                        break;
                    }

                    await BaseScript.Delay(0);
                }

                SetNotificationTextEntry("STRING");
                AddTextComponentSubstringPlayerName(NotifText);
                DrawNotificationAward(Txd, Txn, Xp, 6, "FM_GEN_UNLOCK"); //109 gold //6 red

                SetStreamedTextureDictAsNoLongerNeeded(Txd);
            }
        }
    }

    internal class XpElements : BaseScript
    {
        private readonly Dictionary<string, XpElementFormat> XpElementsDic = new Dictionary<string, XpElementFormat>()
        {
            ["IRV_Xp_Kill"] = new XpElementFormat(18, "IRV_Xp_Kill", "Kill Xp", "prop_screen_arena_giant", "xp_kill"),
            ["IRV_Xp_Melee_Kill"] = new XpElementFormat(30, "IRV_Xp_Melee_Kill", "Melee Kill Xp", "prop_screen_arena_giant", "injury_xp"),
            ["IRV_Xp_Head_Kill"] = new XpElementFormat(25, "IRV_Xp_Head_Kill", "HeadShot Xp", "prop_screen_arena_giant", "xp_headshot"),
            ["IRV_Xp_Ex_Kill"] = new XpElementFormat(20, "IRV_Xp_Ex_Kill", "Kill Xp", "prop_screen_arena_giant", "xp_boobytrap"),
            ["IRV_Xp_Win"] = new XpElementFormat(250, "IRV_Xp_Win", "Win Xp", "prop_screen_arena_giant", "dm_pos1")
        };

        public XpElements()
        {
            GameEventManager.OnPlayerKillPlayer += new Action<int, uint, bool, bool>(SetKillXp);
            EventHandlers["IRV:Xp:onPlayerWin"] += new Action(() => XpElementsDic["IRV_Xp_Win"].Trigger());
        }

        private void SetKillXp(int victim, uint wephash, bool isMelee, bool isHead)
        {
            if (isMelee)
            {
                XpElementsDic["IRV_Xp_Melee_Kill"].Trigger();
                return;
            }

            if (isHead)
            {
                XpElementsDic["IRV_Xp_Head_Kill"].Trigger();
                return;
            }

            if ((uint)GetWeapontypeGroup(wephash) == (uint)WeaponData.GameWeaponCategory.Melee)
            {
                XpElementsDic["IRV_Xp_Melee_Kill"].Trigger();
            }
            else if ((uint)GetWeapontypeGroup(wephash) == (uint)WeaponData.GameWeaponCategory.throwables)
            {
                XpElementsDic["IRV_Xp_Ex_Kill"].Trigger();
            }
            else
            {
                XpElementsDic["IRV_Xp_Kill"].Trigger();
            }
        }
    }
}
