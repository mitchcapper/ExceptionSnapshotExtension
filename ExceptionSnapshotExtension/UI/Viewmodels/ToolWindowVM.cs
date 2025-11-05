using ExceptionSnapshotExtension.Model;
using ExceptionSnapshotExtension.Services;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace ExceptionSnapshotExtension.Viewmodels {
	internal class ToolWindowVM : BaseObservableClass {


		private readonly IExceptionManager m_ExceptionManager;
		private readonly DebugManager m_DebugManager = DebugManager.Instance;




		public ObservableCollection<SnapshotVM> SnapshotVms { get; private set; }

		public IEnumerable<Snapshot> Snapshots {
			get {
				return SnapshotVms.Select(vm => vm.Snapshot);
			}
			set {
				SnapshotVms.Clear();
				foreach (var snapshot in value ?? []) {
					SnapshotVms.Add(new SnapshotVM(snapshot));
				}
			}
		}
		public string NewSnapshotName {
			get;
			set {
				if (SetProperty(ref field, value) && !string.IsNullOrEmpty(value))
					FailedValidation = false;


			}
		}

		/// <summary>
		/// One way to source binding
		/// </summary>
		public SnapshotVM SelectedSnapshot { get; set; }

		/// <summary>
		/// Two way binding
		/// </summary>
		public int SelectedSnapshotIndex { get; set => SetProperty(ref field, value); }

		public bool FailedValidation {
			get; set => SetProperty(ref field, value);
		}
		protected void OnException(Exception ex){
			System.Diagnostics.Debug.WriteLine($"Exception in ToolWindowVM Command: {ex}");
		}

		public RelayCommand GoCommand => field ??= new RelayCommand(p => true, p => m_DebugManager.ResumeDebugging());

		public RelayCommand EnableAllCommand => field ??= new RelayCommand(p => true, p => m_ExceptionManager.EnableAll());

		public RelayCommand DisableAllCommand => field ??= new RelayCommand(p => true, p => m_ExceptionManager.DisableAll());

		public NiceRelayCommand SaveSnapshotCommand => field ??= new NiceRelayCommand(p => true, p => {

			if (!string.IsNullOrEmpty(NewSnapshotName)) {
				var snapshot = m_ExceptionManager.GetCurrentExceptionSnapshot();
				snapshot.Name = NewSnapshotName;
				SnapshotVms.Add(new SnapshotVM(snapshot));
			} else
				FailedValidation = true;


		},onException:OnException);
		public RelayCommand IgnoreThisExceptionCommand => field ??= new RelayCommand(p => true, p => {
			if (!CurrentState.IsExceptionActive) //slight race condition should copylocal first
				return;
			m_ExceptionManager.SetBreakOnException(CurrentState.ExceptionType, false);
			GoCommand.Execute(null);
		});

		public RelayCommand IgnoreAllFromModuleCommand => field ??= new RelayCommand(p => true, p => {
			if (!CurrentState.IsExceptionActive)
				return;
			m_ExceptionManager.ExcludeModuleFromExceptions(CurrentState.ModuleName);
			GoCommand.Execute(null);
		});
		public RelayCommand IgnoreThisFromModuleCommand => field ??= new RelayCommand(p => true, p => {
			if (!CurrentState.IsExceptionActive)
				return;
			m_ExceptionManager.ExcludeModuleFromExceptions(CurrentState.ModuleName, CurrentState.ExceptionType);
			GoCommand.Execute(null);
		});

		public RelayCommand DeleteSnapshotCommand => field ??= new RelayCommand(p => true, p => {
			if (SelectedSnapshot != null) {
				Debug.Assert(SnapshotVms.Contains(SelectedSnapshot));
				var res = MessageBox.Show($"Are you sure you want to delete snapshot: {SelectedSnapshot.Name}", "Confirm Delete", MessageBoxButton.YesNo);
				if (res != MessageBoxResult.Yes)
					return;
				int selectedId = SelectedSnapshotIndex;
				SnapshotVms.Remove(SelectedSnapshot);

				SelectedSnapshotIndex = SnapshotVms.Count > selectedId ? selectedId : SnapshotVms.Count - 1;
			}
		});


		public NiceRelayCommand ActivateSnapshotCommand => field ??= new NiceRelayCommand(p => true, p => {
			if (this.SetToDefaultsBeforeRestoring)
				m_ExceptionManager.ResetExceptionsToDefaults();

			if (p is SnapshotVM snapshotVM) {
				for (int i = 0; i < 10; i++) // I've had issues with snapshot not being restored after first attempt.
				{
					if (!m_ExceptionManager.VerifySnapshot(snapshotVM.Snapshot)) {
						if (i > 0) {
							System.Diagnostics.Trace.WriteLine($"Failed to Restore. Attempt {i + 1}");
						}

						m_ExceptionManager.RestoreSnapshot(snapshotVM.Snapshot);
						break;
					}
				}
			}

		},onException:OnException);

		public ToolWindowVM() // For designer
		{
			if (DesignerProperties.GetIsInDesignMode(new DependencyObject()) == false)
				throw new InvalidOperationException("This ctor is only for designer support");
			CurrentState = new() { DebuggerMode = DBGMODE.DBGMODE_Break, Description = "Invalid url", ExceptionType = "System.Net.WebException", ModuleName = "System.Network.dll" };
			SnapshotVms = new ObservableCollection<SnapshotVM>
			(
				new SnapshotVM[]
				{
					new SnapshotVM(new Snapshot
					{
						Name = "Test Shot"
					}),
					new SnapshotVM(new Snapshot
					{
						Name = "Last Week Big Trace"
					})
				}
			);
		}

		public bool SetToDefaultsBeforeRestoring {
			get; set => SetProperty(ref field, value);
		}

		public ToolWindowVM(IExceptionManager exceptionManager) {
			m_ExceptionManager = exceptionManager;
			SnapshotVms = new ObservableCollection<SnapshotVM>();
		}


		private void DebugStatusChanged(object sender, DBGMODE e) {
			if (e != DBGMODE.DBGMODE_Break) {
				CurrentState = new();
			}
			CurrentState.DebuggerMode = e;

		}

		internal void WindowLoaded() {
			if (!SettingsManager.TrackEvents)
				return;
			m_DebugManager.AttachEventsListener();
			m_DebugManager.DebuggerStatusChanged += DebugStatusChanged;
			m_DebugManager.DebuggerException += DebuggerException;
		}

		internal void WindowUnloaded() {
			if (!SettingsManager.TrackEvents)
				return;
			m_DebugManager.DetachEventsListener();
			m_DebugManager.DebuggerStatusChanged -= DebugStatusChanged;
			m_DebugManager.DebuggerException -= DebuggerException;
		}
		public State CurrentState {
			get; set => SetProperty(ref field, value);
		} = new();


		public class State : BaseObservableClass {
			public string ExceptionType { get; set => SetProperty(ref field, value); }
			public string ModuleName {
				get; set {
					if (SetProperty(ref field, value))
						FirePropertyChanged(nameof(IsExceptionActive));
				}
			}
			public string Description { get; set => SetProperty(ref field, value); }
			public enum_EXCEPTION_STATE Reason { get; set => SetProperty(ref field, value); }
			public DBGMODE DebuggerMode { get; set => SetProperty(ref field, value); }
			public bool IsExceptionActive => !String.IsNullOrWhiteSpace(ModuleName);
		}
		private void DebuggerException(object sender, DebuggerExceptionEventArgs e) {
			CurrentState.ExceptionType = e.exceptionType;
			CurrentState.ModuleName = e.moduleName;
			CurrentState.Description = e.description;
			CurrentState.Reason = e.reason;
		}

	}

}
