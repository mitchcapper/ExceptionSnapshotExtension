using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace ExceptionSnapshotExtension.Model {
	internal delegate void OnExceptionDelegade(ExceptionInfo info, string modlueName, ref bool continueExecution);
	public class DebuggerExceptionEventArgs : EventArgs {
		public string exceptionType;
		public string moduleName;
		internal string description;
		internal enum_EXCEPTION_STATE reason;
		public override string ToString() => $"{exceptionType} thrown in {moduleName} break of: {reason} error: {description}";
	}
	internal interface IExceptionManager {
		bool SessionAvailable { get; }

		void EnableAll();
		void DisableAll();
		void AttachEventsListener();
		void DetachEventsListener();
		event EventHandler<DebuggerExceptionEventArgs> DebuggerException;
		event EventHandler<DBGMODE> DebuggerStatusChanged;

		void RestoreSnapshot(Snapshot snapshot);
		Snapshot GetCurrentExceptionSnapshot();

		/// <summary>
		/// Checks if Break first chance for snapshot matches current exception settings
		/// </summary>
		bool VerifySnapshot(Snapshot snapshot);
		void ResumeDebugging();
		void SetBreakOnException(String exceptionType, bool breakEnabled);
		/// <summary>
		/// If exceptionType = null excludes all exceptions from the specified module but setting the condition on the root types
		/// </summary>
		/// <param name="moduleName"></param>
		/// <param name="exceptionType"></param>
		void ExcludeModuleFromExceptions(String moduleName, String exceptionType=null);
		void ResetExceptionsToDefaults();
	}
}
