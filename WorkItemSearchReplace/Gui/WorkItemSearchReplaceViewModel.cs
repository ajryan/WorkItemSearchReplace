using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using MagenicTechnologies.WorkItemSearchReplace.Annotations;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace MagenicTechnologies.WorkItemSearchReplace.Gui
{
    public class WorkItemSearchReplaceViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly QueryItem _queryItem;
        private readonly ITeamFoundationContext _context;
        private readonly DelegateCommand _previewCommand;
        private readonly DelegateCommand _executeCommand;

        private bool _isBusy;
        private string _queryName;
        private int _queryWorkItemCount;
        private string _searchTerm;
        private string _replaceTerm;
        private bool _previewVisible;
        private string _statusText;

        private List<WorkItem> _workItemMatches;
        private Dictionary<int, List<string>> _workItemFieldMap;
        private HashSet<string> _fieldMatches;


        public WorkItemSearchReplaceViewModel(QueryItem queryItem, ITeamFoundationContext context)
        {
            _queryItem = queryItem;
            _context = context;

            _previewCommand = new DelegateCommand(Preview, CanPreview);
            _executeCommand = new DelegateCommand(Execute, CanExecute);

            PreviewWorkItems = new ObservableCollection<string>();
            PreviewFields = new ObservableCollection<string>();
        }

        public void Initialize()
        {
            StatusText = "Please enter a search term...";

            QueryName = _queryItem.Name;
            GetQueryCount();
        }

        private async void GetQueryCount()
        {
            IsBusy = true;
            await Task.Run(() =>
            {
                var store = _context.TeamProjectCollection.GetService<WorkItemStore>();
                var query = store.GetQueryDefinition(_queryItem.Id);

                QueryWorkItemCount = store.QueryCount(
                    query.QueryText,
                    new Dictionary<string, string> { { "project", _context.TeamProjectName } });
            });
            IsBusy = false;
        }

        public ICommand PreviewCommand
        {
            get { return _previewCommand; }
        }

        public ICommand ExecuteCommand
        {
            get { return _executeCommand; }
        }

        public async void Preview(object parameter)
        {
            IsBusy = true;
            PreviewVisible = false;
            PreviewWorkItems.Clear();
            PreviewFields.Clear();

            await Task.Run(() =>
            {
                var store = _context.TeamProjectCollection.GetService<WorkItemStore>();
                var query = store.GetQueryDefinition(_queryItem.Id);
                var workItems = store.Query(
                    query.QueryText,
                    new Dictionary<string, string> {{"project", _context.TeamProjectName}});

                _workItemMatches = new List<WorkItem>();
                _workItemFieldMap = new Dictionary<int, List<string>>();
                _fieldMatches = new HashSet<string>();

                // TODO:   perf is terrible on this
                // todo    should get the list of string fields from the project
                // todo    then run a single query with contains
                foreach (WorkItem workItem in workItems)
                {
                    bool matchedCurrent = false;
                    foreach (
                        Field field in workItem.Fields.Cast<Field>().Where(f => IsStringField(f.FieldDefinition)))
                    {
                        if (field.Value.ToString().Contains(SearchTerm))
                        {
                            if (!matchedCurrent)
                            {
                                _workItemMatches.Add(workItem);
                                matchedCurrent = true;
                                _workItemFieldMap.Add(workItem.Id, new List<string>());
                            }

                            _fieldMatches.Add(field.Name);
                            _workItemFieldMap[workItem.Id].Add(field.Name);
                        }
                    }
                }
            });

            foreach (var workItem in _workItemMatches)
            {
                PreviewWorkItems.Add(workItem.Id + " - " + workItem.Title);
            }
            foreach (var fieldName in _fieldMatches)
            {
                PreviewFields.Add(fieldName);
            }
            
            bool matchFound = _workItemMatches.Count > 0;

            StatusText = matchFound? "" : "No matches found.";
            PreviewVisible = matchFound;
            IsBusy = false;
        }

        private static bool IsStringField(FieldDefinition fieldDef)
        {
            return 
                fieldDef.FieldType == FieldType.Html ||
                fieldDef.FieldType == FieldType.PlainText ||
                fieldDef.FieldType == FieldType.String;
        }

        public bool CanPreview(object parameter)
        {
            return !IsBusy && !String.IsNullOrWhiteSpace(this.SearchTerm);
        }

        public async void Execute(object parameter)
        {
            IsBusy = true;
            await Task.Run(() =>
            {
                string replaceTerm = ReplaceTerm ?? "";

                foreach (var workItem in _workItemMatches)
                {
                    workItem.Open();

                    foreach (var fieldName in _workItemFieldMap[workItem.Id])
                    {
                        var field = workItem.Fields[fieldName];
                        var original = field.Value.ToString();
                        var replaced = original.Replace(SearchTerm, replaceTerm);

                        field.Value = replaced;
                    }
                }

                var store = _context.TeamProjectCollection.GetService<WorkItemStore>();
                store.BatchSave(_workItemMatches.ToArray());
            });
            StatusText = "Replace complete. You may perform a new search.";
            PreviewVisible = false;
            PreviewWorkItems.Clear();
            PreviewFields.Clear();
            IsBusy = false;
        }

        public bool CanExecute(object parameter)
        {
            return !IsBusy && !String.IsNullOrWhiteSpace(this.SearchTerm); // TODO: enable only when result of current term has been previewed;
        }

        public ObservableCollection<string> PreviewWorkItems { get; private set; }
        public ObservableCollection<string> PreviewFields { get; private set; }

        public string StatusText
        {
            get { return _statusText; }
            set
            {
                if (value == _statusText) return;
                _statusText = value;
                OnPropertyChanged();
            }
        }

        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if (value.Equals(_isBusy)) return;
                _isBusy = value;
                OnPropertyChanged();
                RaiseCanExecuteChanged();
            }
        }

        public bool PreviewVisible
        {
            get { return _previewVisible; }
            set
            {
                if (value.Equals(_previewVisible)) return;
                _previewVisible = value;
                OnPropertyChanged();
            }
        }

        public string QueryName
        {
            get { return _queryName; }
            set
            {
                if (value == _queryName) return;
                _queryName = value;
                OnPropertyChanged();
            }
        }

        public int QueryWorkItemCount
        {
            get { return _queryWorkItemCount; }
            set
            {
                if (value == _queryWorkItemCount) return;
                _queryWorkItemCount = value;
                OnPropertyChanged();
            }
        }

        public string SearchTerm
        {
            get { return _searchTerm; }
            set
            {
                if (value == _searchTerm) return;
                _searchTerm = value;

                StatusText = String.IsNullOrWhiteSpace(_searchTerm) 
                    ? "Please enter a search term..." 
                    : "Click Preview to see the results of the match.";

                PreviewVisible = false;
                OnPropertyChanged();
                RaiseCanExecuteChanged();
            }
        }

        public string ReplaceTerm
        {
            get { return _replaceTerm; }
            set
            {
                if (value == _replaceTerm) return;
                _replaceTerm = value;
                OnPropertyChanged();
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void RaiseCanExecuteChanged()
        {
            _executeCommand.RaiseCanExecuteChanged();
            _previewCommand.RaiseCanExecuteChanged();
        }
    }
}
