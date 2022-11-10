using CitizenFX.Core;
using System;
using static CitizenFX.Core.Native.API;

namespace IRVDM
{
    internal class GameEventManager : BaseScript
    {
        public GameEventManager()
        {
            EventHandlers["gameEventTriggered"] += new Action<string, dynamic>(GameEventTriggered);
        }

        public static event Action<int, uint> OnPlayerKilled;
        public static event Action<int, uint, bool, bool> OnPlayerKillPlayer;
        //public static event Action<int, uint, Vector3> OnPlayerEplodeVehicle;
        //public static event Action<int, uint, bool, bool> OnPlayerKillPed;
        public static event Action<int, int> OnPlayerCollectedPickup;
        /// <summary>
        /// triggers only when victim is alive 
        /// </summary>
        public static event Action<int, uint> OnPlayerHeadShotPlayer;

        private void GameEventTriggered(string eventName, dynamic eventData)
        {
            if (eventName == "CEventNetworkEntityDamage")
            {
                int victim = (int)eventData[0];
                int attacker = (int)eventData[1];
                bool IsEntityDead = ((int)eventData[3] == 1);
                uint WeaponHash = (uint)eventData[4];
                int unknown3 = (int)eventData[8];
                bool boolIsMeleeDamage = ((int)eventData[9] != 0);

                //MainDebug.WriteDebug($"hash = {WeaponHash.ToString()}", MainDebug.Prefixes.info);
                //foreach (var item in weaponNames.Keys)
                //{
                //    if (WeaponHash == GetHashKey(item))
                //    {
                //        MainDebug.WriteDebug($"wep = {item}", MainDebug.Prefixes.info);
                //    }
                //}

                if (IsEntityDead)
                {
                    if (Game.Player.Character.Handle == victim)
                    {
                        if (IsEntityAPed(attacker))
                        {
                            if (IsPedAPlayer(attacker))
                            {
                                if (attacker == Game.PlayerPed.Handle)
                                {
                                    TriggerServerEvent("IRV:SV:onPlayerKilled", -1, WeaponHash);
                                    OnPlayerKilled?.Invoke(-1, WeaponHash);
                                }
                                else
                                {
                                    bool found = false;
                                    foreach (Player player in Players)
                                    {
                                        if (player.Character.Handle == attacker)
                                        {
                                            TriggerServerEvent("IRV:SV:onPlayerKilled", player.ServerId, WeaponHash);
                                            OnPlayerKilled?.Invoke(player.ServerId, WeaponHash);
                                            found = true;
                                            break;
                                        }
                                    }
                                    if (!found)
                                    {
                                        TriggerServerEvent("IRV:SV:onPlayerKilled", -3, WeaponHash);
                                        OnPlayerKilled?.Invoke(-3, WeaponHash);
                                    }
                                }
                            }
                            else
                            {
                                TriggerServerEvent("IRV:SV:onPlayerKilled", -2, WeaponHash);
                                OnPlayerKilled?.Invoke(-2, WeaponHash);
                                MainDebug.WriteDebug($"npc = {WeaponHash.ToString()}", MainDebug.Prefixes.info);
                            }
                        }
                        else if (IsEntityAVehicle(attacker))
                        {
                            bool found = false;
                            foreach (Player playerKiller in Players)
                            {
                                if (playerKiller.Character.IsInVehicle())
                                {
                                    if (playerKiller.Character.CurrentVehicle.Handle == attacker)
                                    {
                                        TriggerServerEvent("IRV:SV:onPlayerKilled", playerKiller.ServerId, WeaponHash);
                                        OnPlayerKilled?.Invoke(playerKiller.ServerId, WeaponHash);
                                        found = true;
                                        break;
                                    }
                                }
                            }
                            if (!found)
                            {
                                TriggerServerEvent("IRV:SV:onPlayerKilled", -3, WeaponHash);
                                OnPlayerKilled?.Invoke(-3, WeaponHash);
                            }
                        }
                        else if (attacker == -1)
                        {
                            TriggerServerEvent("IRV:SV:onPlayerKilled", -1, WeaponHash);
                            OnPlayerKilled?.Invoke(-1, WeaponHash);
                        }
                        else
                        {
                            TriggerServerEvent("IRV:SV:onPlayerKilled", -3, WeaponHash);
                            OnPlayerKilled?.Invoke(-3, WeaponHash);
                        }
                    }
                    else if (Game.Player.Character.Handle == attacker)
                    {
                        if (IsEntityAPed(victim))
                        {
                            if (IsPedAPlayer(victim))
                            {
                                if (Game.Player.Character.Handle != victim)
                                {
                                    int bone = -1;
                                    int HeadBoneId = (int)Bone.SKEL_Head;
                                    if (GetPedLastDamageBone(victim, ref bone)) //return false if killed with throwble
                                    {
                                        if (HeadBoneId == bone)
                                        {
                                            OnPlayerKillPlayer?.Invoke(victim, WeaponHash, boolIsMeleeDamage, true);
                                        }
                                        else
                                            OnPlayerKillPlayer?.Invoke(victim, WeaponHash, boolIsMeleeDamage, false);
                                    }
                                    else
                                        OnPlayerKillPlayer?.Invoke(victim, WeaponHash, boolIsMeleeDamage, false);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (Game.Player.Character.Handle == attacker)
                    {
                        if (IsEntityAPed(victim))
                        {
                            if (IsPedAPlayer(victim))
                            {
                                if (Game.Player.Character.Handle != victim)
                                {
                                    int bone = -1;
                                    int HeadBoneId = (int)Bone.SKEL_Head;
                                    if (GetPedLastDamageBone(victim, ref bone))
                                    {
                                        if (HeadBoneId == bone)
                                        {
                                            if (!boolIsMeleeDamage)
                                            {
                                                OnPlayerHeadShotPlayer?.Invoke(victim, WeaponHash);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                //Debug.WriteLine(JsonConvert.SerializeObject(damageEventData, Formatting.Indented));
                //if (DoesEntityExist(damageEventData.data.attacker) && IsEntityAPed(damageEventData.data.attacker))
                //{
                //    Ped ped = (Ped)Ped.FromHandle(damageEventData.data.attacker);
                //    if (ped.IsPlayer && damageEventData.data.boolIsEntityDead)
                //    {
                //        int player = NetworkGetPlayerIndexFromPed(ped.Handle);
                //        if (NetworkIsPlayerActive(player))
                //        {
                //            string name = GetPlayerName(player);
                //        }
                //    }
                //}
            }

            else if (eventName == "CEventNetworkPlayerCollectedPickup")
            {
                int pickup = (int)eventData[0];
                int unkown0 = (int)eventData[1];//is it just ammo ? if yes return amount of it
                int unkown1 = (int)eventData[2];//its uinq for every pickup
                int unkown2 = (int)eventData[3]; //is it with gun itself if yes return ammo
                int unkown3 = (int)eventData[4]; //bool?
                int ammo = (int)eventData[5]; //ammo ? or ==> ammo


                OnPlayerCollectedPickup?.Invoke(pickup, ammo);
            }

            //else
            //{
            //    var d = new Dictionary<string, dynamic>
            //    {
            //        ["name"] = eventName,
            //    };
            //    int i = 0;
            //    foreach (var data in eventData)
            //    {
            //        d.Add($"unk_{i}", data);
            //        i++;
            //    }
            //    Debug.WriteLine(eventName + ": " + JsonConvert.SerializeObject(d, Formatting.Indented));
            //}
        }
    }
}
