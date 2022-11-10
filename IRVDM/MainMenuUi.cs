using CitizenFX.Core;
using CitizenFX.Core.UI;
using MenuAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using static CitizenFX.Core.Native.API;
using static IRVDM.CommonFunctions;
using static IRVDM.SyncData;

namespace IRVDM
{
    internal class MainMenuUi
    {
        private readonly Menu MainMenu = new Menu("", "Main Menu");
        private readonly Menu WeaponLoadoutMenu = new Menu("", "Loadout");
        private readonly Menu EquipmentsWeaponMenu = new Menu("", "Equipment classes");
        private readonly Menu SeconderyWeaponMenu = new Menu("", "Secondery weapon classes");
        private readonly Menu PrimaryWeaponMenu = new Menu("", "Primary weapon classes");
        private readonly Menu SkinMenu = new Menu("", "Skin");
        private readonly Menu SkinCustomize = new Menu("", "Customize");
        private readonly Menu EffectMenu = new Menu("", "Effects");
        private readonly Dictionary<string, Menu> SkinSubMenus = new Dictionary<string, Menu>();

        private readonly Dictionary<MenuListItem, int> drawablesMenuListItems = new Dictionary<MenuListItem, int>();
        private readonly Dictionary<MenuListItem, int> propsMenuListItems = new Dictionary<MenuListItem, int>();

        public MainMenuLocation MainMenuLocation { get; set; }
        public bool IsShowingLoadOut { get; private set; } = false;

        private readonly UnlockablesData Unlockables = new UnlockablesData();

        public Camera MenuCam;
        public MenuCams MenuCamStatus { get; private set; } = MenuCams.None;
        public PlayerInMenuStatus MenuStatus { get; private set; } = PlayerInMenuStatus.Main;

        //public readonly string OnSkinChoosenScenario = "WORLD_HUMAN_CHEERING";
        public bool IsPedDoingOnSkinChoosenScenario = false;


        public Game.WeaponHudStats PrimaryWeaponStates;
        public Game.WeaponHudStats SeconderyWeaponStats;
        public Game.WeaponHudStats EquipmentsWeaponStats;

        private int ReadySpam = 0;
        private readonly int SpamTime = 8000;
        private int GameTimer = -1;

        private readonly WeaponData weaponData = new WeaponData();

        public WepLoadOut SelectedUiItem { get; private set; } = WepLoadOut.Slot01;

        public Prop PrimaryProp { get; private set; }
        public Prop SeconderyProp { get; private set; }
        public Prop EquipmentProp { get; private set; }

        public event Action<string, string, WepLoadOut> OnWeaponLoadOutChanged;
        public event Action<string, string> OnSkinChanged;

        /// <summary>
        /// Tkey is Slots and Tvalue is Dictionary of NameOfcategory(Tkey) and MenuOfCategory(Tvalue)
        /// </summary>
        private readonly Dictionary<string, Dictionary<string, Menu>> WeaponSlotSubMenus = new Dictionary<string, Dictionary<string, Menu>>()
        {
            //locked on 3 slot (as the other part of scripts have 3 slot for now)
            [WepLoadOut.Slot01.ToString()] = new Dictionary<string, Menu>(),
            [WepLoadOut.Slot02.ToString()] = new Dictionary<string, Menu>(),
            [WepLoadOut.Slot03.ToString()] = new Dictionary<string, Menu>(),
        };
        /// <summary>
        /// tKey is ModelName and Tvale is Menu of it
        /// </summary>
        private readonly Dictionary<string, Menu> WeaponMenus = new Dictionary<string, Menu>();
        private readonly List<Menu> TintMenus = new List<Menu>();

        public enum PlayerInMenuStatus
        {
            Main,
            ChengingLoadOut,
            ChengingSkin,
            ChangingEffect
        }

        public enum MenuCams
        {
            None,
            MainMenuCamera,
            ShowLoadOutCamera,
            ChangeLoadOutCamera,
            ChangeSkinCamera01,
            ChangeSkinCamera02,
            VoteMapCamera,
        }

