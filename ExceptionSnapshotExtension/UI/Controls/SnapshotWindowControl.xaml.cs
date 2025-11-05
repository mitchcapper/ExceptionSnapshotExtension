using ExceptionSnapshotExtension.Viewmodels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace ExceptionSnapshotExtension {
	/// <summary>
	/// Interaction logic for SnapshotWindowControl.
	/// </summary>
	public partial class SnapshotWindowControl : UserControl {
		/// <summary>
		/// Initializes a new instance of the <see cref="SnapshotWindowControl"/> class.
		/// </summary>
		public SnapshotWindowControl() {
			this.InitializeComponent();
			Dispatcher.VerifyAccess();
			this.DataContext = ExceptionPackage.MasterViewModel;
			Loaded += (_, _) => vm.WindowLoaded();
			Unloaded += (_, _) => vm.WindowUnloaded();
		}
		internal ToolWindowVM vm => DataContext as ToolWindowVM;

		private void ListView_Loaded(object sender, RoutedEventArgs e) {
			UpdateColumnsWidth(sender as ListView);
		}

		private void ListView_SizeChanged(object sender, SizeChangedEventArgs e) {
			UpdateColumnsWidth(sender as ListView);
		}

		private void UpdateColumnsWidth(ListView listView) {
			int autoFillColumnIndex = (listView.View as GridView).Columns.Count - 1;
			if (listView.ActualWidth == Double.NaN) {
				listView.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
			}

			double remainingSpace = listView.ActualWidth;
			for (int i = 0; i < (listView.View as GridView).Columns.Count; i++) {
				if (i != autoFillColumnIndex) {
					remainingSpace -= (listView.View as GridView).Columns[i].ActualWidth;
				}
			}

			(listView.View as GridView).Columns[autoFillColumnIndex].Width = remainingSpace >= 0 ? remainingSpace - 10 : 0;
		}
	}
}
