using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExceptionSnapshotExtension {
	[VisualStudioContribution]
	internal class ExtensibilityExtensionEntrypoint : Extension {
		public override ExtensionConfiguration ExtensionConfiguration => new() {
			RequiresInProcessHosting = true,
		};
		protected override void InitializeServices(IServiceCollection serviceCollection) {

			base.InitializeServices(serviceCollection);

		}
	}

	internal sealed partial class PackageGuids {
		public const string guidVSDebugGroupString = "C9DD4A58-47FB-11D2-83E7-00C04F9902C1";
		public static readonly Guid guidVSDebugGroup = new Guid(guidVSDebugGroupString);
	}
	internal sealed partial class PackageIds {
		public const int IDM_DEBUG_WINDOWS = 0x0402;
		public const int IDG_DEBUG_WINDOWS_GENERAL = 0x002;

				
	}
}
