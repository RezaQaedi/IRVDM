using CitizenFX.Core;
using IRVDMShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;

namespace IRVDMSV
{
    public class Main : BaseScript
    {
        #region Varibles
        /// <summary>
        /// this timer ends <see cref="WaitForClients"/> after clients timers cuz off siwtch in and out 
        /// </summary>
        internal static TimerMain MainMatchTimer = new TimerMain();

        private readonly int WaitForClients = 5000;
        private readonly int BeforeMatchContDownTime = 10000;
        private readonly int BeforMatchWaitForClientsTime = 2000;
        private static DeathMatch NextMatch;
        private Random RandomClass;

        private readonly List<User> PreparedPlayers = new List<User>();
        private bool ArePlayersWatingForStart = false;

        private Dictionary<DeathMatch, int> VoteMapPool;
        public bool ArePlayersVotingForMatch { get; private set; } = false;
        public readonly bool ChooseMapWithVote = true;
        public readonly int VoteTime = 15000;

        private readonly Dictionary<string, DeathMatch> DeathMaches;
        public static readonly int MinPlayerToStart = 1;
        public static readonly int MinPlayerToProgresInMatch = 1;
        private static MatchStatuses _MatchStatus = MatchStatuses.IsWating;

        public static bool DebugMode = true;
        public static MatchStatuses MatchStatus
        {
            get { return _MatchStatus; }
            private set
            {
                _MatchStatus = value;
                SendMatchDataToPlayer();
            }
        }

        public enum MatchStatuses
        {
            /// <summary>
            /// already runnig
            /// </summary>
            IsRunning = 1,
            /// <summary>
            /// wating for players
            /// </summary>
            IsWating = 0,
            /// <summary>
            /// just has ended
            /// </summary>
            HasEnded = 3,
            /// <summary>
            /// counting down to start
            /// </summary>
            IsGoingToStart = 4,
        }

        //private readonly List<string> Weather = new List<string>()
        //{
        //    "EXTRASUNNY",
        //    "CLEAR",
        //    "NEUTRAL",
        //    "SMOG",
        //    "FOGGY",
        //    "CLOUDS",
        //    "OVERCAST",
        //    "CLEARING",
        //    "RAIN",
        //    "THUNDER",
        //    "BLIZZARD",
        //    "SNOW",
        //    "SNOWLIGHT",
        //    "XMAS",
        //    "HALLOWEEN"
        //};

        #endregion
        #region Constractor
        public Main()
        {
            DeathMaches = DataController.GetDeathMatches();
            SetMapName("Wating");
            Tick += Tick1000;
            #region FivemEvents
            EventHandlers["playerDropped"] += new Action<Player>(PlayerDropped);
            #endregion
            #region IRVEvents
            //EventHandlers["IRV:SV:SaveMap"] += new Action<string>(p => File.WriteAllText(@"D:\Menu_" +GetGameTimer().ToString() + ".json", p));
            EventHandlers["IRV:SV:reciveStartTicket"] += new Action(SendBeforeMatchCommand);
            EventHandlers["IRV:SV:reciveBeforeMatchTicket"] += new Action(SendBeforeMatchCommand);
            EventHandlers["IRV:SV:onPlayerPrepared"] += new Action<Player>(OnPlayerPrepared);
            EventHandlers["IRV:SV:onPlayerKilled"] += new Action<Player, int, uint>(OnPlayerKilled);
            EventHandlers["IRV:SV:onPlayerCollectPickup"] += new Action<int>(c =>
            {
                foreach (User user in UsersManager.GetInMatchClients())
                {
                    user.player.TriggerEvent("IRV:Match:removePickup", c);
                }
            });
            EventHandlers["IRV:SV:startParticlefx"] += new Action<string, string, int>((a, n, p) => TriggerClientEvent("IRV:Pl:StartParticleOnEntity", a, n, p));
            EventHandlers["IRV:SV:onPlayerVoted"] += new Action<Player, string>(OnplayerVoted);
            #endregion

            MainMatchTimer.OnTimerEnd += () => SendEndMatchCommands();
        }

        private void OnplayerVoted([FromSource]Player player, string dmName)
        {
            KeyValuePair<DeathMatch, int> i = VoteMapPool.FirstOrDefault(p => p.Key.Name == dmName);
            VoteMapPool[i.Key]++;
            DebugLog.Log($"player {player.Name} voted for {dmName}, that have {VoteMapPool[i.Key]} vote");

            foreach (var item in UsersManager.GetReadyClients())
            {
                item.player.TriggerEvent("IRV:Menu:onPlayerVoted", player.Name, dmName, VoteMapPool[i.Key]);
            }
        }

