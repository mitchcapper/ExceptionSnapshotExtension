using ExceptionSnapshotExtension.Model;
using System;
using System.Collections.ObjectModel;

namespace ExceptionSnapshotExtension.Viewmodels {
	internal class SnapshotVM : IListItemVM {
		private readonly Snapshot m_Snapshot;

		public ObservableCollection<GroupVM> Groups { get; set; }

		public string Name => m_Snapshot.Name;
		public DateTime CreatedOn => m_Snapshot.CreatedOn;
		public bool Break { get => true; set { } }
		public Snapshot Snapshot => m_Snapshot;

		public SnapshotVM(Snapshot snapshot) {
			m_Snapshot = snapshot;
		}
	}
}
