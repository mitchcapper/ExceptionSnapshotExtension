namespace ExceptionSnapshotExtension.Model {
	internal class SettingsManager {
		public static bool TrackEvents = true;
		public enum STORE_SNAPSHOTS_WHERE { InVSSolutionLocalConfig, InJsonNextToSolution };
		public static STORE_SNAPSHOTS_WHERE STORE_WHERE = STORE_SNAPSHOTS_WHERE.InJsonNextToSolution;
	}
}
