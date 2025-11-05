namespace ExceptionSnapshotExtension.Model {
	internal class SettingsManager {
		public static bool TrackEvents = true;
		public enum STORE_SNAPSHOTS_WHERE { InVSSolutionLocalConfig, InJsonNextToSolution };
	}
}
