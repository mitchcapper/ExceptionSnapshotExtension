using Microsoft.VisualStudio.Debugger.Interop;
using System.Collections.Generic;
using System.Linq;

namespace ExceptionSnapshotExtension.Services {
	internal class ExceptionInfoEnumerator : IEnumDebugExceptionInfo150 {
		private readonly IList<EXCEPTION_INFO150> _data;

		private uint _position;

		private object _syncObj = new object();

		internal ExceptionInfoEnumerator(IEnumerable<EXCEPTION_INFO150> data) {
			_data = data.ToList();
		}

		public int Clone(out IEnumDebugExceptionInfo150 ppEnum) {
			ppEnum = null;
			return -2147467263;
		}

		public int GetCount(out uint pcelt) {
			pcelt = (uint)_data.Count;
			return 0;
		}

		public int Next(uint celt, EXCEPTION_INFO150[] rgelt, ref uint pceltFetched) {
			return Move(celt, rgelt, out pceltFetched);
		}

		public int Reset() {
			lock (_syncObj) {
				_position = 0u;
			}
			return 0;
		}

		public int Skip(uint celt) {
			uint celtFetched;
			return Move(celt, null, out celtFetched);
		}
		//clearly this was decompiled fomr somewhere not sure where
		private int Move(uint celt, EXCEPTION_INFO150[] rgelt, out uint celtFetched) {
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			lock (this) {
				int result = 0;
				celtFetched = (uint)(_data.Count - (int)_position);
				if (celt > celtFetched) {
					result = 1;
				} else if (celt < celtFetched) {
					celtFetched = celt;
				}
				if (rgelt != null) {
					for (int i = 0; i < celtFetched; i++) {
						rgelt[i] = _data[(int)_position + i];
					}
				}
				_position += celtFetched;
				return result;
			}
		}
	}

	public static class IEnumDebugExceptionInfo150Extension {
		public static EXCEPTION_INFO150[] ToArray(this IEnumDebugExceptionInfo150 enumerator) {
			uint num = 0;
			uint count = default(uint);
			enumerator.GetCount(out count);
			var array = new EXCEPTION_INFO150[count];
			enumerator.Next(count, array, ref num);
			return array;
		}
	}
}