        public MainMenuUi()
        {
            OnWeaponLoadOutChanged += new Action<string, string, WepLoadOut>((newModel, oldModel, slot) =>
            {
                Debug.WriteLine($"new {newModel}, oldone {oldModel}");
                foreach (var category in WeaponSlotSubMenus[slot.ToString()])
                {
                    foreach (var btns in category.Value.GetMenuItems())
                    {
                        if (btns.ItemData.ModelName == newModel)
                        {
                            if (btns.RightIcon != MenuItem.Icon.TICK)
                                btns.RightIcon = MenuItem.Icon.TICK;
                        }

                        if (btns.ItemData.ModelName == oldModel)
                        {
                            if (btns.RightIcon == MenuItem.Icon.TICK)
                                btns.RightIcon = MenuItem.Icon.NONE;
                        }
                    }
                }
            });

            OnSkinChanged += (newPed, oldPed) =>
            {
                foreach (Menu menu in SkinSubMenus.Values)
                {
                    foreach (var btns in menu.GetMenuItems())
                    {
                        if (newPed == btns.ItemData.ModelName)
                        {
                            if (btns.RightIcon != MenuItem.Icon.TICK)
                            {
                                btns.RightIcon = MenuItem.Icon.TICK;
                            }
                        }

                        if (oldPed == btns.ItemData.ModelName)
                        {
                            if (btns.RightIcon == MenuItem.Icon.TICK)
                            {
                                btns.RightIcon = MenuItem.Icon.NONE;
                            }
                        }
                    }
                }
            };

            //saving the data every time skin menu closes
            SkinMenu.OnMenuClose += p => SavePlayerSkinLoadOutDetails(true);

            EffectMenu.OnMenuClose += P => SaveLoadOut();
            SkinCustomize.OnMenuOpen += p => RefreshCustomizationMenu();

            //creating MainMenuBtns
            MenuCheckboxItem readyBtn = new MenuCheckboxItem("Ready?", "Whener your ready!", PlayerInfo.IsReady);
            MenuItem loadoutMenuBtn = new MenuItem("Change Loadout", "set your loudout before match !")
            {
                RightIcon = MenuItem.Icon.GUN,
            };
            MenuItem exitBtn = new MenuItem("Exit", "Exit")
            {
                RightIcon = MenuItem.Icon.WARNING
            };
            MenuItem spectBtn = new MenuItem("Spector Mode", "spect in match players");
            MenuItem skinMenuBtn = new MenuItem("Change Skin", "set your skin")
            {
                RightIcon = MenuItem.Icon.CLOTHING,
            };
            MenuItem effectMenuBtn = new MenuItem("Change Effect", "Change your effects onSpawn or kill")
            {
                RightIcon = MenuItem.Icon.STAR
            };

            //loadot menu
            MenuItem primaryBtn = new MenuItem("Primary");
            MenuItem seconderyBtn = new MenuItem("Secondary");
            MenuItem EquipmentsBtn = new MenuItem("Equipment");

            //skin customize
            MenuItem skinCusBtn = new MenuItem("Customize")
            {
                RightIcon = MenuItem.Icon.CLOTHING
            };
            SkinMenu.AddMenuItem(skinCusBtn);
            MenuController.AddMenu(SkinCustomize);
            MenuController.AddSubmenu(SkinMenu, SkinCustomize);
            RefreshCustomizationMenu();

            #region ped drawable list changes
            // Manage list changes.
            SkinCustomize.OnListIndexChange += (sender, item, oldListIndex, newListIndex, itemIndex) =>
            {
                if (drawablesMenuListItems.ContainsKey(item))
                {
                    int drawableID = drawablesMenuListItems[item];
                    SetPedComponentVariation(Game.PlayerPed.Handle, drawableID, newListIndex, 0, 0);
                }
                else if (propsMenuListItems.ContainsKey(item))
                {
                    int propID = propsMenuListItems[item];
                    if (newListIndex == 0)
                    {
                        SetPedPropIndex(Game.PlayerPed.Handle, propID, newListIndex - 1, 0, false);
                        ClearPedProp(Game.PlayerPed.Handle, propID);
                    }
                    else
                    {
                        SetPedPropIndex(Game.PlayerPed.Handle, propID, newListIndex - 1, 0, true);
                    }
                    if (propID == 0)
                    {
                        int component = GetPedPropIndex(Game.PlayerPed.Handle, 0);      // helmet index
                        int texture = GetPedPropTextureIndex(Game.PlayerPed.Handle, 0); // texture
                        int compHash = GetHashNameForProp(Game.PlayerPed.Handle, 0, component, texture); // prop combination hash
                        if (N_0xd40aac51e8e4c663((uint)compHash) > 0) // helmet has visor. 
                        {
                            if (!IsHelpMessageBeingDisplayed())
                            {
                                BeginTextCommandDisplayHelp("TWOSTRINGS");
                                AddTextComponentSubstringPlayerName("Hold ~INPUT_SWITCH_VISOR~ to flip your helmet visor open or closed");
                                AddTextComponentSubstringPlayerName("when on foot or on a motorcycle and when vMenu is closed.");
                                EndTextCommandDisplayHelp(0, false, true, 6000);
                            }
                        }
                    }

                }
            };

            // Manage list selections.
            SkinCustomize.OnListItemSelect += (sender, item, listIndex, itemIndex) =>
            {
                if (drawablesMenuListItems.ContainsKey(item)) // drawable
                {
                    int currentDrawableID = drawablesMenuListItems[item];
                    int currentTextureIndex = GetPedTextureVariation(Game.PlayerPed.Handle, currentDrawableID);
                    int maxDrawableTextures = GetNumberOfPedTextureVariations(Game.PlayerPed.Handle, currentDrawableID, listIndex) - 1;

                    if (currentTextureIndex == -1)
                        currentTextureIndex = 0;

                    int newTexture = currentTextureIndex < maxDrawableTextures ? currentTextureIndex + 1 : 0;

                    SetPedComponentVariation(Game.PlayerPed.Handle, currentDrawableID, listIndex, newTexture, 0);
                }
                else if (propsMenuListItems.ContainsKey(item)) // prop
                {
                    int currentPropIndex = propsMenuListItems[item];
                    int currentPropVariationIndex = GetPedPropIndex(Game.PlayerPed.Handle, currentPropIndex);
                    int currentPropTextureVariation = GetPedPropTextureIndex(Game.PlayerPed.Handle, currentPropIndex);
                    int maxPropTextureVariations = GetNumberOfPedPropTextureVariations(Game.PlayerPed.Handle, currentPropIndex, currentPropVariationIndex) - 1;

                    int newPropTextureVariationIndex = currentPropTextureVariation < maxPropTextureVariations ? currentPropTextureVariation + 1 : 0;
                    SetPedPropIndex(Game.PlayerPed.Handle, currentPropIndex, currentPropVariationIndex, newPropTextureVariationIndex, true);
                }
            };
            #endregion


            //create items for skin menu
            CreateSkinSubItems(SkinMenu, Unlockables.UnlockablePeds);

            WeaponLoadoutMenu.AddMenuItem(primaryBtn);
            WeaponLoadoutMenu.AddMenuItem(seconderyBtn);
            WeaponLoadoutMenu.AddMenuItem(EquipmentsBtn);

            //getting weapon loadout stats
            Game.GetWeaponHudStats((uint)GetHashKey(PlayerInfo.Loadout.PrimaryWeapon.WeaponName), ref PrimaryWeaponStates);
            Game.GetWeaponHudStats((uint)GetHashKey(PlayerInfo.Loadout.SecondaryWeapon.WeaponName), ref SeconderyWeaponStats);
            Game.GetWeaponHudStats((uint)GetHashKey(PlayerInfo.Loadout.Equipment.WeaponName), ref EquipmentsWeaponStats);

            //LoadoutMenu.SetWeaponStats(0.5f, PrimaryWeaponStates.hudSpeed / 100, PrimaryWeaponStates.hudAccuracy / 100, PrimaryWeaponStates.hudRange / 100);
            WeaponLoadoutMenu.ShowWeaponStatsPanel = true;

            //priamryMenu            
            //creating submenus and btns 
            CreateBaseLoadOutMenuItems(PrimaryWeaponMenu, Unlockables.UnlockableWeapons);

            //seconderyMenu
            //creating submenus and btns 
            CreateBaseLoadOutMenuItems(SeconderyWeaponMenu, Unlockables.UnlockableWeapons);

            //EquipmentsMenu
            //creating submenus and btns 
            CreateBaseLoadOutMenuItems(EquipmentsWeaponMenu, Unlockables.UnlockableWeapons);

            #region LoadOutSubMenu eventHandelers

            foreach (var slotCategory in WeaponSlotSubMenus)
            {
                foreach (var CategoryMenus in slotCategory.Value)
                {
                    CategoryMenus.Value.OnIndexChange += (menu, oldItem, newItem, oldIndex, newIndex) =>
                    {
                        //if item is in loadout then give it with 1 ammo 
                        if (newItem.ItemData.ModelName == PlayerInfo.Loadout.PrimaryWeapon.WeaponName.ToUpper() ||
                        newItem.ItemData.ModelName == PlayerInfo.Loadout.SecondaryWeapon.WeaponName.ToUpper() ||
                        newItem.ItemData.ModelName == PlayerInfo.Loadout.Equipment.WeaponName.ToUpper())
                        {
                            EquipOnlyThisWeapon(newItem.ItemData.ModelName, 1);
                        }
                        else
                            EquipOnlyThisWeapon(newItem.ItemData.ModelName);

                        //changing weapon statePanle values 
                        Game.WeaponHudStats subTempHudStats = new Game.WeaponHudStats();
                        Game.GetWeaponHudStats((uint)GetHashKey(newItem.ItemData.ModelName), ref subTempHudStats);
                        CategoryMenus.Value.SetWeaponComponentStats((float)subTempHudStats.hudDamage / 100, (float)subTempHudStats.hudSpeed / 100, (float)subTempHudStats.hudAccuracy / 100, (float)subTempHudStats.hudRange / 100);
                    };

                    CategoryMenus.Value.OnMenuOpen += m =>
                    {
                        m.RefreshIndex();
                        EquipOnlyThisWeapon(CategoryMenus.Value.GetMenuItems()[0].ItemData.ModelName);

                        //setting defualt weaponhud state
                        Game.WeaponHudStats tempHudStats = new Game.WeaponHudStats();
                        Game.GetWeaponHudStats((uint)GetHashKey(CategoryMenus.Value.GetMenuItems()[0].ItemData.ModelName), ref tempHudStats);
                        CategoryMenus.Value.SetWeaponComponentStats((float)tempHudStats.hudDamage / 100, (float)tempHudStats.hudSpeed / 100, (float)tempHudStats.hudAccuracy / 100, (float)tempHudStats.hudRange / 100);
                        CategoryMenus.Value.ShowWeaponStatsPanel = true;
                    };
                }
            }

            PrimaryWeaponMenu.OnMenuClose += menu =>
            {
                EquipOnlyThisWeapon(PlayerInfo.Loadout.PrimaryWeapon.WeaponName);
            };

            SeconderyWeaponMenu.OnMenuClose += menu =>
            {
                EquipOnlyThisWeapon(PlayerInfo.Loadout.SecondaryWeapon.WeaponName);
            };

            EquipmentsWeaponMenu.OnMenuClose += menu =>
            {
                EquipOnlyThisWeapon(PlayerInfo.Loadout.Equipment.WeaponName);
            };

            #endregion
            #region Creating EachWeaponMenu
            foreach (var slot in WeaponSlotSubMenus)
            {
                foreach (var Category in slot.Value)
                {
                    foreach (MenuItem CategoryMenuItems in Category.Value.GetMenuItems())
                    {
                        string name = CategoryMenuItems.ItemData.ModelName;
                        string nameToPrint = CategoryMenuItems.ItemData.LableName;

                        Menu wepMenu = new Menu("", nameToPrint);
                        MenuItem selectWep = new MenuItem("Select")
                        {
                            ItemData = CategoryMenuItems.ItemData,
                            RightIcon = MenuItem.Icon.GUN
                        };


                        wepMenu.AddMenuItem(selectWep);
                        MenuController.AddSubmenu(Category.Value, wepMenu);
                        MenuController.AddMenu(wepMenu);

                        if (Unlockables.UnlockableWeaponComponentDic.Keys.Contains(name))
                        {
                            List<WeaponItem> weaponItems = Unlockables.UnlockableWeaponComponentDic[name].Where(p => p.IdType == "UNLOCKBLE_WEAPON_COMPONENT").ToList();
                            List<WeaponItem> WeaponTints = Unlockables.UnlockableWeaponComponentDic[name].Where(p => p.IdType == "UNLOCKBLE_WEAPON_TINTS").ToList();


                            if (WeaponTints.Count != 0)
                            {
                                MenuItem tintsMenuBtn = new MenuItem("Tints");
                                wepMenu.AddMenuItem(tintsMenuBtn);
                                Menu tintsMenu = new Menu("", "Tints");



                                wepMenu.OnItemSelect += new Menu.ItemSelectEvent((menu, item, index) =>
                                {
                                    if (item == tintsMenuBtn)
                                    {
                                        wepMenu.Visible = false;
                                        tintsMenu.OpenMenu();
                                    }
                                });

                                foreach (var item in WeaponTints)
                                {
                                    MenuItem tintsBtn = new MenuItem(item.LableName)
                                    {
                                        ItemData = item,
                                        Description = $"Unlock at ~r~{item.UnlockTarget} ~g~{item.Type}",
                                        RightIcon = MenuItem.Icon.LOCK,
                                        Enabled = false,
                                    };

                                    IRVDM.Unlockables.OnWeaponComponentUnlocked += new Action<WeaponItem>(p =>
                                    {
                                        if (p.Id == item.Id)
                                        {
                                            EnableNewUnlockableMenuItem(tintsBtn);
                                        }
                                    });

                                    tintsMenu.AddMenuItem(tintsBtn);
                                }

                                tintsMenu.OnItemSelect += new Menu.ItemSelectEvent((menu, menuItem, index) =>
                                {
                                    SetPedWeaponTintIndex(Game.PlayerPed.Handle, (uint)GetHashKey(menuItem.ItemData.TargetWeapon), Convert.ToInt32(menuItem.ItemData.ModelName));

                                    if (menuItem.RightIcon != MenuItem.Icon.TICK)
                                        menuItem.RightIcon = MenuItem.Icon.TICK;

                                    foreach (var tintBtn in menu.GetMenuItems())
                                    {
                                        if (tintBtn != menuItem)
                                        {
                                            if (tintBtn.RightIcon == MenuItem.Icon.TICK)
                                                tintBtn.RightIcon = MenuItem.Icon.NONE;
                                        }
                                    }
                                });

                                tintsMenu.OnMenuOpen += m =>
                                {
                                    foreach (var tintsBtn in m.GetMenuItems())
                                    {
                                        int tintIndex = Convert.ToInt32(tintsBtn.ItemData.ModelName);
                                        int weaponTintIndex = GetPedWeaponTintIndex(Game.PlayerPed.Handle, (uint)GetHashKey(tintsBtn.ItemData.TargetWeapon));

                                        if (tintsBtn.RightIcon == MenuItem.Icon.TICK)
                                        {
                                            if (tintIndex != weaponTintIndex)
                                                tintsBtn.RightIcon = MenuItem.Icon.NONE;
                                        }
                                        else
                                        {
                                            if (tintIndex == weaponTintIndex)
                                                tintsBtn.RightIcon = MenuItem.Icon.TICK;
                                        }
                                    }
                                };

                                TintMenus.Add(tintsMenu);
                                MenuController.AddMenu(tintsMenu);
                                MenuController.AddSubmenu(wepMenu, tintsMenu);
                            }

                            foreach (WeaponItem component in weaponItems)
                            {
                                MenuItem menuItem = new MenuItem(component.LableName)
                                {
                                    ItemData = component,
                                    Enabled = false,
                                    RightIcon = MenuItem.Icon.LOCK
                                };

                                foreach (var data in weaponData.weaponDatas)
                                {
                                    if (data.ModelName.ToUpper() == component.TargetWeapon.ToUpper())
                                    {
                                        foreach (var item in data.weaponComponents)
                                        {
                                            if (item.ModelName.ToUpper() == component.ModelName.ToUpper())
                                            {
                                                menuItem.Description = item.Description;
                                            }
                                        }
                                        break;
                                    }
                                }

                                menuItem.Description += $"~r~(Unlock with {component.UnlockTarget} ~w~{component.Type.ToString()}~r~)";

                                //unlock eventHandeler
                                IRVDM.Unlockables.OnWeaponComponentUnlocked += new Action<WeaponItem>(p =>
                                {
                                    if (p.Id == component.Id)
                                    {
                                        EnableNewUnlockableMenuItem(menuItem);
                                    }
                                });

                                wepMenu.AddMenuItem(menuItem);
                            }
                        }

                        WeaponMenus.Add(name, wepMenu);
                    }

                    Category.Value.OnItemSelect += new Menu.ItemSelectEvent((menu, menuItem, index) =>
                    {
                        string name = menuItem.ItemData.ModelName;
                        menu.CloseMenu();
                        WeaponMenus[name].OpenMenu();
                    });
                }
            }
            #region WeaponMenu eventHandelers

            foreach (var wepMenu in WeaponMenus)
            {
                wepMenu.Value.OnItemSelect += new Menu.ItemSelectEvent((menu, menuItem, index) =>
                {
                    if (menuItem.Text != "Select" && menuItem.Text != "Tints")
                    {
                        WeaponItem comp = menuItem.ItemData;
                        if (HasPedGotWeaponComponent(Game.PlayerPed.Handle, (uint)GetHashKey(comp.TargetWeapon), (uint)GetHashKey(comp.ModelName)))
                        {
                            RemoveWeaponComponentFromPed(Game.PlayerPed.Handle, (uint)GetHashKey(comp.TargetWeapon), (uint)GetHashKey(comp.ModelName));

                            if (!HasPedGotWeaponComponent(Game.PlayerPed.Handle, (uint)GetHashKey(comp.TargetWeapon), (uint)GetHashKey(comp.ModelName)))
                            {
                                if (menuItem.RightIcon == MenuItem.Icon.TICK)
                                    menuItem.RightIcon = MenuItem.Icon.NONE;
                            }

                            BaseUI.ShowNotfi("Component removed.");
                        }
                        else
                        {
                            int ammo = GetAmmoInPedWeapon(Game.PlayerPed.Handle, (uint)GetHashKey(comp.TargetWeapon));

                            int clipAmmo = GetMaxAmmoInClip(Game.PlayerPed.Handle, (uint)GetHashKey(comp.TargetWeapon), false);
                            GetAmmoInClip(Game.PlayerPed.Handle, (uint)GetHashKey(comp.TargetWeapon), ref clipAmmo);

                            GiveWeaponComponentToPed(Game.PlayerPed.Handle, (uint)GetHashKey(comp.TargetWeapon), (uint)GetHashKey(comp.ModelName));

                            SetAmmoInClip(Game.PlayerPed.Handle, (uint)GetHashKey(comp.TargetWeapon), clipAmmo);

                            SetPedAmmo(Game.PlayerPed.Handle, (uint)GetHashKey(comp.TargetWeapon), ammo);
                            BaseUI.ShowNotfi("Component equiped.");

                            if (menuItem.RightIcon == MenuItem.Icon.STAR)
                                menuItem.RightIcon = MenuItem.Icon.NONE;

                            foreach (var item in menu.GetMenuItems())
                            {
                                if (item.Text != "Select" && item.Text != "Tints")
                                {
                                    WeaponItem otherComp = item.ItemData;
                                    if (HasPedGotWeaponComponent(Game.PlayerPed.Handle, (uint)GetHashKey(otherComp.TargetWeapon), (uint)GetHashKey(otherComp.ModelName)))
                                    {

                                        if (item.RightIcon != MenuItem.Icon.TICK)
                                            item.RightIcon = MenuItem.Icon.TICK;

                                    }
                                    else
                                    {
                                        if (item.RightIcon == MenuItem.Icon.TICK)
                                            item.RightIcon = MenuItem.Icon.NONE;
                                    }
                                }
                            }
                        }
                    }
                    else if (menuItem.Text == "Select")
                    {
                        string name = menuItem.ItemData.ModelName;
                        string nameToPrint = name.Remove(0, 6).Replace("_", " ");
                        if (menuItem.ItemData.Slot == WepLoadOut.Slot01)
                        {
                            if (menuItem.ItemData.ModelName != PlayerInfo.Loadout.PrimaryWeapon.WeaponName.ToUpper())
                            {
                                string oldweapon = PlayerInfo.Loadout.PrimaryWeapon.WeaponName.ToUpper();
                                string newWeapon = menuItem.ItemData.ModelName;

                                PlayerInfo.Loadout.PrimaryWeapon.WeaponName = menuItem.ItemData.ModelName;
                                PlayerInfo.Loadout.PrimaryWeapon.MaxAmount = menuItem.ItemData.MaxAmmo;
                                BaseUI.ShowNotfi($"Primary weapon has changed to ~u~{nameToPrint} ", false, BaseUI.HudColors.DarkRed);
                                Screen.Effects.Start(ScreenEffect.CamPushInTrevor, 1000);
                                PlaySoundFrontend(-1, "MEDAL_UP", "HUD_MINI_GAME_SOUNDSET", true);
                                Game.PlayerPed.Weapons.Current.Ammo = 1;

                                Game.GetWeaponHudStats((uint)GetHashKey(menuItem.ItemData.ModelName), ref PrimaryWeaponStates);
                                OnWeaponLoadOutChanged?.Invoke(newWeapon, oldweapon, WepLoadOut.Slot01);
                            }
                            else
                            {
                                BaseUI.ShowNotfi("This weapon is already in your weapon loadout");
                                Screen.Effects.Start(ScreenEffect.DeathFailOut, 1000);
                            }
                        }
                        else if (menuItem.ItemData.Slot == WepLoadOut.Slot02)
                        {
                            if (menuItem.ItemData.ModelName != PlayerInfo.Loadout.SecondaryWeapon.WeaponName.ToUpper())
                            {
                                string oldweapon = PlayerInfo.Loadout.SecondaryWeapon.WeaponName.ToUpper();
                                string newWeapon = menuItem.ItemData.ModelName;

                                PlayerInfo.Loadout.SecondaryWeapon.WeaponName = menuItem.ItemData.ModelName;
                                PlayerInfo.Loadout.SecondaryWeapon.MaxAmount = menuItem.ItemData.MaxAmmo;
                                BaseUI.ShowNotfi($"Secondary weapon has changed to ~u~{nameToPrint} ", false, BaseUI.HudColors.Green);
                                Screen.Effects.Start(ScreenEffect.CamPushInFranklin, 1000);
                                PlaySoundFrontend(-1, "MEDAL_UP", "HUD_MINI_GAME_SOUNDSET", true);

                                Game.PlayerPed.Weapons.Current.Ammo = 1;

                                Game.GetWeaponHudStats((uint)GetHashKey(menuItem.ItemData.ModelName), ref SeconderyWeaponStats);
                                OnWeaponLoadOutChanged?.Invoke(newWeapon, oldweapon, WepLoadOut.Slot02);
                            }
                            else
                            {
                                BaseUI.ShowNotfi("This weapon is already in your weapon loadout");
                                Screen.Effects.Start(ScreenEffect.DeathFailOut, 1000);
                            }
                        }
                        else if (menuItem.ItemData.Slot == WepLoadOut.Slot03)
                        {
                            if (menuItem.ItemData.ModelName != PlayerInfo.Loadout.Equipment.WeaponName.ToUpper())
                            {
                                string oldweapon = PlayerInfo.Loadout.Equipment.WeaponName.ToUpper();
                                string newWeapon = menuItem.ItemData.ModelName;

                                PlayerInfo.Loadout.Equipment.MaxAmount = menuItem.ItemData.MaxAmmo;
                                PlayerInfo.Loadout.Equipment.WeaponName = menuItem.ItemData.ModelName;
                                BaseUI.ShowNotfi($"Equipment weapon has changed to ~u~{nameToPrint} ", false, BaseUI.HudColors.Blue);
                                Screen.Effects.Start(ScreenEffect.CamPushInMichael, 1000);
                                PlaySoundFrontend(-1, "MEDAL_UP", "HUD_MINI_GAME_SOUNDSET", true);

                                Game.GetWeaponHudStats((uint)GetHashKey(menuItem.ItemData.ModelName), ref EquipmentsWeaponStats);
                                OnWeaponLoadOutChanged?.Invoke(newWeapon, oldweapon, WepLoadOut.Slot03);
                            }
                            else
                            {
                                BaseUI.ShowNotfi("This weapon is already in your weapon loadout");
                                Screen.Effects.Start(ScreenEffect.DeathFailOut, 1000);
                            }
                        }
                    }
                });

                wepMenu.Value.OnMenuOpen += m =>
                {
                    foreach (var item in m.GetMenuItems())
                    {
                        if (item.Text != "Select" && item.Text != "Tints")
                        {
                            WeaponItem otherComp = item.ItemData;
                            if (HasPedGotWeaponComponent(Game.PlayerPed.Handle, (uint)GetHashKey(otherComp.TargetWeapon), (uint)GetHashKey(otherComp.ModelName)))
                            {

                                if (item.RightIcon != MenuItem.Icon.TICK)
                                    item.RightIcon = MenuItem.Icon.TICK;

                            }
                            else
                            {
                                if (item.RightIcon == MenuItem.Icon.TICK)
                                    item.RightIcon = MenuItem.Icon.NONE;
                            }
                        }
                    }
                };

                wepMenu.Value.OnMenuClose += m =>
                {
                    List<string> selectedComponents = new List<string>();
                    WeaponFormat weapon = new WeaponFormat();
                    foreach (var item in m.GetMenuItems())
                    {
                        if (item.Text != "Select")
                        {
                            if (item.RightIcon == MenuItem.Icon.TICK)
                                selectedComponents.Add(item.ItemData.ModelName);
                        }
                        else if (item.Text == "Select")
                            weapon = item.ItemData;
                    }

                    if (weapon.Slot == WepLoadOut.Slot01)
                    {
                        if (weapon.ModelName == PlayerInfo.Loadout.PrimaryWeapon.WeaponName)
                        {
                            PlayerInfo.Loadout.PrimaryWeapon.WeaponComponent = selectedComponents;
                            PlayerInfo.Loadout.PrimaryWeapon.TintIndex = GetPedWeaponTintIndex(Game.PlayerPed.Handle, (uint)GetHashKey(weapon.ModelName));
                        }
                    }
                    else if (weapon.Slot == WepLoadOut.Slot02)
                    {
                        if (weapon.ModelName == PlayerInfo.Loadout.SecondaryWeapon.WeaponName)
                        {
                            PlayerInfo.Loadout.SecondaryWeapon.WeaponComponent = selectedComponents;
                            PlayerInfo.Loadout.SecondaryWeapon.TintIndex = GetPedWeaponTintIndex(Game.PlayerPed.Handle, (uint)GetHashKey(weapon.ModelName));
                        }
                    }
                    else if (weapon.Slot == WepLoadOut.Slot03)
                    {
                        if (weapon.ModelName == PlayerInfo.Loadout.Equipment.WeaponName)
                        {
                            PlayerInfo.Loadout.Equipment.WeaponComponent = selectedComponents;
                            PlayerInfo.Loadout.Equipment.TintIndex = GetPedWeaponTintIndex(Game.PlayerPed.Handle, (uint)GetHashKey(weapon.ModelName));
                        }
                    }
                };
            }

            #endregion

            #endregion


            //effectMenu
            CreateEffectSubMenus(EffectMenu, ParticleFxData.ParticalFxs);

            MainMenu.AddMenuItem(readyBtn);
            MainMenu.AddMenuItem(spectBtn);
            MainMenu.AddMenuItem(skinMenuBtn);
            MainMenu.AddMenuItem(loadoutMenuBtn);
            MainMenu.AddMenuItem(effectMenuBtn);
            //MainMenu.AddMenuItem(exitBtn);
            MainMenu.InstructionalButtons.Add(Control.MultiplayerInfo, "Show/Hide");

            //parenting menus
            MenuController.AddSubmenu(MainMenu, WeaponLoadoutMenu);
            MenuController.AddSubmenu(WeaponLoadoutMenu, PrimaryWeaponMenu);
            MenuController.AddSubmenu(WeaponLoadoutMenu, SeconderyWeaponMenu);
            MenuController.AddSubmenu(WeaponLoadoutMenu, EquipmentsWeaponMenu);
            MenuController.AddSubmenu(MainMenu, SkinMenu);
            MenuController.AddSubmenu(MainMenu, EffectMenu);

            //adding them
            MenuController.MainMenu = MainMenu;
            MenuController.AddMenu(MainMenu);
            MenuController.AddMenu(WeaponLoadoutMenu);
            MenuController.AddMenu(PrimaryWeaponMenu);
            MenuController.AddMenu(SeconderyWeaponMenu);
            MenuController.AddMenu(EquipmentsWeaponMenu);
            MenuController.AddMenu(SkinMenu);
            MenuController.AddMenu(EffectMenu);

            MenuController.BindMenuItem(MainMenu, WeaponLoadoutMenu, loadoutMenuBtn);
            MenuController.BindMenuItem(MainMenu, SkinMenu, skinMenuBtn);
            MenuController.BindMenuItem(MainMenu, EffectMenu, effectMenuBtn);

            MainMenu.OnItemSelect += (menu, menuItem, itemIndex) =>
            {
                if (!readyBtn.Checked)
                {
                }
                else
                {
                    BaseUI.ShowNotfi("You cant change your loadout or skin while your ~g~ready!", true, flashColor: new ColorFormat() { Red = 255, Blue = 0, Green = 0, Alpha = 100 });
                }
            };

            SkinMenu.OnItemSelect += (menu, menuItem, itemIndex) =>
            {
                if (menuItem == skinCusBtn)
                {
                    //not closing skinMenu for saving the data every time player goes back from skin menu to mainMenu 
                    SkinMenu.Visible = false;
                    SkinCustomize.OpenMenu();
                }
            };

            SkinMenu.OnMenuOpen += async menu =>
            {
                MenuController.DisableMenuButtons = true;

                if (Game.PlayerPed.IsPositionFrozen)
                    Game.PlayerPed.IsPositionFrozen = false;

                if (IsPedDoingOnSkinChoosenScenario)
                {
                    StopPlayerPedTask();
                    IsPedDoingOnSkinChoosenScenario = false;
                }

                if (MenuCamStatus == MenuCams.MainMenuCamera)
                    SetCamera(MenuCams.ChangeSkinCamera01);

                MenuStatus = PlayerInMenuStatus.ChengingSkin;

                if (Game.PlayerPed.Position != MainMenuLocation.ChangeSkinPlayerPos)
                {
                    StopPlayerPedTaskNow();
                    await Delay(1000);
                    TaskGoStraightToCoord(Game.PlayerPed.Handle, MainMenuLocation.ChangeSkinPlayerPos.X, MainMenuLocation.ChangeSkinPlayerPos.Y, MainMenuLocation.ChangeSkinPlayerPos.Z, 1f, MainMenuLocation.MaxTimeWaitForPlayerToReachChangeSkinPos, MainMenuLocation.ChangeSkinPlayerHeding, 1f);
                    await BaseScript.Delay(MainMenuLocation.MaxTimeWaitForPlayerToReachChangeSkinPos + 500);
                }

                if (MenuCamStatus == MenuCams.ChangeSkinCamera01)
                {
                    SetCamera(MenuCams.ChangeSkinCamera02);
                    Screen.Effects.Start(ScreenEffect.CamPushInNeutral, 2000);
                }

                MenuController.DisableMenuButtons = false;
            };

            //hooking loadoutBtns with class Menus
            WeaponLoadoutMenu.OnItemSelect += (menu, menuItem, itemIndex) =>
            {
                if (menuItem == primaryBtn)
                {
                    PrimaryWeaponMenu.OpenMenu();
                    WeaponLoadoutMenu.Visible = false;

                    if (IsShowingLoadOut)
                        HideLoadOutShowCase();
                }
                else if (menuItem == seconderyBtn)
                {
                    SeconderyWeaponMenu.OpenMenu();
                    WeaponLoadoutMenu.Visible = false;

                    if (IsShowingLoadOut)
                        HideLoadOutShowCase();
                }
                else if (menuItem == EquipmentsBtn)
                {
                    EquipmentsWeaponMenu.OpenMenu();
                    WeaponLoadoutMenu.Visible = false;

                    if (IsShowingLoadOut)
                        HideLoadOutShowCase();
                }
            };

            WeaponLoadoutMenu.OnMenuOpen += LoadoutMenu_OnMenuOpen;
            WeaponLoadoutMenu.OnMenuClose += m =>
            {
                //saving the data
                SaveLoadOut();

                if (IsShowingLoadOut)
                    HideLoadOutShowCase();
            };

            WeaponLoadoutMenu.OnIndexChange += (menu, oldItem, newItem, oldIndex, newIndex) =>
            {
                if (newItem == primaryBtn)
                {
                    EquipOnlyThisWeapon(PlayerInfo.Loadout.PrimaryWeapon.WeaponName);
                    WeaponLoadoutMenu.SetWeaponStats((float)PrimaryWeaponStates.hudDamage / 100, (float)PrimaryWeaponStates.hudSpeed / 100, (float)PrimaryWeaponStates.hudAccuracy / 100, (float)PrimaryWeaponStates.hudRange / 100);
                    SelectedUiItem = WepLoadOut.Slot01;
                }
                else if (newItem == seconderyBtn)
                {
                    EquipOnlyThisWeapon(PlayerInfo.Loadout.SecondaryWeapon.WeaponName);
                    WeaponLoadoutMenu.SetWeaponStats((float)SeconderyWeaponStats.hudDamage / 100, (float)SeconderyWeaponStats.hudSpeed / 100, (float)SeconderyWeaponStats.hudAccuracy / 100, (float)SeconderyWeaponStats.hudRange / 100);
                    SelectedUiItem = WepLoadOut.Slot02;
                }
                else if (newItem == EquipmentsBtn)
                {
                    EquipOnlyThisWeapon(PlayerInfo.Loadout.Equipment.WeaponName);
                    WeaponLoadoutMenu.SetWeaponStats((float)EquipmentsWeaponStats.hudDamage / 100, (float)EquipmentsWeaponStats.hudSpeed / 100, (float)EquipmentsWeaponStats.hudAccuracy / 100, (float)EquipmentsWeaponStats.hudRange / 100);
                    Game.PlayerPed.Task.ClearAll();
                    SelectedUiItem = WepLoadOut.Slot03;
                }
            };


            MainMenu.OnItemSelect += (s, i, d) =>
            {
                if (i == exitBtn)
                {
                    TriggerEvent("IRV:Menu:shutdown");
                    PlayerInfo.PlayerStatus = PlayerStatuses.none;
                }

                if (i == spectBtn)
                {
                    if (MatchManger.MatchStatusInTotal == MatchManger.MatchStatuses.IsRunnig)
                    {
                        if (PlayerInfo.PlayerStatus != PlayerStatuses.IsInMatch && PlayerInfo.PlayerStatus != PlayerStatuses.IsInSpectorMode)
                        {
                            TriggerEvent("IRV:Menu:shutdown");
                            SpectorMode.Start();
                        }
                    }
                    else
                        BaseUI.ShowNotfi("There is no in progress match right now", true);
                }
            };

            MainMenu.OnMenuOpen += async m =>
            {
                if (MenuCamStatus != MenuCams.MainMenuCamera)
                    SetCamera(MenuCams.MainMenuCamera);

                if (readyBtn.Checked)
                {
                    SetPlayerReady(false);
                    readyBtn.Checked = false;
                }

                MenuStatus = PlayerInMenuStatus.Main;

                if (Game.PlayerPed.Position != MainMenuLocation.PlayerPedPos)
                {
                    StopPlayerPedTask();
                    await BaseScript.Delay(500);

                    if (Game.PlayerPed.IsPositionFrozen)
                        Game.PlayerPed.IsPositionFrozen = false;

                    TaskGoStraightToCoord(Game.PlayerPed.Handle, MainMenuLocation.PlayerPedPos.X, MainMenuLocation.PlayerPedPos.Y, MainMenuLocation.PlayerPedPos.Z, 1f, 10000, MainMenuLocation.PlayerHeading, 1f);
                }
            };

            MainMenu.OnCheckboxChange += (s, i, c, d) =>
            {
                if (i == readyBtn)
                {
                    if (MatchManger.MatchStatusInTotal != MatchManger.MatchStatuses.IsRunnig)
                    {
                        if (MatchManger.MatchStatusInTotal != MatchManger.MatchStatuses.IsGoingToStart)
                        {
                            SetPlayerReady(d);
                            Screen.ShowNotification("~r~Ready status: ~b~" + d);

                            //making sure player dont spam this chekbox
                            //setting spamtime to zero after 10000 ms
                            if (GameTimer == -1)
                                GameTimer = GetGameTimer();
                            else if (GetGameTimer() > GameTimer + 10000)
                            {
                                GameTimer = -1;
                                ReadySpam = 0;
                            }

                            //disabaling and enabling after 4 time and spamtime ms
                            if (ReadySpam <= 4)
                                ReadySpam++;
                            else
                            {
                                Screen.ShowNotification("~r~Spam lock!");
                                ReadySpam = 0;
                                i.Enabled = false;
                                EnableItemAfter(i, SpamTime);
                            }
                        }
                        else
                        {
                            if (PlayerInfo.IsReady)
                            {
                                Screen.ShowNotification("~r~You cant go while match is going to start!");
                                i.Checked = true;
                                if (!PlayerInfo.IsReady)
                                {
                                    SetPlayerReady(true);
                                }
                            }
                            else
                            {
                                SetPlayerReady(d);
                                Screen.ShowNotification("~r~Ready status: ~b~" + d);
                            }
                        }
                    }
                    else
                    {
                        Screen.ShowNotification("~r~Match is in progress please wait!");
                        i.Checked = false;
                        if (PlayerInfo.IsReady)
                        {
                            SetPlayerReady(false);
                        }
                    }
                }
            };

            //if any of loadout options opens
            PrimaryWeaponMenu.OnMenuOpen += m =>
            {
                SetCamera(MenuCams.ChangeLoadOutCamera);
            };

            SeconderyWeaponMenu.OnMenuOpen += m =>
            {
                SetCamera(MenuCams.ChangeLoadOutCamera);
            };

            EquipmentsWeaponMenu.OnMenuOpen += m =>
            {
                SetCamera(MenuCams.ChangeLoadOutCamera);
            };

            EffectMenu.OnMenuOpen += async m =>
            {
                MenuStatus = PlayerInMenuStatus.ChangingEffect;
                if (MenuCamStatus != MenuCams.ChangeSkinCamera01)
                    SetCamera(MenuCams.ChangeSkinCamera01);
                if (Game.PlayerPed.Position != MainMenuLocation.ChangeSkinPlayerPos)
                {
                    if (Game.PlayerPed.IsPositionFrozen)
                        Game.PlayerPed.IsPositionFrozen = false;

                    StopPlayerPedTaskNow();
                    await Delay(1000);
                    TaskGoStraightToCoord(Game.PlayerPed.Handle, MainMenuLocation.ChangeSkinPlayerPos.X, MainMenuLocation.ChangeSkinPlayerPos.Y, MainMenuLocation.ChangeSkinPlayerPos.Z, 1f, MainMenuLocation.MaxTimeWaitForPlayerToReachChangeSkinPos, MainMenuLocation.ChangeSkinPlayerHeding, 1f);
                    await BaseScript.Delay(MainMenuLocation.MaxTimeWaitForPlayerToReachChangeSkinPos + 500);
                }
            };
        }

