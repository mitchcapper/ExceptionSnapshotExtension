using ExceptionSnapshotExtension.Model;
using ExceptionSnapshotExtension.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ExceptionSnapshotExtension.Viewmodels
{
    internal class ToolWindowVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly IExceptionManager m_ExceptionManager;
        private RelayCommand m_EnableAllCommand;
        private RelayCommand m_DisableAllCommand;
        private RelayCommand m_SaveSnapshotCommand;
        private RelayCommand m_DeleteSnapshotCommand;
        private RelayCommand m_ActivateSnapshotCommand;

        private string m_NewSnapshotName;

        public ObservableCollection<SnapshotVM> SnapshotVms { get; private set; }

        public IEnumerable<Snapshot> Snapshots
        {
            get
            {
                return SnapshotVms.Select(vm => vm.Snapshot);
            }
            set
            {
                SnapshotVms.Clear();
                foreach (var snapshot in value ?? new Snapshot[] { })
                {
                    SnapshotVms.Add(new SnapshotVM(snapshot));
                }
            }
        }
        public string NewSnapshotName
        {
            get { return m_NewSnapshotName; }
            set
            {
                m_NewSnapshotName = value;
                if (!string.IsNullOrEmpty(m_NewSnapshotName))
                {
                    FailedValidation = false;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FailedValidation)));
                }
            }
        }

        /// <summary>
        /// One way to source binding
        /// </summary>
        public SnapshotVM SelectedSnapshot { get; set; }

        /// <summary>
        /// Two way binding
        /// </summary>
        public int SelectedSnapshotIndex { get; set; }

        public bool FailedValidation { get; set; }

        public RelayCommand EnableAllCommand
        {
            get
            {
                if (m_EnableAllCommand == null)
                {
                    m_EnableAllCommand = new RelayCommand(p => true, p =>
                    {
                        m_ExceptionManager.EnableAll();
                    });
                }

                return m_EnableAllCommand;
            }
        }

        public RelayCommand DisableAllCommand
        {
            get
            {
                if (m_DisableAllCommand == null)
                {
                    m_DisableAllCommand = new RelayCommand(p => true, p =>
                    {
                        m_ExceptionManager.DisableAll();
                    });
                }

                return m_DisableAllCommand;
            }
        }

        public RelayCommand SaveSnapshotCommand
        {
            get
            {
                if (m_SaveSnapshotCommand == null)
                {
                    m_SaveSnapshotCommand = new RelayCommand(p => true, p =>
                    {
                        if (!string.IsNullOrEmpty(NewSnapshotName))
                        {
                            var snapshot = m_ExceptionManager.GetCurrentExceptionSnapshot();
                            snapshot.Name = NewSnapshotName;
                            SnapshotVms.Add(new SnapshotVM(snapshot));
                            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SnapshotVms)));
                        }
                        else
                        {
                            FailedValidation = true;
                            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FailedValidation)));
                        }
                    });
                }

                return m_SaveSnapshotCommand;
            }
        }

        public RelayCommand DeleteSnapshotCommand
        {
            get
            {
                if (m_DeleteSnapshotCommand == null)
                {
                    m_DeleteSnapshotCommand = new RelayCommand(p => true, p =>
                    {
                        if (SelectedSnapshot != null)
                        {
                            Debug.Assert(SnapshotVms.Contains(SelectedSnapshot));
							var res = MessageBox.Show($"Are you sure you want to delete snapshot: {SelectedSnapshot.Name}","Confirm Delete",MessageBoxButton.YesNo);
							if (res != MessageBoxResult.Yes)
								return;
                            int selectedId = SelectedSnapshotIndex;
                            SnapshotVms.Remove(SelectedSnapshot);
                            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SnapshotVms)));

                            SelectedSnapshotIndex = SnapshotVms.Count > selectedId ? selectedId : SnapshotVms.Count - 1;
                            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedSnapshotIndex)));
                        }
                    });
                }

                return m_DeleteSnapshotCommand;
            }
        }

        public RelayCommand ActivateSnapshotCommand
        {
            get
            {
                if (m_ActivateSnapshotCommand == null)
                {
                    m_ActivateSnapshotCommand = new RelayCommand(p => true, p =>
                    {
                        if (p is SnapshotVM snapshotVM)
                        {
                            for (int i = 0; i < 10; i++) // I've had issues with snapshot not being restored after first attempt.
                            {
                                if (!m_ExceptionManager.VerifySnapshot(snapshotVM.Snapshot))
                                {
                                    if (i > 0)
                                    {
                                        System.Diagnostics.Trace.WriteLine($"Failed to Restore. Attempt {i + 1}");
                                    }

                                    m_ExceptionManager.RestoreSnapshot(snapshotVM.Snapshot);
                                }
                            }
                        }
                    });
                }

                return m_ActivateSnapshotCommand;
            }
        }

        public ToolWindowVM() // For designer
        {
            SnapshotVms = new ObservableCollection<SnapshotVM>
            (
                new SnapshotVM[]
                {
                    new SnapshotVM(new Snapshot
                    {
                        Name = "Test Shot"
                    })
                }
            );
        }

        public ToolWindowVM(IExceptionManager exceptionManager)
        {
            m_ExceptionManager = exceptionManager;
            SnapshotVms = new ObservableCollection<SnapshotVM>();
        }
    }
}
