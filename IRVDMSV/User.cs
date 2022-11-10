using CitizenFX.Core;

namespace IRVDMSV
{
    public enum UserStatuses
    {
        none,
        IsInMainMenu,
        IsInMatch,
        IsInLoby,
        IsInSpectorMode
    }

    internal class User
    {
        public Player player { get; set; }
        public int UserKills { get; set; } = 0;
        public int UserDeaths { get; set; } = 0;
        public int InTotalUserKills { get; set; } = 0;
        public int InTotalUserDeaths { get; set; } = 0;
        public int Xp { get; set; } = 0;
        public bool HaveRegisterd { get; set; } = false;
        public bool IsReady { get; set; } = false;
        public UserStatuses UserStatus { get; set; } = UserStatuses.none;

    }
}
