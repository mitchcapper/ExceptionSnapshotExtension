
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExceptionSnapshotExtension.Viewmodels {
	internal class NiceRelayCommand : RelayCommand, INotifyPropertyChanged {
		private Action<object> origExecute;
		private bool onUiThread;
		private Action<Exception> onException;
		private static readonly TimeSpan preDelay = TimeSpan.FromMilliseconds(50); //needed for if on ui thread the ui to update
		private static readonly TimeSpan postDelay = TimeSpan.FromMilliseconds(350); // make sure the user sees the ui change

		public event PropertyChangedEventHandler PropertyChanged;

		public NiceRelayCommand(Predicate<object> canExecute, Action<object> execute, bool onUiThread = true, Action<Exception> onException = null) : base(canExecute, execute) {
			this.origExecute = this.m_Execute;
			this.m_Execute = DelayedExecute;
			this.onUiThread = onUiThread;
			this.onException = onException;
		}
		public bool Executable => ! amRunning && base.CanExecute(null);
		
		private bool amRunning {
			get; set {
				field = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Executable)));
				NotifyCanExecuteChanged();
			}
		}
		override public bool CanExecute(object parameter) {
			if (amRunning)
				return false;

			return base.CanExecute(parameter);
		}

		private async void DelayedExecute(object obj) {
			amRunning = true;
			try {
				if (onUiThread) {
					await Task.Delay(preDelay).ConfigureAwait(true);
					origExecute(obj);
				} else
					await Task.Run(() => origExecute(obj));
				await Task.Delay(postDelay);
			} catch (Exception ex) {
				System.Diagnostics.Trace.WriteLine($"Exception in NiceRelayCommand: {ex}");
				if (onException != null)
					onException.Invoke(ex);
				else
					throw;
			} finally {
				amRunning = false;
			}
		}
	}
}
