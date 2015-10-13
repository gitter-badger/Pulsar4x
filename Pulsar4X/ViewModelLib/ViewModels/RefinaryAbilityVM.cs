using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ViewModel
{
    public class RefinaryAbilityVM : IViewModel 
    {
        private Entity _colonyEntity;
        private ColonyRefiningDB RefiningDB { get { return _colonyEntity.GetDataBlob<ColonyRefiningDB>(); } }
        private StaticDataStore _staticData;

        public int PointsPerDay { get { return RefiningDB.RefinaryPoints; } }

        private ObservableCollection<RefinaryJobVM> _itemJobs;
        public ObservableCollection<RefinaryJobVM> ItemJobs
        {
            get { return _itemJobs;}
            set{_itemJobs = value; OnPropertyChanged();}
        }

        public Dictionary<string, Guid> ItemDictionary { get; set; }
        public Guid NewJobSelectedItem { get; set; }
        public ushort NewJobBatchCount { get; set; }
        public bool NewJobRepeat { get; set; }


        #region Constructor
        public RefinaryAbilityVM(StaticDataStore staticData, Entity colonyEntity)
        {
            _staticData = staticData;
            _colonyEntity = colonyEntity;
            SetupRefiningJobs();
            
            ItemDictionary = new Dictionary<string, Guid>();
            foreach (var kvp in _staticData.RefinedMaterials)
            {
                ItemDictionary.Add(kvp.Value.Name, kvp.Key);
            }
            NewJobBatchCount = 1;
            NewJobRepeat = false;
        }
        #endregion


        private ICommand _addNewJob;
        public ICommand AddNewJob
        {
            get
            {
                return _addNewJob ?? (_addNewJob = new CommandHandler(OnNewBatchJob, true));
            }
        }


        public void OnNewBatchJob()
        {
            RefineingJob newjob = new RefineingJob();
            newjob.MaterialGuid = NewJobSelectedItem;
            newjob.NumberCompleted = 0;
            newjob.NumberOrdered = NewJobBatchCount;
            newjob.PointsLeft = _staticData.RefinedMaterials[NewJobSelectedItem].RefinaryPointCost;
            newjob.Auto = NewJobRepeat;
            RefiningProcessor.AddJob(_staticData, _colonyEntity, newjob);
            Refresh();
        }

        #region Refresh

        private void SetupRefiningJobs()
        {
            var jobs = RefiningDB.JobBatchList;
            _itemJobs = new ObservableCollection<RefinaryJobVM>();
            foreach (var item in jobs)
            {
                _itemJobs.Add(new RefinaryJobVM(_staticData, _colonyEntity, item, this));
            }
            ItemJobs = ItemJobs;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void Refresh(bool partialRefresh = false)
        {
            SetupRefiningJobs();
            //if (_refiningDB.JobBatchList.Count != RefinaryJobs.Count)
            //    SetupRefiningJobs();
            //else
            //    foreach (var job in RefinaryJobs)
            //    {
            //        job.Refresh();
            //    }
        }

        #endregion

    }


    public class RefinaryJobVM : IViewModel
    {
        private StaticDataStore _staticData;
        private RefineingJob _job;
        private Entity _colonyEntity;
        private RefinaryAbilityVM _parentRefiningVM;

        public string Material { get { return _staticData.RefinedMaterials[_job.MaterialGuid].Name; } }
        public ushort Completed { get { return _job.NumberCompleted; } }
        public ushort BatchQuantity { get { return _job.NumberOrdered; } set { _job.NumberOrdered = value; } }
        public bool Repeat { get { return _job.Auto; } set { _job.Auto = value; } }
        private int PriorityIndex { get { return _parentRefiningVM.ItemJobs.IndexOf(this); } }

        public RefinaryJobVM(StaticDataStore staticData, Entity colonyEntity, RefineingJob refiningJob, RefinaryAbilityVM parentRefiningVM)
        {
            _staticData = staticData;
            _colonyEntity = colonyEntity;
            _job = refiningJob;
            _parentRefiningVM = parentRefiningVM;
        }

        public void IncreasePriority()
        {
            if (PriorityIndex > 0)
            {
                RefiningProcessor.MoveJob(_colonyEntity, _job, -1);
            }
        }
        public void DecresePriorty()
        {
            if (PriorityIndex < _parentRefiningVM.ItemJobs.Count - 2)
            {
                RefiningProcessor.MoveJob(_colonyEntity, _job, 1);
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public void Refresh(bool partialRefresh = false)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("Completed"));
                PropertyChanged(this, new PropertyChangedEventArgs("BatchQuantity"));
                PropertyChanged(this, new PropertyChangedEventArgs("Repeat"));
            }
        }
    }

    public class CommandHandler : ICommand
    {
        private Action _action;
        private bool _canExecute;
        public CommandHandler(Action action, bool canExecute)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            _action();
        }
    }

}