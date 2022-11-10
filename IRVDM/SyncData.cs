using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace IRVDM
{
    class SyncData : BaseScript
    {
        internal static Dictionary<int, BasePlayerData> UsersDataDic = new Dictionary<int, BasePlayerData>();
        public static BasePlayerData PlayerInfo { get; internal set; } = new BasePlayerData();
        public static event Action<int> OnPlayerJoiend;
        public static event Action<int> OnPlayerDropped;
        /// <summary>
        /// when skin loaded from kvm data this event triggers
        /// </summary>
        internal static event Action OnPlayerSkinLoaded;

        private static readonly int Slot01DefaultMaxAmount = 220;
        private static readonly int Slot02DefaultMaxAmount = 110;
        private static readonly int Slot03DefaultMaxAmount = 1;
        public static bool HasPlayerRankDataRecived { get; set; } = false;

        private static readonly PlayerLoadout DefaultPlayerLoadout = new PlayerLoadout()
        {
            PrimaryWeapon = new WeaponLoadout() { WeaponName = "WEAPON_MICROSMG", MaxAmount = Slot01DefaultMaxAmount },
            SecondaryWeapon = new WeaponLoadout() { WeaponName = "WEAPON_PISTOL", MaxAmount = Slot02DefaultMaxAmount },
            Equipment = new WeaponLoadout() { WeaponName = "WEAPON_GRENADE", MaxAmount = Slot03DefaultMaxAmount },
            Skin = new PlayerSkinLoadOut("a_m_m_afriamer_01", "UNLOCKABLE_PED_A_M_M_AFRIAMER_01") { Gender = PedGender.Female, PedModel = PedModel.WorldPed, Category = "Female" },
            ParticleFx = ParticleFxData.ParticalFxs[0],
        };

        public SyncData()
        {
            EventHandlers["IRV:Data:reciveUsersData"] += new Action<int, dynamic>(ReciveUsersData);
            EventHandlers["IRV:Data:removeUserData"] += new Action<int>(id =>
            {
                UsersDataDic.Remove(id);
                OnPlayerDropped?.Invoke(id);
            });
        }

        private void ReciveUsersData(int id, dynamic data)
        {
            if (id != Game.Player.ServerId)
            {
                BasePlayerData temp = new BasePlayerData()
                {
                    Id = id,
                    UserKills = (int)data[0],
                    UserDeaths = (int)data[1],
                    InTotalUserKills = (int)data[2],
                    InTotalUserDeaths = (int)data[3],
                    Xp = (int)data[4],
                    HaveRegisterd = (bool)data[5],
                    IsReady = (bool)data[6],
                    PlayerStatus = (PlayerStatuses)data[7],
                };

                if (UsersDataDic.Keys.Contains(id) == false)
                {
                    UsersDataDic.Add(id, temp);
                    OnPlayerJoiend?.Invoke(id);
                }
                else
                    UsersDataDic[id] = temp;
            }
            else
            {
                PlayerInfo.Id = id;
                PlayerInfo.UserKills = (int)data[0];
                PlayerInfo.UserDeaths = (int)data[1];
                PlayerInfo.InTotalUserKills = (int)data[2];
                PlayerInfo.InTotalUserDeaths = (int)data[3];
                PlayerInfo.Xp = (int)data[4];
                PlayerInfo.HaveRegisterd = (bool)data[5];
                PlayerInfo.IsReady = (bool)data[6];
                PlayerInfo.PlayerStatus = (PlayerStatuses)data[7];

                if (!HasPlayerRankDataRecived)
                    HasPlayerRankDataRecived = true;
            }
        }

        public static void UpdatePlayerStatus(PlayerStatuses status)
        {
            //Info.PlayerStatus = status;
            TriggerServerEvent("IRV:SV:clientMatchStatus", (int)status);
        }

        public static void IsReady(bool toggle)
        {
            TriggerServerEvent("IRV:SV:clientReady", toggle);
            //Info.IsReady = toggle;
        }

        /// <summary>
        /// Save skin values <see cref="PedInfo"/> that in use of player into <see cref="PlayerInfo"/>
        /// </summary>
        /// <param name="modelName">save data to kvm</param>
        public static void SavePlayerSkinLoadOutDetails(bool SaveData = false)
        {
            int ped = Game.PlayerPed.Handle;
            uint model = (uint)GetEntityModel(ped);

            var drawables = new Dictionary<int, int>();
            var drawableTextures = new Dictionary<int, int>();
            for (var i = 0; i < 21; i++)
            {
                int drawable = GetPedDrawableVariation(ped, i);
                int textureVariation = GetPedTextureVariation(ped, i);
                drawables.Add(i, drawable);
                drawableTextures.Add(i, textureVariation);
            }
            PlayerInfo.Loadout.Skin.SkinInfo.DrawableVariations = drawables;
            PlayerInfo.Loadout.Skin.SkinInfo.DrawableVariationTextures = drawableTextures;

            var props = new Dictionary<int, int>();
            var propTextures = new Dictionary<int, int>();
            // Loop through all prop variations.
            for (var i = 0; i < 21; i++)
            {
                int prop = GetPedPropIndex(ped, i);
                int propTexture = GetPedPropTextureIndex(ped, i);
                props.Add(i, prop);
                propTextures.Add(i, propTexture);
            }

            PlayerInfo.Loadout.Skin.SkinInfo.Props = props;
            PlayerInfo.Loadout.Skin.SkinInfo.PropTextures = propTextures;
            PlayerInfo.Loadout.Skin.Version = 1;

            if (SaveData)
                SaveLoadOut();
        }

        /// <summary>
        /// save <see cref="PlayerInfo"/> to kvm
        /// </summary>
        public static void SaveLoadOut()
        {
            string json = JsonConvert.SerializeObject(PlayerInfo.Loadout);
            bool result = CommonFunctions.SaveJsonData("IRV_dm_loadout_version_1_1", json, true);
            if (result)
                BaseUI.ShowNotfi("Loadout Saved!", true, flashColor: new ColorFormat() { Red = 0, Alpha = 255, Blue = 0, Green = 255 });
            else
                BaseUI.ShowNotfi("Failed to save loadout!", true, flashColor: new ColorFormat() { Red = 255, Alpha = 255, Blue = 0, Green = 0 });
        }

        /// <summary>
        /// load saved data from kvm into <see cref="PlayerInfo"/>
        /// </summary>
        /// <param name="setSkin">set skin after loading the data</param>
        public static async Task LoadPlayerLoadOut(bool setSkin = false)
        {
            CleanUpData();
            string json = CommonFunctions.GetJsonData("IRV_dm_loadout_version_1_1");
            if (json != null)
            {
                PlayerLoadout loadout = (PlayerLoadout)JsonConvert.DeserializeObject(json, typeof(PlayerLoadout));

                while (!HasPlayerRankDataRecived)
                {
                    await Delay(5);
                }

                if (loadout.Skin.ModelName.ToUpper() != DefaultPlayerLoadout.Skin.ModelName.ToUpper())
                {
                    if (Unlockables.Data.DoesUnlockbleExist(loadout.Skin))
                    {
                        //find it 
                        PedFormat ped = Unlockables.Data.AllUnlockablePeds.Find(p => p.Id.ToUpper() == loadout.Skin.Id.ToUpper());

                        if (ped.MaxArmour == loadout.Skin.MaxArmour &&
                            ped.MaxHealth == loadout.Skin.MaxHealth &&
                            ped.Stamina == loadout.Skin.Stamina &&
                            ped.Accuracy == loadout.Skin.Accuracy&& 
                            ped.Strength == loadout.Skin.Strength)
                        {

                            //cheak if not player unlocked loadout items from saved data
                            if (!Unlockables.Data.DoesPlayerUnlockedItem(loadout.Skin))
                            {
                                //set defualt skin
                                Debug.WriteLine($"player rank didnt meet unlockble [{loadout.Skin.LableName}]! we have to set the defualt.");
                                loadout.Skin = DefaultPlayerLoadout.Skin;
                            }
                        }
                        else //if the values somehow changed for this model then dont let the player have it
                        {
                            Debug.WriteLine($"player skin loaudout didnt match unlockble [{loadout.Skin.LableName}]! we have to set the defualt.");
                            loadout.Skin = DefaultPlayerLoadout.Skin;
                        }
                    }
                    else
                    {
                        //if we dont have this item then set the defualt
                        Debug.WriteLine($"coldent find player skin loaudout! we have to set the defualt.");
                        loadout.Skin = DefaultPlayerLoadout.Skin;
                    }
                }

                loadout.PrimaryWeapon = CheakWeaponLoadOut(loadout.PrimaryWeapon, DefaultPlayerLoadout.PrimaryWeapon);
                loadout.SecondaryWeapon = CheakWeaponLoadOut(loadout.SecondaryWeapon, DefaultPlayerLoadout.SecondaryWeapon);
                loadout.Equipment = CheakWeaponLoadOut(loadout.Equipment, DefaultPlayerLoadout.Equipment);

                PlayerInfo.Loadout = loadout;
                BaseUI.SetBaseHudColors(loadout.Skin.HudColor);
                MainDebug.WriteDebug("we have just loaded player loadout!");
            }
            else
            {
                PlayerInfo.Loadout = DefaultPlayerLoadout;
                MainDebug.WriteDebug("couldent find loadout data so we loaded default values");
            }

            OnPlayerSkinLoaded?.Invoke();

            if (setSkin)
            {
                if (PlayerInfo.Loadout.Skin.Version != -1)
                    await PlayerController.SetSkin(PlayerInfo.Loadout.Skin.ModelName, PlayerInfo.Loadout.Skin.SkinInfo);
                else
                    await PlayerController.SetSkin(PlayerInfo.Loadout.Skin.ModelName);

                MainDebug.WriteDebug("we have just set player skin");
            }
        }

        private static void CleanUpData()
        {
            string json = CommonFunctions.GetJsonData("IRV_dm_loadout");
            string json1 = CommonFunctions.GetJsonData("IRV_dm_loadout_version_1");
            if (json != null)
            {
                CommonFunctions.DeleteSavedStorageItem("IRV_dm_loadout");
            }
            if(json1 != null)
            {
                CommonFunctions.DeleteSavedStorageItem("IRV_dm_loadout_version_1");
            }
        }

        /// <summary>
        /// cheak if <paramref name="loadout"/> doest have any item out of range player unlocked items
        /// </summary>
        /// <param name="loadout"> load out to cheak</param>
        /// <param name="defualt">defualt loadout for changing valus in case</param>
        /// <returns>return changed or cheakded loadout</returns>
        private static WeaponLoadout CheakWeaponLoadOut(WeaponLoadout loadout, WeaponLoadout defualt)
        {
            if (loadout.WeaponName.ToUpper() != defualt.WeaponName.ToUpper())
            {
                //find wep load out unlockble 
                Unlockable wep = null;
                foreach (var unlockable in Unlockables.Data.GetUnlockables)
                {
                    if (unlockable.ModelName.ToUpper() == loadout.WeaponName.ToUpper())
                    {
                        wep = unlockable;
                        break;
                    }
                }

                //if we find it
                if (wep != null)
                {
                    WeaponFormat weapon = Unlockables.Data.UnlockableWeapons.Find(p => p.ModelName.ToUpper() == loadout.WeaponName.ToUpper());

                    if (loadout.MaxAmount == weapon.MaxAmmo)
                    {
                        if (!Unlockables.Data.DoesPlayerUnlockedItem(wep))
                        {
                            //set defualt wep with its all component
                            Debug.WriteLine($"player rank didnt meet unlockble {wep.LableName}! we have to set the defualt.");
                            loadout = defualt;
                        }
                        else
                        {
                            MainDebug.WriteDebug($"were good player unlocked this item [{wep.LableName}]");
                        }
                    }
                    else
                    {
                        //set the defualt
                        Debug.WriteLine($"player weapon loadout values didnt match with unlockble {wep.LableName}! we have to set the defualt.");
                        loadout = defualt;
                    }
                }
                else
                {
                    //set the defualt
                    Debug.WriteLine($"coludnt find player weapon loadout! we have to set the defualt.");
                    loadout = defualt;
                }
            }
            else
            {
                //were good let player have it
            }

            //ckeak every weapon component
            //making a new list of component for setting the in the end
            List<string> validComponents = new List<string>();
            //if component list exist
            if (loadout.WeaponComponent != null)
            {
                //add the items
                validComponents.AddRange(loadout.WeaponComponent);


                foreach (var item in loadout.WeaponComponent)
                {
                    Unlockable component = null;
                    foreach (var unlockable in Unlockables.Data.GetUnlockables)
                    {
                        if (item.ToUpper() == unlockable.ModelName.ToUpper())
                        {
                            component = unlockable;
                        }
                    }

                    //if find it
                    if (component != null)
                    {
                        if (!Unlockables.Data.DoesPlayerUnlockedItem(component))
                        {
                            //remove component
                            Debug.WriteLine($"player rank didnt meet unlockble {component.LableName}! we have to remove item.");
                            validComponents.Remove(item);
                        }
                        else
                        {
                            MainDebug.WriteDebug($"were good player unlocked this item [{component.LableName}]");
                        }
                    }
                    else
                    {
                        //remove component
                        validComponents.Remove(item);
                    }
                }
            }
            loadout.WeaponComponent = validComponents;

            //ckeak for wep tint
            //cheak if its not the defualt 0
            if (loadout.TintIndex != 0)
            {
                //find it
                Unlockable tint = null;
                foreach (var item in Unlockables.Data.AllUnlockableWeaponComponent)
                {
                    if (item.ModelName == loadout.TintIndex.ToString() && item.IdType == "UNLOCKBLE_WEAPON_COMPONENT" && item.TargetWeapon.ToUpper() == loadout.WeaponName.ToUpper())
                    {
                        tint = item;
                        break;
                    }
                }

                //if we had it
                if (tint != null)
                {
                    if (!Unlockables.Data.DoesPlayerUnlockedItem(tint))
                    {
                        //set the defualt
                        Debug.WriteLine($"player rank didnt meet unlockble weapon Tint! we have to remove item.");
                        loadout.TintIndex = 0;
                    }
                    else
                    {
                        MainDebug.WriteDebug($"were good player unlocked this tint [{tint.LableName}] for its weapon {loadout.WeaponName}");
                    }
                }
                else
                {
                    //we dont have it set the defualt
                    loadout.TintIndex = 0;
                }
            }

            return loadout;
        }

        /// <summary>
        /// update <see cref="PlayerInfo"/> with new ped and resting <see cref="PedInfo"/> of it to emty values
        /// </summary>
        /// <param name="newPed"></param>
        public static void UpdatePlayerSkin(PedFormat newPed)
        {
            var skin = PlayerInfo.Loadout.Skin;
            skin.SkinInfo = new PedInfo();
            skin.Version = -1;
            skin.Category = newPed.Category;
            skin.Gender = newPed.Gender;
            skin.IdType = newPed.IdType;
            skin.IsAnimal = newPed.IsAnimal;
            skin.LableName = newPed.LableName;
            skin.ModelName = newPed.ModelName;
            skin.PedModel = newPed.PedModel;
            skin.Type = newPed.Type;
            skin.UnlockTarget = newPed.UnlockTarget;
            skin.HudColor = newPed.HudColor;
            skin.MaxArmour = newPed.MaxArmour;
            skin.MaxHealth = newPed.MaxHealth;
            skin.Stamina = newPed.Stamina;
            skin.Accuracy = newPed.Accuracy;
            skin.Strength = newPed.Strength;
            skin.ChoosenScenario = newPed.ChoosenScenario;
            skin.Id = newPed.Id;
        }
    }
}
