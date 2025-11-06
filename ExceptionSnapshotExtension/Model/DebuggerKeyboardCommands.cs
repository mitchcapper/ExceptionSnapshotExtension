using Microsoft;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Shell;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ExceptionSnapshotExtension.Model {
	// rip works for command pallette but not keybaord shortcut manager: https://developercommunity.visualstudio.com/t/VisualStudioExtensibility-Support-key/10893871 will need to use old style commands.
	/// <summary>
	/// DebuggerKeyboardCommands handler.
	/// </summary>
	[VisualStudioContribution]
	internal class DebuggerKeyboardCommands : Command {
		private readonly TraceSource logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="DebuggerKeyboardCommands"/> class.
		/// </summary>
		/// <param name="traceSource">Trace source instance to utilize.</param>
		public DebuggerKeyboardCommands(TraceSource traceSource) {
			// This optional TraceSource can be used for logging in the command. You can use dependency injection to access
			// other services here as well.
			this.logger = Requires.NotNull(traceSource, nameof(traceSource));
		}

		/// <inheritdoc />
		public override CommandConfiguration CommandConfiguration => new(displayName: "ESE Debugger Ignore Current Exception Type") {
			// Use this object initializer to set optional parameters for the command. The required parameter,
			// displayName, is set above. To localize the displayName, add an entry in .vsextension\string-resources.json
			// and reference it here by passing "%ExceptionSnapshotExtension.Model.DebuggerKeyboardCommands.DisplayName%" as a constructor parameter.
			//Placements = [CommandPlacement.KnownPlacements.ExtensionsMenu],
			//Icon = new(ImageMoniker.KnownValues.Extension, IconSettings.IconAndText),
		};

		/// <inheritdoc />
		public override Task InitializeAsync(CancellationToken cancellationToken) {
			// Use InitializeAsync for any one-time setup or initialization.
			return base.InitializeAsync(cancellationToken);
		}

		/// <inheritdoc />
		public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken) {
			await this.Extensibility.Shell().ShowPromptAsync("Hello from an extension!", PromptOptions.OK, cancellationToken);
		}
	}
}
