using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;
using Newtonsoft.Json;
using CitizenFX.Core;
using MenuAPI;

namespace IRVDM
{
    class MapCreator : BaseScript
    {
        public static List<string> props = new List<string>()
        {
            "Set_Pit_Fence_Closed",
            "Set_Pit_Fence_Demolition",
            "Set_Pit_Fence_Oval",
            "set_pit_fence_ovala",
            "set_pit_fence_ovalb",
            "Set_Pit_Fence_Wall",
            "set_wall_no_pit",
            "set_centreline_dystopian_05",
            "set_centreline_scifi_05",
            "Set_CentreLine_Wasteland_05",
            "Set_Turrets",
            "set_turrets_scifi",
            "set_turrets_wasteland",
            "Set_Team_Band_A",
            "Set_Team_Band_B",
            "Set_Team_Band_C",
            "Set_Team_Band_D",
            "Set_Lights_atlantis",
            "Set_Lights_evening",
            "Set_Lights_hell",
            "Set_Lights_midday", "Set_Lights_morning",
            "Set_Lights_night" ,"set_lights_sfnight",
            "Set_Lights_saccharine", "Set_Lights_sandstorm",
            "Set_Lights_storm", "Set_Lights_toxic",
            "Set_Dystopian_01",
            "Set_Dystopian_02",
        "Set_Dystopian_03",
            "Set_Dystopian_04",
        "Set_Dystopian_05"
        ,"Set_Dystopian_06",
            "Set_Dystopian_07"
            ,"Set_Dystopian_08",
        "Set_Dystopian_09" ,"Set_Dystopian_10",
        "Set_Dystopian_11" ,"Set_Dystopian_12",
        "Set_Dystopian_13" ,"Set_Dystopian_14",
        "Set_Dystopian_15" ,"Set_Dystopian_16",
        "Set_Dystopian_17",
            "Set_Scifi_01",
            "Set_Scifi_02",
            "Set_Scifi_03",
            "Set_Scifi_04",
            "Set_Scifi_05",
            "Set_Scifi_06" ,"Set_Scifi_07",
            "Set_Scifi_08", "Set_Scifi_09",
            "Set_Scifi_10",
            "Set_Wasteland_01",
            "Set_Wasteland_02" ,
            "Set_Wasteland_03",
            "Set_Wasteland_04" ,"Set_Wasteland_05",
            "Set_Wasteland_06" ,"Set_Wasteland_07",
            "Set_Wasteland_08" ,"Set_Wasteland_09",
            "Set_Wasteland_10" ,"Set_Dystopian_Scene",
            "Set_Scifi_Scene" ,
            "Set_Wasteland_Scene",
            "Set_Crowd_A", "Set_Crowd_B",
            "Set_Crowd_C" ,"Set_Crowd_D"
        };

        static List<string> prop2 = new List<string>()
        {
            "Set_Int_MOD_SHELL_DEF" ,"Set_Int_MOD2_B1",
  "Set_Int_MOD2_B2" ,"Set_Int_MOD2_B_TINT",
  "Set_Int_MOD_BOOTH_DEF" ,"Set_Int_MOD_BOOTH_BEN",
  "Set_Int_MOD_BOOTH_WP", "Set_Int_MOD_BOOTH_COMBO",
  "Set_Int_MOD_BEDROOM_BLOCKER", "Set_Int_MOD_CONSTRUCTION_01",
  "Set_Int_MOD_CONSTRUCTION_02", "Set_Int_MOD_CONSTRUCTION_03",
  "SET_OFFICE_STANDARD" ,"SET_OFFICE_INDUSTRIAL",
  "SET_OFFICE_HITECH", "Set_Mod1_Style_01",
  "Set_Mod1_Style_02", "Set_Mod1_Style_03",
  "Set_Mod1_Style_04" ,"Set_Mod1_Style_05",
  "Set_Mod1_Style_06", "Set_Mod1_Style_07",
  "Set_Mod1_Style_08", "Set_Mod1_Style_09",
  "Set_Mod2_Style_01","Set_Mod2_Style_02",
  "Set_Mod2_Style_03", "Set_Mod2_Style_04",
  "Set_Mod2_Style_05", "Set_Mod2_Style_06",
  "Set_Mod2_Style_07", "Set_Mod2_Style_08",
  "Set_Mod2_Style_09", "set_arena_peds",
  "set_arena_no", "peds",
  "SET_XMAS_DECORATIONS", "Set_Int_MOD_TROPHY_CAREER",
  "Set_Int_MOD_TROPHY_SCORE", "Set_Int_MOD_TROPHY_WAGEWORKER",
  "Set_Int_MOD_TROPHY_TIME_SERVED", "Set_Int_MOD_TROPHY_GOT_ONE",
  "Set_Int_MOD_TROPHY_OUTTA_HERE" ,"Set_Int_MOD_TROPHY_SHUNT",
  "Set_Int_MOD_TROPHY_BOBBY", "Set_Int_MOD_TROPHY_KILLED",
  "Set_Int_MOD_TROPHY_CROWD", "Set_Int_MOD_TROPHY_DUCK",
  "Set_Int_MOD_TROPHY_BANDITO","Set_Int_MOD_TROPHY_SPINNER",
  "Set_Int_MOD_TROPHY_LENS", "Set_Int_MOD_TROPHY_WAR",
  "Set_Int_MOD_TROPHY_UNSTOPPABLE" ,"Set_Int_MOD_TROPHY_CONTACT",
  "Set_Int_MOD_TROPHY_TOWER", "Set_Int_MOD_TROPHY_STEP",
  "Set_Int_MOD_TROPHY_PEGASUS" ,"SET_BANDITO_RC",
  "SET_OFFICE_TRINKET_07", "SET_OFFICE_TRINKET_06",
  "SET_OFFICE_TRINKET_03" ,"SET_OFFICE_TRINKET_04",
  "SET_OFFICE_TRINKET_02", "SET_OFFICE_TRINKET_01"
        };

