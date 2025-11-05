using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExceptionSnapshotExtension.Model {
	internal class SettingsManager {
		public static bool TrackEvents = true;
		public enum STORE_SNAPSHOTS_WHERE { InVSSolutionLocalConfig, InJsonNextToSolution};
	}
}
