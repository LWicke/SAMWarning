using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Core.Plugins;

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
        #region Oxide Hooks
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



        private object CanSamSiteShoot(SamSite site)
        {
            if (site == null) return 1;

            List<BasePlayer> targetPlayers = new List<BasePlayer>();
            switch (site.currentTarget.prefabID)
            {
                case 2278499844: //minicopter
                    MiniCopter targetCopter = site.currentTarget as MiniCopter;
                    BaseVehicleSeat driverSeat = null;
                    //if (targetCopter == null) PrintToChat("copter not found");
                    if (targetCopter != null) driverSeat = targetCopter.GetComponentsInChildren<BaseVehicleSeat>()[0];
                    //if (driverSeat == null) PrintToChat("seat not found");
                    if (driverSeat != null) targetPlayers.Add(driverSeat._mounted);
                    break;

                case 3111236903: //hotairballoon
                    HotAirBalloon targetBalloon = site.currentTarget as HotAirBalloon;
                    Vis.Entities(targetBalloon.transform.position, 2, targetPlayers);
                    break;

                default:
                    return false;
            }

            if (targetPlayers.Count == 0) return null;

            //calculate distance
            float distance = Vector3.Distance(site.transform.position, site.currentTarget.transform.position);

            //warning
            if (distance > firingRange)
            {
                foreach (var player in targetPlayers)
                {
                    //player.ChatMessage(warningMsg);
                    GUIAnnouncements?.Call("CreateAnnouncement", warningMsg, "grey", "white", player);
                    return false;
                }
            }

            //firing
            else
            {
                foreach (var player in targetPlayers)
                {
                    //player.ChatMessage(firingMsg);
                    GUIAnnouncements?.Call("CreateAnnouncement", firingMsg, "red", "white", player);
                    return null;
                }
            }
            return false;
        }

        #endregion
        #region helper

        T GetConfig<T>(string name, T defaultValue) => Config[name] == null ? defaultValue : (T)Convert.ChangeType(Config[name], typeof(T));
        #endregion

    }
}