using Microsoft.VisualStudio.Extensibility.UI;

namespace ExceptionSnapshotExtension {
	/// <summary>
	/// A remote user control to use as tool window UI content.
	/// </summary>
	internal class OurExceptionWindowWrapperContent : RemoteUserControl {
		/// <summary>
		/// Initializes a new instance of the <see cref="OurExceptionWindowWrapperContent" /> class.
		/// </summary>
		public OurExceptionWindowWrapperContent()
			: base(dataContext: ExceptionPackage.MasterViewModel) {
		}
	}
}
