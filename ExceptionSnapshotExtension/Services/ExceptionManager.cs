using EnvDTE;
using ExceptionSnapshotExtension.Model;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Debugger.Interop.Internal;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;

using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace ExceptionSnapshotExtension.Services {
	internal class ExceptionManager : IExceptionManager {
		private delegate void UpdateException(ref EXCEPTION_INFO150 exception, out bool changed);
		/// <summary>
		/// Note if conditions are changed you need to manually set the HasChanged flag
		/// </summary>
		/// <param name="exception"></param>
		private delegate void UpdateExceptionGeneric(ExceptionInfo exception);

		private IDebuggerInternal15 InternalDebugger {
			get {
				var debugger = Package.GetGlobalService(typeof(SVsShellDebugger)) as IDebuggerInternal15;
				return debugger;
			}
		}

		private IDebugSession150 Session {
			get {
				return InternalDebugger?.CurrentSession as IDebugSession150;
			}
		}

		/*
		Can't seem to set items o nthe actual toplevel groups.
			//we could get the defaults then remove all then restore defaults then load ours but this allows layered loading and the user already has a reset button
			var dte = Package.GetGlobalService(typeof(DTE)) as EnvDTE80.DTE2;
			var d  = (EnvDTE90.Debugger3)dte.Debugger;
		var grps = d?.ExceptionGroups;
			var itm = grps.Item("CMake Exceptions");
			foreach (EnvDTE90.ExceptionSettings settings in grps)
            {
				System.Diagnostics.Debug.WriteLine(settings.ToString());
			}
			TopExceptions = GetExceptions(null, Session);
			var expTest = TopExceptions.Single(te => te.bstrExceptionName == "CMake Exceptions");
			var it = ConvertToGeneric(expTest);
			it.BreakFirstChance = false;
			it.Conditions = [new() {CallStackBehavior = EXCEPTION_CONDITION_CALLSTACK_BEHAVIOR.TopFrameOnly,
				Operator=EXCEPTION_CONDITION_OPERATOR.Equals,Type=EXCEPTION_CONDITION_TYPE.ModuleName,Value="myNew.dll" }];
			expTest = ConvertFromGeneric(it);
			Marshal.ThrowExceptionForHR( Session.RemoveSetExceptions(new ExceptionInfoEnumerator([expTest])));
			//expTest.bstrExceptionName=null;
			SetExceptions([expTest]);
			return;
		*/

		public bool SessionAvailable => Session != null;

		EXCEPTION_INFO150[] TopExceptions { get => field ??= GetExceptions(null, Session); set; }

		#region  public

	
		public Snapshot GetCurrentExceptionSnapshot() {
			ThrowIfNoSession();
			var ret = new Snapshot {
				CreatedOn = DateTime.Now,
				Exceptions = GetAllExceptions()
			};
			return ret;
		}
		public ExceptionInfo[] GetAllExceptions() {
			ThrowIfNoSession();
			var exceptions = TopExceptions.SelectMany(top => GetExceptions(top, Session));
			return exceptions.Select(ConvertToGeneric).ToArray();
		}
		public void RestoreSnapshot(Snapshot snapshot) {

			var exceptionsLoad = snapshot.Exceptions;
			//todo: add exceptions not already known
			SetAll((info) => {
				var corresponding = exceptionsLoad.FirstOrDefault(snapEx => snapEx.Name == info.Name && snapEx.GroupName == info.GroupName);
				if (corresponding == null)
					return;
				info.State = corresponding.State;
				if (ExceptionInfo.ConditionsEqual(info.Conditions, corresponding.Conditions))
					return;
				info.Conditions = corresponding.Conditions;
				info.HasChanged = true;
			}, Session);


		}
		void SetExceptions(IEnumerable<EXCEPTION_INFO150> exceptions) {
			ExceptionInfoEnumerator enumerator =
					new ExceptionInfoEnumerator(exceptions);
			Marshal.ThrowExceptionForHR(
				Session.SetExceptions(enumerator));
		}
		public bool VerifySnapshot(Snapshot snapshot) {
			var current = GetCurrentExceptionSnapshot();
			foreach (var ex in snapshot.Exceptions) {
				var corresponding = current.Exceptions.SingleOrDefault(corEx =>
				corEx.Name == ex.Name &&
				corEx.Code == ex.Code &&
				corEx.GroupName == ex.GroupName);

				if (!ex.Equals(corresponding))
					return false;

			}

			return true;
		}

		#endregion

		#region Private

		bool IsExceptionTopException(EXCEPTION_INFO150 exception) {
			return TopExceptions.Any(top => top.bstrExceptionName == exception.bstrExceptionName);
		}

		/// <summary>
		/// resets all exceptions back to default settings
		/// </summary>
		public void ResetExceptionsToDefaults() {
			// while we could implement this why not just have VS do it for us
			ThreadHelper.ThrowIfNotOnUIThread();
			var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
			dte.ExecuteCommand("DebuggerContextMenus.ExceptionSettingsWindow.RestoreDefaults");
		}





		EXCEPTION_INFO150 ConvertFromGeneric(ExceptionInfo exception) {
			return new EXCEPTION_INFO150 {
				bstrExceptionName = exception.Name,
				guidType = TopExceptions.First(ex => ex.bstrExceptionName == exception.GroupName).guidType,
				dwState = (uint)exception.State,
				dwCode = exception.Code,
				pConditions = new ConditionEnumerator(exception.Conditions ?? [])
			};
		}

		ExceptionInfo ConvertToGeneric(EXCEPTION_INFO150 exception) {
			return new ExceptionInfo(exception.bstrExceptionName, TopExceptions.First(ex => ex.guidType == exception.guidType).bstrExceptionName) {
				State = (enum_EXCEPTION_STATE)exception.dwState,
				Code = exception.dwCode,
				Conditions = exception.pConditions.ToArray()
			};
		}

		#endregion


		void ThrowIfNoSession() {
			if (!SessionAvailable) {
				throw new Exception("Session not available.");
			}
		}





		private IVsDebugger VsDebugger {
			get {
				ThreadHelper.ThrowIfNotOnUIThread();
				return InternalDebugger as IVsDebugger;
			}
		}


		private IDebugSession3 Session3 {
			get {
				return Session as IDebugSession3;
			}
		}

		private Debugger Debugger {
			get {
				ThreadHelper.ThrowIfNotOnUIThread();
				var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
				return dte.Debugger as Debugger;
			}
		}


		public void EnableAll() => SetAll((info) => info.BreakFirstChance = true, Session);
		/// <summary>
		/// returns true if changed
		/// </summary>
		/// <param name="exception"></param>
		/// <param name="breakFirstChance"></param>
		/// <returns></returns>

		public void DisableAll() => SetAll((info) => info.BreakFirstChance = false, Session);

		public void Go() {
			ThreadHelper.ThrowIfNotOnUIThread();

			if (InternalDebugger.InBreakMode) {
				Debugger.Go(false);
			}
		}

		


		private void SetAll(UpdateExceptionGeneric action, IDebugSession150 session) {
			ThrowIfNoSession();
			ThreadHelper.ThrowIfNotOnUIThread();
			var exceptions = GetAllExceptions();
			var updated = new List<ExceptionInfo>();

			foreach (var e in exceptions) {
				e.HasChanged = false;
				action(e);
				if (e.HasChanged)
					updated.Add(e);

			}

			if (updated.Any()) {
				var toSet = updated.Select(ConvertFromGeneric).ToList();
				Marshal.ThrowExceptionForHR( session.RemoveSetExceptions(new ExceptionInfoEnumerator(toSet)));
				Marshal.ThrowExceptionForHR( session.SetExceptions(new ExceptionInfoEnumerator(toSet)));
			}
		}


		private EXCEPTION_INFO150[] GetExceptions(EXCEPTION_INFO150? parent, IDebugSession150 session) {
			uint num = 0u;
			var val = default(IEnumDebugExceptionInfo150);
			EXCEPTION_INFO150[] array2;
			//default exceptions returns us all exceptions really we just want changed exceptions

			//if (session.EnumDefaultExceptions(array, out val) == 0 && val != null) {

			if (parent.HasValue && session.EnumSetExceptions(parent.Value.guidType, out val) == 0 && val != null) {

			} else if (!parent.HasValue && session.EnumDefaultExceptions(null, out val) == 0 && val != null) {
			} else {
				return [];
			}

			uint num2 = default(uint);
			val.GetCount(out num2);
			array2 = new EXCEPTION_INFO150[num2];
			val.Next(num2, array2, ref num);

			return array2;
		}

		public static EXCEPTION_INFO Convert(EXCEPTION_INFO150 info) {
			return new EXCEPTION_INFO {
				bstrExceptionName = info.bstrExceptionName,
				bstrProgramName = info.bstrProgramName,
				dwCode = info.dwCode,
				dwState = (enum_EXCEPTION_STATE)info.dwState,
				guidType = info.guidType,
				pProgram = info.pProgram
			};
		}
		
		public void SetBreakOnException(string exceptionType, bool breakEnabled) {
			// Todo if exception type not present need to add....

			SetAll((info) => {
				if (info.Name == exceptionType)
					info.BreakFirstChance = breakEnabled;

			}, Session);
		}

		public void ExcludeModuleFromExceptions(string moduleName, string exceptionType = null) {
			//Technically I can't figure out a way to set it on the root actually but in the UI setting it on the root sets all the children so we can at least do that part
			var condition = new Condition {
				Type = EXCEPTION_CONDITION_TYPE.ModuleName,
				Operator = EXCEPTION_CONDITION_OPERATOR.NotEquals,
				Value = moduleName,
				CallStackBehavior = EXCEPTION_CONDITION_CALLSTACK_BEHAVIOR.TopFrameOnly
			};
			SetAll((info) => {
				if (exceptionType != null && info.Name != exceptionType)
					return;

				var conditions = info.Conditions.ToList();
				conditions.RemoveAll(c => c.Value == moduleName);
				conditions.Add(condition);
				if (ExceptionInfo.ConditionsEqual(info.Conditions, conditions))
					return;
				info.Conditions = conditions.ToArray();
				info.HasChanged = true;
				info.State = Helpers.EXP_ENABLE_FLAGS; // oddly info.State before this is helpers.EXP_CONDITION_FLAGS so we need to re-enable it but not sure why

			}, Session);

		}
	}
}
