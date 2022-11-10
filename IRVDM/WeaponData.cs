using Newtonsoft.Json;
using System.Collections.Generic;
using static CitizenFX.Core.Native.API;

namespace IRVDM
{
    public enum WeaponCategoryBlips
    {
        melee = 154,
        handguns = 156,
        machine = 159,
        assault = 150,
        heavy_machineGuns = 150,
        shotgun = 158,
        sniper = 160,
        heavy_explosion = 157,
        heavy = 157,
        thrown = 152,
        thrown_burn = 155,
        health = 153
    }

    internal class WeaponData
    {
        public readonly List<WeaponDataGxt> weaponDatas = new List<WeaponDataGxt>();

        public enum GameWeaponCategory : uint
        {
            Rifle = 970310034,
            Handgun = 416676503,
            Stungun = 690389602,
            Shotgun = 860033945,
            Submachinegun = 3337201093,
            Lightmachinegun = 1159398588,
            throwables = 1548507267,
            Fire_extinghuiser = 4257178988,
            Jerrycan = 1595662460,
            Melee = 3566412244,
            Knuckle_duster = 2685387236,
            Heavy = 2725924767,
            Sniper = 3082541095,
        }

        public static readonly Dictionary<string, uint> CuseOfDeathHashes = new Dictionary<string, uint>()
        {
            ["weapon_unarmed"] = 2725352035,
            ["weapon_run_over_by_car"] = 2741846334,
            ["weapon_rammed_by_car"] = 133987706,
            ["weapon_fall"] = 3452007600,
            ["weapon_animal"] = 4194021054,
            ["weapon_bleeding"] = 2339582971,
            ["weapon_drowning"] = 4284007675,
            ["weapon_drowning_in_vehicle"] = 1936677264,
            ["weapon_explosion"] = 539292904,
            ["weapon_fire"] = 3750660587,
            ["weapon_heli_crash"] = 341774354,
            ["weapon_electric_fence"] = 2461879995,
            ["weapon_exhaustion"] = 910830060
        };

        public static readonly Dictionary<string, string> KilledBySuicideNotifSuffixes = new Dictionary<string, string>()
        {
            ["weapon_fall"] = "~r~fell down ~w~and killed",
            ["weapon_bleeding"] = "~w~died From ~r~bleeding",
            ["weapon_drowning"] = "~r~drowned",
            ["weapon_run_over_by_car"] = "didnt know how to drive",
            ["weapon_rammed_by_car"] = "didnt know how to drive",
            ["weapon_drowning_in_vehicle"] = "~r~drowned ~w~in their vehicle",
            ["weapon_explosion"] = "~r~blew ~w~themselves up",
            ["weapon_fire"] = "~r~burned ~w~themselves",
            ["weapon_heli_crash"] = "~r~fell down ~w~or killed",
            ["weapon_electric_fence"] = "died by ~r~electricity",
            ["weapon_exhaustion"] = "died from ~r~exhaustion"
        };

        public static readonly Dictionary<string, string> KilledByPedNotifSuffixes = new Dictionary<string, string>()
        {
            ["weapon_drowning"] = "~r~drowned~w~",
            ["weapon_drowning_in_vehicle"] = "~r~drowned~w~",
            ["weapon_explosion"] = "~r~Exploded~w~",
            ["weapon_run_over_by_car"] = "~r~ran over~w~",
            ["weapon_rammed_by_car"] = "~r~rammed~w~",
            ["weapon_fire"] = "~r~burned~w~",
            ["weapon_unarmed"] = "~r~beated up~w~",
        };

        public static readonly Dictionary<string, string> KilledByPlayerNotifPrefixes = new Dictionary<string, string>()
        {
            ["weapon_unarmed"] = "~r~beated up~w~",
            ["weapon_run_over_by_car"] = "~r~ran over~w~",
            ["weapon_rammed_by_car"] = "~r~rammed~w~",
            ["weapon_explosion"] = "~r~blew up~w~",
            ["weapon_fire"] = "~r~burned~w~",
        };

        public static string GetNotifSPForWep(uint wep, Dictionary<string, string> NotifSuffixPrefixDic)
        {
            foreach (var item in CuseOfDeathHashes)
            {
                if (item.Value == wep)
                {
                    if (NotifSuffixPrefixDic.ContainsKey(item.Key))
                    {
                        return NotifSuffixPrefixDic[item.Key];
                    }
                    break;
                }
            }

            return "";
        }

        public string GetWeaponLableName(uint w)
        {
            foreach (var item in weaponDatas)
            {
                if ((uint)GetHashKey(item.ModelName) == w)
                {
                    return item.Name;
                }
            }

            return "";
        }

        public WeaponData()
        {
            string data = LoadResourceFile(GetCurrentResourceName(), "config/WeaponData.json");
            weaponDatas = (List<WeaponDataGxt>)JsonConvert.DeserializeObject(data, typeof(List<WeaponDataGxt>));
        }
    }

    public struct WeaponDataGxt
    {
        public string ModelName;
        public string Description;
        public string Name;
        public string NameGXT;
        public string DescriptionGXT;
        public string ModelHashKey;
        public List<WeaponDataGxt> weaponComponents;
    }
}