        private void ShowLoadOutShowCase()
        {
            Screen.Effects.Start(ScreenEffect.MpCelebPreloadFade, 0, true);

            IsShowingLoadOut = true;


            uint model_1 = (uint)GetHashKey(PlayerInfo.Loadout.PrimaryWeapon.WeaponName.ToUpper());
            uint model_2 = (uint)GetHashKey(PlayerInfo.Loadout.SecondaryWeapon.WeaponName.ToUpper());
            uint model_3 = (uint)GetHashKey(PlayerInfo.Loadout.Equipment.WeaponName.ToUpper());


            PrimaryProp = new Prop(CreateWeaponObject(model_1, 10, MainMenuLocation.PrimaryWeaponObjPos.X, MainMenuLocation.PrimaryWeaponObjPos.Y, MainMenuLocation.PrimaryWeaponObjPos.Z, true, 1f, 0));
            SeconderyProp = new Prop(CreateWeaponObject(model_2, 10, MainMenuLocation.SeconderyWeaponObjPos.X, MainMenuLocation.SeconderyWeaponObjPos.Y, MainMenuLocation.SeconderyWeaponObjPos.Z, true, 1f, 0));
            EquipmentProp = new Prop(CreateWeaponObject(model_3, 10, MainMenuLocation.EquipmentWeaponObjPos.X, MainMenuLocation.EquipmentWeaponObjPos.Y, MainMenuLocation.EquipmentWeaponObjPos.Z, true, 1f, 0));

            SetWeaponObjectTintIndex(PrimaryProp.Handle, PlayerInfo.Loadout.PrimaryWeapon.TintIndex);
            SetWeaponObjectTintIndex(SeconderyProp.Handle, PlayerInfo.Loadout.SecondaryWeapon.TintIndex);
            SetWeaponObjectTintIndex(EquipmentProp.Handle, PlayerInfo.Loadout.Equipment.TintIndex);

            foreach (var item in PlayerInfo.Loadout.PrimaryWeapon.WeaponComponent)
            {
                GiveWeaponComponentToWeaponObject(PrimaryProp.Handle, (uint)GetHashKey(item));
            }
            foreach (var item in PlayerInfo.Loadout.SecondaryWeapon.WeaponComponent)
            {
                GiveWeaponComponentToWeaponObject(SeconderyProp.Handle, (uint)GetHashKey(item));
            }
            foreach (var item in PlayerInfo.Loadout.Equipment.WeaponComponent)
            {
                GiveWeaponComponentToWeaponObject(EquipmentProp.Handle, (uint)GetHashKey(item));
            }


            //making sure other players cants see this prop only local (by calling fun on loop)
            //props are Collided toggeder is that ok ?
            PrimaryProp.IsVisible = false;
            SeconderyProp.IsVisible = false;
            EquipmentProp.IsVisible = false;
            PrimaryProp.IsPositionFrozen = true;
            SeconderyProp.IsPositionFrozen = true;
            EquipmentProp.IsPositionFrozen = true;

            //making sure anyway
            SetEntityCollision(PrimaryProp.Handle, false, false);
            SetEntityCollision(SeconderyProp.Handle, false, false);
            SetEntityCollision(EquipmentProp.Handle, false, false);

            PrimaryProp.Heading = MainMenuLocation.PropsHeading;
            SeconderyProp.Heading = MainMenuLocation.PropsHeading;
            EquipmentProp.Heading = MainMenuLocation.PropsHeading;
            EquipmentProp.Rotation = new Vector3(EquipmentProp.Rotation.X, EquipmentProp.Rotation.Y + 90f, EquipmentProp.Rotation.Z);

            if ((uint)GetWeapontypeGroup(model_2) == 3566412244) //TODO find meele on all of loadouts in case of change 
                SeconderyProp.Rotation = new Vector3(SeconderyProp.Rotation.X, SeconderyProp.Rotation.Y + 90f, SeconderyProp.Rotation.Z);


            if (Screen.Effects.IsActive(ScreenEffect.MpCelebPreloadFade))
                Screen.Effects.Stop(ScreenEffect.MpCelebPreloadFade);
        }

