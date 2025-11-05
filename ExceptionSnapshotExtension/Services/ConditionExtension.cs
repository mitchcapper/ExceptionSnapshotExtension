using ExceptionSnapshotExtension.Model;
using Microsoft.VisualStudio.Debugger.Interop;

namespace ExceptionSnapshotExtension.Services {
	internal static class ConditionExtension {
		public static Condition[] ToArray(this IDebugExceptionConditionList list) {
			if (list != null) {
				Condition[] conditions = new Condition[list.Count];
				for (int i = 0; i < list.Count; i++) {
					conditions[i] = list[i].Clone();
				}

				return conditions;
			} else {
				return new Condition[] { };
			}
		}

		public static Condition Clone(this IDebugExceptionCondition source) {
			return new Condition {
				Type = source.Type,
				Operator = source.Operator,
				CallStackBehavior = source.CallStackBehavior,
				Value = source.Value
			};
		}
	}
}
