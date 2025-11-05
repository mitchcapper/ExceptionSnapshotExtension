using Microsoft.VisualStudio.Debugger.Interop;
using Newtonsoft.Json;
using System;

namespace ExceptionSnapshotExtension.Model {
	internal class ExceptionInfo {

		public string Name { get; }
		public string GroupName { get; }
		public enum_EXCEPTION_STATE State { get; set; }

		// The identification code for the exception or run-time error.
		public uint Code { get; set; }

		[JsonIgnore]
		public bool BreakFirstChance {
			get {
				return State.HasFlag(Helpers.EXP_ENABLE_FLAGS);
			}
			set {
				if (value) {
					State |= Helpers.EXP_ENABLE_FLAGS;
				} else {
					State = enum_EXCEPTION_STATE.EXCEPTION_NONE;
				}

			}
		}

		public Condition[] Conditions { get; set; }

		public ExceptionInfo(string name, string groupName) {
			Name = name;
			GroupName = groupName;
		}
		public override bool Equals(object obj) {
			if (obj is null || this is null && obj != this) return false;
			if (obj is not ExceptionInfo ex) return false;
			if (!Name.Equals(ex.Name) || !GroupName.Equals(ex.GroupName) || Code != ex.Code || State != ex.State) return false;
			if (Conditions?.Length != ex.Conditions?.Length) return false;
			if (Conditions != null) {
				for (int i = 0; i < Conditions.Length; i++) {
					if (!Conditions[i].Equals(ex.Conditions[i])) return false;
				}
			}
			return true;
		}
	}

	internal class Condition : IDebugExceptionCondition {
		public EXCEPTION_CONDITION_TYPE Type { get; set; }

		public EXCEPTION_CONDITION_CALLSTACK_BEHAVIOR CallStackBehavior { get; set; }

		public EXCEPTION_CONDITION_OPERATOR Operator { get; set; }

		public string Value { get; set; }
		public override bool Equals(object obj) {
			if (obj is null || this is null && obj != this) return false;
			if (obj is not Condition cond) return false;
			return Type == cond.Type &&
				CallStackBehavior == cond.CallStackBehavior &&
				Operator == cond.Operator &&
				Value == cond.Value;
		}
	}

	internal class Snapshot {
		public string Name { get; set; }
		public DateTime CreatedOn { get; set; }
		public ExceptionInfo[] Exceptions { get; set; }
	}
}