        private void HideLoadOutShowCase()
        {
            if (PrimaryProp != null)
                if (PrimaryProp.Exists())
                    PrimaryProp.Delete();
            if (SeconderyProp != null)
                if (SeconderyProp.Exists())
                    SeconderyProp.Delete();
            if (EquipmentProp != null)
                if (EquipmentProp.Exists())
                    EquipmentProp.Delete();


            IsShowingLoadOut = false;

            if (Screen.Effects.IsActive(ScreenEffect.MpCelebPreloadFade))
                Screen.Effects.Stop(ScreenEffect.MpCelebPreloadFade);
        }

        public async void SetCamera(MenuCams menuCam)
        {
            if (MenuCam == null)
                MenuCam = CreateCameraAtThisPos(MainMenuLocation.CameraPos, MainMenuLocation.CameraRot);

            switch (menuCam)
            {
                case MenuCams.MainMenuCamera:
                    MenuCam.Position = MainMenuLocation.CameraPos;
                    MenuCam.Rotation = MainMenuLocation.CameraRot;
                    MenuCam.StopPointing();
                    break;
                case MenuCams.ShowLoadOutCamera:
                    MenuCam.Position = MainMenuLocation.LoadotShowPlayPos;
                    MenuCam.Rotation = MainMenuLocation.CameraRot;
                    MenuCam.StopPointing();
                    break;
                case MenuCams.ChangeLoadOutCamera:
                    var camPos = new KeyValuePair<Vector3, Vector3>(Game.PlayerPed.GetOffsetPosition(new Vector3(0f, 1.2f, 0.40f)), Game.PlayerPed.Position + new Vector3(0f, 0f, 0.35f)); // upper body 2
                    //MenuCam.Position = camPos.Key;
                    //MenuCam.PointAt(camPos.Value);
                    MenuCam = await MoveCamToNewSpot(MenuCam, camPos.Key, camPos.Value);
                    //MenuCam.Rotation = MainMenuLocation.CameraRot;
                    break;
                case MenuCams.ChangeSkinCamera01:
                    MenuCam.Position = MainMenuLocation.ChangeSkinCameraPos01;
                    MenuCam.Rotation = MainMenuLocation.ChangeSkinCameraRot01;
                    MenuCam.StopPointing();
                    break;
                case MenuCams.ChangeSkinCamera02:

                    int timer = GetGameTimer();

                    while (!Game.PlayerPed.IsStopped || GetGameTimer() < timer + 900)
                    {
                        await BaseScript.Delay(0);
                    }

                    camPos = new KeyValuePair<Vector3, Vector3>(Game.PlayerPed.GetOffsetPosition(new Vector3(0f, 1.8f, 0.2f)), Game.PlayerPed.Position + new Vector3(0f, 0f, 0.0f));     // default 0
                    //MenuCam.Position = camPos.Key;
                    //MenuCam.Rotation = MainMenuLocation.ChangeSkinCameraRot02;
                    //MenuCam.PointAt(camPos.Value);
                    MenuCam = await MoveCamToNewSpot(MenuCam, camPos.Key, camPos.Value);
                    //MenuCam.StopPointing();
                    break;
                case MenuCams.VoteMapCamera:
                    CameraSceneFormat sceneFormat =
                        new CameraSceneFormat("vote",
                        new Vector3(MenuCam.Position.X, MenuCam.Position.Y, Game.PlayerPed.Position.Z + 10f), MenuCam.Rotation)
                        {
                            Cam = MenuCam,
                            EaseTime = 2000,
                            HaveEffect = true,
                            Effect = ScreenEffect.SwitchHudOut,
                        };
                    CameraScene scene = new CameraScene(sceneFormat, "voteScene");
                    scene.Goto("vote");
                    break;
            }

            MenuCamStatus = menuCam;
            RenderScriptCams(true, true, 1000, true, true);
            Game.PlayerPed.Task.LookAt(MenuCam.Position, 2500);
        }

