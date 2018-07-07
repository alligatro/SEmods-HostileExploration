using System;
using Sandbox.ModAPI;

namespace HostileExploration{
	
	public class PlanetaryInstallationsConfig{
		
		public int SpawnTimerTrigger {get; set;}
		public int MaximumActiveStationsPerPlanet {get; set;}
		public double MinimumSpawnDistanceFromOtherGrids {get; set;}
		public int MediumSpawnChanceBaseValue {get; set;}
		public int MediumSpawnChanceIncrement {get; set;}
		public int MediumInstallationAttempts {get; set;}
		public int LargeSpawnChanceBaseValue {get; set;}
		public int LargeSpawnChanceIncrement {get; set;}
		public int LargeInstallationAttempts {get; set;}
		public double PlayerDistanceSpawnTrigger {get; set;}
		public double PlayerMaximumDistanceFromSurface {get; set;}
		public double MinimumSpawnDistanceFromPlayers {get; set;}
		public double MaximumSpawnDistanceFromPlayers {get; set;}
		public double MediumSpawnDistanceIncrement {get; set;}
		public double LargeSpawnDistanceIncrement {get; set;}
		public double MinimumSpawnDistanceFromExistingSpawn {get; set;}
		public int RandomTerrainSurfaceChecks {get; set;}
		public int MediumLocationAttemptIncrement {get; set;}
		public int LargeLocationAttemptIncrement {get; set;}
		public double MinimumTerrainVariance {get; set;}
		public double MaximumTerrainVariance {get; set;}
		public double TerrainCheckIncrementDistance {get; set;}
		public double SmallTerrainCheckDistance {get; set;}
		public double MediumTerrainCheckDistance {get; set;}
		public double LargeTerrainCheckDistane {get; set;}
		public double UnpoweredDespawnDistance {get; set;}
		public int RegenerationTimerTrigger {get; set;}
		public int RegenerationMinimumBlocks {get; set;}
		public int RegenerationMaximumBlocks {get; set;}
		public bool IgnoreTerritoryRules {get; set;}
		public bool ReuseTerritories {get; set;}
		
		public PlanetaryInstallationsConfig(){
			
			SpawnTimerTrigger = 2;
			MaximumActiveStationsPerPlanet = 30;
			MinimumSpawnDistanceFromOtherGrids = 2500;
			MediumSpawnChanceBaseValue = 15;
			MediumSpawnChanceIncrement = 15;
			MediumInstallationAttempts = 15;
			LargeSpawnChanceBaseValue = 5;
			LargeSpawnChanceIncrement = 15;
			LargeInstallationAttempts = 10;
			PlayerDistanceSpawnTrigger = 10000;
			MinimumSpawnDistanceFromPlayers = 4000;
			MaximumSpawnDistanceFromPlayers = 5000;
			MediumSpawnDistanceIncrement = 500;
			LargeSpawnDistanceIncrement = 1000;
			MinimumSpawnDistanceFromExistingSpawn = 3000;
			RandomTerrainSurfaceChecks = 150;
			MediumLocationAttemptIncrement = 50;
			LargeLocationAttemptIncrement = 100;
			MinimumTerrainVariance = -3.5;
			MaximumTerrainVariance = 2.5;
			TerrainCheckIncrementDistance = 10;
			SmallTerrainCheckDistance = 30;
			MediumTerrainCheckDistance = 80;
			LargeTerrainCheckDistane = 100;
			UnpoweredDespawnDistance = 12000;
			RegenerationTimerTrigger = 300;
			RegenerationMinimumBlocks = 15;
			RegenerationMaximumBlocks = 30;
			IgnoreTerritoryRules = false;
			ReuseTerritories = false;
			
		}
		
		public static PlanetaryInstallationsConfig LoadConfigFile(){
			
			if(MyAPIGateway.Utilities.FileExistsInLocalStorage("planetaryinstallationconfig.xml", typeof(PlanetaryInstallationsConfig)) == true){
				
				try{
					
					PlanetaryInstallationsConfig config = null;
					var reader = MyAPIGateway.Utilities.ReadFileInLocalStorage("planetaryinstallationconfig.xml", typeof(PlanetaryInstallationsConfig));
					string configcontents = reader.ReadToEnd();
					config = MyAPIGateway.Utilities.SerializeFromXML<PlanetaryInstallationsConfig>(configcontents);
					//LogEntry("Found Config File");
					return config;
					
				}catch(Exception exc){
					
					//Not a good config, defaults will be loaded instead.
					
				}
				
				
			}
			
			PlanetaryInstallationsConfig defaultconfig = new PlanetaryInstallationsConfig();
			//LogEntry("Config File Not Found. Using Default Values");
			
			using (var writer = MyAPIGateway.Utilities.WriteFileInLocalStorage("planetaryinstallationconfig.xml", typeof(PlanetaryInstallationsConfig))){
				
				writer.Write(MyAPIGateway.Utilities.SerializeToXML<PlanetaryInstallationsConfig>(defaultconfig));
				
			}
			
			return defaultconfig;
				
		}
		
	}
	
}