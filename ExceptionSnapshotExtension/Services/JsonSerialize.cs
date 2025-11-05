using ExceptionSnapshotExtension.Model;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace ExceptionSnapshotExtension.Services {
	class JsonSerialize : ISerialize {
		public IEnumerable<Snapshot> Deserialize(Stream snapshots) {
			using (StreamReader sr = new StreamReader(snapshots))
			using (JsonReader reader = new JsonTextReader(sr)) {
				JsonSerializer serializer = new JsonSerializer();
				return serializer.Deserialize<IEnumerable<Snapshot>>(reader);
			}
		}

		public void Serialize(IEnumerable<Snapshot> snapshots, Stream targetStream) {
			using (StreamWriter sw = new StreamWriter(targetStream))
			using (JsonWriter writer = new JsonTextWriter(sw)) {
				JsonSerializer serializer = new JsonSerializer();
				serializer.Serialize(writer, snapshots);
			}
		}
	}
}