        static List<string> p = new List<string>() { "VIP_XMAS_DECS" };

        Menu MapCreatorMenu = new Menu("", "Menu");
        MenuItem AddSpawnPoint = new MenuItem("add spawn point");
        //
        List<int> MenuPosDic = new List<int>();
        MenuItem EnableCamera = new MenuItem("Enable Camera");
        MenuItem SetPlayerPos = new MenuItem("player pos");
        MenuItem SetCameraPos = new MenuItem("Main camera pos");
        MenuItem SetChangeLoadOutPos = new MenuItem("camera loadot");
        MenuItem AddPrimaryWeaponObjPos = new MenuItem("slot01 obj");
        MenuItem SeconderyWeaponObjPos = new MenuItem("slot02 obj");
        MenuItem EquipmentWeaponObjPos = new MenuItem("slot03 obj");
        MenuItem LoadotShowPlayPos = new MenuItem("player pos load out show");
        MenuItem ChangeSkinCameraPos01 = new MenuItem("skin camera 01");
        MenuItem ChangeSkinCameraPos02 = new MenuItem("skin camera 02");
        MenuItem ChangeSkinCameraPos03 = new MenuItem("skin camera 03[unused but enter sth]");
        MenuItem ChangeSkinPlayerPos = new MenuItem("player pos skin");
        //
        MenuItem ClearAllSpawnPoints = new MenuItem("clear all spawn points");
        MenuItem AddWeaponSpawnPoint = new MenuItem("add weapon spawn point");
        MenuItem ClearAllWepSPoints = new MenuItem("clear all weapon spawn points");
        MenuItem SetMiddlePos = new MenuItem("set middle pos");
        MenuItem SetRangeNumber = new MenuItem("set range");
        MenuItem SetName = new MenuItem("set name");
        MenuItem SetDescribe = new MenuItem("set describe");
        MenuItem AddIPLs = new MenuItem("add ipls");
        MenuItem AddPickupWeapon = new MenuItem("add wep");
        MenuItem SetTime = new MenuItem("set time");
        MenuItem PrintData = new MenuItem("print data");
        MenuItem Save = new MenuItem("save data");
        MenuListItem MenuListItem = new MenuListItem("ipls", p, 0);
        IRVDMShared.DeathMatch DeathMatch;
        MainMenuLocation mainMenu;
        Camera Cam;

        private bool Enabled = true;

