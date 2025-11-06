using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Shell;
using Microsoft.VisualStudio.Extensibility.ToolWindows;
using Microsoft.VisualStudio.RpcContracts.RemoteUI;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ExceptionSnapshotExtension {
	/// <summary>
	/// SnapshotOpenWindowCmd handler.
	/// </summary>
	[VisualStudioContribution]
	internal class SnapshotOpenWindowCmd : Command {
		private readonly TraceSource logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="SnapshotOpenWindowCmd"/> class.
		/// </summary>
		/// <param name="traceSource">Trace source instance to utilize.</param>
		public SnapshotOpenWindowCmd(TraceSource traceSource) {
			// This optional TraceSource can be used for logging in the command. You can use dependency injection to access
			// other services here as well.
			this.logger = Requires.NotNull(traceSource, nameof(traceSource));
		}

		/// <inheritdoc />
		public override CommandConfiguration CommandConfiguration => new(displayName: "Exception Snapshots") {
			Placements = [CommandPlacement.VsctParent(PackageGuids.guidVSDebugGroup, PackageIds.IDG_DEBUG_WINDOWS_GENERAL, 0x0210)],
			TooltipText = "Open the Exception Snapshots window",

			Icon = new CommandIconConfiguration(ImageMoniker.Custom("ExceptionBreak"), IconSettings.IconAndText),

		};

		/// <inheritdoc />
		public override Task InitializeAsync(CancellationToken cancellationToken) {
			// Use InitializeAsync for any one-time setup or initialization.
			return base.InitializeAsync(cancellationToken);
		}

		/// <inheritdoc />
		public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken) {
			//await this.Extensibility.Shell().ShowToolWindowAsync<MySnapshotWindow>(activate: true, cancellationToken);
			await this.Extensibility.Shell().ShowToolWindowAsync<OurExceptionWindowWrapper>(activate: true, cancellationToken);
		}
	}
	
}
