using NativeUI;
using System.Collections.Generic;

namespace IRVDM
{
    public class PedInfo
    {
        public Dictionary<int, int> Props { get; set; }
        public Dictionary<int, int> PropTextures { get; set; }
        public Dictionary<int, int> DrawableVariations { get; set; }
        public Dictionary<int, int> DrawableVariationTextures { get; set; }
    }

    public class PlayerSkinLoadOut : PedFormat
    {
        public PlayerSkinLoadOut() { }
        public PlayerSkinLoadOut(string model, string id, int version = -1) { ModelName = model; Version = version; Id = id; }
        public PedInfo SkinInfo { get; set; } = new PedInfo();
        public int Version;

    };

    public class PedFormat : Unlockable
    {
        public PedGender Gender { get; set; } = PedGender.Male;
        public PedModel PedModel { get; set; } = PedModel.WorldPed;
        public HudColor HudColor { get; set; } = HudColor.HUD_COLOUR_DAMAGE;
        public int Accuracy { get; set; } = 0;
        public int MaxHealth { get; set; } = 300;
        public int MaxArmour { get; set; } = 0;
        public int Stamina { get; set; } = 0;
        public int Strength { get; set; } = 0;
        public bool IsAnimal { get; set; } = false;
        public string Category { get; set; } = "none";
        public string ChoosenScenario { get; set; } = "";

        public PedFormat() { }

        public PedFormat(string modelName, string lableName)
        {
            LableName = lableName;
            ModelName = modelName;
        }
    }

    public enum PedGender
    {
        Male,
        Female,
        None
    }

    public enum PedModel
    {
        WorldPed,
        Michael,
        Trevor,
        Franklin,
        MpMale,
        MpFemale
    }
}