        public MapCreator()
        {
            //RequestIpl("xs_arena_interior_vip");
            //new CommonFunctions().TeleportPlayerPedToThisPosition(new Vector3(2799.529f, -3930.539f, 184.000f), new Vector3(0.0f, 0.0f, 0.0f), 0.0f, true);
            Tick += MapCreator_Tick;
            MapCreatorMenu.AddMenuItem(SetName);
            MapCreatorMenu.AddMenuItem(EnableCamera);
            MapCreatorMenu.AddMenuItem(SetPlayerPos);
            MapCreatorMenu.AddMenuItem(SetCameraPos);
            MapCreatorMenu.AddMenuItem(SetChangeLoadOutPos);
            MapCreatorMenu.AddMenuItem(AddPrimaryWeaponObjPos);
            MapCreatorMenu.AddMenuItem(SeconderyWeaponObjPos);
            MapCreatorMenu.AddMenuItem(EquipmentWeaponObjPos);
            MapCreatorMenu.AddMenuItem(LoadotShowPlayPos);
            MapCreatorMenu.AddMenuItem(ChangeSkinCameraPos01);
            MapCreatorMenu.AddMenuItem(ChangeSkinCameraPos02);
            MapCreatorMenu.AddMenuItem(ChangeSkinCameraPos03);
            MapCreatorMenu.AddMenuItem(ChangeSkinPlayerPos);
            //MapCreatorMenu.AddMenuItem(MenuListItem);
            //MapCreatorMenu.AddMenuItem(SetDescribe);
            //MapCreatorMenu.AddMenuItem(AddIPLs);
            //MapCreatorMenu.AddMenuItem(AddSpawnPoint);
            //MapCreatorMenu.AddMenuItem(ClearAllSpawnPoints);
            //MapCreatorMenu.AddMenuItem(AddWeaponSpawnPoint);
            //MapCreatorMenu.AddMenuItem(ClearAllWepSPoints);
            //MapCreatorMenu.AddMenuItem(SetMiddlePos);
            //MapCreatorMenu.AddMenuItem(SetRangeNumber);
            //MapCreatorMenu.AddMenuItem(AddPickupWeapon);
            //MapCreatorMenu.AddMenuItem(SetTime);
            //MapCreatorMenu.AddMenuItem(PrintData);
            MapCreatorMenu.AddMenuItem(Save);
            MapCreatorMenu.IgnoreDontOpenMenus = true;
            MenuController.AddMenu(MapCreatorMenu);

            MapCreatorMenu.OnListItemSelect += MapCreatorMenu_OnListItemSelect;

            MapCreatorMenu.OnItemSelect += async (menu, menuItem, itemIndex) =>
            {
                await Delay(0);

                if (menuItem == EnableCamera)
                {
                    if (Cam == null)
                    {
                        Cam = CommonFunctions.CreateCameraAtThisPos(Game.PlayerPed.Position, GameplayCamera.Rotation);
                        RenderScriptCams(true, true, 1000, true, true);                            
                    }
                }
                else if (menuItem == SetPlayerPos)
                {
                    mainMenu.PlayerPedPos = Game.PlayerPed.Position;
                    mainMenu.PlayerHeading = Game.PlayerPed.Heading;
                    BaseUI.ShowNotfi("done");

                    CreatMarker(Game.PlayerPed.Position, Game.PlayerPed.Rotation, b:100);
                }
                else if (menuItem == SetCameraPos)
                {
                    if (Cam == null)
                        return;

                    mainMenu.CameraPos = Cam.Position;
                    mainMenu.CameraRot = Cam.Rotation;
                    BaseUI.ShowNotfi("done");

                    CreatMarker(Cam.Position, Cam.Rotation);
                }
                else if (menuItem == SetChangeLoadOutPos)
                {
                    if (Cam == null)
                        return;

                    mainMenu.ChangeLoadOutPos = Cam.Position;
                    BaseUI.ShowNotfi("done");

                    CreatMarker(Cam.Position, Cam.Rotation);
                }
                else if (menuItem == AddPrimaryWeaponObjPos)
                {
                    if (Cam == null)
                        return;

                    mainMenu.PrimaryWeaponObjPos = Cam.Position;
                    BaseUI.ShowNotfi("done");

                    CreatMarker(Cam.Position, Cam.Rotation);
                }
                else if (menuItem == SeconderyWeaponObjPos)
                {
                    if (Cam == null)
                        return;

                    mainMenu.SeconderyWeaponObjPos = Cam.Position;
                    BaseUI.ShowNotfi("done");

                    CreatMarker(Cam.Position, Cam.Rotation);
                }
                else if (menuItem == EquipmentWeaponObjPos)
                {
                    if (Cam == null)
                        return;

                    mainMenu.EquipmentWeaponObjPos = Cam.Position;
                    BaseUI.ShowNotfi("done");

                    CreatMarker(Cam.Position, Cam.Rotation);
                }
                else if (menuItem == LoadotShowPlayPos)
                {

                    mainMenu.LoadotShowPlayPos = Game.PlayerPed.Position;
                    BaseUI.ShowNotfi("done");

                    CreatMarker(Game.PlayerPed.Position, Game.PlayerPed.Rotation, r:100);
                }
                else if (menuItem == ChangeSkinCameraPos01)
                {
                    if (Cam == null)
                        return;

                    mainMenu.ChangeSkinCameraPos01 = Cam.Position;
                    mainMenu.ChangeSkinCameraRot01 = Cam.Rotation;
                    BaseUI.ShowNotfi("done");

                    CreatMarker(Cam.Position, Cam.Rotation);
                }
                else if (menuItem == ChangeSkinCameraPos02)
                {
                    if (Cam == null)
                        return;

                    mainMenu.ChangeSkinCameraPos02 = Cam.Position;
                    mainMenu.ChangeSkinCameraRot02 = Cam.Rotation;
                    BaseUI.ShowNotfi("done");

                    CreatMarker(Cam.Position, Cam.Rotation);
                }
                else if (menuItem == ChangeSkinCameraPos03)
                {
                    if (Cam == null)
                        return;

                    mainMenu.ChangeSkinCameraPos03 = Cam.Position;
                    mainMenu.ChangeSkinCameraRot03 = Cam.Rotation;
                    BaseUI.ShowNotfi("done");
                    CreatMarker(Cam.Position, Cam.Rotation);
                }
                else if (menuItem == ChangeSkinPlayerPos)
                {

                    mainMenu.ChangeSkinPlayerPos = Game.PlayerPed.Position;
                    BaseUI.ShowNotfi("done");

                    CreatMarker(Game.PlayerPed.Position, Game.PlayerPed.Rotation, 100);
                }
                //if (menuItem == AddSpawnPoint)
                //{
                //    if (DeathMatch.SpawnPoints == null)
                //        DeathMatch.SpawnPoints = new List<IRVDMShared.MatchSpawnPoint>();

                //    DeathMatch.SpawnPoints.Add(new IRVDMShared.MatchSpawnPoint(Game.PlayerPed.Position, Game.PlayerPed.Heading));
                //}
                //else if (menuItem == AddWeaponSpawnPoint)
                //{
                //    if (DeathMatch.PickupWeaponSpawnPoints == null)
                //    {
                //        DeathMatch.PickupWeaponSpawnPoints = new List<Vector3>();
                //    }

                //    DeathMatch.PickupWeaponSpawnPoints.Add(Game.PlayerPed.Position);
                //}
                //else if (menuItem == SetMiddlePos)
                //{
                //    DeathMatch.MiddlePosition = Game.PlayerPed.Position;
                //}
                //else if (menuItem == SetRangeNumber)
                //{
                //    string imput = await CommonFunctions.GetUserInput("enter a number", "10", 4);
                //    if (int.TryParse(imput, out int number))
                //    {
                //        DeathMatch.RangeNumber = number;
                //    }
                //    else
                //    {
                //        BaseUI.ShowNotfi("Please enter a number");
                //    }
                //}
                else if (menuItem == SetName)
                {
                    mainMenu.Name = await CommonFunctions.GetUserInput("set name", "MapName", 999);
                }
                //else if (menuItem == SetDescribe)
                //{
                //    DeathMatch.Description = await CommonFunctions.GetUserInput("describe", "Change me!", 99999);
                //}
                //else if (menuItem == AddIPLs)
                //{
                //    string ipl = await CommonFunctions.GetUserInput("ENTER VALI IPL", "", 999);

                //    if (!string.IsNullOrEmpty(ipl))
                //    {
                //        DeathMatch.IPLs.Add(ipl);
                //        RequestIpl(ipl);
                //    }
                //}
                //else if (menuItem == AddPickupWeapon)
                //{
                //    if (Game.PlayerPed.Weapons.Current.Hash != WeaponHash.Unarmed)
                //    {
                //        DeathMatch.PickupWeaponHashes.Add((uint)Game.PlayerPed.Weapons.Current.Hash);
                //        BaseUI.ShowNotfi($"add this wep {Game.PlayerPed.Weapons.Current.LocalizedName}");
                //    }
                //    else
                //    {
                //        BaseUI.ShowNotfi("select a weapon first");
                //    }
                //}
                //else if (menuItem == SetTime)
                //{
                //    string imput = await CommonFunctions.GetUserInput("enter time in milisceond", "10", 99);
                //    if (int.TryParse(imput, out int number))
                //    {
                //        DeathMatch.TimeInMiliSecond = number;
                //    }
                //    else
                //    {
                //        BaseUI.ShowNotfi("Please enter a number");
                //    }
                //}
                //else if (menuItem == ClearAllSpawnPoints)
                //{
                //    DeathMatch.SpawnPoints.Clear();
                //    CitizenFX.Core.UI.Screen.ShowNotification("Done");
                //}
                //else if (menuItem == ClearAllWepSPoints)
                //{
                //    DeathMatch.PickupWeaponSpawnPoints.Clear();
                //    CitizenFX.Core.UI.Screen.ShowNotification("Done");
                //}
                //else if (menuItem == PrintData)
                //{
                //    Debug.WriteLine(JsonConvert.SerializeObject(DeathMatch, Formatting.Indented));
                //}
                else if (menuItem == Save)
                {
                    DeathMatch.Author = Game.Player.Name;

                    TriggerServerEvent("IRV:SV:SaveMap", JsonConvert.SerializeObject(mainMenu, Formatting.Indented));
                }
            };

            MapCreatorMenu.OnMenuOpen += (_) =>
            {
                if (Cam != null)
                {
                    RenderScriptCams(false, true, 1000, true, true);
                    Cam.Delete();
                    Cam = null;
                }

                ClearMarkers();
                DeathMatch = new IRVDMShared.DeathMatch();
                mainMenu = new MainMenuLocation();
            };

            MapCreatorMenu.OnMenuClose += _ =>
            {
                //foreach (var item in DeathMatch.IPLs)
                //{
                //    RemoveIpl(item);
                //}
            };

            RegisterCommand("creator", new Action(() =>
            {
                if (Enabled)
                    Enabled = false;
                else
                    Enabled = true;

            }), false /*This command is also not restricted, anyone can use it.*/ );
        }

