using System;
using System.Windows.Input;

namespace ExceptionSnapshotExtension.Viewmodels {
	public class RelayCommand : ICommand {
		private Predicate<object> m_CanExecute;
		private Action<object> m_Execute;
#pragma warning disable 67
		public event EventHandler CanExecuteChanged;
#pragma warning restore 67

		public RelayCommand(Predicate<object> canExecute, Action<object> execute) {
			this.m_CanExecute = canExecute;
			this.m_Execute = execute;
		}

		public bool CanExecute(object parameter) {
			return m_CanExecute(parameter);
		}

		public void Execute(object parameter) {
			m_Execute(parameter);
		}
	}
}
