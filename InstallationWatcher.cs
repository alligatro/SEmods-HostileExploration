using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace HostileExploration
{
    public class InstallationWatcher
    {
        public InstallationWatcher()
        {
            TempStationName = new List<string>();
            TempStationSize = new List<string>();
            TempStationPlanet = new List<string>();
            TempRegenAllowed = new List<bool>();
            TempStationCoords = new List<Vector3D>();
            TempStationOwner = new List<long>();
            FoundInstallation = new List<bool>();

        }

        /// <summary>
        /// While true, the mod will scan for new installations after spawning has occured.
        /// </summary>
        public bool SearchForInstallations { get; set; }

        public List<string> TempStationName { get; private set; }
        public List<string> TempStationSize { get; private set; }
        public List<string> TempStationPlanet { get; private set; }
        public List<bool> TempRegenAllowed { get; private set; }
        public List<Vector3D> TempStationCoords { get; private set; }
        public List<long> TempStationOwner { get; private set; }
        public List<bool> FoundInstallation { get; private set; }

        public void Reset()
        {
        
                TempStationName.Clear();
				TempStationSize.Clear();
				TempStationPlanet.Clear();
				TempRegenAllowed.Clear();
				TempStationCoords.Clear();
				TempStationOwner.Clear();
				FoundInstallation.Clear();
				SearchForInstallations = false;
        }
    }
}