        private void MapCreatorMenu_OnListItemSelect(Menu menu, MenuListItem listItem, int selectedIndex, int itemIndex)
        {
            //int id = GetInteriorAtCoords(2799.529f, -3930.539f, 184.000f);


            //if (IsInteriorPropEnabled(id, listItem.ListItems[selectedIndex]))
            //{
            //    DisableInteriorProp(id, listItem.ListItems[selectedIndex]);
            //}
            //else
            //{
            //    EnableInteriorProp(id, listItem.ListItems[selectedIndex]);
            //}


            //RefreshInterior(id);
        }

        public void CreatMarker(Vector3 pos, Vector3 pointTo, int r = 0, int g = 255, int b = 0)
        {
            int id = CreateCheckpoint((int)CheckpointIcon.SingleArrow, pos.X, pos.Y, pos.Z,
             pointTo.X, pointTo.Y, pointTo.Z, 0.5f, r, g, b, 255, 0);

            MenuPosDic.Add(id);
        }

        public void ClearMarkers()
        {
            if (MenuPosDic != null)
            {
                foreach (var item in MenuPosDic)
                {
                    DeleteCheckpoint(item);
                }                
            }

            MenuPosDic = new List<int>();
        }

        public async Task MapCreator_Tick()
        {
            if (Enabled)
            {
                if (Game.IsControlJustPressed(0, Control.MultiplayerInfo))
                {
                    MapCreatorMenu.OpenMenu();
                }

                if (Cam != null)
                {
                    Cam.Rotation = GameplayCamera.Rotation;
                    Cam.Position = new Vector3(Game.PlayerPed.Position.X, Game.PlayerPed.Position.Y, Game.PlayerPed.Position.Z + 0.25f);
                }

                if (MapCreatorMenu.Visible)
                {
                    if (DeathMatch != null)
                    {
                        //foreach (var item in DeathMatch.SpawnPoints)
                        //{

                        //    BaseUI.DrawMarker(0, item.Position, new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1f, 1f, 1f), 0, 0, 255, 255);
                        //}

                        //foreach (var item in DeathMatch.PickupWeaponSpawnPoints)
                        //{
                        //    BaseUI.DrawMarker(0, item, new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1f, 1f, 1f), 255, 0, 0, 255);
                        //}

                        //if (!DeathMatch.MiddlePosition.IsZero)
                        //{
                        //    BaseUI.DrawMarker(0, DeathMatch.MiddlePosition, new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1f, 1f, 1f), 0, 255, 0, 100);
                        //    if (DeathMatch.RangeNumber != 0)
                        //    {
                        //        BaseUI.DrawMarker(28, DeathMatch.MiddlePosition, new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(DeathMatch.RangeNumber, DeathMatch.RangeNumber, 30.0f), 255, 0, 0, 100);
                        //    }
                        //}
                    }
                }
            }
        }
    }
}
