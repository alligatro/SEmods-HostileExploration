using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace MeridiusIX{
	
	[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
	
	public class PlanetaryInstallations : MySessionComponentBase{
		
		//To-Do: Create Removal Exception For Unowned Grids before making a 'wrecks' mod.
		
		//General Spawning Rules
		public int spawnTimerTrigger = 30; //Seconds until mod attempts to spawn new Installation(s)
		public int maxActiveInstallationsPerPlanet = 30; //No more than this number of active installations on a planet at a time.
		public double minDistFromOtherGrids = 2500; //Installations will not spawn if any grid is within this distance of proposed spawn location.
		
		public int mediumInstallationChanceBase = 10; //If spawning small station, this is chance percent a medium station will appear instead.
		public int mediumInstallationChanceIncrement = 10; //For each small station spawn, mediumInstallationChanceBase increases by this amount
		public int mediumInstallationAttempts = 15;
		public int largeInstallationChanceBase = 5; //If spawning medium station, this is chance percent a large station will appear instead.
		public int largeInstallationChanceIncrement = 10; //For each medium station spawn, largeInstallationChanceBase increases by this amount
		public int largeInstallationAttempts = 10;
		
		public double playerTravelTrigger = 6000; //Spawn will initiate in player area once player travels this distance along the surface.
		public double maxPlayerDistFromSurface = 6000; //Spawning will not occur for player if they are this far from planet surface.
		
		//Player Distance Spawning Rules
		public double minSpawnDistFromPlayer = 3000; //Minimum distance from player to spawn at.
		public double maxSpawnDistFromPlayer = 6000; //Maximum distance from player to spawn at.
		public double mediumSpawnDistFromPlayerAdd = 2000; //This value is added to the min/max spawndistfromplayer if installation is Medium
		public double largeSpawnDistFromPlayerAdd = 4000; //This value is added to the min/max spawndistfromplayer if installation is Large
		
		//Existing Installation Spawning Rules
		public double minSpawnDistFromExisting = 3000; //Minimum distance from exisiting spawn location to spawn at.
		
		//Spawn Point Terrain Level Rules
		public int randomLocationAttempts = 150; //Number of times the script will try to find an area to spawn around a player.
		public int mediumLocationAttemptIncrement = 50;
		public int largeLocationAttemptIncrement = 100;
		public double minTerrainLevelVariance = -3.5; //While checking near-by terrain, checked points cannot be lower than this value compared to the initial spawn location.
		public double maxTerrainLevelVariance = 2.5; //While checking near-by terrain, checked points cannot be higher than this value compared to the initial spawn location.
		public double terrainCheckIncrement = 10; //Terrain checks in 8 directions are increased by this distance
		public double smallCheckDistance = 30; //Small station terrain checks are done until this distance is reached in 8 directions from spawn location
		public double mediumCheckDistance = 80; //Medium station terrain checks are done until this distance is reached in 8 directions from spawn location
		public double largeCheckDistance = 100; //Large station terrain checks are done until this distance is reached in 8 directions from spawn location
		
		//Despawn Rules
		double noPowerDespawnDist = 12000; //If installation loses power and isn't owned by player, it will despawn when all players are further than this distance.
		
		//Installation Regeneration Rules
		int installationRegenTimerTrigger = 300; //Seconds until installation attempts to regenerate blocks.
		int installationRegenMinBlocks = 15; //Minimum blocks that script will try to regenerate.
		int installationRegenMaxBlocks = 30; //Maximum blocks that script will try to regenerate.
		
		//Territory Spawning Rules
		bool ignoreTerritoryRules = false;
		bool reuseTerritories = false;
		
		//Medium/Large Station Tier Timeouts
		int stationSizeAttemptBuff = 0;
		int mediumInstallationAttemptTimeout = 0;
		int largeInstallationAttemptTimeout = 0;
		
		//NPC Faction Values
		List<IMyFaction> npcFactionList = new List<IMyFaction>(); //List of all Default Factions.
		long npcFounder = 0;
		
		//Installation Spawn Group Lists
		List<MySpawnGroupDefinition> smallInstallationSpawnGroups = new List<MySpawnGroupDefinition>();
		List<MySpawnGroupDefinition> mediumInstallationSpawnGroups = new List<MySpawnGroupDefinition>();
		List<MySpawnGroupDefinition> largeInstallationSpawnGroups = new List<MySpawnGroupDefinition>();
		bool foundValidSpawnGroup = false; //Used by script to determine if there are valid spawn groups where the player is located.
		
		//Entity Storage: Planet Territories
		Guid planetTerritoryStorageKey = new Guid("9A13A7EA-21A6-4E97-8114-6EE4FE52EEB5");
		string planetTerritoryStorageValue = "";
		List<Vector3D> planetTerritoriesUsed = new List<Vector3D>(); 
		
		//Entity Storage: Planet Next Installation
		Guid planetNextInstStorageKey = new Guid("7910F6EE-3B2C-4A99-921D-DAEDA6C26B1A");
		string planetNextInstStorageValue = "";
		int mediumInstallationChance = 0;
		int largeInstallationChance = 0;
		
		//Entity Storage: Planet Unique SpawnGroups
		Guid planetUniqueSpawnGroupsKey = new Guid("A61421E8-45A3-4F3B-9EB9-88BD94D906C6");
		string planetUniqueSpawnGroupsValue = "";
		List<string> planetUniqueSpawnGroupsUsed = new List<string>(); 
		
		//Entity Storage: Installation Grid
		Guid installationGridStorageKey = new Guid("8F7ECF87-4630-4F62-A490-81C0D4D75E6D");
		string installationStatus = ""; //Possible Status: Active / NoPower / Captured
		string installationSizeTier = "";
		string installationPlanet = "";
		bool installationRegenAllowed = false;
		
		//Player Tracker
		Dictionary<long, Vector3D> playerLocationTracker = new Dictionary<long, Vector3D>(); //Used to store player location on the planet to trigger spawning when they've traveled far enough.
		
		//New Installation Watcher
		bool searchForInstallations = false; //While true, the mod will scan for new installations after spawning has occured.
		List<string> tempStationName = new List<string>();
		List<string> tempStationSize = new List<string>();
		List<string> tempStationPlanet = new List<string>();
		List<bool> tempRegenAllowed = new List<bool>();
		List<Vector3D> tempStationCoords = new List<Vector3D>();
		List<long> tempStationOwner = new List<long>();
		List<bool> foundInstallation = new List<bool>();
		
		//Grid Regeneration
		int gridRegenTimerTrigger = 450;
		int gridRegenProcessTimer = 0;
		List<IMyCubeGrid> regenQueue = new List<IMyCubeGrid>();
		bool regenProcessing = false;
		bool gridRegenDebug = false;
		
		//Wreck Direction Template List
		List<Vector3D> wreckTemplateUpA = new List<Vector3D>();
		List<Vector3D> wreckTemplateForwardA = new List<Vector3D>();
		
		//Wreck Temp Directions
		Vector3D wreckTempDirUp = new Vector3D(0,0,0);
		Vector3D wreckTempDirForward = new Vector3D(0,0,0);
		
		//Territory Values
		List<string> territoryNameList = new List<string>();
		List<string> territoryTagList = new List<string>();
		List<Vector3D> territoryCoordsList = new List<Vector3D>();
		List<double> territoryRadiusList = new List<double>();
		
		//Spawning Queue
		List<IMyPlayer> installationSpawningQueue = new List<IMyPlayer>();
		bool successfulSpawn = false;
		
		//Active Installations
		List<IMyCubeGrid> activeInstallations = new List<IMyCubeGrid>();
		List<IMyCubeGrid> activeWrecks = new List<IMyCubeGrid>();
		
		//Lists for Players / Entities
		List<IMyPlayer> playerList = new List<IMyPlayer>();
		HashSet<IMyEntity> entityList = new HashSet<IMyEntity>();
		HashSet<MyPlanet> planetList = new HashSet<MyPlanet>();
		List<string> planetNameList = new List<string>();
		
		//Counters and Timer Triggers
		int scriptRun = 0; 
		int scriptRunTrigger = 60; 
		int installationMonitorTimer = 0;
		int installationMonitorTimerTrigger = 10;
		int spawnTimer = 0;
		int installationRegenTimer = 0;
		int gridRegenTimer = 0;
		
		//Misc
		Random rnd = new Random();
		Vector3D blankPosition = new Vector3D(0,0,0);
		bool debugMode = false;
		bool scriptInitialized = false;
		bool scriptInitFailed = false;		
		
		public override void UpdateBeforeSimulation(){
			
			if(MyAPIGateway.Multiplayer.IsServer == false){
				
				return;
				
			}
			
			if(scriptInitFailed == true){
				
				return;
				
			}
			
			if(scriptInitialized == false){
				
				Initialize();
				scriptInitialized = true;
				
			}
			
			if(scriptRun < scriptRunTrigger){
				
				scriptRun++;
				return;
				
			}
			
			if(regenProcessing == true){
				
				GridRegeneration();
				
			}
			
			scriptRun = 0;
			Main();
			

		}
		
		void Main(){
			
			spawnTimer++;
			installationMonitorTimer++;
			
			if(installationMonitorTimer >= installationMonitorTimerTrigger){
				
				installationMonitorTimer = 0;
				ActiveInstallationWatcher();
				
			}
			
			if(searchForInstallations == true){
				
				NewInstallationWatcher();
				
			}
			
			
			
			if(spawnTimer >= spawnTimerTrigger){
				
				spawnTimer = 0;
				InstallationSpawning();
				
			}
			
						
		}
		
		void Initialize(){
			
			RefreshLists();
			
			//Custom Congfig File Reading and Sanity Checks
			PlanetaryInstallationsConfig loadconfig = PlanetaryInstallationsConfig.LoadConfigFile();
			
			spawnTimerTrigger = loadconfig.SpawnTimerTrigger;
			maxActiveInstallationsPerPlanet = loadconfig.MaximumActiveStationsPerPlanet;
			minDistFromOtherGrids = loadconfig.MinimumSpawnDistanceFromOtherGrids;
			mediumInstallationChanceBase = loadconfig.MediumSpawnChanceBaseValue;
			mediumInstallationChanceIncrement = loadconfig.MediumSpawnChanceIncrement;
			mediumInstallationAttempts = loadconfig.MediumInstallationAttempts;
			largeInstallationChanceBase = loadconfig.LargeSpawnChanceBaseValue;
			largeInstallationChanceIncrement = loadconfig.LargeSpawnChanceIncrement;
			largeInstallationAttempts = loadconfig.LargeInstallationAttempts;
			playerTravelTrigger = loadconfig.PlayerDistanceSpawnTrigger;
			maxPlayerDistFromSurface = loadconfig.PlayerMaximumDistanceFromSurface;
			
			if(loadconfig.MinimumSpawnDistanceFromPlayers < loadconfig.MaximumSpawnDistanceFromPlayers){
				
				minSpawnDistFromPlayer = loadconfig.MinimumSpawnDistanceFromPlayers;
				maxSpawnDistFromPlayer = loadconfig.MaximumSpawnDistanceFromPlayers;
				
			}else{
				
				LogEntry("Config Warning: MinimumSpawnDistanceFromPlayers value should be less than MaximumSpawnDistanceFromPlayers value. Mod default values will be used instead.");
				
			}
			
			mediumSpawnDistFromPlayerAdd = loadconfig.MediumSpawnDistanceIncrement;
			largeSpawnDistFromPlayerAdd = loadconfig.LargeSpawnDistanceIncrement;
			minSpawnDistFromExisting = loadconfig.MinimumSpawnDistanceFromExistingSpawn;
			randomLocationAttempts = loadconfig.RandomTerrainSurfaceChecks;
			mediumLocationAttemptIncrement = loadconfig.MediumLocationAttemptIncrement;
			largeLocationAttemptIncrement = loadconfig.LargeLocationAttemptIncrement;
			
			if(loadconfig.MinimumTerrainVariance < loadconfig.MaximumTerrainVariance){
				
				minTerrainLevelVariance = loadconfig.MinimumTerrainVariance;
				maxTerrainLevelVariance = loadconfig.MaximumTerrainVariance;
				
			}else{
				
				LogEntry("Config Warning: MinimumTerrainVariance value should be less than MaximumTerrainVariance value. Mod default values will be used instead.");
				
			}
			
			terrainCheckIncrement = loadconfig.TerrainCheckIncrementDistance;
			smallCheckDistance = loadconfig.SmallTerrainCheckDistance;
			mediumCheckDistance = loadconfig.MediumTerrainCheckDistance;
			largeCheckDistance = loadconfig.LargeTerrainCheckDistane;
			noPowerDespawnDist = loadconfig.UnpoweredDespawnDistance;
			installationRegenTimerTrigger = loadconfig.RegenerationTimerTrigger;
			
			if(loadconfig.RegenerationMinimumBlocks < loadconfig.RegenerationMaximumBlocks){
				
				installationRegenMinBlocks = loadconfig.RegenerationMinimumBlocks;
				installationRegenMaxBlocks = loadconfig.RegenerationMaximumBlocks;
				
			}else{
				
				LogEntry("Config Warning: RegenerationMinimumBlocks value should be less than RegenerationMaximumBlocks value. Mod default values will be used instead.");
				
			}
			
			ignoreTerritoryRules = loadconfig.IgnoreTerritoryRules;
			reuseTerritories = loadconfig.ReuseTerritories;
			
			if(planetList.Count == 0){
				
				LogEntry("No Planets Found. Mod Cannot Initialize. Please Add Planets Or Use A Planet Start And Then Reload The World.");
				scriptInitFailed = true;
				return;
				
			}
			
			//Setup Wreck Direction Profiles
			
			wreckTemplateUpA.Add(new Vector3D(0.471277594566345,0.869170486927032,-0.149800226092339));
			wreckTemplateUpA.Add(new Vector3D(-0.333788454532623,0.87928694486618,0.339764207601547));
			wreckTemplateUpA.Add(new Vector3D(0.578473806381226,0.815646708011627,-0.00940560176968575));
			wreckTemplateUpA.Add(new Vector3D(0.0449797473847866,0.838983714580536,-0.542294383049011));
			wreckTemplateUpA.Add(new Vector3D(-0.37333881855011,0.819878816604614,0.434070110321045));
			
			wreckTemplateForwardA.Add(new Vector3D(0.131272882223129,-0.2370775192976,-0.962580740451813));
			wreckTemplateForwardA.Add(new Vector3D(0.0755081623792648,0.384217709302902,-0.920149564743042));
			wreckTemplateForwardA.Add(new Vector3D(0.152337029576302,-0.119354099035263,-0.981095314025879));
			wreckTemplateForwardA.Add(new Vector3D(0.111227437853813,-0.543674468994141,-0.83189332485199));
			wreckTemplateForwardA.Add(new Vector3D(0.0794214978814125,0.494431376457214,-0.8655806183815));
			
			//Get SpawnGroups
			var allSpawnGroups = MyDefinitionManager.Static.GetSpawnGroupDefinitions();
			int eligibleSpawnGroups = 0;
			foreach (var spawnGroup in allSpawnGroups){
				
				int frequency = (Int32)Math.Ceiling(spawnGroup.Frequency); //Get Spawn Group Frequency and Round Up.
				
				if (spawnGroup.IsEncounter == false && spawnGroup.IsPirate == true && spawnGroup.Id.SubtypeName.Contains("(Inst-")){
					
					eligibleSpawnGroups++;
					for(int i = 0; i < frequency; i++){
						
						if(spawnGroup.Id.SubtypeName.Contains("(Inst-1")){
							
							smallInstallationSpawnGroups.Add(spawnGroup);
							
						}
						
						if(spawnGroup.Id.SubtypeName.Contains("(Inst-2")){
							
							mediumInstallationSpawnGroups.Add(spawnGroup);
							
						}
						
						if(spawnGroup.Id.SubtypeName.Contains("(Inst-3")){
							
							largeInstallationSpawnGroups.Add(spawnGroup);
							
						}
						
					}
					
				}
				
				if(spawnGroup.Prefabs[0].SubtypeId == "TerritoryPlaceholder"){
					
					territoryNameList.Add(spawnGroup.Id.SubtypeName);
					territoryTagList.Add(spawnGroup.Prefabs[0].BeaconText);
					territoryCoordsList.Add((Vector3D)spawnGroup.Prefabs[0].Position);
					territoryRadiusList.Add((double)spawnGroup.Prefabs[0].Speed);
					
				}
				
			}
			
			
			
			LogEntry("Found " + eligibleSpawnGroups.ToString() + " Installation Spawn Groups During Startup.");
			
			//Get Default Factions
			var defaultFactionList = MyDefinitionManager.Static.GetDefaultFactions();
			foreach(var faction in defaultFactionList){
				
				var defaultFaction = MyAPIGateway.Session.Factions.TryGetFactionByTag(faction.Tag);
				
				if(defaultFaction != null){
					
					npcFactionList.Add(defaultFaction);
					
				}
				
			}

			//Get Active Installations
			foreach(var entity in entityList){
				
				var cubeGrid = entity as IMyCubeGrid;
				if(cubeGrid == null || MyAPIGateway.Entities.Exist(entity) == false){
					
					continue;
					
				}

				string gridStorageValue = "";
				
				if(entity.Storage == null){
					
					continue;
					
				}
				
				entity.Storage.TryGetValue(installationGridStorageKey, out gridStorageValue);

				if(gridStorageValue == null){
					
					continue;
				
				}

				if(gridStorageValue == ""){
					
					continue;
					
				}
				
				string [] storageSplit = gridStorageValue.Split('\n');
				
				if(storageSplit.Length < 4){
					
					continue;
					
				}
				
				installationStatus = storageSplit[0];
				installationSizeTier = storageSplit[1];
				installationPlanet = storageSplit[2];
				bool.TryParse( storageSplit[3], out installationRegenAllowed);
				
				if(installationStatus.Contains("Active")){
					
					activeInstallations.Add(cubeGrid);
					
				}
				
			}
			
		}
		
		void InstallationSpawning(){
			
			//Check Player Locations
			PlayerDistanceCheck();
				
			if(installationSpawningQueue.Count == 0){
				
				return;
				
			}
			
			LogEntry("One or more player has travelled " + playerTravelTrigger.ToString() + "m or more along a planet surface. Attempting to Spawn Installation(s).");
			RefreshLists();
			
			foreach(var player in installationSpawningQueue){
				
				//Get Nearest Planet (No checks needed to confirm player is on planet since this was already done earlier)
				MyPlanet planet = GetNearestPlanet(player.GetPosition());
				IMyEntity planetEntity = planet as IMyEntity;
				
				//Check if Planet is at installation limit.
				if(PlanetActiveInstallationCount(planet) >= maxActiveInstallationsPerPlanet){
					
					LogEntry("Active Installation Limit Reached Near Player " + player.DisplayName + " For Planet " + planet.StorageName);
					continue;
					
				}
				
				//Get Existing Territories
				planetTerritoriesUsed.Clear();
				planetTerritoryStorageValue = "";
				if(planetEntity.Storage == null){
					
					planetEntity.Storage = new MyModStorageComponent();
					
				}else{
					
					planetEntity.Storage.TryGetValue(planetTerritoryStorageKey, out planetTerritoryStorageValue);
					
					if(planetTerritoryStorageValue != null && planetTerritoryStorageValue != ""){
						
						string [] storageSplit = planetTerritoryStorageValue.Split('\n');
						for(int i = 0; i < storageSplit.Length; i++){
							
							if(storageSplit[i] != null && storageSplit[i] != ""){
								
								Vector3D territoryCoords = new Vector3D(0,0,0);
								Vector3D.TryParse(storageSplit[i], out territoryCoords);
								
								if(territoryCoords != blankPosition){
									
									planetTerritoriesUsed.Add(territoryCoords);
									
								}
								
							}
							
						}
						
					}
					
				}
				
				//Get Next Installation Size
				string installationSize = "Small";
				
				planetNextInstStorageValue = "";
				mediumInstallationChance = mediumInstallationChanceBase;
				largeInstallationChance = largeInstallationChanceBase;
				
				if(planetEntity.Storage == null){
					
					planetEntity.Storage = new MyModStorageComponent();
					mediumInstallationChance = mediumInstallationChanceBase;
					largeInstallationChance = largeInstallationChanceBase;
					
				}else{
					
					planetEntity.Storage.TryGetValue(planetNextInstStorageKey, out planetNextInstStorageValue);
					
					if(planetNextInstStorageValue != null && planetNextInstStorageValue != ""){
						
						string [] storageSplit = planetNextInstStorageValue.Split('\n');
						
						if(storageSplit.Length < 2){
							
							//Storage wasn't stored or retrieved properly - resetting targets.
							mediumInstallationChance = mediumInstallationChanceBase;
							largeInstallationChance = largeInstallationChanceBase;
							
						}else{
							
							Int32.TryParse(storageSplit[0], out mediumInstallationChance);
							Int32.TryParse(storageSplit[1], out largeInstallationChance);
							
							//We determine the station type via RNG.
							
							stationSizeAttemptBuff = 0;
							
							int randomChance = rnd.Next(0, 100);
							
							MySpawnGroupDefinition sampleSpawnGroup = GetRandomInstSpawnGroup(planet, "Small", true, player.GetPosition());
							
							if(randomChance < mediumInstallationChance || foundValidSpawnGroup == false){
								
								installationSize = "Medium";
								stationSizeAttemptBuff = mediumLocationAttemptIncrement;
								sampleSpawnGroup = GetRandomInstSpawnGroup(planet, "Medium", true, player.GetPosition());
								randomChance = rnd.Next(0, 100);
								
								if(randomChance < largeInstallationChance || foundValidSpawnGroup == false){
									
									installationSize = "Large";
									stationSizeAttemptBuff = largeLocationAttemptIncrement;
									sampleSpawnGroup = GetRandomInstSpawnGroup(planet, "Large", true, player.GetPosition());
									
									if(foundValidSpawnGroup == false){
										
										//Values Reset and Spawning Fails
										mediumInstallationChance = mediumInstallationChanceBase;
										largeInstallationChance = largeInstallationChanceBase;
										
									}
									
								}
								
							}
							
						}
						
					}
				
				}
				
				//Get Previously Spawned Unique Spawn Groups For Planet
				planetUniqueSpawnGroupsUsed.Clear();
				planetUniqueSpawnGroupsValue = "";
				planetEntity.Storage.TryGetValue(planetUniqueSpawnGroupsKey, out planetUniqueSpawnGroupsValue);
					
				if(planetUniqueSpawnGroupsValue != null && planetUniqueSpawnGroupsValue != ""){
					
					string [] storageSplit = planetUniqueSpawnGroupsValue.Split('\n');
					
					foreach(var uniqueGroup in storageSplit){
						
						if(uniqueGroup == "" || uniqueGroup == null){
							
							continue;
							
						}
						
						planetUniqueSpawnGroupsUsed.Add(uniqueGroup);
						
					}
					
				}
				
				//Try Spawning Near The Player
				LogEntry("Attempting Station Spawning Near Player: " + player.DisplayName);
				TrySpawningInstallation(player.GetPosition(), planet, installationSize);
				
				//If successful, rebuild territory list for planet.
				if(successfulSpawn == true){
					
					//Rebuild Territory Storage
					planetTerritoryStorageValue = "";
					
					foreach(var coords in planetTerritoriesUsed){
						
						planetTerritoryStorageValue += coords.ToString() + "\n";
						
					}
					
					planetEntity.Storage[planetTerritoryStorageKey] = planetTerritoryStorageValue;
					
					//Rebuild Planet Unique Spawn Groups
					planetUniqueSpawnGroupsValue = "";
					
					foreach(var uniqueGroup in planetUniqueSpawnGroupsUsed){
						
						planetUniqueSpawnGroupsValue += uniqueGroup + "\n";
						
					}
					
					planetEntity.Storage[planetUniqueSpawnGroupsKey] = planetUniqueSpawnGroupsValue;
					
				}
				
				//Rebuild Next Installation Target Storage
				
				if(installationSize == "Small" && successfulSpawn == true){
					
					mediumInstallationChance += mediumInstallationChanceIncrement;
					
				}
				
				if(installationSize == "Medium" && successfulSpawn == true){
					
					mediumInstallationAttemptTimeout = 0;
					mediumInstallationChance = mediumInstallationChanceBase;
					largeInstallationChance += largeInstallationChanceIncrement;
					
				}
				
				if(installationSize == "Large" && successfulSpawn == true){
					
					largeInstallationAttemptTimeout = 0;
					largeInstallationChance = largeInstallationChanceBase;
					
				}
				
				LogEntry("Debug: MediumChance: " + mediumInstallationChance.ToString());
				LogEntry("Debug: LargeChance: " + largeInstallationChance.ToString());
				
				planetNextInstStorageValue = mediumInstallationChance.ToString() + "\n";
				planetNextInstStorageValue += largeInstallationChance.ToString();
				
				planetEntity.Storage[planetNextInstStorageKey] = planetNextInstStorageValue;
				
			}
			
			installationSpawningQueue.Clear();
			
		}
		
		void PlayerDistanceCheck(){
			
			playerList.Clear();
			MyAPIGateway.Players.GetPlayers(playerList);
			
			foreach(var player in playerList){
			
				//Check For Bots
				if(player.IsBot == true){
					
					continue;
					
				}
				
				//Get Nearest Planet & Check if Player is in Range
				MyPlanet planet = GetNearestPlanet(player.GetPosition());
				IMyEntity planetEntity = planet as IMyEntity;
				Vector3D playerCoords = player.GetPosition();
				bool playerInGravity = planetEntity.Components.Get<MyGravityProviderComponent>().IsPositionInRange(player.GetPosition());
				Vector3D playerSurfacePoint = planet.GetClosestSurfacePointGlobal(ref playerCoords);
				
				if(playerInGravity == false || MeasureDistance(playerSurfacePoint, player.GetPosition()) > maxPlayerDistFromSurface){
					
					playerLocationTracker.Remove(player.PlayerID);
					continue;
					
				}
				
				//Check For Existing Dictionary Entry
				if(playerLocationTracker.ContainsKey(player.PlayerID)){
					
					Vector3D playerPreviousLocation = playerLocationTracker[player.PlayerID];
					
					if(MeasureDistance(playerSurfacePoint, playerPreviousLocation) > playerTravelTrigger){
						
						playerLocationTracker[player.PlayerID] = playerSurfacePoint;
						installationSpawningQueue.Add(player);
						
					}
					
				}else{
					
					playerLocationTracker.Add(player.PlayerID, playerSurfacePoint);
					continue;
					
				}
				
			}
		
		}
		
		void TrySpawningInstallation(Vector3D playerPosition, MyPlanet planet, string installationSize){
			
			successfulSpawn = false;
			IMyEntity planetEntity = planet as IMyEntity;
			Vector3D spawningCoords = new Vector3D(0,0,0);
			Vector3D upwardDirection = Vector3D.Normalize(playerPosition - planetEntity.GetPosition());
			double spawnDistanceModifier = 0;
			double terrainCheckModifier = smallCheckDistance;
			bool gotSpawningLocation = false;
			
			//Try Getting a Valid Spawn Group
			MySpawnGroupDefinition installationSpawnGroup = GetRandomInstSpawnGroup(planet, installationSize, false, playerPosition);
			
			if(foundValidSpawnGroup == false){
				
				LogEntry("No suitable "+installationSize+" spawngroup could be found for spawning at this location");
				return;
				
			}
			
			if(installationSize == "Small"){
				
				terrainCheckModifier = smallCheckDistance;
				
			}
					
			if(installationSize == "Medium"){
				
				spawnDistanceModifier = mediumSpawnDistFromPlayerAdd;
				terrainCheckModifier = mediumCheckDistance;
				
			}
			
			if(installationSize == "Large"){
				
				spawnDistanceModifier = largeSpawnDistFromPlayerAdd;
				terrainCheckModifier = largeCheckDistance;
				
			}
			
			//Try to get valid spawning location near player
			for(int i = 0; i < randomLocationAttempts + stationSizeAttemptBuff; i++){
				
				//Get Random Spawning Area
				double distanceToSpawn = RandomNumberBetween(minSpawnDistFromPlayer, maxSpawnDistFromPlayer) + spawnDistanceModifier; //Calculates how far from the player to attempt spawning
				Vector3D randomDirection = MyUtils.GetRandomPerpendicularVector(ref upwardDirection); //Picks a random 'compass direction' from the player
				Vector3D roughSpawningArea = randomDirection * distanceToSpawn + playerPosition; //Draws a line from the player using the random distance and direction determined above.
				Vector3D surfacePoint = planet.GetClosestSurfacePointGlobal(ref roughSpawningArea); //Get the position of the surface either above or below where the end of the line was drawn.
				
				//Run Checks On Location
				if(CheckProposedSpawnLocation(surfacePoint) == false){
					
					continue;
					
				}
				
				//Check Terrain Level Of Spawning Area
				bool terrainIsLevel = true;
				double distanceToCore = MeasureDistance(surfacePoint, planetEntity.GetPosition());
				Vector3D spawningUpDirection = Vector3D.Normalize(surfacePoint - planetEntity.GetPosition());
				Vector3D spawningRandomDirection = MyUtils.GetRandomPerpendicularVector(ref spawningUpDirection);
				MatrixD levelCheckMatrix = MatrixD.CreateWorld(surfacePoint, spawningRandomDirection, spawningUpDirection);
				
				for(double j = 10; j < terrainCheckModifier; j += terrainCheckIncrement){
					
					if(terrainIsLevel == false){
						
						break;
						
					}
					
					List<Vector3D> directionChecksList = new List<Vector3D>();
					directionChecksList.Add(new Vector3D(j, 0, 0));
					directionChecksList.Add(new Vector3D(j * -1, 0, 0));
					directionChecksList.Add(new Vector3D(0, 0, j));
					directionChecksList.Add(new Vector3D(0, 0, j * -1));
					directionChecksList.Add(new Vector3D(j, 0, j));
					directionChecksList.Add(new Vector3D(j * -1, 0, j));
					directionChecksList.Add(new Vector3D(j, 0, j * -1));
					directionChecksList.Add(new Vector3D(j * -1, 0, j * -1));

					//Check Direction
					foreach(var directionCheckPoint in directionChecksList){
						
						Vector3D levelCheck = Vector3D.Transform(directionCheckPoint, levelCheckMatrix);
						Vector3D levelCheckSurface = planet.GetClosestSurfacePointGlobal(ref levelCheck);
						double levelCheckCoreDist = MeasureDistance(levelCheckSurface, planetEntity.GetPosition());
						double levelDifference = levelCheckCoreDist - distanceToCore;
						
						if(levelDifference < minTerrainLevelVariance || levelDifference > maxTerrainLevelVariance){
							
							terrainIsLevel = false;
							break;
							
						}
						
					}
					
				}
				
				if(terrainIsLevel == false){
					
					continue;
					
				}
				
				spawningCoords = surfacePoint;
				gotSpawningLocation = true;
				LogEntry("Found Terrain For Station Spawning After " + i.ToString() + " Attempts");
				break;
				
			}
			
			if(gotSpawningLocation == false){
				
				LogEntry("Could Not Find Terrain For Station Spawning After " + randomLocationAttempts.ToString() + " Attempts");
				
				if(installationSize == "Medium"){
					
					mediumInstallationAttemptTimeout++;
					
					if(mediumInstallationAttemptTimeout >= mediumInstallationAttempts){
						
						mediumInstallationChance = mediumInstallationChanceBase;
						
					}
					
				}
				
				if(installationSize == "Large"){
					
					largeInstallationAttemptTimeout++;
					
					if(largeInstallationAttemptTimeout >= largeInstallationAttempts){
						
						largeInstallationChance = largeInstallationChanceBase;
						
					}
					
				}
				
				return;
				
			}
			
			//Setup the Matrix that all the Prefabs will use.
			Vector3D coreDirection = Vector3D.Normalize(spawningCoords - planetEntity.GetPosition());
			Vector3D randomForward = MyUtils.GetRandomPerpendicularVector(ref coreDirection);
			MatrixD spawnGroupMatrix = MatrixD.CreateWorld(spawningCoords, randomForward, coreDirection);
			List<IMyCubeGrid> tempList = new List<IMyCubeGrid>();
			
			//Now we iterate through prefabs from the spawn group.
			
			int prefabIndex = 0;
			foreach(var prefab in installationSpawnGroup.Prefabs){
				
				Vector3D prefabPosition = (Vector3D)prefab.Position;
				Vector3D prefabUp = coreDirection;
				Vector3D prefabForward = randomForward;
				Vector3D prefabRoughSurface = Vector3D.Transform(new Vector3D(prefabPosition.X, 0, prefabPosition.Z), spawnGroupMatrix);
				Vector3D prefabSurface = planet.GetClosestSurfacePointGlobal(ref prefabRoughSurface);
				MatrixD prefabMatrix = MatrixD.CreateWorld(prefabSurface, prefabForward, prefabUp);
				//Some Shit Is Wrong With Height Offset - Figure It Out
				Vector3D prefabHeightOffset = new Vector3D(0,0,0);
				prefabHeightOffset.Y = prefabPosition.Y;
				Vector3D prefabSpawningPosition = Vector3D.Transform(prefabHeightOffset, prefabMatrix);
				
				//Check if SpawnGroup is a Wreck
				wreckTempDirUp = prefabUp;
				wreckTempDirForward = prefabForward;
				
				if(installationSpawnGroup.Id.SubtypeName.Contains("(Wreck")){
				
					string wreckProfile = "";
					
					if(installationSpawnGroup.Id.SubtypeName.Contains("(Wreck-A)")){
						
						wreckProfile = "A";
						
					}
					
					RandomDerelictAngle(wreckProfile, prefabIndex, prefabMatrix);
					prefabUp = wreckTempDirUp;
					prefabForward = wreckTempDirForward;
				
				}
				
				//Try to get custom faction assignment, if it exists.
				var owner = GetCustomNPCFactionFounder(installationSpawnGroup.Id.SubtypeName);
				
				//Spawn The Prefab!
				LogEntry("Spawned " + installationSize + " Station: " + prefab.SubtypeId);
				MyAPIGateway.PrefabManager.SpawnPrefab(tempList, prefab.SubtypeId, prefabSpawningPosition, prefabForward, prefabUp, new Vector3(0f), spawningOptions: SpawningOptions.SetNeutralOwner | SpawningOptions.RotateFirstCockpitTowardsDirection | SpawningOptions.SpawnRandomCargo, beaconName: prefab.BeaconText, ownerId: owner, updateSync: false);
				bool allowRegen = false;
				
				if(installationSpawnGroup.Id.SubtypeName.Contains("(Regen)")){
					
					allowRegen = true;
					
				}

				//Add Spawn Details To Temp Lists
				tempStationName.Add(prefab.SubtypeId);
				tempStationSize.Add(installationSize);
				tempStationPlanet.Add(planet.StorageName);
				tempRegenAllowed.Add(allowRegen);
				tempStationCoords.Add(prefabSpawningPosition);
				tempStationOwner.Add(owner);
				foundInstallation.Add(false);
				prefabIndex++;
				
			}

			if(installationSpawnGroup.Id.SubtypeName.Contains("(Unique)")){
				
				planetUniqueSpawnGroupsUsed.Add(installationSpawnGroup.Id.SubtypeName);
				
			}
			
			successfulSpawn = true;
			planetTerritoriesUsed.Add(spawningCoords);
			searchForInstallations = true;
			
		}
		
		int PlanetActiveInstallationCount(MyPlanet planet){
			
			int instCount = 0;
			
			if(activeInstallations.Count == 0){
				
				return 0;
				
			}
			
			foreach(var cubeGrid in activeInstallations){
				
				var cubeGridEntity = cubeGrid as IMyEntity;
				
				if(cubeGrid == null || MyAPIGateway.Entities.Exist(cubeGridEntity) == false){
					
					continue;
					
				}
				
				string gridStorageValue = "";
				cubeGridEntity.Storage.TryGetValue(installationGridStorageKey, out gridStorageValue);
				
				if(gridStorageValue == null || gridStorageValue == ""){
					
					continue;
				
				}
				
				if(gridStorageValue.Contains(planet.StorageName)){
					
					instCount++;
					
				}
				
			}
			
			return instCount;
			
		}
		
		bool CheckProposedSpawnLocation(Vector3D spawnLocation){
			
			//Check against existing territories
			if(planetTerritoriesUsed.Count != 0){
				
				foreach(var territory in planetTerritoriesUsed){
					
					if(MeasureDistance(spawnLocation, territory) < minSpawnDistFromExisting && reuseTerritories == false){
						
						return false;
						
					}
					
				}
				
			}
			
			//Check against existing grids
			foreach(var entity in entityList){
				
				IMyCubeGrid cubeGrid = entity as IMyCubeGrid;
				
				if(cubeGrid == null){
					
					continue;
					
				}
				
				if(MeasureDistance(cubeGrid.GetPosition(), spawnLocation) < minDistFromOtherGrids){
					
					return false;
					
				}
				
			}
			
			//Check against other non-bot players
			foreach(var player in playerList){
				
				if(player.IsBot == true){
					
					continue;
					
				}
				
				if(MeasureDistance(player.GetPosition(), spawnLocation) < minDistFromOtherGrids){
					
					return false;
					
				}
				
			}
			
			return true;
			
		}
		
		MySpawnGroupDefinition GetRandomInstSpawnGroup(MyPlanet planet, string installationSize, bool checkOnly, Vector3D playerPosition){
			
			foundValidSpawnGroup = false;
			IMyEntity planetEntity = planet as IMyEntity; //Planet Converted To Entity So We Can Do A GetPosition()
			List<MySpawnGroupDefinition> filteredSpawnGroups = new List<MySpawnGroupDefinition>(); //The Final List That We Choose From
			List<MySpawnGroupDefinition> sizedSpawnGroups = new List<MySpawnGroupDefinition>();
			
			if(installationSize == "Small"){
				
				sizedSpawnGroups = smallInstallationSpawnGroups;
				
			}
			
			if(installationSize == "Medium"){
				
				sizedSpawnGroups = mediumInstallationSpawnGroups;
				
			}
			
			if(installationSize == "Large"){
				
				sizedSpawnGroups = largeInstallationSpawnGroups;
				
			}
			
			if(sizedSpawnGroups.Count == 0){
				
				return null;
				
			}
			
			foreach(var spawnGroup in sizedSpawnGroups){
				
				//Checking For Planet Specific Tags
				bool foundPlanetName = false;
				bool thisPlanetFound = false;
				
				//Planet WhiteList
				foreach(string planetName in planetNameList){
					
					if(spawnGroup.Id.SubtypeName.Contains("(" + planetName + ")")){
						
						foundPlanetName = true;
						
						if(planet.StorageName.Contains(planetName)){
							
							thisPlanetFound = true;
							
						}
						
					}
					
				}
				
				if(foundPlanetName == true && thisPlanetFound == false){
					
					continue; //None Of The Planets Identified In SpawnGroup Name Match This Planet. Group Will Not Be Added.
					
				}
				
				//Planet BlackList
				foundPlanetName = false;
				thisPlanetFound = false;
				
				foreach(string planetName in planetNameList){
					
					if(spawnGroup.Id.SubtypeName.Contains("(!" + planetName + ")")){
						
						foundPlanetName = true;
						
						if(planet.StorageName.Contains(planetName)){
							
							thisPlanetFound = true;
							
						}
						
					}
					
				}
				
				if(foundPlanetName == true && thisPlanetFound == true){
					
					continue; //BlackListed Planet Listed In SpawnGroup Matches Current Planet. Group Will Not Be Added
					
				}
				
				//Territory Check
				if(SpawnGroupInTerritory(playerPosition, spawnGroup.Id.SubtypeName) == false){
					
					continue;
					
				}
				
				//Check for Unique SpawnGroup
				
				if(spawnGroup.Id.SubtypeName.Contains("(Unique)")){
					
					foreach(var uniqueGroup in planetUniqueSpawnGroupsUsed){
						
						if(spawnGroup.Id.SubtypeName == uniqueGroup){
							
							continue; //Unique SpawnGroup has already appeared on this planet. Group Will Not Be Added.
							
						}
						
					}
					
				}
				
				//Checking For Distance Further From Center
				if(spawnGroup.Id.SubtypeName.Contains("(DistC|")){
					
					string [] nameSplit = spawnGroup.Id.SubtypeName.Split('|');
					int distanceFromCenter = 0;
					
					for(int i = 0; i < nameSplit.Length; i++){
						
						if(nameSplit[i].Contains("(DistC") && i != nameSplit.Length){
							
							string [] secondSplit = nameSplit[i + 1].Split(')');
							Int32.TryParse(secondSplit[0], out distanceFromCenter);
							break;
							
						}
						
					}
					
					if(distanceFromCenter == 0){
						
						
						
					}else{
						
						if(MeasureDistance(blankPosition, planetEntity.GetPosition()) < (double)distanceFromCenter){
							
							continue; //Too Close To Center, Group Will Not Be Added.
							
						}
						
					}
					
				}
				
				//Checking For Distance No Further From Center
				if(spawnGroup.Id.SubtypeName.Contains("(!DistC|")){
					
					string [] nameSplit = spawnGroup.Id.SubtypeName.Split('|');
					int distanceFromCenter = 0;
					
					for(int i = 0; i < nameSplit.Length; i++){
						
						if(nameSplit[i].Contains("(!DistC") && i != nameSplit.Length){
							
							string [] secondSplit = nameSplit[i + 1].Split(')');
							Int32.TryParse(secondSplit[0], out distanceFromCenter);
							break;
							
						}
						
					}
					
					if(distanceFromCenter == 0){
						
						
						
					}else{
						
						if(MeasureDistance(blankPosition, planetEntity.GetPosition()) > (double)distanceFromCenter){
							
							continue; //Too Close To Center, Group Will Not Be Added.
							
						}
						
					}
					
				}
				
				filteredSpawnGroups.Add(spawnGroup); //SpawnGroup Passed All Checks And Is Added To Filtered List.
				
			}
			
			//Now We Get A Random SpawnGroup From The Filtered List.
			
			if(filteredSpawnGroups.Count == 0){
				
				if(installationSize == "Medium" && checkOnly == false){
					
					mediumInstallationChance = mediumInstallationChanceBase;
					
				}
				
				if(installationSize == "Large" && checkOnly == false){
					
					largeInstallationChance = largeInstallationChanceBase;
					
				}
				
				return null;
				
			}
			
			var randomfilteredSpawnGroup = filteredSpawnGroups[rnd.Next(0, filteredSpawnGroups.Count)];
			
			foundValidSpawnGroup = true;
			return randomfilteredSpawnGroup;

		}
		
		void NewInstallationWatcher(){
			
			if(searchForInstallations == false){
				
				return;
				
			}
			
			RefreshLists();
			bool foundAllInstallations = true;
			
			for(int i = 0; i < foundInstallation.Count; i++){
				
				if(foundInstallation[i] == false){
					
					foundAllInstallations = false;
					
					foreach(var entity in entityList){
						
						var cubeGrid = entity as IMyCubeGrid;
						
						if(cubeGrid == null){
							
							continue;
							
						}
												
						if(MeasureDistance(tempStationCoords[i], cubeGrid.GetPosition()) < 1000 && cubeGrid.CustomName == tempStationName[i]){
							
							string gridStatus = "Active";
						
							//Check For Nobody Ownership
							if(tempStationOwner[i] == 0){
								
								ForceCustomFactionOwnership(cubeGrid, 0);
								gridStatus = "Wreck";
								
							}
							
							//Set Storage on this grid
							if(entity.Storage == null){
								
								entity.Storage = new MyModStorageComponent();
								
							}
							
							string gridStorageCombined = "";
							gridStorageCombined += gridStatus + "\n";
							gridStorageCombined += tempStationSize[i] + "\n";
							gridStorageCombined += tempStationPlanet[i] + "\n";
							gridStorageCombined += tempRegenAllowed[i].ToString();
							
							entity.Storage[installationGridStorageKey] = gridStorageCombined;
							
							//Add to Active Grids List
							activeInstallations.Add(cubeGrid);
							
							RemoveGridAuthorship(cubeGrid);
														
							//Mark As Found for Watcher
							foundInstallation[i] = true;
							
						}
						
					}
					
				}
				
			}
			
			if(foundAllInstallations == true){
				
				tempStationName.Clear();
				tempStationSize.Clear();
				tempStationPlanet.Clear();
				tempRegenAllowed.Clear();
				tempStationCoords.Clear();
				tempStationOwner.Clear();
				foundInstallation.Clear();
				searchForInstallations = false;
				
			}
		
		}
		
		void ActiveInstallationWatcher(){

			if(activeInstallations.Count == 0){
				
				return;
				
			}
			
			gridRegenTimer += 10;
			int targetIndex = 0;
			IMyCubeGrid targetGrid = null;
			bool removeFromList = false;
			bool removeGrid = false;
			
			for(int i = 0; i < activeInstallations.Count; i++){

				var cubeGrid = activeInstallations[i];
				var cubeGridEntity = cubeGrid as IMyEntity;
				if(cubeGrid == null || MyAPIGateway.Entities.Exist(cubeGridEntity) == false){
					
					removeFromList = true;
					break;
					
				}
				
				targetIndex = i;
				targetGrid = cubeGrid;
				
				//Get Grid Storage
				string gridStorageValue = "";
				cubeGridEntity.Storage.TryGetValue(installationGridStorageKey, out gridStorageValue);
				
				if(gridStorageValue == null || gridStorageValue == ""){
					
					continue;
				
				}
				
				string [] storageSplit = gridStorageValue.Split('\n');
				installationStatus = storageSplit[0];
				installationSizeTier = storageSplit[1];
				installationPlanet = storageSplit[2];
				bool.TryParse( storageSplit[3], out installationRegenAllowed);
				
				if(installationStatus.Contains("Captured")){
					
					removeFromList = true;
					break;
					
				}
				
				if(installationStatus.Contains("Wreck")){
					
					removeFromList = true;
					activeWrecks.Add(cubeGrid);
					break;
					
				}
								
				//Check Ownership / Powered
				if(NPCOwnershipCheck(cubeGrid, true) == false){
					
					installationStatus = "Captured";
					removeFromList = true;
					break;
					
				}
				
				if(RemoveThisGrid(cubeGrid) == true){
					
					installationStatus = "NoPower";
					removeFromList = true;
					removeGrid = true;
					break;
					
				}
				
				//Trigger Regen If Applicable
				
				if(installationRegenAllowed == true){
					
					if(gridRegenTimer > gridRegenTimerTrigger || gridRegenDebug == true){
						
						regenQueue.Add(cubeGrid);
						regenProcessing = true;
					
					}
					
				}
				
				
				
				//Save Grid Storage

				if(gridRegenTimer > gridRegenTimerTrigger){
						
					gridRegenTimer = 0;
						
				}
				
			}
			
			if(removeGrid == true){
				
				Vector3D deletePosition = targetGrid.GetPosition();
				targetGrid.Delete();
				
				RefreshLists();
				foreach(var entity in entityList){
					
					var subGrid = entity as IMyCubeGrid;
					
					if(subGrid != null){
						
						if(MeasureDistance(subGrid.GetPosition(), deletePosition) < 500 && NPCOwnershipCheck(subGrid, true) == true){
							
							subGrid.Delete();
							
						}
						
					}
					
				}
				
			}
			
			if(removeFromList == true){
				
				activeInstallations.RemoveAt(targetIndex);
				
			}
		
		}
		
		bool RemoveThisGrid(IMyCubeGrid cubeGrid){
			
			//Get Closest Player
			double closestPlayerDistance = 0;
			
			if(playerList.Count != 0){
				
				foreach(var player in playerList){
				
					if(MeasureDistance(player.GetPosition(), cubeGrid.GetPosition()) < closestPlayerDistance || closestPlayerDistance == 0){
						
						closestPlayerDistance = MeasureDistance(player.GetPosition(), cubeGrid.GetPosition());
						
					}
				
				}
				
			}
			
			if(closestPlayerDistance < noPowerDespawnDist){
				
				return false;
				
			}
			
			var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
			List<Sandbox.ModAPI.IMyTerminalBlock> blockList = new List<Sandbox.ModAPI.IMyTerminalBlock>();
			gts.GetBlocksOfType<Sandbox.ModAPI.IMyTerminalBlock>(blockList);
			
			foreach(var block in blockList){
				
				var reactor = block as Sandbox.ModAPI.Ingame.IMyReactor;
				if(reactor != null){
					
					if(reactor.CurrentOutput > 0){
					
						return false;
					
					}
					
				}
				
				var battery = block as Sandbox.ModAPI.Ingame.IMyBatteryBlock;
				if(battery != null){
					
					if(battery.CurrentOutput > 0){
					
						return false;
					
					}
					
				}
				
				var solarPanel = block as IMySolarPanel;
				if(solarPanel != null){
					
					if(solarPanel.CurrentOutput > 0){
					
						return false;
					
					}
					
				}
				
				
				
			}
			
			return true;
		
		}
				
		long GetCustomNPCFactionFounder(string spawnGroupName){
			
			long result = 0;
			
			if(spawnGroupName.Contains("(Nobody)")){
				
				return result;
				
			}
			
			result = MyAPIGateway.Session.Factions.TryGetFactionByTag("SPRT").FounderId; //Sets the result to default as Space Pirate if no custom faction is found.
						
			foreach(var faction in npcFactionList){
		
				if(spawnGroupName.Contains(faction.Tag)){
					
					result = faction.FounderId;
					return result;
					
				}
				
			}
			
			return result;
			
		}
		
		bool NPCOwnershipCheck(IMyCubeGrid cubeGrid, bool allBlocks){
			
			bool playerOwnedBlockDetected = false;
			bool npcOwnedBlockDetected = false;
			List<IMySlimBlock> blockList = new List<IMySlimBlock>();
			cubeGrid.GetBlocks(blockList, b => b.FatBlock is Sandbox.ModAPI.Ingame.IMyTerminalBlock);
			foreach(var block in blockList){
				
				var blockOwner = block.OwnerId;
				
				if(blockOwner == 0){
					
					continue;
					
				}
				
				var ownerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(blockOwner);
				
				if(ownerFaction == null){
					
					//Factionless Player Found, therefore not an NPC.
					playerOwnedBlockDetected = true;
					continue;
					
				}
				
				string ownerfactionTag = ownerFaction.Tag;
				
				if(ownerfactionTag.Length > 3 && ownerfactionTag != "FSTC"){
					
					//Faction tag is 4 characters long or longer, therefore an NPC
					npcOwnedBlockDetected = true;
					
				}else{
					
					//Faction tag is 3 or less characters, therefore not an NPC
					playerOwnedBlockDetected = true;
					
				}

			}
			
			if(allBlocks == false && npcOwnedBlockDetected == false){
				
				return false;
				
			}
			
			if(allBlocks == false && npcOwnedBlockDetected == true){
				
				return true;
				
			}
			
			if(allBlocks == true && playerOwnedBlockDetected == true){
				
				return false;
				
			}
			
			if(allBlocks == true && playerOwnedBlockDetected == false){
				
				return true;
				
			}
			
			return true;
					
		}
		
		void ForceCustomFactionOwnership(IMyCubeGrid cubeGrid, long factionLeader){

			cubeGrid.ChangeGridOwnership(factionLeader, 0);
			foreach(var entity in entityList){
				
				var subGrid = entity as IMyCubeGrid;
				if(subGrid != null){
					
					if(NPCOwnershipCheck(cubeGrid, true) == true && MeasureDistance(subGrid.GetPosition(), cubeGrid.GetPosition()) < 500){
						
						subGrid.ChangeGridOwnership(factionLeader, 0);
						
					}
					
				}
				
			}
			
		}
		
		MyPlanet GetNearestPlanet(Vector3D nearCoords){
			
			double distanceFromPlanet = 0;
			MyPlanet nearestPlanet = null;
			foreach(var planet in planetList){
			
				var planetEntity = planet as IMyEntity;
				if(MeasureDistance(nearCoords, planetEntity.GetPosition()) < distanceFromPlanet || distanceFromPlanet == 0){
					
					distanceFromPlanet = MeasureDistance(nearCoords, planetEntity.GetPosition());
					nearestPlanet = planet;
					
				}
			
			}
			
			return nearestPlanet;

		}

		void GridRegeneration(){
			
			if(regenQueue.Count == 0){
				
				regenProcessing = false;
				return;
				
			}
						
			foreach(var cubeGrid in regenQueue){
				
				var cubeGridEntity = cubeGrid as IMyEntity;
				
				if(cubeGrid == null || MyAPIGateway.Entities.Exist(cubeGridEntity) == false){
					
					continue;
					
				}
				
				var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
				List<Sandbox.ModAPI.IMyProjector> blockList = new List<Sandbox.ModAPI.IMyProjector>();
				gts.GetBlocksOfType<Sandbox.ModAPI.IMyProjector>(blockList);
				
				if(blockList.Count == 0){
					
					return;
					
				}
				
				bool attemptRepair = false;
				
				foreach(var projector in blockList){
					
					if(projector.IsFunctional == true){
						
						long projectorOwner = projector.OwnerId;
											
						projector.Enabled = true;
						
						if(gridRegenProcessTimer < 5){
							
							continue;
							
						}
						
						if(projector.ProjectedGrid != null){
							
							List<IMySlimBlock> projectorBlockList = new List<IMySlimBlock>();
							projector.ProjectedGrid.GetBlocks(projectorBlockList);
							int projectorBlockCounter = 0;
							int gridRegenTarget = rnd.Next(installationRegenMinBlocks, installationRegenMaxBlocks + 1);
							
							foreach(var block in projectorBlockList){
								
								if(projector.CanBuild(block, true) == 0){
									
									projector.Build(block, projector.OwnerId, projector.OwnerId, true);
									projectorBlockCounter++;
									if(projectorBlockCounter >= gridRegenTarget){
										
										break;
										
									}
									
								}
								
							}
							
						}
						
						projector.Enabled = false;
						
					}
					
				}
				
			}
			
			if(gridRegenProcessTimer < 5){
				
				gridRegenProcessTimer++;
				
			}else{
				
				gridRegenProcessTimer = 0;
				regenProcessing = false;
				regenQueue.Clear();
				
			}
			
		}
		
		void RefreshLists(){
			
			playerList.Clear();
			entityList.Clear();
			planetList.Clear();
			planetNameList.Clear();
			
			MyAPIGateway.Players.GetPlayers(playerList);
			MyAPIGateway.Entities.GetEntities(entityList);
			
			if(entityList.Count != 0){
				
				foreach(var entity in entityList){
					
					var planet = entity as MyPlanet;
					
					if(planet != null){
						
						planetList.Add(planet);
						
					}
					
				}
				
			}
			
			planetNameList.Clear();
			var planetDefList = MyDefinitionManager.Static.GetPlanetsGeneratorsDefinitions();
			foreach(var planetDef in planetDefList){
				
				planetNameList.Add(planetDef.Id.SubtypeName);
				
			}
			
		}
		
		double MeasureDistance(Vector3D coordsStart, Vector3D coordsEnd){
			
			double distance = Math.Round( Vector3D.Distance( coordsStart, coordsEnd ), 2 );
			return distance;
			
		}
		
		double RandomNumberBetween(double minValue, double maxValue){

			var next = rnd.NextDouble();
			return minValue + (next * (maxValue - minValue));

		}
		
		void RandomDerelictAngle(string wreckProfile, int prefabIndex, MatrixD prefabMatrix){
			
			if(wreckProfile == "" || prefabIndex >= 4){
				
				Vector3D prefabCoords = Vector3D.Transform(new Vector3D(0,0,0), prefabMatrix);
				Vector3D randomDirectionRaw = new Vector3D(RandomNumberBetween(-5, 5), 20, RandomNumberBetween(-5, 5));
				Vector3D randomDirectionTrans = Vector3D.Transform(randomDirectionRaw, prefabMatrix);
				wreckTempDirUp = Vector3D.Normalize(randomDirectionTrans - prefabCoords);
				wreckTempDirForward = MyUtils.GetRandomPerpendicularVector(ref wreckTempDirUp);
				return;
				
			}
			
			if(wreckProfile == "A"){
				
				Vector3D prefabCoords = Vector3D.Transform(new Vector3D(0,0,0), prefabMatrix);
				Vector3D tempDirUp = Vector3D.Transform(wreckTemplateUpA[prefabIndex], prefabMatrix);
				Vector3D tempDirForward = Vector3D.Transform(wreckTemplateForwardA[prefabIndex], prefabMatrix);
				wreckTempDirUp = Vector3D.Normalize(tempDirUp - prefabCoords);
				wreckTempDirForward = Vector3D.Normalize(tempDirForward - prefabCoords);
				return;
				
			}

			
		}
		
		bool SpawnGroupInTerritory(Vector3D playerLocation, string spawnGroupId){
			
			bool result = true;
			
			if(territoryTagList.Count == 0 || ignoreTerritoryRules == true){
				
				return true;
				
			}
			
			for(int i = 0; i < territoryTagList.Count; i++){
				
				if(spawnGroupId.Contains(territoryTagList[i]) == false){
					
					continue;
					
				}
				
				if(MeasureDistance(playerLocation, territoryCoordsList[i]) < territoryRadiusList[i]){
					
					return true;
					
				}else{
					
					result = false;
					
				}
				
			}
			
			return result;
			
		}
		
		void RemoveGridAuthorship(IMyCubeGrid originGrid){
		
			var gridList = MyAPIGateway.GridGroups.GetGroup(originGrid, GridLinkTypeEnum.Mechanical);
			
			foreach(var grid in gridList){
				
				var gridOwners = grid.BigOwners;
				
				var gridEntity = grid as IMyEntity;
				var cubeGrid = gridEntity as MyCubeGrid;
				
				foreach(var owner in gridOwners){
					
					cubeGrid.TransferBlocksBuiltByID(owner, 0);
					
				}
				
				foreach(var player in playerList){
					
					if(player.IsBot == true){
						
						continue;
						
					}
					
					cubeGrid.TransferBlocksBuiltByID(player.IdentityId, 0);
					
				}
			
			}
		
		}
		
		public void LogEntry(string argument){
			
			if(argument.Contains("Debug") == true && debugMode == false){
				
				return;
				
			}
			
			MyLog.Default.WriteLineAndConsole("Planetary Installations: " + argument);
			
			if(debugMode == true){
				
				MyVisualScriptLogicProvider.ShowNotificationToAll(argument, 5000, "White");
				
			}
			
		}

	}
	
}