        public void DeleteMenuCamera()
        {
            if (MenuCam != null)
            {
                RenderScriptCams(false, true, 1000, true, true);
                MenuCam.IsActive = false;
                MenuCam.Delete();
                MenuCam = null;
            }
        }

        private void LoadoutMenu_OnMenuOpen(Menu menu)
        {
            menu.RefreshIndex();

            //showing primary weapon state by default
            WeaponLoadoutMenu.SetWeaponStats((float)PrimaryWeaponStates.hudDamage / 100f, (float)PrimaryWeaponStates.hudSpeed / 100f, (float)PrimaryWeaponStates.hudAccuracy / 100f, (float)PrimaryWeaponStates.hudRange / 100f);

            SelectedUiItem = WepLoadOut.Slot01;
            SetCamera(MenuCams.ShowLoadOutCamera);

            if (!IsShowingLoadOut)
            {
                ShowLoadOutShowCase();
            }

            //priymary weapon index will always be index 0 so after refleshing index we givving this wep to ped for showcase 
            EquipOnlyThisWeapon(PlayerInfo.Loadout.PrimaryWeapon.WeaponName.ToString());

            MenuStatus = PlayerInMenuStatus.ChengingLoadOut;
        }

        private void CreateBaseLoadOutMenuItems(Menu BaseLoadoutMenu, List<WeaponFormat> weapons)
        {

            if (BaseLoadoutMenu == PrimaryWeaponMenu)
            {
                //Tkey is category and values are weapon that belonged to category
                Dictionary<string, List<WeaponFormat>> slot01 = new Dictionary<string, List<WeaponFormat>>();
                foreach (WeaponFormat weapon in weapons)
                {
                    if (weapon.Slot == WepLoadOut.Slot01)
                    {
                        if (!slot01.Keys.Contains(weapon.Category))
                            slot01.Add(weapon.Category, new List<WeaponFormat>());

                        slot01[weapon.Category].Add(weapon);
                    }
                }

                foreach (var item in slot01)
                    CreateCategoryWithItemsForSlot(BaseLoadoutMenu, item, WepLoadOut.Slot01);
            }
            else if (BaseLoadoutMenu == SeconderyWeaponMenu)
            {
                //Tkey is category and values are weapon that belonged to category
                Dictionary<string, List<WeaponFormat>> slot02 = new Dictionary<string, List<WeaponFormat>>();
                foreach (WeaponFormat weapon in weapons)
                {
                    if (weapon.Slot == WepLoadOut.Slot02)
                    {
                        if (!slot02.Keys.Contains(weapon.Category))
                            slot02.Add(weapon.Category, new List<WeaponFormat>());

                        slot02[weapon.Category].Add(weapon);
                    }
                }

                foreach (var item in slot02)
                    CreateCategoryWithItemsForSlot(BaseLoadoutMenu, item, WepLoadOut.Slot02);
            }
            else if (BaseLoadoutMenu == EquipmentsWeaponMenu)
            {

                //Tkey is category and values are weapon that belonged to category
                Dictionary<string, List<WeaponFormat>> slot03 = new Dictionary<string, List<WeaponFormat>>();
                foreach (WeaponFormat weapon in weapons)
                {
                    if (weapon.Slot == WepLoadOut.Slot03)
                    {
                        if (!slot03.Keys.Contains(weapon.Category))
                            slot03.Add(weapon.Category, new List<WeaponFormat>());

                        slot03[weapon.Category].Add(weapon);
                    }
                }

                foreach (var item in slot03)
                    CreateCategoryWithItemsForSlot(BaseLoadoutMenu, item, WepLoadOut.Slot03);
            }
        }

