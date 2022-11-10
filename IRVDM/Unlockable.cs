using CitizenFX.Core;
using NativeUI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using static CitizenFX.Core.Native.API;

namespace IRVDM
{
    public class Unlockable
    {
        public string IdType { get; set; } = "";
        public string ModelName { get; set; } = "";
        public string LableName { get; set; } = "";
        public string Id { get; set; }
        public UnlockablesType Type { get; set; } = UnlockablesType.Xp;
        public int UnlockTarget { get; set; } = 0;
        public bool IsAddon { get; set; } = false;
        public string DependOnId { get; set; } = "";
    }

    public enum UnlockablesType
    {
        Xp,
        Kill,
        Level,
    }

    internal class UnlockablesData
    {
        public readonly Dictionary<string, List<PedFormat>> UnlockablePeds = new Dictionary<string, List<PedFormat>>();
        /// <summary>
        /// Tkey is TargetModelName and Tvalues are component
        /// </summary>
        public readonly Dictionary<string, List<WeaponItem>> UnlockableWeaponComponentDic = new Dictionary<string, List<WeaponItem>>();
        public readonly List<PedFormat> AllUnlockablePeds = new List<PedFormat>();
        public readonly List<WeaponFormat> UnlockableWeapons = new List<WeaponFormat>();
        public readonly List<Unlockable> GetUnlockables = new List<Unlockable>();
        public readonly List<WeaponItem> AllUnlockableWeaponComponent = new List<WeaponItem>();

