using System.Collections.Generic;
using System.IO;

namespace ExceptionSnapshotExtension.Model {
	internal interface ISerialize {
		void Serialize(IEnumerable<Snapshot> snapshots, Stream targetStream);
		IEnumerable<Snapshot> Deserialize(Stream snapshots);
	}
}