        private void CreateCategoryWithItemsForSlot(Menu BaseLoadoutMenu, KeyValuePair<string, List<WeaponFormat>> item, WepLoadOut slot)
        {
            string slotName = slot.ToString();
            //if we dont have the categorySubMenu then create it and stuff
            if (!WeaponSlotSubMenus[slotName].Keys.Contains(item.Key))
            {
                //creating Submenus
                WeaponSlotSubMenus[slotName].Add(item.Key, new Menu("", item.Key));
                //creating btn to acces it 
                MenuItem categoryBtn = new MenuItem(item.Key);

                BaseLoadoutMenu.AddMenuItem(categoryBtn);

                MenuController.BindMenuItem(BaseLoadoutMenu, WeaponSlotSubMenus[slotName][item.Key], categoryBtn);

                //adding new menu to menu controller[for no good reason(!)]
                MenuController.AddMenu(WeaponSlotSubMenus[slotName][item.Key]);
                //and parenting categoryMenu to priymary Menu
                MenuController.AddSubmenu(BaseLoadoutMenu, WeaponSlotSubMenus[slotName][item.Key]);
            }

            //then we need to add category btns for each weaponFormat in list of Tvalue
            foreach (WeaponFormat weaponFormat in item.Value)
            {
                //if this weapon belonged to slot
                if (weaponFormat.Slot == slot)
                {
                    //then create btn and and add the btn to category the weapon belonged to
                    string btnName = weaponFormat.LableName;
                    MenuItem weaponBtn = new MenuItem(btnName)
                    {
                        //adding weapon data into each Btn if we needed the data
                        ItemData = weaponFormat,
                        RightIcon = MenuItem.Icon.LOCK,
                        Enabled = false,
                    };

                    foreach (var data in weaponData.weaponDatas)
                    {
                        if (data.ModelName == weaponFormat.ModelName)
                        {
                            weaponBtn.Description = data.Description;
                            break;
                        }
                    }

                    weaponBtn.Description += $"~r~(Unlock at {weaponFormat.UnlockTarget} ~w~{weaponFormat.Type.ToString()}~r~)";

                    //unlock eventHandeler
                    IRVDM.Unlockables.OnWeaponUnlocked += new Action<WeaponFormat>(p =>
                    {
                        if (p.Id == weaponFormat.Id)
                        {
                            EnableNewUnlockableMenuItem(weaponBtn);
                        }
                    });

                    WeaponSlotSubMenus[slotName][item.Key].AddMenuItem(weaponBtn);
                }
            }
        }