        public UnlockablesData()
        {
            try
            {
                string pedJsonData = LoadResourceFile(GetCurrentResourceName(), "config/UnlockingRulesPed.json");
                List<PedFormat> peds = (List<PedFormat>)JsonConvert.DeserializeObject(pedJsonData, typeof(List<PedFormat>));
                foreach (var item in peds)
                {
                    if (!UnlockablePeds.Keys.Contains(item.Category))
                        UnlockablePeds[item.Category] = new List<PedFormat>();

                    UnlockablePeds[item.Category].Add(item);
                    AllUnlockablePeds.Add(item);
                    GetUnlockables.Add(item);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Somthing went wrong when trying to load skins data let the server owner know of this problem[{ e.Message}]");
            }

            try
            {
                string wepdata = LoadResourceFile(GetCurrentResourceName(), "config/UnlockingRulesWeapons.json");
                UnlockableWeapons = (List<WeaponFormat>)JsonConvert.DeserializeObject(wepdata, typeof(List<WeaponFormat>));
                GetUnlockables.AddRange(UnlockableWeapons);
                //foreach (var item in AllWeapons)
                //{
                //    if (!UnlockableWeapons.Keys.Contains(item.Category))
                //        UnlockableWeapons[item.Category] = new List<WeaponFormat>();

                //    UnlockableWeapons[item.Category].Add(item);
                //}
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Somthing went wrong when trying to load skins data let the server owner know of this problem[{ e.Message}]");
            }

            try
            {
                string wepdata = LoadResourceFile(GetCurrentResourceName(), "config/UnlockingRules_Weapon_Components.json");
                AllUnlockableWeaponComponent = (List<WeaponItem>)JsonConvert.DeserializeObject(wepdata, typeof(List<WeaponItem>));
                GetUnlockables.AddRange(AllUnlockableWeaponComponent);
                foreach (var item in AllUnlockableWeaponComponent)
                {
                    if (!UnlockableWeaponComponentDic.Keys.Contains(item.TargetWeapon.ToUpper()))
                        UnlockableWeaponComponentDic.Add(item.TargetWeapon.ToUpper(), new List<WeaponItem>());

                    UnlockableWeaponComponentDic[item.TargetWeapon.ToUpper()].Add(item);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Somthing went wrong when trying to load Weapon_Components data let the server owner know of this problem[{ e.Message}]");
            }
        }

        public bool DoesPlayerUnlockedItem(Unlockable unlockable)
        {
            if (!string.IsNullOrEmpty(unlockable.DependOnId))
            {
                Unlockable dependency = GetUnlockbleFromId(unlockable.DependOnId);

                if (dependency != null)
                {
                    if (!DoesPlayerUnlockedItem(dependency))
                    {
                        return false;
                    }
                }
            }

            switch (unlockable.Type)
            {
                case UnlockablesType.Xp:
                    if (SyncData.PlayerInfo.Xp >= unlockable.UnlockTarget)
                        return true;
                    break;
                case UnlockablesType.Kill:
                    if (SyncData.PlayerInfo.InTotalUserKills >= unlockable.UnlockTarget)
                        return true;
                    break;
                case UnlockablesType.Level:
                    if (RankManager.GetLevelWithXp(SyncData.PlayerInfo.Xp) >= unlockable.UnlockTarget)
                        return true;
                    break;
            }
            return false;
        }

        public bool DoesUnlockbleExist(Unlockable unlockable)
        {
            var temp = GetUnlockbleFromId(unlockable.Id);

            if (temp != null)
            {
                if (temp.IdType == unlockable.IdType && temp.ModelName.ToUpper() == unlockable.ModelName.ToUpper()
            && temp.Type == unlockable.Type)
                {
                    return true;
                }
            }

            return false;
        }

        public Unlockable GetUnlockbleFromId(string id)
        {
            foreach (var item in GetUnlockables)
            {
                if (id.ToUpper() == item.Id.ToUpper())
                {
                    return item;
                }
            }

            return null;
        }
    }

    internal class ItemUnlockNotifFormat
    {
        public string Message { get; set; }

        public virtual void Draw()
        {
            BigMessageThread.MessageInstance.ShowMissionPassedMessage(Message);
        }
    }

    internal class WeaponUnlockNotif : ItemUnlockNotifFormat
    {
        private readonly WeaponFormat Weapon;

        public WeaponUnlockNotif(WeaponFormat weapon)
        {
            Weapon = weapon;
        }

        public override void Draw()
        {
            string color = "~r~";
            switch (Weapon.Slot)
            {
                case WepLoadOut.Slot01:
                    color = "~r~";
                    break;
                case WepLoadOut.Slot02:
                    color = "~g~";
                    break;
                case WepLoadOut.Slot03:
                    color = "~b~";
                    break;
            }

            BigMessageThread.MessageInstance.ShowWeaponPurchasedMessage(Message, color + Weapon.LableName, (uint)GetHashKey(Weapon.ModelName));
            PlaySoundFrontend(-1, "MEDAL_UP", "HUD_MINI_GAME_SOUNDSET", true);
            CitizenFX.Core.UI.Screen.Effects.Start(CitizenFX.Core.UI.ScreenEffect.FocusIn, 1000);
        }
    }

    internal class WeaponItemUnlockNotif : ItemUnlockNotifFormat
    {
        public WeaponItem WeaponItem;

        public WeaponItemUnlockNotif(WeaponItem item)
        {
            WeaponItem = item;
        }
        public override void Draw()
        {
            BigMessageThread.MessageInstance.ShowWeaponPurchasedMessage(Message, "~r~" + WeaponItem.LableName, (uint)GetHashKey(WeaponItem.TargetWeapon));
            PlaySoundFrontend(-1, "MEDAL_UP", "HUD_MINI_GAME_SOUNDSET", true);
            CitizenFX.Core.UI.Screen.Effects.Start(CitizenFX.Core.UI.ScreenEffect.FocusIn, 1000);
        }
    }

    internal class PedUnlockNotfi : ItemUnlockNotifFormat
    {
        private readonly PedFormat Ped;

        public PedUnlockNotfi(PedFormat ped)
        {
            Ped = ped;
        }

        public override void Draw()
        {
            BigMessageThread.MessageInstance.ShowSimpleShard(Message, "~w~" + Ped.LableName);
            PlaySoundFrontend(-1, "MEDAL_UP", "HUD_MINI_GAME_SOUNDSET", true);
            CitizenFX.Core.UI.Screen.Effects.Start(CitizenFX.Core.UI.ScreenEffect.FocusIn, 1000);
        }
    }

    internal class Unlockables : BaseScript
    {
        public static event Action<WeaponFormat> OnWeaponUnlocked;
        public static event Action<PedFormat> OnPedUnlocked;
        public static event Action<WeaponItem> OnWeaponComponentUnlocked;
        public static readonly UnlockablesData Data = new UnlockablesData();
        private bool IsAnyNotifOnGoing = false;

        public Unlockables()
        {
            RankManager.OnPlayerGetXp += RankManager_OnPlayerGetXp;
            RankManager.OnPlayerLevelUp += RankManager_OnPlayerLevelUp;
            GameEventManager.OnPlayerKillPlayer += GameEventManager_OnPlayerKillPlayer;
            OnWeaponUnlocked += new Action<WeaponFormat>(p => Debug.WriteLine($"weapon has unlocked {p.ModelName}"));
            OnPedUnlocked += new Action<PedFormat>(p => Debug.WriteLine($"Ped has unlocked {p.LableName}"));
        }

        private async void GameEventManager_OnPlayerKillPlayer(int arg1, uint wepHash, bool arg3, bool arg4)
        {
            await Delay(20);
            TriggerEventsBasedOnUnlockble(SyncData.PlayerInfo.InTotalUserKills, SyncData.PlayerInfo.InTotalUserKills - 1, UnlockablesType.Kill);
        }

        private void RankManager_OnPlayerLevelUp(int newLevel, int oldLevel, int lastXpAdded)
        {
            TriggerEventsBasedOnUnlockble(newLevel, oldLevel, UnlockablesType.Level);
        }

        private void RankManager_OnPlayerGetXp(int amount, int newXp, int oldXp, string reason)
        {
            TriggerEventsBasedOnUnlockble(newXp, oldXp, UnlockablesType.Xp);
        }

        private async void ShowUnlockNotif(params ItemUnlockNotifFormat[] itemUnlock)
        {
            if (!IsAnyNotifOnGoing)
                IsAnyNotifOnGoing = true;

            foreach (var item in itemUnlock)
            {
                item.Draw();
                await Delay(5500);
            }

            IsAnyNotifOnGoing = false;
        }

        /// <summary>
        /// Will trigger events for unlockble items in the range of <paramref name="startUnlockRange"/> to <paramref name="endUnlockRange"/> with this <paramref name="type"/> 
        /// </summary>
        /// <param name="endUnlockRange"></param>
        /// <param name="startUnlockRange"></param>
        /// <param name="type"></param>
        private void TriggerEventsBasedOnUnlockble(int endUnlockRange, int startUnlockRange, UnlockablesType type)
        {
            List<Unlockable> unlockables = Data.GetUnlockables.Where(p => p.Type == type).ToList();
            List<Unlockable> newOnes = new List<Unlockable>();
            List<ItemUnlockNotifFormat> notifs = new List<ItemUnlockNotifFormat>();

            foreach (var item in unlockables)
            {
                //if its in range of new unlocke[type] value 
                if (item.UnlockTarget > startUnlockRange && item.UnlockTarget <= endUnlockRange)
                {
                    //if its unloked 
                    if (Data.DoesPlayerUnlockedItem(item))
                    {
                        //you can add it
                        newOnes.Add(item);

                        //search for other items depend on this item 
                        foreach (var depenentItem in Data.GetUnlockables)
                        {
                            //if we find it
                            if (depenentItem.DependOnId == item.Id)
                            {
                                //if its unlocked 
                                if (Data.DoesPlayerUnlockedItem(depenentItem))
                                {
                                    //you can add it
                                    newOnes.Add(depenentItem);
                                }
                            }
                        }
                    }
                }
            }

            //then loop throw all new items 
            foreach (var item in newOnes)
            {
                //if its a ped
                if (item.IdType == "UNLOCKABLE_PED")
                {
                    PedFormat pedFormat = Data.AllUnlockablePeds.Find(p => p.Id == item.Id);
                    OnPedUnlocked?.Invoke(pedFormat);
                    notifs.Add(new PedUnlockNotfi(pedFormat) { Message = $"~g~Skin Unlocked" });
                }

                //weapon
                else if (item.IdType == "UNLOCKABLE_WEAPON")
                {
                    WeaponFormat weaponFormat = Data.UnlockableWeapons.Find(p => p.Id == item.Id);
                    OnWeaponUnlocked?.Invoke(weaponFormat);
                    notifs.Add(new WeaponUnlockNotif(weaponFormat) { Message = "Weapon Unlocked" });
                }

                //weapon item
                else if (item.IdType == "UNLOCKBLE_WEAPON_COMPONENT" || item.IdType == "UNLOCKBLE_WEAPON_TINTS")
                {
                    WeaponItem weaponItem = Data.AllUnlockableWeaponComponent.Find(p => p.Id == item.Id);
                    OnWeaponComponentUnlocked?.Invoke(weaponItem);
                    notifs.Add(new WeaponItemUnlockNotif(weaponItem) { Message = "Item Unlocked" });
                }
            }

            //show all new items for player
            ShowUnlockNotif(notifs.ToArray());
        }
    }
}
