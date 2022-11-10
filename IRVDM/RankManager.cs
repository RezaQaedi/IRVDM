using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;

namespace IRVDM
{
    internal class RankManager : BaseScript
    {
        private static readonly List<int> RockstartRanks = new List<int>()
        {
            800, 2100, 3800, 6100, 9500, 12500, 16000, 19800, 24000, 28500, 33400, 38700, 44200, 50200, 56400, 63000, 69900, 77100, 84700, 92500,
            100700, 109200, 118000, 127100, 136500, 146200, 156200, 166500, 177100, 188000, 199200, 210700, 222400, 234500, 246800, 259400, 272300,
            285500, 299000, 312700, 326800, 341000, 355600, 370500, 385600, 401000, 416600, 432600, 448800, 465200, 482000, 499000, 516300, 533800,
            551600, 569600, 588000, 606500, 625400, 644500, 663800, 683400, 703300, 723400, 743800, 764500, 785400, 806500, 827900, 849600, 871500,
            893600, 916000, 938700, 961600, 984700, 1008100, 1031800, 1055700, 1079800, 1104200, 1128800, 1153700, 1178800, 1204200, 1229800, 1255600,
            1281700, 1308100, 1334600, 1361400, 1388500, 1415800, 1443300, 1471100, 1499100, 1527300, 1555800, 1584350
        };
        private const int MaxPlayerLevel = 500;
        /// <summary>
        /// amount, newxp, oldxp, reason
        /// </summary>
        public static event Action<int, int, int, string> OnPlayerGetXp;
        /// <summary>
        /// newLevel, oldlevel, last xp added
        /// </summary>
        public static event Action<int, int, int> OnPlayerLevelUp;

        public RankManager()
        {
            EventHandlers["IRV:Rp:Add"] += new Action<int, int, int, string>((p, n, o, r) =>
            {
                AddPlayerXp(p);
                OnPlayerGetXp?.Invoke(p, n, o, r);
                int oldLevel = GetLevelWithXp(o);
                int newLevel = GetLevelWithXp(n);
                if (oldLevel != newLevel)
                    OnPlayerLevelUp?.Invoke(newLevel, oldLevel, p);
            });
            EventHandlers["IRV:Rp:Set"] += new Action<int>(p => SetPlayerXp(p, false, false));

            OnPlayerGetXp += (p, n, o, r) => MainDebug.WriteDebug($"Player Just Gets Xp Amount {p}, newXp {n}, oldXp {o}, reason {r}");
            OnPlayerLevelUp += (n, o, p) => MainDebug.WriteDebug($"Player Just leveled up newLevel {n}, oldLevel {o}, lastXpAdded {p}");

            Tick += async () =>
            {
                if (Game.IsControlJustPressed(1, Control.MultiplayerInfo))
                {
                    await Show();
                }
            };
        }

        public async void AddPlayerXp(int amount)
        {
            await Load();

            Exports["XNLRankBar"].Exp_XNL_AddPlayerXP(amount);
        }

        public async void RemovePlayerXp(int amount)
        {
            await Load();
            Exports["XNLRankBar"].Exp_XNL_RemovePlayerXP(amount);
        }

        public async Task Show()
        {
            await Load();
            Exports["XNLRankBar"].Exp_XNL_ShowRankBar();
        }

        public async void SetPlayerXp(int amount, bool showRankBar, bool animated)
        {
            await Load();
            Exports["XNLRankBar"].Exp_XNL_SetInitialXPLevels(amount, showRankBar, animated);
        }

        public int GetPlayerXp()
        {
            return Exports["XNLRankBar"].Exp_XNL_GetCurrentPlayerXP();
        }

        public int GetLevelFromXp(int amount)
        {
            return Exports["XNLRankBar"].Exp_XNL_GetLevelFromXP(amount);
        }

        public int GetPlayerLevel()
        {
            return Exports["XNLRankBar"].Exp_XNL_GetCurrentPlayerLevel();
        }

        public static int GetLevelWithXp(int xp)
        {
            if (xp < 0)
                return 1;

            if (xp < RockstartRanks[98])
            {
                int level = 0;

                foreach (var item in RockstartRanks)
                {
                    level++;
                    if (xp < item)
                        return level;
                }

            }
            else
            {
                int ExtraAddPerLevel = 50;
                int MainAddPerLevel = 28550;
                int xpNeeded = 0;
                int lastRockstartXp = RockstartRanks[98];

                for (int i = 1; i < MaxPlayerLevel - 99; i++)
                {
                    MainAddPerLevel += ExtraAddPerLevel;
                    xpNeeded += MainAddPerLevel;
                    if (xp < lastRockstartXp + xpNeeded) return i + RockstartRanks.Count;
                }
            }

            return -1;
        }

        public async Task Load()
        {
            if (!HasHudScaleformLoaded(19))
                RequestHudScaleform(19);
            int gameTime = GetGameTimer();
            while (!HasHudScaleformLoaded(19))
            {
                if (GetGameTimer() > gameTime + 3000)
                {
                    break;
                }

                await Delay(0);
            }
        }
    }
}
