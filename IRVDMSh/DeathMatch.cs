using System.Collections.Generic;
using CitizenFX.Core;

namespace IRVDMShared
{
    class DeathMatch
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Author { get; set; } = "";
        public string Type { get; set; } = "";
        public string TextureName { get; set; } = "";
        public string MusicEventEventId { get; set; } = "";
        public string StopMusicCommandEventId { get; set; } = "";
        public float RangeNumber { get; set; } = 0;
        public int TimeInMiliSecond { get; set; } = 10000;
        public List<MatchSpawnPoint> SpawnPoints { get; set; } = new List<MatchSpawnPoint>();
        public List<MatchProp> MatchProps { get; set; } = new List<MatchProp>();
        public List<string> IPLs { get; set; } = new List<string>();
        public List<MatchPickupSpawnPoint> PickupWeapons { get; set; } = new List<MatchPickupSpawnPoint>();
        public Dictionary<string, int> StartWithWeapons { get; set; } = new Dictionary<string, int>();
        public List<string> InteriorPropNames { get; set; } = new List<string>();
        public bool Shape { get; set; } = true;
        public bool LockOnWeapons { get; set; } = false;
        public string WorldWeather { get; set; } = "CLEAR";
        public Vector3 MiddlePosition { get; set; } = new Vector3();
        public TimeFormat WorldTime { get; set; } = new TimeFormat() {Hour = 0, MilliSecond = 0, Second = 0, Minutes = 0};

        public override string ToString()
        {
            return "[DM]" + Name;
        }
    }

    public class MatchSpawnPoint
    {
        public MatchSpawnPoint(Vector3 coord, float heading = 0.0f)
        {
            Position = coord;
            Heading = heading;
        }

        public float Heading { get; set; }
        public Vector3 Position { get; set; }
    }
    
    public class MatchPickupSpawnPoint
    {
        public MatchPickupSpawnPoint(uint _hash, int _ammo, Vector3 _coord)
        {
            PickupHash = _hash;
            MaxAmmo = _ammo;
            Position = _coord;
        }

        public uint PickupHash { get; set; }
        public int MaxAmmo { get; set; }
        public Vector3 Position { get; set; }
    }

    public class MatchProp
    {
        public string ModelName { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
    }
}
