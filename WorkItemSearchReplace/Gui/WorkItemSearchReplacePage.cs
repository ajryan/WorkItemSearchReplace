using System;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace MagenicTechnologies.WorkItemSearchReplace.Gui
{
    [TeamExplorerPage(WorkItemSearchReplacePage.PageId)]
    public class WorkItemSearchReplacePage : TeamExplorerPageBase
    {
        private WorkItemSearchReplaceViewModel _viewModel;
        public const string PageId = "544753A9-8B19-4155-9A05-3EFECA5E66B3";

        public override void Initialize(object sender, PageInitializeEventArgs e)
        {
            base.Initialize(sender, e);

            Title = "Work Item Search and Replace";

            _viewModel = new WorkItemSearchReplaceViewModel(
                e.Context as QueryItem, 
                GetTeamFoundationContext());
            _viewModel.Initialize();
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            var view = new WorkItemSearchReplaceView {DataContext = _viewModel};
            PageContent = view;
        }

        protected override void DisposeViewModel()
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
 	         base.DisposeViewModel();
        }

        public override bool IsBusy
        {
            get { return _viewModel.IsBusy; }
        }

        private ITeamFoundationContext GetTeamFoundationContext()
        {
            var tfsContextManager = (ITeamFoundationContextManager)ServiceProvider.GetService(typeof(ITeamFoundationContextManager));
            return tfsContextManager.CurrentContext;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsBusy")
                RaisePropertyChanged("IsBusy");
        }
    }
}