        private void CreateSkinSubItems(Menu baseMenu, Dictionary<string, List<PedFormat>> skin)
        {
            foreach (var item in skin)
            {
                //if we dont have category menu then create it 
                if (!SkinSubMenus.Keys.Contains(item.Key))
                {
                    SkinSubMenus[item.Key] = new Menu("", item.Key);
                    MenuItem subBtn = new MenuItem(item.Key);
                    baseMenu.AddMenuItem(subBtn);

                    baseMenu.OnItemSelect += (menu, menuItem, itemIndex) =>
                    {
                        if (menuItem == subBtn)
                        {
                            //not closing Skin Menu for saving the data every time player goest to mainMenu only save the data
                            baseMenu.Visible = false;
                            SkinSubMenus[item.Key].OpenMenu();
                        }
                    };

                    MenuController.AddSubmenu(baseMenu, SkinSubMenus[item.Key]);
                }

                //creating items in category 
                foreach (PedFormat pedSkin in item.Value)
                {
                    MenuItem skinBtn = new MenuItem(pedSkin.LableName)
                    {
                        ItemData = pedSkin,
                        //lets lock the item and we can unlock it when player finished loading 
                        Enabled = false,
                        RightIcon = MenuItem.Icon.LOCK,
                    };

                    skinBtn.Description = $"Unlock at ~r~{pedSkin.UnlockTarget} ~w~{skinBtn.ItemData.Type.ToString()}";

                    //unlock eventhandels
                    IRVDM.Unlockables.OnPedUnlocked += new Action<PedFormat>(p =>
                    {
                        if (p.Id == pedSkin.Id)
                        {
                            EnableNewUnlockableMenuItem(skinBtn);
                        }
                    });

                    SkinSubMenus[item.Key].AddMenuItem(skinBtn);
                }
            }

            //looping in all skin subMenus and handeling on item select
            foreach (Menu subMenu in SkinSubMenus.Values)
            {
                //setting loadout skin
                subMenu.OnItemSelect += async (menu, menuItem, itemIndex) =>
                {
                    if (menuItem.ItemData.ModelName != PlayerInfo.Loadout.Skin.ModelName)
                    {
                        PedFormat oldSkin = PlayerInfo.Loadout.Skin;
                        PedFormat newSkin = menuItem.ItemData;

                        MenuController.DisableMenuButtons = true;
                        await PlayerController.SetSkin(newSkin.ModelName);
                        await StartParticleFxLocalyOnEntity(PlayerInfo.Loadout.ParticleFx.AssestName, PlayerInfo.Loadout.ParticleFx.OnPlayerSawnParticleName, Game.PlayerPed.Handle);
                        Screen.Effects.Start(ScreenEffect.MenuMgHeistOut, 2000);
                        BaseUI.ShowNotfi($"You just Changed your skin to ~g~{menuItem.Text}", true);
                        PlaySoundFrontend(-1, "MEDAL_UP", "HUD_MINI_GAME_SOUNDSET", true);
                        OnSkinChanged?.Invoke(newSkin.ModelName, oldSkin.ModelName);
                        SetPedDefaultComponentVariation(Game.PlayerPed.Handle);
                        ClearAllPedProps(Game.PlayerPed.Handle);
                        ClearPedDecorations(Game.PlayerPed.Handle);
                        ClearPedFacialDecorations(Game.PlayerPed.Handle);
                        if (!string.IsNullOrEmpty(newSkin.ChoosenScenario))
                        {
                            TaskStartScenarioInPlace(Game.PlayerPed.Handle, newSkin.ChoosenScenario, 0, true);
                            IsPedDoingOnSkinChoosenScenario = true;
                        }
                        BaseUI.SetBaseHudColors(newSkin.HudColor);
                        MenuController.DisableMenuButtons = false;
                        //and updating our data with new skin
                        UpdatePlayerSkin(newSkin);

                        //acurrcy is not enough for shooting skill but for now we set it anyway
                        PlayerController.SetPlayerStates(PlayerController.PlayerHudState.Shooting, newSkin.Accuracy);
                        //statmina will set one more time when match begins
                        PlayerController.SetPlayerStates(PlayerController.PlayerHudState.Stamina, newSkin.Stamina);
                        //for now
                        PlayerController.SetPlayerStates(PlayerController.PlayerHudState.StealthAblility, newSkin.Stamina);
                        PlayerController.SetPlayerStates(PlayerController.PlayerHudState.Strength, newSkin.Strength);
                        PlayerController.SetPlayerStates(PlayerController.PlayerHudState.LungCapacity, newSkin.Stamina);
                    }
                    else
                        BaseUI.ShowNotfi($"Your Skin is already {menuItem.Text}");
                };
            }
        }

        public void EnableNewUnlockableMenuItem(MenuItem item)
        {
            item.Enabled = true;
            item.Description = "~g~(Unlocked)";
            item.RightIcon = MenuItem.Icon.STAR;
        }

        public void CreateEffectSubMenus(Menu baseMenu, List<ParticleFxFormat> particles)
        {
            foreach (var data in particles)
            {
                MenuItem fxMenuBtn = new MenuItem(data.Name) { ItemData = data };
                Menu fxMenu = new Menu("", data.Name);
                MenuItem selectBtn = new MenuItem("Select", "Select this effects as your default")
                {
                    ItemData = data,
                    RightIcon = MenuItem.Icon.STAR,
                };

                MenuItem killBtn = new MenuItem("Kill", "When you kill someone");
                MenuItem spawnBtn = new MenuItem("Spawn", "When you resapwned");

                //trail?
                baseMenu.AddMenuItem(fxMenuBtn);
                MenuController.AddSubmenu(baseMenu, fxMenu);
                fxMenu.AddMenuItem(selectBtn);
                fxMenu.AddMenuItem(killBtn);
                fxMenu.AddMenuItem(spawnBtn);

                baseMenu.OnItemSelect += (menu, menuItem, itemIndex) =>
                {
                    if (menuItem == fxMenuBtn)
                    {
                        baseMenu.Visible = false;
                        fxMenu.OpenMenu();
                        fxMenu.RefreshIndex();
                    }
                };

                //setting tick for item when data loaded
                SyncData.OnPlayerSkinLoaded += () =>
                {
                    if (PlayerInfo.Loadout.ParticleFx.Name == data.Name)
                        fxMenuBtn.RightIcon = MenuItem.Icon.TICK;
                };

                fxMenu.OnItemSelect += async (menu, menuItem, itemIndex) =>
                {
                    MenuController.DisableMenuButtons = true;

                    if (menuItem == killBtn)
                    {
                        await StartParticleFxLocalyOnEntity(data.AssestName, data.OnPlayerKillParticleName, Game.PlayerPed.Handle);
                    }
                    else if (menuItem == spawnBtn)
                    {
                        await StartParticleFxLocalyOnEntity(data.AssestName, data.OnPlayerSawnParticleName, Game.PlayerPed.Handle);
                    }
                    else if (menuItem == selectBtn)
                    {
                        if (data.Name != PlayerInfo.Loadout.ParticleFx.Name)
                        {
                            baseMenu.GetMenuItems().Find(p => p.ItemData.Name == PlayerInfo.Loadout.ParticleFx.Name).RightIcon = MenuItem.Icon.NONE;
                            Screen.Effects.Start(ScreenEffect.CamPushInNeutral, 1000);
                            PlaySoundFrontend(-1, "MEDAL_UP", "HUD_MINI_GAME_SOUNDSET", true);
                            PlayerInfo.Loadout.ParticleFx = data;
                            fxMenuBtn.RightIcon = MenuItem.Icon.TICK;
                            BaseUI.ShowNotfi($"Your effect has changed to ~g~{menuItem.ItemData.Name}");
                        }
                        else
                        {
                            BaseUI.ShowNotfi($"Your effect is already ~g~{menuItem.ItemData.Name}");
                            Screen.Effects.Start(ScreenEffect.FocusOut, 500);
                        }
                    }

                    MenuController.DisableMenuButtons = false;
                };
            }
        }

        //cuz of showing weapon one fream for others i maked this 
        /// <summary>
        /// give this weapon to ped and after that equip it 
        /// </summary>
        /// <param name="weaponName"></param>
        private void EquipOnlyThisWeapon(string weaponName, int ammo = 0)
        {
            uint weaponHash = (uint)GetHashKey(weaponName);
            int _ammo;
            if (ammo == 0)
            {
                _ammo = 0;

                //is that nessecry to make sure ammo is 0 or at least one ?
                if ((uint)GetWeapontypeGroup(weaponHash) == (uint)WeaponData.GameWeaponCategory.Fire_extinghuiser
                    || (uint)GetWeapontypeGroup(weaponHash) == (uint)WeaponData.GameWeaponCategory.Jerrycan || (uint)GetWeapontypeGroup(weaponHash) == (uint)WeaponData.GameWeaponCategory.Melee
                    || (uint)GetWeapontypeGroup(weaponHash) == (uint)WeaponData.GameWeaponCategory.throwables)
                    _ammo = 1;
            }
            else
                _ammo = ammo;

            Game.PlayerPed.Weapons.RemoveAll();
            GiveWeaponToPed(Game.PlayerPed.Handle, (uint)GetHashKey(weaponName), _ammo, false, false);

            if (weaponName == PlayerInfo.Loadout.PrimaryWeapon.WeaponName)
            {
                foreach (string modelName in PlayerInfo.Loadout.PrimaryWeapon.WeaponComponent)
                {
                    GiveWeaponComponentToPed(Game.PlayerPed.Handle, (uint)GetHashKey(PlayerInfo.Loadout.PrimaryWeapon.WeaponName),
                        (uint)GetHashKey(modelName));
                }

                SetPedWeaponTintIndex(Game.PlayerPed.Handle, (uint)GetHashKey(weaponName), PlayerInfo.Loadout.PrimaryWeapon.TintIndex);
            }
            else if (weaponName == PlayerInfo.Loadout.SecondaryWeapon.WeaponName)
            {
                foreach (string modelName in PlayerInfo.Loadout.SecondaryWeapon.WeaponComponent)
                {
                    GiveWeaponComponentToPed(Game.PlayerPed.Handle, (uint)GetHashKey(PlayerInfo.Loadout.SecondaryWeapon.WeaponName),
                        (uint)GetHashKey(modelName));
                }
                SetPedWeaponTintIndex(Game.PlayerPed.Handle, (uint)GetHashKey(weaponName), PlayerInfo.Loadout.SecondaryWeapon.TintIndex);
            }
            else if (weaponName == PlayerInfo.Loadout.Equipment.WeaponName)
            {
                foreach (string modelName in PlayerInfo.Loadout.Equipment.WeaponComponent)
                {
                    GiveWeaponComponentToPed(Game.PlayerPed.Handle, (uint)GetHashKey(PlayerInfo.Loadout.Equipment.WeaponName),
                        (uint)GetHashKey(modelName));
                }
                SetPedWeaponTintIndex(Game.PlayerPed.Handle, (uint)GetHashKey(weaponName), PlayerInfo.Loadout.Equipment.TintIndex);
            }

            Game.PlayerPed.Weapons.Select((WeaponHash)weaponHash, true);
        }

