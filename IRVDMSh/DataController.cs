using CitizenFX.Core;
using Newtonsoft.Json;
using System.Collections.Generic;
using static CitizenFX.Core.Native.API;
#if CLIENT
using IRVDM;
#endif

namespace IRVDMShared
{
    internal class DataController
    {
        public static Dictionary<string, DeathMatch> GetDeathMatches()
        {
            Dictionary<string, DeathMatch> temp = new Dictionary<string, DeathMatch>();
            string deathmatchfile = LoadResourceFile(GetCurrentResourceName(), "config/DeathMatchs.json");
            if (string.IsNullOrEmpty(deathmatchfile))
            {

                Debug.WriteLine("Cant load deathmatchs locations!");

                return null;
            }
            else
            {
                try
                {
                    List<DeathMatch> deathMatches = (List<DeathMatch>)JsonConvert.DeserializeObject(deathmatchfile, typeof(List<DeathMatch>));

                    foreach (var item in deathMatches)
                    {
                        temp.Add(item.Name, item);
                    }
                }
                catch (JsonException e)
                {
                    Debug.WriteLine("well somthing went wrong here is exception " + e.Message);
                    return null;
                }
            }
            return temp;
        }
#if CLIENT


        //public static List<WeaponFormat> GetWeaponData()
        //{
        //    List<WeaponFormat> temp = new List<WeaponFormat>();
        //    string file = LoadResourceFile(GetCurrentResourceName(), "config/AllWeapons.json");
        //    if (string.IsNullOrEmpty(file))
        //    {
        //        MainDebug.WriteDebug("Cant load weapon data !", MainDebug.Prefixes.error);
        //        return null;
        //    }
        //    else
        //    {
        //        try
        //        {
        //            Newtonsoft.Json.Linq.JObject jObject = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(file);
        //            foreach (Newtonsoft.Json.Linq.JToken data in jObject["Weapons"])
        //            {

        //                WeaponFormat wpTemp = new WeaponFormat();
        //                wpTemp.ModelName = data["Name"].ToString().ToUpper();
        //                //wpTemp.Category = data["category"].ToString();
        //                wpTemp.Model = data["Model"].ToString().ToUpper();
        //                wpTemp.BonPos = new Vector3((float)data["BonPos"]["X"], (float)data["BonPos"]["Y"], (float)data["BonPos"]["Z"]);
        //                wpTemp.BoneRot = new Vector3((float)data["BoneRot"]["X"], (float)data["BoneRot"]["Y"], (float)data["BoneRot"]["Z"]);
        //                wpTemp.Bone = (int)data["Bone"];
        //                WeaponClass weaponClass;
                        

        //                switch (data["Category"].ToString())
        //                {
        //                    case "melee":
        //                        weaponClass = WeaponClass.melee;
        //                        break;
        //                    case "assault":
        //                        weaponClass = WeaponClass.assault;
        //                        break;
        //                    case "handguns":
        //                        weaponClass = WeaponClass.handguns;
        //                        break;
        //                    case "heavy":
        //                        weaponClass = WeaponClass.heavy;
        //                        break;
        //                    case "heavy_explosion":
        //                        weaponClass = WeaponClass.heavy_explosion;
        //                        break;
        //                    case "heavy_machineGuns":
        //                        weaponClass = WeaponClass.heavy_machine;
        //                        break;
        //                    case "machine":
        //                        weaponClass = WeaponClass.machine;
        //                        break;
        //                    case "shotgun":
        //                        weaponClass = WeaponClass.shotgun;
        //                        break;
        //                    case "thrown":
        //                        weaponClass = WeaponClass.thrown;
        //                        break;
        //                    case "sniper":
        //                        weaponClass = WeaponClass.sniper;
        //                        break;
        //                    default:
        //                        weaponClass = WeaponClass.none;
        //                        break;
        //                }

        //                wpTemp.Category = weaponClass;

        //                temp.Add(wpTemp);
        //            }
        //            MainDebug.WriteDebug("Just loaded weapon data", MainDebug.Prefixes.info);
        //        }
        //        catch (JsonException e)
        //        {
        //            MainDebug.WriteDebug("well somthing went wrong When loading weapon data is exception " + e.Message, MainDebug.Prefixes.error);
        //            return null;
        //        }
        //    }
        //    return temp;
        //}

        public static Dictionary<string, MainMenuLocation> GetMainMenus()
        {
            Dictionary<string, MainMenuLocation> temp = new Dictionary<string, MainMenuLocation>();
            string file = LoadResourceFile(GetCurrentResourceName(), "config/MainMenus.json");
            if (string.IsNullOrEmpty(file))
            {
                MainDebug.WriteDebug("Cant load Menu locations!", MainDebug.Prefixes.error);
                return null;
            }
            else
            {
                try
                {
                    List<MainMenuLocation> mainMenus = (List<MainMenuLocation>)JsonConvert.DeserializeObject(file, typeof(List<MainMenuLocation>));

                    foreach (var item in mainMenus)
                    {
                        temp.Add(item.Name, item);
                    }
                    //Newtonsoft.Json.Linq.JObject jObject = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(file);
                    //foreach (Newtonsoft.Json.Linq.JToken menu in jObject["Menus"])
                    //{
                    //    MainMenuLocation menutemp = new MainMenuLocation();
                    //    menutemp.Name = menu["Name"].ToString();
                    //    menutemp.PlayerPedPos = new Vector3((float)menu["PlayerPedPos"]["x"], (float)menu["PlayerPedPos"]["y"], (float)menu["PlayerPedPos"]["z"]);
                    //    menutemp.PlayerPedLookPos = new Vector3((float)menu["PlayerPedLookPos"]["x"], (float)menu["PlayerPedLookPos"]["y"], (float)menu["PlayerPedLookPos"]["z"]);
                    //    menutemp.CameraPos = new Vector3((float)menu["CameraPos"]["x"], (float)menu["CameraPos"]["y"], (float)menu["CameraPos"]["z"]);
                    //    menutemp.CameraRot = new Vector3((float)menu["CameraRot"]["x"], (float)menu["CameraRot"]["y"], (float)menu["CameraRot"]["z"]);
                    //    menutemp.PlayerHeading = (int)menu["PlayerHeading"];

                    //    temp.Add(menu["Name"].ToString(), menutemp);


                    //    MainDebug.WriteDebug("Just added This map [" + menutemp.Name + "]", MainDebug.Prefixes.info);

                    //}

                }
                catch (JsonException e)
                {
                    MainDebug.WriteDebug("well somthing went wrong When loading Menu data is exception " + e.Message, MainDebug.Prefixes.error);
                    return null;
                }
            }
            return temp;
        }


#endif
    }
}