        #endregion
        #region Player EventHanelers
        private void PlayerDropped([FromSource]Player player)
        {
            foreach (var user in PreparedPlayers)
            {
                if (user.player.Identifiers[UsersManager.Identifre] == player.Identifiers[UsersManager.Identifre])
                {
                    PreparedPlayers.Remove(user);
                }
            }
        }

        private void OnPlayerPrepared([FromSource]Player player)
        {
            //age zamane load shodane player ha khelyi beshe player ha to halat loading mimonan
            PreparedPlayers.Add(UsersManager.GetUserFromPlayer(player));
            DebugLog.Log($"client has prepared needed to start match : {UsersManager.GetReadyClients().Count}, have : {PreparedPlayers.Count}");
            ArePlayersWatingForStart = true;
        }

        private void OnPlayerKilled([FromSource]Player victim, int killerId, uint wepHash)
        {
            //killer ids
            //-2= ped ,-1= victim ,-3= not found ,else players
            TriggerClientEvent("IRV:Match:notifKD", UsersManager.GetPlayerServerId(victim), killerId, wepHash);

            UsersManager.UpdatePlayerDValue(victim, 1);
            foreach (Player player in Players)
            {
                if (UsersManager.GetPlayerServerId(player) == killerId)
                {
                    UsersManager.UpdatePlayerKValue(player, 1);
                    break;
                }
            }
        }
        #endregion
        #region Tick1000
        private async Task Tick1000()
        {
            await Delay(1000);

            if (MatchStatus == MatchStatuses.IsRunning)
            {
                await Delay(2000);//bayad bastegi dashte bashe be time bazi  zire 1 min ==> show winner balash ==> matchfailed
                if (UsersManager.GetInMatchClients().Count < MinPlayerToProgresInMatch)
                {
                    DebugLog.Log("low player have been reached shutting down the match in 3 second...", DebugLog.LogLevel.warning);
                    await Delay(3000);
                    SendMatchFailedCommand();
                }
            }

            //to do : when a player taking to long to load just send him back to menu and start ==> (important)
            if (ArePlayersWatingForStart)
            {
                if (MainMatchTimer.IsRunning == false && MatchStatus != MatchStatuses.IsRunning)
                {
                    if (PreparedPlayers.Count >= UsersManager.GetReadyClients().Count)
                    {
                        DebugLog.Log($"we have them [ {PreparedPlayers.Count} ] sending commands");
                        SendStartCommand();
                        MainMatchTimer.StartTimerNow(NextMatch.TimeInMiliSecond + WaitForClients);
                        PreparedPlayers.Clear();
                        ArePlayersWatingForStart = false;
                    }
                }
            }

            if (MatchStatus == MatchStatuses.IsGoingToStart)
            {
                if (UsersManager.GetReadyClients().Count < MinPlayerToStart)
                {
                    MatchStatus = MatchStatuses.IsWating;
                    TriggerClientEvent("IRV:UI:showNotif", "Not enough clients are ready to start");
                }
            }
        }

        #endregion
        #region Managing match functions
        private void SendMatchFailedCommand()
        {
            foreach (User user in UsersManager.GetInMatchClients())
            {
                TriggerClientEvent(user.player, "IRV:Pl:reciveFailCommand");
            }
            TriggerClientEvent("IRV:UI:showNotif", "~r~Match just has Failed!");

            if (MainMatchTimer.IsRunning)
                MainMatchTimer.StopTimerNow();
            MatchStatus = MatchStatuses.IsWating;
        }

        public static void SendMatchDataToPlayer([FromSource]Player player = null)
        {

            if (player != null)
            {
                TriggerClientEvent(player, "IRV:Match:reciveMatchData", (int)MatchStatus);
                DebugLog.Log("Sending Match data to this client " + MatchStatus.ToString(), DebugLog.LogLevel.info);
            }
            else
            {
                TriggerClientEvent("IRV:Match:reciveMatchData", (int)MatchStatus);
                DebugLog.Log("Sending Match data to all clients " + MatchStatus.ToString(), DebugLog.LogLevel.info);
            }
        }

