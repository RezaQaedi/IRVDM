namespace IRVDM
{
    class ParticleFxFormat
    {
        public string Name { get; set; }
        public string AssestName { get; set; }
        public string OnPlayerSawnParticleName { get; set; }
        public string OnPlayerKillParticleName { get; set; }
        public string TrailParticleName { get; set; }

        public ParticleFxFormat(string name, string assest, string onSpawn = "", string onKill = "", string trail = "")
        {
            Name = name;
            AssestName = assest;
            OnPlayerSawnParticleName = onSpawn;
            OnPlayerKillParticleName = onKill;
            TrailParticleName = trail;
        }
    }
}
