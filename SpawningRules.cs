using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HostileExploration
{
    public class SpawningRules
    {

        private TerritorySpawningRules _territory;
        private SpawnPointTerrainLevelRules _spawnPointTerrainLevel;
        private ExistingInstallationSpawningRules _existingInstallation;
        private GeneralSpawningRules _general;
        private PlayerDistanceSpawningRules _playerDistance;


        public TerritorySpawningRules Territory
        {
            get
            {
                return _territory ?? (_territory = new TerritorySpawningRules());
            }
            set
            {
                _territory = value;
            }
        }

        public SpawnPointTerrainLevelRules SpawnPointTerrainLevel
        {
            get
            {
                return _spawnPointTerrainLevel ?? (_spawnPointTerrainLevel = new SpawnPointTerrainLevelRules());
            }
            set
            {
                _spawnPointTerrainLevel = value;
            }
        }

        public ExistingInstallationSpawningRules ExistingInstallation
        {
            get
            {
                return _existingInstallation ?? (_existingInstallation = new ExistingInstallationSpawningRules());
            }
            set
            {
                _existingInstallation = value;
            }
        }

        public GeneralSpawningRules General
        {
            get
            {
                return _general ?? (_general = new GeneralSpawningRules());
            }
            set
            {
                _general = value;
            }
        }

        public PlayerDistanceSpawningRules PlayerDistance
        {
            get
            {
                return _playerDistance ?? (_playerDistance = new PlayerDistanceSpawningRules());
            }
            set
            {
                _playerDistance = value;
            }
        }
    }

    public class GeneralSpawningRules
    {



        /// <summary>
        /// Seconds until mod attempts to spawn new Installation(s)
        /// </summary>
        public int SpawnTimerTrigger { get; set; }


        /// <summary>
        /// No more than this number of active installations on a planet at a time.
        /// </summary>
        public int MaxActiveInstallationsPerPlanet { get; set; }

        /// <summary>
        /// Installations will not spawn if any grid is within this distance of proposed spawn location.
        /// </summary>
        public double MinDistFromOtherGrids { get; set; }

        /// <summary>
        /// If spawning small station, this is chance percent a medium station will appear instead.
        /// </summary>
        public int MediumInstallationChanceBase { get; set; }

        /// <summary>
        /// For each small station spawn, mediumInstallationChanceBase increases by this amount
        /// </summary>
        public int MediumInstallationChanceIncrement { get; set; }

        public int MediumInstallationAttempts { get; set; }

        /// <summary>
        /// If spawning medium station, this is chance percent a large station will appear instead.
        /// </summary>
		public int LargeInstallationChanceBase { get; set; }

        /// <summary>
        /// For each medium station spawn, largeInstallationChanceBase increases by this amount
        /// </summary>
        public int LargeInstallationChanceIncrement { get; set; }

        public int LargeInstallationAttempts { get; set; }

        /// <summary>
        /// Spawn will initiate in player area once player travels this distance along the surface.
        /// </summary>
        public double PlayerTravelTrigger { get; set; }


        /// <summary>
        /// Custom Congfig File Reading and Sanity Checks
        /// </summary>
        /// <param name="loadconfig"> the PlanataryInstallationsConfig</param>
        public void LoadConfig(PlanetaryInstallationsConfig loadconfig)
        {


            SpawnTimerTrigger = loadconfig.SpawnTimerTrigger;
            MaxActiveInstallationsPerPlanet = loadconfig.MaximumActiveStationsPerPlanet;
            MinDistFromOtherGrids = loadconfig.MinimumSpawnDistanceFromOtherGrids;
            MediumInstallationChanceBase = loadconfig.MediumSpawnChanceBaseValue;
            MediumInstallationChanceIncrement = loadconfig.MediumSpawnChanceIncrement;
            MediumInstallationAttempts = loadconfig.MediumInstallationAttempts;
            LargeInstallationChanceBase = loadconfig.LargeSpawnChanceBaseValue;
            LargeInstallationChanceIncrement = loadconfig.LargeSpawnChanceIncrement;
            LargeInstallationAttempts = loadconfig.LargeInstallationAttempts;
            PlayerTravelTrigger = loadconfig.PlayerDistanceSpawnTrigger;
        }
    }

    public class PlayerDistanceSpawningRules
    {
        /// <summary>
        /// Minimum distance from player to spawn at.
        /// </summary>
        public double MinSpawnDistFromPlayer { get; set; }

        /// <summary>
        /// Maximum distance from player to spawn at.
        /// </summary>
        public double MaxSpawnDistFromPlayer { get; set; }

        /// <summary>
        /// This value is added to the min/max spawndistfromplayer if installation is Medium
        /// </summary>
        public double MediumSpawnDistFromPlayerAdd { get; set; }

        /// <summary>
        /// This value is added to the min/max spawndistfromplayer if installation is Large
        /// </summary>
        public double LargeSpawnDistFromPlayerAdd { get; set; }
    }

    public class ExistingInstallationSpawningRules
    {
        /// <summary>
        /// Minimum distance from exisiting spawn location to spawn at.
        /// </summary>
        public double MinSpawnDistFromExisting { get; set; }
    }

    public class SpawnPointTerrainLevelRules
    {

        /// <summary>
        /// Number of times the script will try to find an area to spawn around a player.
        /// </summary>
        public int RandomLocationAttempts { get; set; }

        public int MediumLocationAttemptIncrement { get; set; }
        public int LargeLocationAttemptIncrement { get; set; }

        /// <summary>
        /// While checking near-by terrain, checked points cannot be lower than this value compared to the initial spawn location.
        /// </summary>
		public double MinTerrainLevelVariance { get; set; }

        /// <summary>
        /// While checking near-by terrain, checked points cannot be higher than this value compared to the initial spawn location.
        /// </summary>
        public double MaxTerrainLevelVariance { get; set; }

        /// <summary>
        /// Terrain checks in 8 directions are increased by this distance
        /// </summary>
        public double TerrainCheckIncrement { get; set; }

        /// <summary>
        /// Small station terrain checks are done until this distance is reached in 8 directions from spawn location
        /// </summary>
        public double SmallCheckDistance { get; set; }

        /// <summary>
        /// Medium station terrain checks are done until this distance is reached in 8 directions from spawn location 
        /// </summary>
        public double MediumCheckDistance { get; set; }

        /// <summary>
        /// Large station terrain checks are done until this distance is reached in 8 directions from spawn location
        /// </summary>
        public double LargeCheckDistance { get; set; }
    }

    public class TerritorySpawningRules
    {

        public bool IgnoreTerritoryRules { get; set; }
        public bool ReuseTerritories { get; set; }
    }

}