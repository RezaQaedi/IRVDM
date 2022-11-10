namespace IRVDM
{
    public class WeaponFormat : Unlockable
    {
        public string Category { get; set; }
        public WepLoadOut Slot { get; set; }
        public int MaxAmmo { get; set; } = 200;
    }

    public class WeaponItem : Unlockable
    {
        public string TargetWeapon { get; set; }
    }

    public enum WeaponClass
    {
        none,
        handguns,
        melee,
        machine,
        heavy_machine,
        assault,
        shotgun,
        sniper,
        heavy,
        heavy_explosion,
        thrown,
    }

    public enum WepLoadOut
    {
        Slot01,
        Slot02,
        Slot03,
    }
}
