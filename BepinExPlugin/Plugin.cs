using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BepInEx;
using EFT;
using EFT.Game.Spawning;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;


namespace RealPlayerSpawnPlugin
{
    //created this static class to stores values accross all modulepatch calls, very usefull but idk if there is a better way
    public static class pluginSharedValues
    {
        public static int pmc_spawned = 0;
        public static int min_PMCs = 1;
        public static int max_PMCs = 1;
        public static ISpawnPoint playerSpawnPoint;
        public static ISpawnSystem spawnSystemClass;

        public static bool isScavRun;

        public static void ResetValues()
        {
            pmc_spawned = 0;
            min_PMCs = 1;
            max_PMCs = 1;
            isScavRun = false;
            playerSpawnPoint = null;
            spawnSystemClass = null;
        }

        //use this function to set player point, it will prevent overriding the value
        //because of the multiple "selectPlayerSpawn" method call, 
        //the value must be fixed the first time and never written except on reset
        public static void SetPlayerSpawnPoint(ISpawnPoint isp)
        {
            if (playerSpawnPoint == null) 
            {
                playerSpawnPoint = isp;
            }
        }
    }

    [BepInPlugin("RealPlayerSpawn.UniqueGUID", "RealPlayerSpawn", "1.0.0")]
    [BepInDependency("com.SPT.custom", "3.10.0")]
    public  class RealPlayerSpawn : BaseUnityPlugin
    {

        private void Awake()
        {
            new ModifyLocalGame().Enable();
            new ChangePmcSpawnWaves().Enable();
            new ChangePmcSpawnpoints().Enable();
            new GetPlayerSpawnPoint().Enable();
            new ResetDataWhenLeavingRaid().Enable();
        }
    }

