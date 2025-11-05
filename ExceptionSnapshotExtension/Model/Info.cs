using ExceptionSnapshotExtension.Services;
using Microsoft.VisualStudio.Debugger.Interop;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExceptionSnapshotExtension.Model
{
    internal class ExceptionInfo
    {
        public string Name { get; }
        public string GroupName { get; }
        public enum_EXCEPTION_STATE State { get; set; }
        public uint Code { get; set; }

        [JsonIgnore]
        public bool BreakFirstChance
        {
            get
            {
                return State.HasFlag(Helpers.EXP_ENABLE_FLAGS);
            }
            set
            {
                if (value)
                {
                    State |= Helpers.EXP_ENABLE_FLAGS;
                }
                else
                {
                    State = enum_EXCEPTION_STATE.EXCEPTION_NONE;
                }

            }
        }

        public Condition[] Conditions { get; set; }

        public ExceptionInfo(string name, string groupName)
        {
            Name = name;
            GroupName = groupName;
        }
    }

    internal class Condition : IDebugExceptionCondition
    {
        public EXCEPTION_CONDITION_TYPE Type { get; set; }

        public EXCEPTION_CONDITION_CALLSTACK_BEHAVIOR CallStackBehavior { get; set; }

        public EXCEPTION_CONDITION_OPERATOR Operator { get; set; }

        public string Value { get; set; }
    }

    internal class Snapshot
    {
        public string Name { get; set; }
        public ExceptionInfo[] Exceptions { get; set; }
    }
}
