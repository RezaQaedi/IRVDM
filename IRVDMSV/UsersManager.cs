using CitizenFX.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace IRVDMSV
{
    //ye bug hast beyne zamani ke crash mikhore va zamani ke disconect call mishe delay hast 
    //age player beyne on zaman bargarde dg to users nemire 
    //ye bug dg age 2 ta client ba ye identifre join beshan 2vomi add nemishe be users vali hamchenan join mishe client (ba onplayerconnectiong bayad chek konam)
    internal class UsersManager : BaseScript
    {
        //correctly if this resource gets stopped[or server goes down] before the match ends data will be not saved !
        /// <summary>
        /// Main User List To get or send data from mysql or to clients
        /// </summary>
        internal static List<User> Users { get; private set; } = new List<User>();
        public static readonly string Identifre = "steam";
        public static readonly Vector3 DefaultSpawnPos = new Vector3(-2166.38f, 5195.86f, 16.90f);

        /// <summary>
        /// Generating all players that already are in server
        /// </summary>
        public UsersManager()
        {
            OnResourceStarts();
            #region Events 
            //EventHandlers["playerConnecting"] += new Action<Player>(PlayerConnecting);
            EventHandlers["playerDropped"] += new Action<Player>(PlayerDropped);
            EventHandlers["IRV:SV:clientReady"] += new Action<Player, bool>(ToggleReady); //meybe its better to update all propertys ?
            EventHandlers["IRV:SV:clientMatchStatus"] += new Action<Player, int>(UpdateClientMatchStatus);
            EventHandlers["IRV:SV:clientJustJoiend"] += new Action<Player>(ClientJoiend);
            EventHandlers["IRV:SV:addXpToPlayer"] += new Action<Player, int, string>(AddXpToPlayer);

            Main.MainMatchTimer.OnTimerEnd += async () => await SaveAllUsersIdentityData();
            Main.MainMatchTimer.OnTimerDisposed += async () => await SaveAllUsersIdentityData();
            #endregion
        }

        private void AddXpToPlayer([FromSource]Player player, int amount, string reason)
        {
            User user = GetUserFromPlayer(player);
            int OldXp = user.Xp;
            int NewXp = user.Xp + amount;
            user.Xp += amount;
            SendUserDataToClient(user);
            user.player.TriggerEvent("IRV:Rp:Add", amount, NewXp, OldXp, reason);
        }

        private async void ClientJoiend([FromSource]Player client)
        {
            Main.SendMatchDataToPlayer(client);
            bool doseExist = await DoesIdentityExist(client);
            if (doseExist)
                await LoadPlayerIdentity(client);
            else
                await CreatPlayerIdentity(client);

            //SendUserDataToClient(GetUserFromPlayer(client));
            SendUsersDataToClient(client);
            client.TriggerEvent("IRV:Pl:SpawnPlayer", DefaultSpawnPos.X, DefaultSpawnPos.Y, DefaultSpawnPos.Z, new Random().Next(250));
            await Delay(1000);
            //client.TriggerEvent("IRV:Pl:SendToMainMenu");
            client.TriggerEvent("IRV:Rp:Set", GetUserFromPlayer(client).Xp);
        }


        #region Modifying Users property

        /// <summary>
        /// will add <paramref name="kill"/> amount to player kill value
        /// </summary>
        /// <param name="player"></param>
        /// <param name="kill"></param>
        public static void UpdatePlayerKValue(Player player, int kill)
        {
            User user = GetUserFromPlayer(player);
            user.UserKills += kill;
            user.InTotalUserKills += kill;
            //update other clients as well
            SendUserDataToClient(user);
        }

        /// <summary>
        /// will add <paramref name="deaths"/> amount to player death value
        /// </summary>
        /// <param name="player"></param>
        /// <param name="deaths"></param>
        public static void UpdatePlayerDValue(Player player, int deaths)
        {
            User user = GetUserFromPlayer(player);
            user.UserDeaths += deaths;
            user.InTotalUserDeaths += deaths;
            Debug.WriteLine($"death called for this user {player.Name} amount {deaths}");
            //update other clients as well
            SendUserDataToClient(user);
        }

        /// <summary>
        /// setting kd values
        /// </summary>
        /// <param name="player"></param>
        /// <param name="kill"></param>
        /// <param name="deaths"></param>
        public static void SetPlayerKDValue(Player player, int kill = -1, int deaths = -1)
        {
            User user = GetUserFromPlayer(player);
            if (kill != -1)
                user.UserKills = kill;
            if (deaths != -1)
                user.UserDeaths = deaths;

            SendUserDataToClient(user);
        }

        /// <summary>
        /// Set User IsReady [true] or [false]
        /// </summary>
        /// <param name="_player"></param>
        /// <param name="isready"></param>
        private void ToggleReady([FromSource]Player _player, bool isready)
        {
            User user = GetUserFromPlayer(_player);
            user.IsReady = isready;
            SendUserDataToClient(user);
            DebugLog.Log($"this user changed isready  {user.player.Name}");
            //temp
            if ((Main.MatchStatus == Main.MatchStatuses.IsWating || Main.MatchStatus == Main.MatchStatuses.HasEnded) && isready && GetReadyClients().Count >= Main.MinPlayerToStart)
            {
                TriggerEvent("IRV:SV:reciveBeforeMatchTicket");
            }
        }

        private void UpdateClientMatchStatus([FromSource]Player player, int status)
        {
            GetUserFromPlayer(player).UserStatus = (UserStatuses)status;
            SendUserDataToClient(GetUserFromPlayer(player));
        }
        #endregion
        #region FiveM EventHandeller functions
        ///// <summary>
        ///// add new player to userlist if its not already there 
        ///// </summary>
        ///// <param name="_player"></param>
        //public async void PlayerConnecting([FromSource]Player _player)
        //{

        //    //User newuser = new User()
        //    //{
        //    //    player = _player
        //    //};

        //    //Users.Add(newuser);
        //}

        private async void OnResourceStarts()
        {
            foreach (Player _player in Players)
            {
                if (_player.Name != "**Invalid**" && _player.Name != "** Invalid **")
                {
                    //if network is active ?
                    bool doesExist = await DoesIdentityExist(_player);
                    if (doesExist)
                        await LoadPlayerIdentity(_player); //load 
                    else
                        await CreatPlayerIdentity(_player); //creat 
                }

            }
            DebugLog.Log($"GameMode restarted we have loaded players data from database for [{Users.Count}] users", DebugLog.LogLevel.success);
        }

        /// <summary>
        /// remove player from userlist if its exist 
        /// </summary>
        /// <param name="_player"></param>
        public async void PlayerDropped([FromSource]Player _player)
        {
            if (DoesPlayerExistInUsers(_player))
            {
                User user = GetUserFromPlayer(_player);
                await SaveUserIdentityData(user);

                if (user != null)
                    Users.Remove(user);

                DebugLog.Log($"Player {_player.Name} just disconnected and we saved the data", DebugLog.LogLevel.success);
                TriggerClientEvent("IRV:Data:removeUserData", GetPlayerServerId(_player));
            }
            else
                DebugLog.Log($"Player {_player.Name} just disconnected!", DebugLog.LogLevel.warning);
        }
        #endregion
        #region getting property values of clients 

        /// <summary>
        /// return user belongs to this player 
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static User GetUserFromPlayer(Player player)
        {
            try
            {
                return Users.SingleOrDefault(u => u.player.Identifiers[Identifre] == player.Identifiers[Identifre]);
            }
            catch (Exception e)
            {
                DebugLog.Log($"somthing whent wrong when trying to get user from player user identifier {player.Identifiers}, " +
                    $"Users list data count {Users.Count}  and exeption {e}", DebugLog.LogLevel.error);
            }
            return Users[0];
        }

        public static bool DoesPlayerExistInUsers(Player player)
        {
            if (Users.Find(u => u.player.Identifiers[Identifre] == player.Identifiers[Identifre]) != null)
                return true;
            return false;
        }

        /// <summary>
        /// return Ready Clients 
        /// </summary>
        /// <returns></returns>
        public static List<User> GetReadyClients()
        {
            return Users.Where(u => u.IsReady == true).ToList();
        }

        public static List<User> GetInMatchClients()
        {
            return Users.Where(u => u.UserStatus == UserStatuses.IsInMatch).ToList();
        }

        public static int GetPlayerServerId(Player player)
        {
            return Convert.ToInt32(player.Handle) <= 65535 ? Convert.ToInt32(player.Handle) : Convert.ToInt32(player.Handle) - 65535;
        }

        public bool DoesThisUserExist(User user)
        {
            foreach (User item in Users)
            {
                if (item.player.Identifiers[Identifre] == user.player.Identifiers[Identifre])
                {
                    return true;
                }
            }
            return false;
        }

        #endregion
        #region sync Client data with other clints
        /// <summary>
        /// send user values to client or clients  
        /// </summary>
        /// <param name="user"></param>
        /// <param name="player"> client you want to send to</param>
        public static void SendUserDataToClient(User user, Player player = null)
        {
            var data = GetUserDataArrayList(user);

            if (player != null)
                TriggerClientEvent(player, "IRV:Data:reciveUsersData", GetPlayerServerId(user.player), data);
            else
                TriggerClientEvent("IRV:Data:reciveUsersData", GetPlayerServerId(user.player), data);
        }

        public static void SendUsersDataToClient(Player player)
        {
            foreach (User user in Users)
            {
                var data = GetUserDataArrayList(user);
                TriggerClientEvent(player, "IRV:Data:reciveUsersData", GetPlayerServerId(user.player), data);
            }
        }

        public static void SendUsersDataToAllClients()
        {
            foreach (User user in Users)
            {
                var data = GetUserDataArrayList(user);
                TriggerClientEvent("IRV:Data:reciveUsersData", GetPlayerServerId(user.player), data);
            }
        }

        public static ArrayList GetUserDataArrayList(User user)
        {
            int statusid = Convert.ToInt32(user.UserStatus);
            ArrayList data = new ArrayList
            {
                user.UserKills,
                user.UserDeaths,
                user.InTotalUserKills,
                user.InTotalUserDeaths,
                user.Xp,
                user.HaveRegisterd,
                user.IsReady,
                statusid,
            };
            return data;
        }
        #endregion
        #region mysql data import exporting

        public async Task SaveUserIdentityData(User user)
        {

            try
            {
                string sql = "UPDATE users SET kills = '" + user.InTotalUserKills + "', deaths = '" + user.InTotalUserDeaths + "' ,xp = '" + user.Xp + "' WHERE steam_id ='" + user.player.Identifiers[Identifre] + "'";

                await Exports["ghmattimysql"].executeSync(sql);

                DebugLog.Log($"we have changed kills to {user.InTotalUserKills} and death to {user.InTotalUserDeaths} for this client {user.player.Name}", DebugLog.LogLevel.info);
            }
            catch (Exception e)
            {
                DebugLog.Log($"somthing went wrong when trying to save data to database here is exeption {e}");
            }
        }

        public async Task SaveAllUsersIdentityData()
        {
            foreach (User user in Users)
            {
                await SaveUserIdentityData(user);
            }
        }

        private async Task CreatPlayerIdentity(Player _player)
        {
            try
            {
                string sql = "INSERT INTO users (steam_id ,kills ,deaths ,xp) " +
            "VALUES('" + _player.Identifiers[Identifre] + "', '" + 0 + "', '" + 0 + "', '" + 0 + "');";
                dynamic data = await Exports["ghmattimysql"].executeSync(sql);
                Users.Add(new User() { player = _player });

                DebugLog.Log($"we have just created this client [{_player.Name}]", DebugLog.LogLevel.success);
            }
            catch (Exception e)
            {
                DebugLog.Log($"somthing went wrong when trying to creat data into database here is exeption {e}");
            }
        }

        private async Task LoadPlayerIdentity(Player player)
        {
            try
            {
                dynamic data = await Exports["ghmattimysql"].executeSync("SELECT * FROM users WHERE steam_id = '" + player.Identifiers[Identifre] + "'");
                User newuser = new User();
                foreach (var user in data)
                {
                    newuser = new User()
                    {
                        player = player,
                        InTotalUserKills = (int)user.kills,
                        InTotalUserDeaths = (int)user.deaths,
                        Xp = (int)user.xp,
                    };
                }

                if (DoesThisUserExist(newuser))
                    return;

                Users.Add(newuser);
                DebugLog.Log($"we have just loaded this client [{player.Name}]", DebugLog.LogLevel.success);
            }
            catch (Exception e)
            {
                DebugLog.Log($"somthing went wrong when trying to load data from database here is exeption {e}");
            }
            //await Exports["ghmattimysql"].executeSync("INSERT INTO `users` (`steam_id`,`kills`,`deaths`,`level`) " +
            //    "VALUES('110000100000638', '" + 0 + "', '" + 0 + "', '" + 0 + "');");
        }


        //private bool HasThisPlayerRegisterd(Player player)
        //{
        //    bool register;
        //    foreach (DataRow row in Appi.MySql.ExecuteQueryWithResult("SELECT * FROM users WHERE identifier = '" + player.Identifiers[Identifre] + "'").Rows)
        //    {
        //        if ((string)row["have_registerd"] == "true") register = true;
        //        else
        //            register = false;
        //        return register;
        //    }
        //    return false;
        //}

        private async Task<bool> DoesIdentityExist(Player player)
        {
            try
            {
                dynamic data = await Exports["ghmattimysql"].executeSync("SELECT * FROM users WHERE steam_id = '" + player.Identifiers[Identifre] + "'");
                if (data != null && data.Count != 0)
                    return true;
            }
            catch (Exception e)
            {
                DebugLog.Log($"somthing went wrong when trying to chek data from database here is exeption {e}");
            }
            return false;
        }
        #endregion
    }
}
