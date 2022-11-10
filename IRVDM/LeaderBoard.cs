using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using System.Linq;
using static CitizenFX.Core.Native.API;

namespace IRVDM
{
    class LeaderBoard
    {
        private Scaleform _sc;
        public string Title { get; set; } = "IRANO-V";

        public LeaderBoard()
        {
            Load();
        }

        public async void Load()
        {
            if (_sc != null) return;
            _sc = new Scaleform("sc_leaderboard");
            while (!_sc.IsLoaded) await BaseScript.Delay(0);

            _sc.CallFunction("SET_DISPLAY_TYPE");
            _sc.CallFunction("SET_MULTIPLAYER_TITLE", Title);
            _sc.CallFunction("SET_TITLE", "PLAYER", "Kills", "Deaths", "Xp", "Status");
        }

        public bool IsLoaded()
        => _sc.IsLoaded;

        public void Draw(bool inMatchUsers = true, bool inMenuUsers = true, bool inSpectorModeUsers = true)
        {
            if (_sc == null) return;
            var users = SyncData.UsersDataDic;
            if (!users.ContainsKey(GetPlayerServerId(PlayerId())))
                users.Add(GetPlayerServerId(PlayerId()), SyncData.PlayerInfo);
            int i = 0;
            int j = 0;
            int k = 0;

            _sc.CallFunction("CLEAR_ALL_SLOTS");



            List<BasePlayerData> inMenuUsersList = users.Values.Where(u => u.PlayerStatus == PlayerStatuses.IsInMainMenu).OrderBy(u => u.InTotalUserKills).ToList();
            List<BasePlayerData> inMatchUsersList = users.Values.Where(u => u.PlayerStatus == PlayerStatuses.IsInMatch).OrderBy(u => u.UserKills).ToList();
            List<BasePlayerData> inSpectorModUsersList = users.Values.Where(u => u.PlayerStatus == PlayerStatuses.IsInSpectorMode).OrderBy(u => u.UserKills).ToList();

            if (inMenuUsers)
            {
                if (inMenuUsersList.Count() > 0)
                    _sc.CallFunction("SET_SLOT", i, 16, "IN MENU PLAYERS");

                for (int _i = inMenuUsersList.Count - 1; _i >= 0; _i--)
                {
                    i++;
                    string status = inMenuUsersList[_i].IsReady ? "~g~ Ready" : "~r~ Ready";

                    _sc.CallFunction("SET_SLOT", i, 0, i, GetPlayerName(GetPlayerFromServerId((inMenuUsersList[_i].Id))), "", inMenuUsersList[_i].InTotalUserKills + "~g~(T)", inMenuUsersList[_i].InTotalUserDeaths + "~g~(T)", inMenuUsersList[_i].Xp, status);
                } 
            }

            if (inMatchUsers)
            {
                if (inMatchUsersList.Count() > 0)
                {
                    if (i != 0) i++;
                    _sc.CallFunction("SET_SLOT", i, 16, "IN MATCH PLAYERS");
                }

                for (int _j = inMatchUsersList.Count - 1; _j >= 0; _j--)
                {
                    i++;
                    j++;
                    string status = "~r~InMatch";

                    _sc.CallFunction("SET_SLOT", i, 0, j, GetPlayerName(GetPlayerFromServerId((inMatchUsersList[_j].Id))), "", inMatchUsersList[_j].UserKills + "~r~(M)", inMatchUsersList[_j].UserDeaths + "~r~(M)", inMatchUsersList[_j].Xp, status);
                } 
            }

            if (inSpectorModeUsers)
            {
                if (inSpectorModUsersList.Count > 0)
                {
                    if (i != 0) i++;
                    _sc.CallFunction("SET_SLOT", i, 16, "Spectors");
                }

                for (int _k = inSpectorModUsersList.Count - 1; _k >= 0; _k--)
                {
                    i++;
                    k++;
                    string status = "~b~Specting";

                    _sc.CallFunction("SET_SLOT", i, 0, k, GetPlayerName(GetPlayerFromServerId((inSpectorModUsersList[_k].Id))), "", inSpectorModUsersList[_k].InTotalUserKills + "~b~(T)", inSpectorModUsersList[_k].InTotalUserDeaths + "~b~(T)", inSpectorModUsersList[_k].Xp, status);
                } 
            }

            _sc.Render2D();
        }
    }
}
