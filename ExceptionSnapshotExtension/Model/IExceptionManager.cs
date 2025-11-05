using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace ExceptionSnapshotExtension.Model {
	
	internal interface IExceptionManager {
		bool SessionAvailable { get; }

		void EnableAll();
		void DisableAll();

		void RestoreSnapshot(Snapshot snapshot);
		Snapshot GetCurrentExceptionSnapshot();

		/// <summary>
		/// Checks if Break first chance for snapshot matches current exception settings
		/// </summary>
		bool VerifySnapshot(Snapshot snapshot);
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
