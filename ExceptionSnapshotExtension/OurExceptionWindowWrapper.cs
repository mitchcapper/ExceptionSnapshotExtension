using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.ToolWindows;
using Microsoft.VisualStudio.RpcContracts.RemoteUI;
using System.Threading;
using System.Threading.Tasks;

namespace ExceptionSnapshotExtension {
	/* For extensibility they require remote UI even for in-proc tool windows, we cheat to get around this by just using the 'remote ui' as a content control around our actual usercontrol*/
	/// <summary>
	/// A sample tool window.
	/// </summary>
	[VisualStudioContribution]
	public class OurExceptionWindowWrapper : ToolWindow {
		private readonly OurExceptionWindowWrapperContent content = new();

		/// <summary>
		/// Initializes a new instance of the <see cref="OurExceptionWindowWrapper" /> class.
		/// </summary>
		public OurExceptionWindowWrapper() {
			this.Title = "Exception Snapshots";
		}

		/// <inheritdoc />
		public override ToolWindowConfiguration ToolWindowConfiguration => new() {
			// Use this object initializer to set optional parameters for the tool window.
			Placement = ToolWindowPlacement.Floating,
			AllowAutoCreation = true,
			DockDirection = Dock.Right,
		};

		/// <inheritdoc />
		public override Task InitializeAsync(CancellationToken cancellationToken) {
			// Use InitializeAsync for any one-time setup or initialization.
			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public override Task<IRemoteUserControl> GetContentAsync(CancellationToken cancellationToken) {
			return Task.FromResult<IRemoteUserControl>(content);
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing) {
			if (disposing)
				content.Dispose();

			base.Dispose(disposing);
		}
	}
}