    public class ModifyLocalGame : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            //right before you get the countdown before a raid, all info are created, ai are frozen, player haven't spawn
            return AccessTools.Method(typeof(LocalGame), "method_21"); 
        }

        [PatchPostfix]
        private static void PatchPostfix(LocalGame __instance)
        {
            pluginSharedValues.isScavRun = (__instance.Profile_0.Side == EPlayerSide.Savage);   //that's so elegent ;)

            pluginSharedValues.min_PMCs = __instance.Location_0.MinPlayers;
            pluginSharedValues.max_PMCs = __instance.Location_0.MaxPlayers;


            //consider delete the player spawn of the array of possible spawns
            //ConsoleScreen.Log( "playerspawnParams in  array ? " + Array.FindIndex(pluginSharedValues.pmcSpawns , pmcspawn => pmcspawn.Id == pluginSharedValues.playerSpawnPoint.Id) );

            ///research about spawn points : 
            ///only 3 EplayermMaskSides is used : "PMC", "savage", "all" 
            ///i don't know what "all" is refered about ? bosses , followers, gifter ??? 
            ///on every "savage" spawnpoint , the CorepointId is != 0, otherwise its 0
            /// almsot sure but every PMC point has a Infiltration value, 
            /// on certain locations, infiltration show what zone of the map you are (maybe used for ingame maps, or map to map trasit ?)
            /// some "Savage" spawn point have a BotZoneName empty

            /*
            ConsoleScreen.Log("players count : " + __instance.AllPlayers.Count); //return always 1 real person/user, does return all real players with fika isntalled ? 
            ConsoleScreen.Log("gameworld players count : "+ __instance.GameWorld_0.AllAlivePlayersList.Count);

            foreach (Player player in __instance.GameWorld_0.AllAlivePlayersList)
            {
                ConsoleScreen.Log("GameWorld_0 player name : " + player.Profile.Nickname + " (" + player.Side + ") spawned at position : " + player.Position.WideLog() + " / team id : " + player.TeamId + " isAi ? :" + player.IsAI);
            };

            BotsController BC = __instance.GetComponent<IBotGame>().BotsController;

            foreach (Player p in BC.Players)
            {
                ConsoleScreen.Log("BotController player " + p.name + " / " + p.Profile.Nickname);
            }
            */

        }

    }

    public class ChangePmcSpawnWaves : ModulePatch
    {
        protected override MethodBase GetTargetMethod() {  return AccessTools.Method( typeof(WavesSpawnScenario), "smethod_0"); }

        [PatchPrefix]
        public static bool PatchPreFix(GameObject game, WildSpawnWave[] waves, Func<BotWaveDataClass, Task> spawnAction, LocationSettingsClass.Location location = null)
        {
            bool found = false;
            List<WildSpawnWave> wavesList = waves.ToList();

            for (int i = wavesList.Count- 1; i >= 0; i--) 
            {
                if (wavesList[i].isPlayers == true )
                {
                    if (found == false)
                    {
                        found = true;
                        if( location != null)
                        {
                            wavesList[i].slots_min = location.MinPlayers - 1; //-1 because you must count yourself : ) 
                            wavesList[i].slots_max = location.MaxPlayers - 1;
                        }
                        else
                        {
                            wavesList[i].slots_min = pluginSharedValues.min_PMCs - 1;
                            wavesList[i].slots_min = pluginSharedValues.min_PMCs - 1;
                        }
                        wavesList[i].time_min = 0;
                        wavesList[i].time_max = 120;

                    }
                    else
                    {
                        wavesList.RemoveAt(i);
                    }   
                }
            }

            waves = wavesList.ToArray();

            return true;
        }
    }

    public class ChangePmcSpawnpoints : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotSpawner), "SpawnBotsInZoneOnPositions");
        }

        [PatchPrefix]
        public static bool PatchPrefix(List<ISpawnPoint> openedPositions, BotZone botZone, BotCreationDataClass data, Action<BotOwner> callback = null)
        {
            if ( data.Profiles[0].Side != EPlayerSide.Savage)
            {
                //this way there is less players, so less loot but easier to survive
                if( pluginSharedValues.pmc_spawned + data.Profiles.Count > pluginSharedValues.max_PMCs )
                {
                    return false;
                }

                /*
                //this way, a few more pmcs so more difficulty
                int ttlppl = (pluginSharedValues.pmc_spawned + data.Profiles.Count) - pluginSharedValues.max_PMCs;
                if (ttlppl > pluginSharedValues.max_PMCs )
                {
                    for (int i = ttlppl; i > 0; i--)
                    {
                        data.RemoveProfile( data.Profiles[i] );
                    }
                }

                //prevent NullException
                if(data.Profiles.Count == 0)
                {
                    return false;
                }
               */

                //get a spawnpoint just like a real player, spawnsystemclass instance get at  GetPlayerSpawnPoint modulepatch
                ISpawnPoint isp = pluginSharedValues.spawnSystemClass.SelectSpawnPoint(ESpawnCategory.Player, data.Profiles[0].Side);
                
                //prevent spawn at the same spawn as player
                while (isp.Id == pluginSharedValues.playerSpawnPoint.Id)  {  isp = pluginSharedValues.spawnSystemClass.SelectSpawnPoint(ESpawnCategory.Player, data.Profiles[0].Side);  }

                for( int i = 0; i < data.Profiles.Count; i++ )
                {
                    //openedPositions[i] = pluginSharedValues.playerSpawnPoint; //<- funny but no
                    isp.Position.Set(isp.Position.x, isp.Position.y+(i*3), isp.Position.z); //move it a little bit
                    openedPositions[i] = new SpawnPoint() //create new object and mix values, otherwise group of pmc will freeze
                    {
                        Id = isp.Id,
                        CorePointId = openedPositions[i].CorePointId,
                        Infiltration = isp.Infiltration,
                        Categories = openedPositions[i].Categories,
                        DelayToCanSpawnSec = openedPositions[i].DelayToCanSpawnSec,
                        Rotation = isp.Rotation,
                        Position = isp.Position,
                        Name = isp.Name,
                        Sides = isp.Sides
                    };
                    pluginSharedValues.pmc_spawned++;
                    Logger.LogInfo("pmc spawned : " + data.Profiles[i].Nickname + " (" + (i+1) + "/" + data.Profiles.Count + ") count : " + pluginSharedValues.pmc_spawned + " position :" + openedPositions[i].Position.WideLog());
                }

            }
            else
            {
                //return false; //for debug purposes, disables "savage" spawning
            }

            return true;
        }
    }

    public class GetPlayerSpawnPoint : ModulePatch
    {
        //SpawnSystemClass.SelectSpawnPoint is for the player only, usefull for fixing map to map transit wrong spawn
        //SpawnSystemClass.ValidateSpawnPosition is never called ..?
        //SpawnSystemClass.SelectPlayerSavageSpawn is called only for scav Runs 
        //SpawnSystemClass.SelectAISpawnPoints is called on each waves and define spawns for each type of ai 
        protected override MethodBase GetTargetMethod() { return AccessTools.Method(typeof(SpawnSystemClass), "GInterface418.SelectSpawnPoint"); }

        [PatchPrefix]
        public static bool PatchPrefix() { return true; } //pluginSharedValues.ResetValues();  

        [PatchPostfix]
        public static void PatchPostFix(SpawnSystemClass __instance, ref ISpawnPoint __result)
        {
            pluginSharedValues.SetPlayerSpawnPoint(__result);
            pluginSharedValues.spawnSystemClass = __instance;
        }
    } 
    
    public class ResetDataWhenLeavingRaid : ModulePatch
    {
        protected override MethodBase GetTargetMethod() { return AccessTools.Method(typeof(LocalGame), "Stop"); }

        [PatchPrefix]
        public static bool PatchPrefix() { pluginSharedValues.ResetValues(); return true; } 
    }





}

