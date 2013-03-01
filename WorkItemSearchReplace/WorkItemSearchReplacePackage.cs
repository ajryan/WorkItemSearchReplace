using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE80;
using MagenicTechnologies.WorkItemSearchReplace.Gui;
using Microsoft.TeamFoundation.Controls;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TeamFoundation.WorkItemTracking.Extensibility;

namespace MagenicTechnologies.WorkItemSearchReplace
{
    /// <summary>
    /// Main VSPackage entry point
    /// </summary>
    // Identify this class as a package to PkgDef utility
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // Register our metadata for the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // Register our menu resources
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // GUID for our package
    [Guid(GuidList.guidWorkItemSearchReplacePkgString)]
    public sealed class WorkItemSearchReplacePackage : Package
    {
        public WorkItemSearchReplacePackage()
        {
            Debug.WriteLine("WorkITemSearchReplacePackage ctor");
        }

        /// <summary>
        /// Initialization of the package: register the handler for our context menu command
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine("WorkItemSearchReplacePackage.Initialize");
            base.Initialize();

            var mcs = (OleMenuCommandService) GetService(typeof(IMenuCommandService));

            // TODO:    guid is same for a folder and a query
            // todo     should switch to dynamic activation
            // todo     and hide when item is a folder

            // Register our callback
            var searchReplaceWorkItemsCommandId = new CommandID(GuidList.guidWorkItemSearchReplaceCmdSet, (int)PkgCmdIdList.cmdidSearchAndReplaceInWorkItems);
            var menuItem = new MenuCommand(OnSearchReplaceWorkItems, searchReplaceWorkItemsCommandId);
            mcs.AddCommand(menuItem);
        }

        private void OnSearchReplaceWorkItems(object sender, EventArgs e)
        {
            Debug.WriteLine("WorkItemSearchReplace command invoked");

            var teamExplorer = (ITeamExplorer)(this.GetService(typeof(ITeamExplorer)));
            var wiQueriesExt = teamExplorer.CurrentPage.GetService<IWorkItemQueriesExt>();
            teamExplorer.NavigateToPage(new Guid(WorkItemSearchReplacePage.PageId), wiQueriesExt.SelectedQueryItems.FirstOrDefault());
        }
    }
}
