using ExceptionSnapshotExtension.Model;
using ExceptionSnapshotExtension.Services;
using ExceptionSnapshotExtension.Viewmodels;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace ExceptionSnapshotExtension {
	/// <summary>
	/// This is the class that implements the package exposed by this assembly.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The minimum requirement for a class to be considered a valid package for Visual Studio
	/// is to implement the IVsPackage interface and register itself with the shell.
	/// This package uses the helper classes defined inside the Managed Package Framework (MPF)
	/// to do it: it derives from the Package class that provides the implementation of the
	/// IVsPackage interface and uses the registration attributes defined in the framework to
	/// register itself and its components with the shell. These attributes tell the pkgdef creation
	/// utility what data to put into .pkgdef file.
	/// </para>
	/// <para>
	/// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
	/// </para>
	/// </remarks>
	[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
	[Guid(ExceptionPackage.PackageGuidString)]
	[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
	[ProvideMenuResource("Menus.ctmenu", 1)]
	//[ProvideToolWindow(typeof(SnapshotWindow))]
	[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
	[ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
	public sealed class ExceptionPackage : AsyncPackage {
		/// <summary>
		/// ExceptionPackage GUID string.
		/// </summary>
		public const string PackageGuidString = "adc51aeb-591e-44f7-99e8-6dc173da31a0";
		private const string SETTINGS_KEY = "VladHExceptionPackageSnapshots";

		// I'm making an assumption here that VS will not instantiate more than one ExceptionPackage per app domain
		internal static ToolWindowVM MasterViewModel { get; private set; }

		private readonly Model.ISerialize m_SnapshotSerializer;
		private readonly IExceptionManager m_ExceptionManager;

		/// <summary>
		/// Initializes a new instance of the <see cref="ExceptionPackage"/> class.
		/// </summary>
		public ExceptionPackage() {
			// Inside this method you can place any initialization code that does not require
			// any Visual Studio service because at this point the package object is created but
			// not sited yet inside Visual Studio environment. The place to do all the other
			// initialization is the Initialize method.
			this.AddOptionKey(SETTINGS_KEY);
			m_SnapshotSerializer = new JsonSerialize();

			m_ExceptionManager = new ExceptionManager();

			MasterViewModel = new ToolWindowVM(m_ExceptionManager);

		}

		#region Package Members

		/// <summary>
		/// Initialization of the package; this method is called right after the package is sited, so this is the place
		/// where you can put all the initialization code that rely on services provided by VisualStudio.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
		/// <param name="progress">A provider for progress updates.</param>
		/// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
		protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
			// When initialized asynchronously, the current thread may be a background thread at this point.
			// Do any initialization that requires the UI thread after switching to the UI thread.
			await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
			//await SnapshotWindowCommand.InitializeAsync(this);
		}

		protected override async void OnSaveOptions(string key, Stream stream) {
			if (key != SETTINGS_KEY) {
				base.OnSaveOptions(key, stream);
			} else {
				try {
					//TODO: see what will happen if exception is thrown here
					if (SettingsManager.STORE_WHERE == SettingsManager.STORE_SNAPSHOTS_WHERE.InJsonNextToSolution) {
						await this.JoinableTaskFactory.SwitchToMainThreadAsync(default);
						var fl = GetNextToSlnFile();
						if (fl != null) {
							using var fStream = File.Create(fl);
							m_SnapshotSerializer.Serialize(MasterViewModel.Snapshots, fStream);
						}


					} else
						m_SnapshotSerializer.Serialize(MasterViewModel.Snapshots, stream);

				} catch (Exception ex) {

				}
			}
		}

		protected override async void OnLoadOptions(string key, Stream stream) {
			if (key != SETTINGS_KEY) {
				base.OnLoadOptions(key, stream);
			} else {
				try {
					//TODO: add error handling
					IEnumerable<Snapshot> snaposhots;
					if (SettingsManager.STORE_WHERE == SettingsManager.STORE_SNAPSHOTS_WHERE.InJsonNextToSolution) {
						await this.JoinableTaskFactory.SwitchToMainThreadAsync(default);
						using var fStream = File.OpenRead(GetNextToSlnFile());
						snaposhots = m_SnapshotSerializer.Deserialize(snapshots: fStream);
					} else
						snaposhots = m_SnapshotSerializer.Deserialize(stream);
					MasterViewModel.Snapshots = snaposhots;
				} catch { }
			}
		}
		protected string GetNextToSlnFile() {
			var solutionPath = GetSolutionPath();
			if (string.IsNullOrEmpty(solutionPath))
				return null;
			var dir = Path.GetDirectoryName(solutionPath);
			var fileName = Path.GetFileNameWithoutExtension(solutionPath) + ".ExceptionSnapshots.json";
			return Path.Combine(dir, fileName);
		}

		private string GetSolutionPath() {
			ThreadHelper.ThrowIfNotOnUIThread();
			var solution = GetService(typeof(SVsSolution)) as IVsSolution;
			if (solution != null) {
				solution.GetSolutionInfo(out string solutionDirectory, out string solutionFile, out string userOptsFile);
				return solutionFile;
			}
			return null;
		}


		#endregion
	}
}
