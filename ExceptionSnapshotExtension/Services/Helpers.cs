using Microsoft.VisualStudio.Debugger.Interop;

namespace ExceptionSnapshotExtension {
	static class Helpers {
		public const enum_EXCEPTION_STATE EXP_ENABLE_FLAGS = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE |
																enum_EXCEPTION_STATE.EXCEPTION_STOP_USER_FIRST_CHANCE;
		public const enum_EXCEPTION_STATE EXP_DISABLE_FLAGS = enum_EXCEPTION_STATE.EXCEPTION_NONE;
		public static void StateSet(ref this EXCEPTION_INFO150 ex, enum_EXCEPTION_STATE state) {
			ex.dwState = (uint)state;
		}
		public static void StateAddFlags(ref this EXCEPTION_INFO150 ex, enum_EXCEPTION_STATE state) {
			ex.dwState |= (uint)state;
		}
		public static bool StateHasFlag(in this EXCEPTION_INFO150 ex, enum_EXCEPTION_STATE state) {
			return (ex.dwState & (uint)state) == (uint)state;
		}

	}
}
