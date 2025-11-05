using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.Debugger.Interop.Internal {
	[Guid("1DA40549-8CCC-48CF-B99B-FC22FE3AFEDF")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IDebuggerInternal15 {
		int GetSourceFileWithChecksum(string bstrSearchFilePath, ref Guid checksumAlgorithm, Array Checksum, out string bstrFoundFilePath);
		int GetSourceFileFromDocumentContext(Interop.IDebugDocumentContext2 pDocumentContext, Interop.IDebugCodeContext2 pCodeContext, out string bstrFoundFilePath, out Guid checksumAlgorithm, out Array Checksum);
		int GetCodeContextOfExpression(string expression, out Interop.IDebugCodeContext2 ppCodeContext, out string error);
		int GetDebuggerOption(DEBUGGER_OPTIONS option, out uint value);
		int GetDebuggerStringOption(DEBUGGER_STRING_OPTIONS option, out string value);
		int SetDebuggerOption(DEBUGGER_OPTIONS option, uint value);
		int SetDebuggerStringOption(DEBUGGER_STRING_OPTIONS option, string value);
		int SetNextStatement(Interop.IDebugCodeContext2 pCodeContext);
		int RunToStatement(Interop.IDebugCodeContext2 pCodeContext);
		int GoToSource(Interop.IDebugDocumentContext2 pDocContext, Interop.IDebugCodeContext2 pCodeContext);
		int GoToDisassembly(Interop.IDebugCodeContext2 pCodeContext, Interop.IDebugProgram2 pProgramOfCodeContext);
		int EditBreakpoint(IBreakpoint pBreakpoint, BREAKPOINT_EDIT_OPERATION op);
		int SetEngineMetric(ref Guid engine, string metric, object var);
		int UpdateAddressMarkers();
		int DeleteAddressMarkers();
		int ShowVisualizer([ComAliasName("AD7InteropA.DEBUG_PROPERTY_INFO")] Interop.DEBUG_PROPERTY_INFO[] data, uint visualizerId);
		int UpdateExpressionValue([ComAliasName("AD7InteropA.DEBUG_PROPERTY_INFO")] Interop.DEBUG_PROPERTY_INFO[] data, string newValue, out string error);
		int CreateObjectID([ComAliasName("AD7InteropA.DEBUG_PROPERTY_INFO")] Interop.DEBUG_PROPERTY_INFO[] data);
		int DestroyObjectID([ComAliasName("AD7InteropA.DEBUG_PROPERTY_INFO")] Interop.DEBUG_PROPERTY_INFO[] data);
		int IndicateEvalRequiresRefresh([ComAliasName("AD7InteropA.DEBUG_PROPERTY_INFO")] Interop.DEBUG_PROPERTY_INFO[] src);
		int RegisterInternalEventSink(IDebuggerInternalEvents pEvents);
		int UnregisterInternalEventSink(IDebuggerInternalEvents pEvents);
		int IsProcessActivelyBeingDebugged(Interop.IDebugProcess2 pProcess, out bool retVal);
		int OnCaretMoved();
		int GetSymbolPathInternal(out string pbstrSymbolPath, out string pbstrSymbolCachePath);
		int GetImageInfo(string imageName, [ComAliasName("Microsoft.VisualStudio.Debugger.Interop.Internal.IMAGEINFO_TYPE")] IMAGEINFO_TYPE[] pImageInfoType);
		int IsInteropSupported(string imageName);
		int GetErrorMessageByHRESULT(int err, out string bstrErrorMessage);

		[DispId(1610678300)]
		Interop.IDebugSession3 CurrentSession { get; }
		[DispId(1610678301)]
		Interop.IDebugProgram2 CurrentProgram { get; set; }
		[DispId(1610678303)]
		Interop.IDebugThread2 CurrentThread { get; set; }
		[DispId(1610678305)]
		Interop.IEnumDebugFrameInfo2 CurrentStack { get; }
		[DispId(1610678306)]
		Interop.IDebugStackFrame2 CurrentStackFrame { get; set; }
		[DispId(1610678308)]
		Interop.IDebugCodeContext2 CurrentCodeContext { get; }
		[DispId(1610678309)]
		Interop.IDebugStackFrame2 TopMostStackFrame { get; }
		[DispId(1610678310)]
		Interop.IDebugCodeContext2 TopMostCodeContext { get; }
		[DispId(1610678311)]
		bool InDisassemblyMode { get; set; }
		[DispId(1610678313)]
		bool InApplyCodeChanges { get; }
		[DispId(1610678314)]
		bool InBreakMode { get; }
		[DispId(1610678315)]
		IBreakpointManager BreakpointManager { get; }
		[DispId(1610678316)]
		bool ArePendingEditsBlockingSetNext { get; }
		[DispId(1610678317)]
		IDataTipManager DataTipManager { get; }
		[DispId(1610678318)]
		Interop.IDebugProcess2 CurrentRunModeProcess { get; }
	}
}
