using System.Collections.Generic;

namespace IRVDM
{
    class PlayerLoadout
    {
        public WeaponLoadout PrimaryWeapon { get; set; } = new WeaponLoadout() { WeaponName = "WEAPON_SMG", MaxAmount = 220 };
        public WeaponLoadout SecondaryWeapon { get; set; } = new WeaponLoadout() { WeaponName = "WEAPON_APPISTOL", MaxAmount = 110 };
        public WeaponLoadout Equipment { get; set; } = new WeaponLoadout() { WeaponName = "WEAPON_GRENADE", MaxAmount = 1 };
        public PlayerSkinLoadOut Skin { get; set; } = new PlayerSkinLoadOut("a_m_m_afriamer_01", "UNLOCKABLE_PED_A_M_M_ACULT_01") { Gender = PedGender.Male, PedModel = PedModel.WorldPed, Category = "male" };
        public ParticleFxFormat ParticleFx { get; set; } = new ParticleFxFormat(ParticleFxData.ParticalFxs[0].Name, ParticleFxData.ParticalFxs[0].AssestName, ParticleFxData.ParticalFxs[0].OnPlayerSawnParticleName, ParticleFxData.ParticalFxs[0].OnPlayerKillParticleName, ParticleFxData.ParticalFxs[0].TrailParticleName);
    }

    class WeaponLoadout
    {
        public string WeaponName { get; set; } = "";
        public List<string> WeaponComponent { get; set; } = new List<string>();
        public int MaxAmount { get; set; } = 0;
        public int TintIndex { get; set; } = 0;
    }
}