        /// <summary>
        /// called when match is going to start 
        /// </summary>
        private void SendStartCommand()
        {
            if (MatchStatus != MatchStatuses.IsRunning)
            {
                foreach (User user in UsersManager.GetReadyClients())
                {
                    user.player.TriggerEvent("IRV:PL:startTheMatch");
                    DebugLog.Log($"Sending start command to {user.player.Name}", DebugLog.LogLevel.info);
                    MatchStatus = MatchStatuses.IsRunning;
                    SetMapName(NextMatch.Name);
                }
            }
            TriggerClientEvent("IRV:Match:reciveMatchData", (int)MatchStatus);
        }

        /// <summary>
        /// Called when ready players are more than <see cref="MinPlayerToStart"/>
        /// </summary>
        private async void SendBeforeMatchCommand()
        {
            long timer = GetGameTimer();
            long timertoreach = GetGameTimer() + BeforeMatchContDownTime + BeforMatchWaitForClientsTime;
            MatchStatus = MatchStatuses.IsGoingToStart;

            if (ChooseMapWithVote)
            {
                TriggerClientEvent("IRV:Match:reciveBeforeMatchCommand", BeforeMatchContDownTime, true);
                TriggerClientEvent("IRV:UI:showNotif", "Next Match Will be choosen by ~g~[~r~vote~g~]");
            }
            else
            {
                NextMatch = GetRandomMap();
                TriggerClientEvent("IRV:Match:reciveBeforeMatchCommand", BeforeMatchContDownTime, false);
                TriggerClientEvent("IRV:UI:showNotif", "Next Match Will be ~g~[~r~" + NextMatch.Name + "~g~]");
                TriggerClientEvent("IRV:Match:setNexMatch", NextMatch.Name);
            }


            while (timer <= timertoreach)
            {
                timer = GetGameTimer();
                await Delay(500);
            }

            if (UsersManager.GetReadyClients().Count >= MinPlayerToStart)
            {
                if (ChooseMapWithVote)
                {
                    ArePlayersVotingForMatch = true;
                    VoteMapPool = GetVotableMatches();

                    var dmNameList = new List<string>();
                    foreach (var votebleDm in VoteMapPool.Keys)
                    {
                        dmNameList.Add(votebleDm.Name);
                    }

                    foreach (var item in UsersManager.GetReadyClients())
                    {
                        item.player.TriggerEvent("IRV:Menu:StartVoteMap", dmNameList, VoteTime);
                    }

                    timer = GetGameTimer();
                    timertoreach = GetGameTimer() + VoteTime + 3000;

                    while (timer <= timertoreach)
                    {
                        timer = GetGameTimer();
                        await Delay(0);
                    }

                    if (UsersManager.GetReadyClients().Count >= MinPlayerToStart)
                    {
                        var maxValue = VoteMapPool.Values.Max();
                        var dm = VoteMapPool.Where(p => p.Value == maxValue).First();
                        NextMatch = dm.Key;

                        TriggerClientEvent("IRV:Match:setNexMatch", NextMatch.Name);
                        DebugLog.Log($"this map had chooesn by players [{NextMatch.Name}]");

                        ArePlayersVotingForMatch = false;
                    }
                    else
                    {
                        MatchStatus = MatchStatuses.IsWating;
                        TriggerClientEvent("IRV:UI:showNotif", "Not enough clients are ready to start");
                    }
                }

                SendPreparedCommand();
            }
            else
            {
                MatchStatus = MatchStatuses.IsWating;
                TriggerClientEvent("IRV:UI:showNotif", "Not enough clients are ready to start");
            }
        }

        private DeathMatch GetRandomMap()
        {
            if (DeathMaches.Count >= 2)
            {
                List<string> temp = DeathMaches.Keys.ToList();
                RandomClass = new Random();
                int random = RandomClass.Next(0, DeathMaches.Count);
                return DeathMaches[temp[random]];
            }
            else
                return DeathMaches.Values.First();
        }

        private void SendPreparedCommand()
        {
            Random rnd = new Random();
            var randomSp = NextMatch.SpawnPoints.OrderBy(item => rnd.Next()).ToList();
            int spwanPosIndex = 0;

            foreach (User user in UsersManager.GetReadyClients())
            {
                if (NextMatch.SpawnPoints.Count < UsersManager.GetReadyClients().Count)
                {
                    //RandomClass = new Random();
                    //var sp = NextMatch.SpawnPoints[RandomClass.Next(0, NextMatch.SpawnPoints.Count - 1)];

                    //user.player.TriggerEvent("IRV:PL:preparePlayer", sp.Position.X, sp.Position.Y, sp.Position.Z, sp.Heading);
                }
                else
                {
                    user.player.TriggerEvent("IRV:PL:preparePlayer", randomSp[spwanPosIndex].Position.X,
                        randomSp[spwanPosIndex].Position.Y,
                        randomSp[spwanPosIndex].Position.Z,
                        randomSp[spwanPosIndex].Heading);

                    spwanPosIndex++;
                }
            }

            foreach (var user in UsersManager.Users)
            {
                UsersManager.SetPlayerKDValue(user.player, 0, 0);
            }
        }