        public void UnlockItems()
        {
            foreach (var item in SkinSubMenus.Values)
            {
                foreach (var btn in item.GetMenuItems())
                {
                    if (Unlockables.DoesPlayerUnlockedItem(btn.ItemData))
                    {
                        if (!btn.Enabled)
                        {
                            btn.Enabled = true;
                            btn.RightIcon = MenuItem.Icon.NONE;
                            btn.Description = $"~g~Unlocked";
                        }

                        if (btn.ItemData.ModelName == PlayerInfo.Loadout.Skin.ModelName)
                            btn.RightIcon = MenuItem.Icon.TICK;
                    }
                    else
                    {
                        string desc = "Needed item(s) to unlock : ~r~";
                        Unlockable unlk = btn.ItemData;


                        if (!string.IsNullOrEmpty(unlk.DependOnId))
                        {
                            var depend = Unlockables.GetUnlockbleFromId(unlk.DependOnId);
                            if (depend != null)
                            {
                                if (!Unlockables.DoesPlayerUnlockedItem(depend))
                                {
                                    desc += $"({depend.LableName})";
                                }
                            }
                        }

                        if (unlk.UnlockTarget != 0)
                        {
                            desc += $"({unlk.UnlockTarget} {unlk.Type.ToString()})";
                        }

                        btn.Description = desc;
                    }
                }
            }

            foreach (var slot in WeaponSlotSubMenus)
            {
                foreach (var categoryKeyValue in slot.Value)
                {
                    foreach (var btn in categoryKeyValue.Value.GetMenuItems())
                    {
                        if (Unlockables.DoesPlayerUnlockedItem(btn.ItemData))
                        {
                            btn.Enabled = true;
                            btn.RightIcon = MenuItem.Icon.NONE;
                            btn.Description = "";
                            foreach (var data in weaponData.weaponDatas)
                            {
                                if (data.ModelName == btn.ItemData.ModelName)
                                {
                                    btn.Description = data.Description;
                                    break;
                                }
                            }

                            btn.Description += $"~g~(Unlocked)";
                        }
                        else
                        {
                            string desc = "Needed item(s) to unlock : ~r~";
                            Unlockable unlk = btn.ItemData;


                            if (!string.IsNullOrEmpty(unlk.DependOnId))
                            {
                                var depend = Unlockables.GetUnlockbleFromId(unlk.DependOnId);
                                if (depend != null)
                                {
                                    if (!Unlockables.DoesPlayerUnlockedItem(depend))
                                    {
                                        desc += $"({depend.LableName})";
                                    }
                                }
                            }

                            if (unlk.UnlockTarget != 0)
                            {
                                desc += $"({unlk.UnlockTarget} {unlk.Type.ToString()})";
                            }

                            btn.Description = desc;
                        }

                        if (btn.ItemData.ModelName == PlayerInfo.Loadout.PrimaryWeapon.WeaponName
                            || btn.ItemData.ModelName == PlayerInfo.Loadout.SecondaryWeapon.WeaponName
                            || btn.ItemData.ModelName == PlayerInfo.Loadout.Equipment.WeaponName)
                            btn.RightIcon = MenuItem.Icon.TICK;
                    }
                }
            }

            foreach (var item in WeaponMenus.Values)
            {
                foreach (var menuItem in item.GetMenuItems())
                {
                    if (menuItem.Text != "Tints")
                    {
                        if (Unlockables.DoesPlayerUnlockedItem(menuItem.ItemData))
                        {
                            menuItem.Enabled = true;
                            menuItem.Description = "";
                            if (menuItem.Text != "Select")
                            {
                                foreach (var data in weaponData.weaponDatas)
                                {
                                    if (data.ModelName.ToUpper() == menuItem.ItemData.TargetWeapon.ToUpper())
                                    {
                                        foreach (var compdata in data.weaponComponents)
                                        {
                                            if (menuItem.ItemData.ModelName.ToUpper() == compdata.ModelName.ToUpper())
                                            {
                                                menuItem.Description = compdata.Description;
                                            }
                                        }
                                        break;
                                    }
                                }
                            }

                            menuItem.Description += $"~g~(Unlocked)";

                            if (menuItem.RightIcon == MenuItem.Icon.LOCK)
                                menuItem.RightIcon = MenuItem.Icon.NONE;
                        }
                    }
                }
            }

            foreach (var item in TintMenus)
            {
                foreach (var btn in item.GetMenuItems())
                {
                    if (Unlockables.DoesPlayerUnlockedItem(btn.ItemData))
                    {
                        btn.Enabled = true;
                        btn.Description = $"~g~Unlocked";

                        if (btn.RightIcon == MenuItem.Icon.LOCK)
                            btn.RightIcon = MenuItem.Icon.NONE;
                    }
                    else
                    {
                        string desc = "Needed item(s) to unlock : ~r~";
                        Unlockable unlk = btn.ItemData;


                        if (!string.IsNullOrEmpty(unlk.DependOnId))
                        {
                            var depend = Unlockables.GetUnlockbleFromId(unlk.DependOnId);
                            if (depend != null)
                            {
                                if (!Unlockables.DoesPlayerUnlockedItem(depend))
                                {
                                    desc += $"({depend.LableName})";
                                }
                            }
                        }

                        if (unlk.UnlockTarget != 0)
                        {
                            desc += $"({unlk.UnlockTarget} {unlk.Type.ToString()})";
                        }

                        btn.Description = desc;
                    }
                }
            }
        }

        /// <summary>
        /// set player IsReady statues both on client and the server
        /// </summary>
        /// <param name="toggle"></param>
        public void SetPlayerReady(bool toggle) => IsReady(toggle);

        /// <summary>
        /// after amount time will enable menu item
        /// </summary>
        /// <param name="item"></param>
        /// <param name="after"></param>
        private async void EnableItemAfter(MenuItem item, int after)
        {
            await BaseScript.Delay(after);
            item.Enabled = true;
        }

        #region Ped Customization Menu
        ///// <summary>
        ///// Refresh/create the ped customization menu.
        ///// </summary>
        private void RefreshCustomizationMenu()
        {
            drawablesMenuListItems.Clear();
            propsMenuListItems.Clear();
            SkinCustomize.ClearMenuItems();

            #region Ped Drawables
            for (int drawable = 0; drawable < 12; drawable++)
            {
                int currentDrawable = GetPedDrawableVariation(Game.PlayerPed.Handle, drawable);
                int maxVariations = GetNumberOfPedDrawableVariations(Game.PlayerPed.Handle, drawable);
                int maxTextures = GetNumberOfPedTextureVariations(Game.PlayerPed.Handle, drawable, currentDrawable);

                if (maxVariations > 0)
                {
                    List<string> drawableTexturesList = new List<string>();

                    for (int i = 0; i < maxVariations; i++)
                    {
                        drawableTexturesList.Add($"Item #{i + 1} (of {maxVariations})");
                    }

                    MenuListItem drawableTextures = new MenuListItem($"{textureNames[drawable]}", drawableTexturesList, currentDrawable, $"Use ← & → to select a ~o~{textureNames[drawable]} Variation~s~, press ~r~enter~s~ to cycle through the available textures.");
                    drawablesMenuListItems.Add(drawableTextures, drawable);
                    SkinCustomize.AddMenuItem(drawableTextures);
                }
            }
            #endregion

            #region Ped Props
            for (int tmpProp = 0; tmpProp < 5; tmpProp++)
            {
                int realProp = tmpProp > 2 ? tmpProp + 3 : tmpProp;

                int currentProp = GetPedPropIndex(Game.PlayerPed.Handle, realProp);
                int maxPropVariations = GetNumberOfPedPropDrawableVariations(Game.PlayerPed.Handle, realProp);

                if (maxPropVariations > 0)
                {
                    List<string> propTexturesList = new List<string>
                    {
                        $"Prop #1 (of {maxPropVariations + 1})"
                    };
                    for (int i = 0; i < maxPropVariations; i++)
                    {
                        propTexturesList.Add($"Prop #{i + 2} (of {maxPropVariations + 1})");
                    }


                    MenuListItem propTextures = new MenuListItem($"{propNames[tmpProp]}", propTexturesList, currentProp + 1, $"Use ← & → to select a ~o~{propNames[tmpProp]} Variation~s~, press ~r~enter~s~ to cycle through the available textures.");
                    propsMenuListItems.Add(propTextures, realProp);
                    SkinCustomize.AddMenuItem(propTextures);

                }
            }
            SkinCustomize.RefreshIndex();
            #endregion
        }

        #region Textures & Props
        private readonly List<string> textureNames = new List<string>()
        {
            "Head",
            "Mask / Facial Hair",
            "Hair Style / Color",
            "Hands / Upper Body",
            "Legs / Pants",
            "Bags / Parachutes",
            "Shoes",
            "Neck / Scarfs",
            "Shirt / Accessory",
            "Body Armor / Accessory 2",
            "Badges / Logos",
            "Shirt Overlay / Jackets",
        };

        private readonly List<string> propNames = new List<string>()
        {
            "Hats / Helmets", // id 0
            "Glasses", // id 1
            "Misc", // id 2
            "Watches", // id 6
            "Bracelets", // id 7
        };
        #endregion
        #endregion

        public Menu GetMenu()
        {
            return MainMenu;
        }
    }
}
