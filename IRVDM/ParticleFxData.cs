using System.Collections.Generic;

namespace IRVDM
{
    class ParticleFxData
    {
        /// <summary>
        /// key is name to show and value is particlefxformat to apply
        /// </summary>
        public static readonly List<ParticleFxFormat> ParticalFxs = new List<ParticleFxFormat>()
        {
            new ParticleFxFormat("Beast", "scr_powerplay",  "scr_powerplay_beast_vanish", "scr_powerplay_beast_appear", "sp_powerplay_beast_appear_trails"),
            new ParticleFxFormat("Clown", "scr_rcbarry2",  "scr_clown_death", "scr_clown_appears", "sp_clown_appear_trails"),
            new ParticleFxFormat("FireWorker01", "scr_rcpaparazzo1", "scr_mich4_firework_starburst", "scr_mich4_firework_burst_spawn", "scr_mich4_firework_trailburst"),
            new ParticleFxFormat("FireWorker02", "scr_paletoscore", "scr_paleto_roof_impact", "scr_paleto_box_sparks", "scr_paleto_fire_trail"),
            new ParticleFxFormat("FireWorker03", "scr_indep_fireworks", "scr_indep_firework_burst_spawn", "scr_indep_firework_trail_spawn"),
        };
    }
}
