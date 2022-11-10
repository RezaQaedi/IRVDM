namespace IRVDM
{
    public enum PlayerStatuses
    {
        none,
        IsInMainMenu,
        IsInMatch,
        IsInLoby,
        IsInSpectorMode
    }

    public class BasePlayerData
    {
        internal int Id { get; set; }
        internal int UserKills { get; set; } = 0;
        internal int UserDeaths { get; set; } = 0;
        internal int InTotalUserKills { get; set; } = 0;
        internal int InTotalUserDeaths { get; set; } = 0;
        internal int Xp { get; set; } = 0;
        internal bool HaveRegisterd { get; set; } = false;
        internal PlayerStatuses PlayerStatus { get; set; } = PlayerStatuses.none;
        internal bool IsReady { get; set; } = false;
        internal PlayerLoadout Loadout { get; set; } = new PlayerLoadout();
    }
}
