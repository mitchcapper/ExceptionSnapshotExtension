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
	internal class ExceptionManager : IExceptionManager, IDebugEventCallback2, IVsDebuggerEvents {
		private delegate void UpdateException(ref EXCEPTION_INFO150 exception, out bool changed);

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



		public bool SessionAvailable => Session != null;

		EXCEPTION_INFO150[] TopExceptions => field ??= GetExceptions(null, Session);

		#region  public

		public event EventHandler<DBGMODE> DebuggerStatusChanged;
		public event EventHandler<DebuggerExceptionEventArgs> DebuggerException;


		public Snapshot GetCurrentExceptionSnapshot() {
			ThrowIfNoSession();

			var exceptions = TopExceptions.SelectMany(top => GetExceptions(top, Session));

			return new Snapshot {
				Exceptions = exceptions.Select(ConvertToGeneric).ToArray()
			};
		}

		public void RestoreSnapshot(Snapshot snapshot) {
			ThrowIfNoSession();

			RemoveAllSetExceptions();

			var nativeExceptions = snapshot.
				Exceptions.
				Select(ex => ConvertFromGeneric(ex));
			var topExceptions = nativeExceptions.Where(ex => IsExceptionTopException(ex));

			SetExceptions(topExceptions);
			SetExceptions(nativeExceptions.Except(topExceptions));
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

				if (ex.BreakFirstChance != corresponding.BreakFirstChance) {
					return false;
				}
			}

			return true;
		}

		#endregion

		#region Private

		bool IsExceptionTopException(EXCEPTION_INFO150 exception) {
			return TopExceptions.Any(top => top.bstrExceptionName == exception.bstrExceptionName);
		}

		void RemoveAllSetExceptions() {
			Guid guidType = Guid.Empty;
			Marshal.ThrowExceptionForHR(
					  InternalDebugger.CurrentSession.RemoveAllSetExceptions(ref guidType));
		}





		EXCEPTION_INFO150 ConvertFromGeneric(ExceptionInfo exception) {
			return new EXCEPTION_INFO150 {
				bstrExceptionName = exception.Name,
				guidType = TopExceptions.First(ex => ex.bstrExceptionName == exception.GroupName).guidType,
				dwState = (uint)exception.State,
				dwCode = exception.Code,
				pConditions = new ConditionEnumerator(exception.Conditions ?? new Condition[] { })
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



		private bool subscribed = false;


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


		public void EnableAll() {
			ThreadHelper.ThrowIfNotOnUIThread();
			var session = Session;
			if (session != null) {

				SetAll((ref EXCEPTION_INFO150 info, out bool changed) => {
					MsgLog(info.dwCode + "  :  " + info.bstrExceptionName);
					changed = true;
					SetBreakFirstChance(ref info, true);
				}, session);
			}
		}
		void SetBreakFirstChance(ref EXCEPTION_INFO150 exception, bool breakFirstChance) {
			if (breakFirstChance) {
				exception.StateAddFlags(Helpers.EXP_ENABLE_FLAGS);
			} else {
				exception.StateSet(enum_EXCEPTION_STATE.EXCEPTION_NONE);
			}
		}
		public void DisableAll() {
			var session = Session;
			if (session != null) {
				SetAll((ref EXCEPTION_INFO150 info, out bool changed) => {
					changed = true;
					SetBreakFirstChance(ref info, false);
				}, session);
			}
		}

		public void Go() {
			ThreadHelper.ThrowIfNotOnUIThread();

			if (InternalDebugger.InBreakMode) {
				Debugger.Go(false);
			}
		}

		public string GetCurrentExceptionType() {
			ThreadHelper.ThrowIfNotOnUIThread();
			if (InternalDebugger.InBreakMode) {
				var debugger = Debugger;
				var reason = debugger.LastBreakReason;

				if (reason == dbgEventReason.dbgEventReasonExceptionThrown || reason == dbgEventReason.dbgEventReasonExceptionNotHandled) {
					var expr = debugger.GetExpression("$exception.GetType().FullName", false, -1);
					var res = expr.Value;
					return res;
				}
			}

			return null;
		}

		public void AttachEventsListener() {
			ThreadHelper.ThrowIfNotOnUIThread();
			if (subscribed)
				return;
			subscribed = true;
			Marshal.ThrowExceptionForHR(VsDebugger.AdviseDebuggerEvents(this, out AdviseDebuggerEventsCookie));
			var dbgMode = new DBGMODE[1];
			OnModeChange(dbgMode[0]);

		}
		public void DetachEventsListener() {
			ThreadHelper.ThrowIfNotOnUIThread();
			if (!subscribed)
				return;
			if (AdviseDebuggerEventsCookie != 0) {
				VsDebugger.UnadviseDebuggerEvents(AdviseDebuggerEventsCookie);
				OnModeChange(DBGMODE.DBGMODE_Design);//to stop it as well
			}
		}

		private uint AdviseDebuggerEventsCookie;


		private void SetAll(UpdateException action, IDebugSession150 session) {
			var topExceptions = GetExceptions(null, session);

			List<EXCEPTION_INFO150> updated = new List<EXCEPTION_INFO150>();
			for (int i = 0; i < topExceptions.Length; i++) {
				action(ref topExceptions[i], out bool changed);
				if (changed) {
					updated.Add(topExceptions[i]);
				}
			}

			if (updated.Any()) {
				session.SetExceptions(new ExceptionInfoEnumerator(updated.ToList()));
				updated.Clear();
			}

			List<EXCEPTION_INFO150> allChildren = new List<EXCEPTION_INFO150>();
			foreach (var topException in topExceptions) {
				var childExceptions = GetExceptions(topException, session);
				for (int i = 0; i < childExceptions.Count(); i++) {
					action(ref childExceptions[i], out bool changed);
					if (changed) {
						updated.Add(childExceptions[i]);
					}
				}
				allChildren.AddRange(childExceptions);

			}

			if (updated.Any()) {
				session.SetExceptions(new ExceptionInfoEnumerator(updated.ToList()));
			}
		}

		private EXCEPTION_INFO150[] GetExceptions(EXCEPTION_INFO150? parent, IDebugSession150 session) {
			uint num = 0u;
			EXCEPTION_INFO150[] array = (EXCEPTION_INFO150[])((!parent.HasValue) ? null : new EXCEPTION_INFO150[1]
			{
				parent.Value
			});
			IEnumDebugExceptionInfo150 val = default(IEnumDebugExceptionInfo150);
			EXCEPTION_INFO150[] array2;
			if (session.EnumDefaultExceptions(array, out val) == 0 && val != null) {
				uint num2 = default(uint);
				val.GetCount(out num2);
				array2 = (EXCEPTION_INFO150[])new EXCEPTION_INFO150[num2];
				val.Next(num2, array2, ref num);
			} else {
				array2 = (EXCEPTION_INFO150[])new EXCEPTION_INFO150[0];
			}
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
		private enum_FRAMEINFO_FLAGS FrameDetailsWanted =
				enum_FRAMEINFO_FLAGS.FIF_FUNCNAME |
				enum_FRAMEINFO_FLAGS.FIF_RETURNTYPE |
				enum_FRAMEINFO_FLAGS.FIF_ARGS |
				enum_FRAMEINFO_FLAGS.FIF_LANGUAGE |
				enum_FRAMEINFO_FLAGS.FIF_MODULE |
				enum_FRAMEINFO_FLAGS.FIF_STACKRANGE |
				enum_FRAMEINFO_FLAGS.FIF_FRAME |
				enum_FRAMEINFO_FLAGS.FIF_DEBUGINFO |
				enum_FRAMEINFO_FLAGS.FIF_STALECODE |
				enum_FRAMEINFO_FLAGS.FIF_DEBUG_MODULEP |
				enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_FORMAT |
				enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_RETURNTYPE |
				enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS |
				enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_LANGUAGE |
				enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_MODULE |
				enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_LINES |
				enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_OFFSET |
				enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS_ALL |
				enum_FRAMEINFO_FLAGS.FIF_ARGS_ALL;


		private void GetCallstack(IDebugThread2 pThread) {
			var props = new THREADPROPERTIES[1];
			pThread.GetThreadProperties(enum_THREADPROPERTY_FIELDS.TPF_ALLFIELDS, props);

			MsgLog($"%%PRIMARY%% Frame prop {props[0].bstrLocation} -- name: {props[0].bstrName} -- {props[0].dwFields}");
			//IDebugThread2::EnumFrameInfo 
			IEnumDebugFrameInfo2 frame;
			pThread.EnumFrameInfo(FrameDetailsWanted, 0, out frame);
			uint frames;
			frame.GetCount(out frames);
			var frameInfo = new FRAMEINFO[1];
			uint pceltFetched = 0;
			while (frame.Next(1, frameInfo, ref pceltFetched) == VSConstants.S_OK && pceltFetched > 0) {
				var fr = frameInfo[0].m_pFrame as IDebugStackFrame3;
				var modInfo = frameInfo[0].m_pModule as IDebugModule2; //would then request the info we want from it
				MsgLog($"\tFrame Func: {frameInfo[0].m_bstrFuncName} module: {frameInfo[0].m_bstrModule}  ");
				continue;

				IDebugExpressionContext2 expressionContext;
				fr.GetExpressionContext(out expressionContext);
				IDebugExpression2 de; string error; uint errorCode;
				if (expressionContext != null) {
					expressionContext.ParseText("exception.InnerException.StackTrace", enum_PARSEFLAGS.PARSE_EXPRESSION, 0, out de, out error, out errorCode);
					IDebugProperty2 dp2;
					var res = de.EvaluateSync(enum_EVALFLAGS.EVAL_RETURNVALUE, 5000, null, out dp2);

					var myInfo = new DEBUG_PROPERTY_INFO[1];
					dp2.GetPropertyInfo(enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ALL, 0, 5000, null, 0, myInfo);
					var stackTrace = myInfo[0].bstrValue;

					IDebugProperty2 dp;
					fr.GetDebugProperty(out dp);
					IEnumDebugPropertyInfo2 prop;
					//dp.EnumChildren(enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ALL, 0, ref DebugFilterGuids.guidFilterAllLocals,
					//    enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_ACCESS_ALL,
					///*(uint)enum_DBGPROP_ATTRIB_FLAGS.DBGPROP_ATTRIB_ACCESS_PUBLIC,*/
					//null, 5000, out prop);

					EnumerateDebugPropertyChildren(prop);
				}
				//Guid filter = dbgGuids.guidFilterAllLocals; uint pElements; IEnumDebugPropertyInfo2 prop;
				//fr.EnumProperties(enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ALL, 0, ref filter, 5000, out pElements, out prop);

				//fr.GetUnwindCodeContext

				//fr.EnumProperties(enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ALL);
				//fr.GetUnwindCodeContext
				//ulong intCookie;
				//fr.InterceptCurrentException(enum_INTERCEPT_EXCEPTION_ACTION.IEA_INTERCEPT, out intCookie);
				//fr.GetExpressionContext(
				//var s = fr.ToString();
			}
		}

		private static void EnumerateDebugPropertyChildren(IEnumDebugPropertyInfo2 prop) {
			uint numChildren = 0;
			prop.GetCount(out numChildren);
			var count = numChildren;
			while (count > 0) {
				var xxx = new DEBUG_PROPERTY_INFO[1];
				uint xxx_fetched = 0;
				prop.Next(1, xxx, out xxx_fetched);
				if (xxx_fetched == 0)
					break;
				MsgLog(xxx[0].bstrName + ":" + xxx[0].bstrType + ":" + xxx[0].bstrValue);

				count--;
			}
		}

		Regex ExceptionDescExtraInfo = new(@" in (?<module>.+)\n(?<descRest>.+)", RegexOptions.Singleline | RegexOptions.Compiled);
		public int Event(IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib) {
			// 51a94113-8788-4a54-ae15-08b74ff922d0 IDebugExceptionEvent2 IDebugExceptionEvent150 AD7StoppingEvent
			try {
				if (riidEvent == typeof(IDebugExceptionEvent2).GUID
					) {
					IDebugExceptionEvent2 ev = pEvent as IDebugExceptionEvent2;
					if (ev != null) {
						var info = new EXCEPTION_INFO[1];
						var arg = new DebuggerExceptionEventArgs();
						ev.GetExceptionDescription(out arg.description);
						ev.GetException(info);
						arg.exceptionType = info[0].bstrExceptionName;
						arg.reason = info[0].dwState;
						if (arg.description != null) {
							var match = ExceptionDescExtraInfo.Match(arg.description);
							if (match.Success) {
								arg.moduleName = match.Groups["module"]?.Value?.Trim();
								arg.description = match.Groups["descRest"]?.Value?.Trim();
							}
						}
						// guid here is the language for it prolly CLR/C++/etc.
						// name is the full exception type like: "System.Net.Sockets.SocketException" guid is unrelated here
						//state == enum_EXCEPTION_STATE.EXCEPTION_STOP_SECOND_CHANCE
						MsgLog($"#@# NEW EXCEPTION: {arg}");
						this.DebuggerException?.Invoke(this, arg);
						// while we can easily get the thread and stack info the module the UI attributes the exception to is best parsed from {desc} here as it is not always the first frame enor the primary thread info module.
						//GetCallstack(pThread);
					}
				}
				return 0;
				if (typeof(IDebugExceptionEvent2).GUID == riidEvent) {
					//var e2 = pEvent as IDebugExceptionEvent2;
					//var e = pEvent as IDebugExceptionEvent150;
					//var cp = e2.CanPassToDebuggee();
					//EXCEPTION_INFO150 info = new EXCEPTION_INFO150();
					//var arr = new EXCEPTION_INFO150[] { info };
					//var res = e.GetException(arr);
					//res = e2.PassToDebuggee(1);

					//e.GetExceptionDetails(out IDebugExceptionDetails details);
					//details.GetTypeName(1, out string typeName);
					//details.GetSource(out string sourceName);

					//var engine150 = pEngine as IDebugEngine150;


					var session = Session;
					MsgLog("IDebugExceptionEvent2");
					//var topExceptions = GetExceptions(null, session);

					//List<EXCEPTION_INFO150> updated = new List<EXCEPTION_INFO150>();
					//List<EXCEPTION_INFO150> allChildren = new List<EXCEPTION_INFO150>();
					//foreach (var topException in topExceptions)
					//{
					//    var childExceptions = GetExceptions(topException, session);
					//    for (int i = 0; i < childExceptions.Count(); i++)
					//    {
					//        childExceptions[i].dwState &= 4294967278u;
					//        updated.Add(childExceptions[i]);

					//        if (childExceptions[i].bstrExceptionName == typeName.Trim('\"'))
					//        {
					//            pEngine.SetException(new EXCEPTION_INFO[] { Convert(childExceptions[i]) });
					//        }
					//    }
					//    allChildren.AddRange(childExceptions);
					//}

					//if (updated.Any())
					//{
					//    engine150.SetExceptions(new ExceptionInfoEnumerator(updated.ToList()));
					//}
				} else if (Guid.Parse("04bcb310-5e1a-469c-87c6-4971e6c8483a") == riidEvent) {
					MsgLog("Try 1");
					//var ex = new ExceptionManager2017().GetCurrentExceptionType();
					//if (ex != null)
					//{
					//pProgram.Continue(pThread);
					//}
				} else {
					MsgLog(riidEvent.ToString());
				}
			} catch {

			} finally {
				if (pEngine != null) Marshal.ReleaseComObject(pEngine);
				if (pProcess != null) Marshal.ReleaseComObject(pProcess);
				if (pProgram != null) Marshal.ReleaseComObject(pProgram);
				if (pThread != null) Marshal.ReleaseComObject(pThread);
				if (pEvent != null) Marshal.ReleaseComObject(pEvent);
			}
			return 0;
		}
		private bool isListening;


		public int OnModeChange(DBGMODE dbgmodeNew) {
			MsgLog($"Debug Mode Change: {dbgmodeNew}");
			DebuggerStatusChanged?.Invoke(this, dbgmodeNew);
			ThreadHelper.ThrowIfNotOnUIThread();
			if (dbgmodeNew == DBGMODE.DBGMODE_Design && isListening) {
				var res2 = VsDebugger.UnadviseDebugEventCallback(this);
				isListening = false;
			} else if (!isListening && new DBGMODE[] { DBGMODE.DBGMODE_Run, DBGMODE.DBGMODE_Break }.Contains(dbgmodeNew)) {
				var _debuggerPackage = (IVsDebugger)Package.GetGlobalService(typeof(IVsDebugger));
				Marshal.ThrowExceptionForHR(_debuggerPackage.AdviseDebugEventCallback(this));

				isListening = true;
			}

			// Going to run mode

			return VSConstants.S_OK;
		}

		private static void MsgLog(string msg) {
			System.Diagnostics.Debug.WriteLine("ESE: " + msg);
		}


	}
}
