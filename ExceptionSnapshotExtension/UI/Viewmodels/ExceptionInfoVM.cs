using ExceptionSnapshotExtension.Model;

namespace ExceptionSnapshotExtension.Viewmodels {
	class ExceptionInfoVM : IListItemVM {
		private readonly ExceptionInfo m_Info;

		public string Name => m_Info.Name;

		public bool Break { get => m_Info.BreakFirstChance; set => m_Info.BreakFirstChance = value; }

		public ExceptionInfoVM(ExceptionInfo info) {
			m_Info = info;
		}
	}
}
