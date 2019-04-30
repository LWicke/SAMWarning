using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("SAMWarning", "OHM", "1.0")]
    [Description("SAMSite warning System for Pilots")]
    public class SAMWarning : RustPlugin
    {

        [PluginReference]
        Plugin GUIAnnouncements;

        #region config
        private static float warningRange;
        private static float firingRange;
        private static String warningMsg;
        private static String firingMsg;
   

        protected override void LoadDefaultConfig()
        {
            Config["warningRange"] = warningRange = GetConfig("warningRange", 250f);
            Config["firingRange"] = firingRange = GetConfig("firingRange", 150f);
            Config["warningMsg"] = warningMsg = GetConfig("warningMsg", "You are being targeted by a SAM Site, Turn around NOW");
            Config["firingMsg"] = firingMsg = GetConfig("firingMsg", "SAM Site is now firing. you should have turned around...");
            SaveConfig();
        }
        #endregion
        #region Oxide_Hooks
        private void OnServerInitialized()
        {
            //configure all existing SamSites
            LoadDefaultConfig();
            var entities = BaseNetworkable.serverEntities.Where(p => p is SamSite).ToList();
            foreach (var entity in entities)
            {
                var samsite = entity as SamSite;
                samsite.scanRadius = warningRange;
            }
        }

        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            //configure SamSites when built
            var entity = go.GetComponent<BaseEntity>();
            if (entity is SamSite)
            {
                var samsite = entity as SamSite;
                samsite.scanRadius = warningRange;
            }
        }



        private object CanSamSiteShoot(SamSite samSite)
        {
            if (samSite == null) return false;

            List<BasePlayer> targetPlayers = new List<BasePlayer>();
            switch (samSite.currentTarget.prefabID)
            {
                case 2278499844: //minicopter
                    MiniCopter targetCopter = samSite.currentTarget as MiniCopter;
                    BaseVehicleSeat driverSeat = null;
                    //if (targetCopter == null) PrintToChat("copter not found");
                    if (targetCopter != null) driverSeat = targetCopter.GetComponentsInChildren<BaseVehicleSeat>()[0];
                    //if (driverSeat == null) PrintToChat("seat not found");
                    if (driverSeat != null) targetPlayers.Add(driverSeat._mounted);
                    break;

                case 3111236903: //hotairballoon
                    HotAirBalloon targetBalloon = samSite.currentTarget as HotAirBalloon;
                    Vis.Entities(targetBalloon.transform.position, 2, targetPlayers);
                    break;

                default:
                    return false;
            }

            if (targetPlayers.Count == 0) return null;

            //calculate distance
            float distance = Vector3.Distance(samSite.transform.position, samSite.currentTarget.transform.position);

            //warning
            if (distance > firingRange)
            {
                foreach (var player in targetPlayers)
                {
                    //player.ChatMessage(warningMsg);
                    GUIAnnouncements?.Call("CreateAnnouncement", getCustomMsg(false, samSite.GetInstanceID()), "grey", "white", player);
                    return false;
                }
            }

            //firing
            else
            {
                foreach (var player in targetPlayers)
                {
                    //player.ChatMessage(firingMsg);
                    GUIAnnouncements?.Call("CreateAnnouncement", getCustomMsg(true, samSite.GetInstanceID()), "red", "white", player);
                    return null;
                }
            }
            return false;
        }

        #endregion
        #region commands

        [ChatCommand("warningMsg")]
        private void CustomWarning(BasePlayer player, string command, string[] args)
        {
            if (args.Length >= 1)
            {
                RaycastHit hit = new RaycastHit();
                if (Physics.Raycast(player.eyes.HeadRay(), out hit, float.MaxValue))
                {
                    var entity = hit.GetEntity();
                    if (entity.GetBuildingPrivilege().authorizedPlayers.Any(x => x.userid == player.userID))
                    {
                        if (entity is SamSite)
                        {
                            setCustomMsg(false, entity.GetInstanceID(), args[0]);
                            player.ChatMessage("custom Message saved");
                        }
                        else player.ChatMessage("you are not looking at a SAM Site!");
                    }
                    else
                    {
                        player.ChatMessage("you are not authorized here!");
                    }
                }
            }
        }

        [ChatCommand("firingMsg")]
        private void CustomFiring(BasePlayer player, string command, string[] args) {
            if (args.Length >= 1)
            {
                RaycastHit hit = new RaycastHit();
                if (Physics.Raycast(player.eyes.HeadRay(), out hit, float.MaxValue))
                {
                    var entity = hit.GetEntity();
                    if (entity.GetBuildingPrivilege().authorizedPlayers.Any(x => x.userid == player.userID))
                    {
                        if (entity is SamSite)
                        {
                            setCustomMsg(true, entity.GetInstanceID(), args[0]);
                            player.ChatMessage("custom Message saved");
                        }
                        else player.ChatMessage("you are not looking at a SAM Site!");
                    }
                    else
                    {
                        player.ChatMessage("you are not authorized here!");
                    }
                }
            }
        }

        [ChatCommand("removeMsg")]
        private void customClear(BasePlayer player, string command, string[] args) {
            if (args.Length >= 1)
            {
                RaycastHit hit = new RaycastHit();
                if (Physics.Raycast(player.eyes.HeadRay(), out hit, float.MaxValue))
                {
                    var entity = hit.GetEntity();
                    if (entity.GetBuildingPrivilege().authorizedPlayers.Any(x => x.userid == player.userID))
                    {
                        if (entity is SamSite)
                    {
                        String ID = entity.GetInstanceID().ToString();
                        //removing msgs
                        DynamicConfigFile warnings = Interface.Oxide.DataFileSystem.GetDatafile("warningMsgs");
                        DynamicConfigFile firings = Interface.Oxide.DataFileSystem.GetDatafile("firingMsgs");
                        warnings.Remove(ID);
                        firings.Remove(ID);

                        player.ChatMessage("custom Messages removed");
                    }
                    else player.ChatMessage("you are not looking at a SAM Site!");
                    }
                    else
                    {
                        player.ChatMessage("you are not authorized here!");
                    }
                }
            }
        }


        #endregion
        #region helper
        private String getCustomMsg(bool firing, int ID)
        {
            DynamicConfigFile dataFile = null;
            if (firing)
            {
                dataFile = Interface.Oxide.DataFileSystem.GetDatafile("firingMsgs");
            }
            else
            {
                dataFile = Interface.Oxide.DataFileSystem.GetDatafile("warningMsgs");
            }
            if (dataFile[ID.ToString()] != null)
            {
                return (String)dataFile[ID.ToString()];
            }
            else if (firing) return firingMsg;
            else return warningMsg;
        }

        private void setCustomMsg(bool firing, int ID, String input)
        {
            DynamicConfigFile dataFile = null;
            if (firing)
            {
                dataFile = Interface.Oxide.DataFileSystem.GetDatafile("firingMsgs");
            }
            else
            {
                dataFile = Interface.Oxide.DataFileSystem.GetDatafile("warningMsgs");
            }
            dataFile[ID.ToString()] = input;
            dataFile.Save();
        }
        T GetConfig<T>(string name, T defaultValue) => Config[name] == null ? defaultValue : (T)Convert.ChangeType(Config[name], typeof(T));
        #endregion

    }
}