        private void SendEndMatchCommands()
        {
            if (MatchStatus == MatchStatuses.IsRunning)
            {
                DebugLog.Log("Timer has ended calling clients with end commands..", DebugLog.LogLevel.warning);


                //adding xp for winner and make sure there are still players in match
                if (UsersManager.GetInMatchClients().Count != 0)
                {
                    User winner = GetWinner();
                    winner.player.TriggerEvent("IRV:Xp:onPlayerWin");
                    //TriggerClientEvent("IRV:UI:showNotif", $"~w~Player ~g~{winner.player.Name} ~w~win the match with ~r~{winner.UserKills} ~w~kills");
                    TriggerClientEvent("IRV:UI:showPicNotif", UsersManager.GetPlayerServerId(winner.player), $"~w~Player ~g~{winner.player.Name} ~w~win the match with ~r~{winner.UserKills} ~w~kills", "Match", "Winner");
                    foreach (User user in UsersManager.Users.Where(u => u.UserStatus == UserStatuses.IsInMatch))
                    {
                        TriggerClientEvent(user.player, "IRV:PL:reciveEndMatchCommand");
                    }

                    foreach (User user in UsersManager.Users.Where(u => u.UserStatus == UserStatuses.IsInSpectorMode))
                    {
                        user.player.TriggerEvent("IRV:Pl:SendToMainMenu");
                    } 
                }
                MatchStatus = MatchStatuses.HasEnded;
                SetMapName("Wating");
            }
        }

        private User GetWinner()
        {
            User winner;
            List<User> InMatchUsers = UsersManager.Users.Where(u => u.UserStatus == UserStatuses.IsInMatch).ToList();
            int MaxKill = InMatchUsers.Max(u => u.UserKills);
            List<User> HighestKillers = InMatchUsers.Where(u => u.UserKills == MaxKill).ToList();
            int MinDeath = HighestKillers.Min(u => u.UserDeaths);
            List<User> LowestHasKilled = HighestKillers.Where(u => u.UserDeaths == MinDeath).ToList();

            if (HighestKillers.Count == 1)
                winner = HighestKillers.First();
            else
                winner = LowestHasKilled.First();

            return winner;
        }

        private Dictionary<DeathMatch, int> GetVotableMatches()
        {
            Dictionary<DeathMatch, int> temp = new Dictionary<DeathMatch, int>();
            if (DeathMaches.Count > 6)
            {
                Random rnd = new Random();
                var randomList = DeathMaches.Values.OrderBy(item => rnd.Next());
                var sixRandomItem = randomList.Take(6);

                foreach (DeathMatch deathMatch in sixRandomItem)
                {
                    temp.Add(deathMatch, 0);
                }
            }
            else
            {
                foreach (DeathMatch deathMatch in DeathMaches.Values)
                {
                    temp.Add(deathMatch, 0);
                }
            }

            return temp;
        }
        #endregion
    }

    #region Debug
    public static class DebugLog
    {
        public enum LogLevel
        {
            error = 1,
            success = 2,
            info = 4,
            warning = 3,
            none = 0
        }

        /// <summary>
        /// Global log data function, only logs when debugging is enabled.
        /// </summary>
        /// <param name="data"></param>
        public static void Log(dynamic data, LogLevel level = LogLevel.none)
        {
            if (Main.DebugMode || level == LogLevel.error || level == LogLevel.warning)
            {
                string prefix = "[IRVDM] ";
                if (level == LogLevel.error)
                {
                    prefix = "^1[IRVDM] [ERROR]^7 ";
                }
                else if (level == LogLevel.info)
                {
                    prefix = "^5[IRVDM] [INFO]^7 ";
                }
                else if (level == LogLevel.success)
                {
                    prefix = "^2[IRVDM] [SUCCESS]^7 ";
                }
                else if (level == LogLevel.warning)
                {
                    prefix = "^3[IRVDM] [WARNING]^7 ";
                }
                Debug.WriteLine($"{prefix}[DEBUG LOG] {data.ToString()}");
            }
        }
    }
    #endregion
}
