<!--

Title: Developing a Visual Studio 2012 Team Explorer Extension for Search and Replace in Work Items

-->

# Developing a Visual Studio 2012 Team Explorer Extension for Search and Replace in Work Items

A post from Buck Hodges of the Visual Studio ALM (Application Lifecycle Management) team crossed my news reader recently: "[Extending Team Explorer in Visual Studio 2012](http://blogs.msdn.com/b/buckh/archive/2012/12/21/extending-team-explorer-in-visual-studio-2012.aspx)." It points to a code sample that demonstrates how to add a navigation link and a new page to Team Explorer in Visual Studio 2012. I've had an idea for a Team Foundation Server (TFS) utility brewing for a while. Usually I publish my TFS utilities as command-line or standalone desktop applications; this sample has inspired me to create a utility with tighter Visual Studio integration.  Armed with the sample and the Visual Studio 2012 Software Development Kit (available from the [Microsoft Download Center](http://www.microsoft.com/en-us/download/details.aspx?id=30668)), I started hacking. You can find the finished extension on the [Visual Studio Gallery](http://visualstudiogallery.msdn.microsoft.com/ef02938d-7fb7-44ef-9040-798668773306), and all of the code associated with this article is available in my Github repository here: [http://github.com/ajryan/WorkItemSearchReplace](http://github.com/ajryan/WorkItemSearchReplace).

## The Idea

The utility I set out to build has a simple goal: the ability to search and replace text in TFS work items. In Visual Studio 2010 and below, work item search was available only through a 3rd-party add-in. In Visual Studio 2012, a fairly capable search tool has been added to Team Explorer. There is not, however, a way to replace the found text in multiple work items.

As a project evolves, its standard terminology can change. What we are calling the "Multimuxer" feature today may be renamed the "Supermuxer" tomorrow. Finding all of the work items that use the old term is relatively simple, but swapping in the new term is a tedious task. It can be accomplished with the TFS Excel integration, but a solution that is completely native to Visual Studio would be very useful.

## Getting Started

I pulled down the sample solution provided in the article linked above and took a look. It is a C# class library project with a ProjectType GUID `{82b43b9b-a64c-4715-b499-d71e9ca2bd60}`, indicating "Extensibility Project." This is is a project type supported by the Visual Studio SDK: it provides the ability to develop and package Visual Studio Extensions in the form of `VSIX` files. The sample project contains several class files, some WPF User Controls, and a `vsixmanifest` file that ties it all together.

Starting in Visual Studio 2010, support was added for extending the development environment using the Managed Extensibility Framework ([MEF](http://msdn.microsoft.com/en-us/library/dd460648.aspx)). MEF is a framework for adding extensibility to applications in the form of automatically-discovered plugins. It is a part of the .NET framework that you can use it to add extensibility to your own applications. Visual Studio uses MEF to light up the features available depending on the edition and add-ons installed. In order to extend Visual Studio, developers code classes that implement the interface(s) required for hooking into the IDE. The classes are decorated with attributes that indicate to MEF that they represent discoverable plugin implementations. Finally, the developer authors a `vsixmanifest` file that tells Visual Studio how to install the MEF components. A handy IDE is provided by the SDK for creating a manifest file.

To get my own project started, I fired up a new instance of Visual Studio 2012, and chose `File > New Project` from the menu. I navigated under `Other Project Types` to the `Extensibility` folder that was added by the Visual Studio SDK, and selected `Visual Studio Package`. This is the general project type for extending Visual Studio, and includes the ability to author MEF components.

This started a wizard where I selected C# for the project language, chose the option to generate a new strong-name signing key, and entered identifying information about my extension. I created a simple icon, pulled together from the standard icons for Find and Work Item available in the Visual Studio 2012 Image Library (available from [Microsoft Download Center](http://www.microsoft.com/en-us/download/details.aspx?id=35825)). In the list default components, I selected Menu Item, because my plugin will expose its function through a context menu item on a Work Item Query. I gave the command a friendly name and ID. Upon completion of the wizard, I was presented with the `vsixmanifest` editor.

The last thing I needed to do was add an Asset indicating that my package was going to provide MEF components. On the Assets tab of the manifest editor, I clicked `New`, chose `Microsoft.VisualStudio.MefComponent` for the Type, and indicated my `WorkItemSearchReplace` project as the source.

## User Interface

With the empty project skeleton in place, it was time to implement some functionality. I planned to hook into the user interface through Work Item Queries. Team Explorer provides an interface for navigating the tree of Work Item Queries, and I would add a context menu command on the Work Item Query items shown in the tree, exposing my Find and Replace function. That way, the user can leverage the existing, familiar UI for identifying a set of work items and then invoke my tool to find and replace text within the work items returned by the selected query.

The wizard added some boilerplate code to my project for defining and hooking up the context menu item. The following files were added:

* `PkgCmdID.cs`: C# static class named `PkgCmdIDList` with one const member named `cmdidSearchAndReplaceInWorkItems`, setting the ID of my command.
* `Guids.cs`: C# static class named `GuidList` with const and static members defining the GUIDs for my package and command set.
* `WorkItemSearchReplace.vsct`: XML Command Table file providing the metadata about my command. The generated code places the command within the main Tools menu.
* `WorkItemSearchReplacePackage.cs`: C# class inheriting `Microsoft.VisualStudio.Shell.Package`, providing the hooks for bootstrapping the extension. Registers the command with the `OleMenuCommandService`.

### Registering the Context Menu Command

I needed to change the registration of my command from the main Tools menu to the Work Item Query context menu. To do so, I needed the GUID and ID of the context menu displayed when a Work Item query is right-clicked. To find it, I used the `EnableVSIPLogging` registry entry, described in this [blog post](http://blogs.msdn.com/b/dr._ex/archive/2007/04/17/using-enablevsiplogging-to-identify-menus-and-commands-with-vs-2005-sp1.aspx). After adding the registry value (under the `11.0` node, since I am targeting VS2012), I launched Visual Studio and Ctrl + Shift + right-clicked a Work Item query. I noted the GUID and Command ID displayed, and added them to the `GuidSymbol` section of the `WorkItemSearchReplace.vsct` file. Then I changed the `Parent` entry of my `WorkItemSearchReplaceMenuGroup` to refer to the new GUID and ID.

Here are the relevant changed sections of the `WorkItemSearchReplace.vsct` file:

```
<Group guid="guidWorkItemSearchReplaceCmdSet" id="WorkItemSearchReplaceMenuGroup" priority="0x200">
  <Parent guid="WorkItemTrackingGuid" id="TEQuery"/>
</Group>

<GuidSymbol name="WorkItemTrackingGuid" value="{2dc8d6bb-916c-4b80-9c52-fd8fc371acc2}">
  <IDSymbol name="TEQuery" value="0x300" />
</GuidSymbol>
```

This results in my new "Search and Replace" appearing the the context menu when a Work Item Query is right-clicked in Team Explorer.

### Displaying a New Team Explorer Page

When my command is invoked, a user interface needs to be displayed allowing the user to enter the search and replacement terms. I added a new class named `WorkItemSearchReplacePage`, inheriting `TeamExplorerPageBase`, and decorated with the `TeamExplorerPage` attribute. This is where I took my first TFS dependency, so I went ahead and added references to the `Microsoft.TeamFoundation.*` assemblies that I would be using. The attribute identifies the page to MEF so that it is automatically registered and available for navigation at runtime. The `TeamExplorerPageBase` class provides default implementations for the interface contract, and at this stage I am simply focused on displaying my UI, so I only need to set the Title and PageContent for the page in its constructor.

```
[TeamExplorerPage(WorkItemSearchReplacePage.PageId)]
public class WorkItemSearchReplacePage : TeamExplorerPageBase
{
    public const string PageId = "544753A9-8B19-4155-9A05-3EFECA5E66B3";

    public WorkItemSearchReplacePage()
    {
        Title = "Work Item Search and Replace";
        PageContent = new WorkItemSearchReplaceView();
    }

    public override void Initialize(object sender, PageInitializeEventArgs e)
    {
        base.Initialize(sender, e);
    }
}
```

The `ITeamExplorerPage` interface defines a `PageContent` member, where the user interface for the page is exposed. I marked up a simple WPF user control with inputs for the search and replacement text, and set the page's PageContent property to an instance of the user control.

In order to display my new page when my command is invoked, I implemented code in my command handler (hooked up in the default boilerplate code) to navigate Team Explorer to my new page:

```
private void OnSearchReplaceWorkItems(object sender, EventArgs e)
{
    var teamExplorer = (ITeamExplorer)(this.GetService(typeof(ITeamExplorer)));
    teamExplorer.NavigateToPage(new Guid(WorkItemSearchReplacePage.PageId), null);
}
```

When my Search and Replace command is invoked, the new (not-yet-functional) page is displayed in the Team Explorer pane. Now that the basic user interface is hooked up, it's time to fill in the functionality. When the Preview button is clicked, we'll scan through the fields of work items returned by the query and display a list of the work items that will be affected by the change. The user will verify that the list looks correct, and then click the Execute button to apply the change.

## Work Item Manipulation

### Displaying Query Info

With the UI skeleton in place, we can now talk to TFS and do some Work Item manipulation. The first task is to execute the selected Work Item Query and provide the user with some context: once the Search and Replace page is displayed, we need to show the name of the selected query and the number of total work items it returns.

In my command handler, I added code to get the `IWorkItemQueriesExt` service provided by the Work Item Queries section of the Work Items page. This provides a `SelectedQueryItems` property which exposes the currently-selected work item queries. (Note that in the final implementation, code should be added to selectively disable the Search and Replace menu item when more than one query is selected.)

```
private void OnSearchReplaceWorkItems(object sender, EventArgs e)
{
	var teamExplorer = (ITeamExplorer)(this.GetService(typeof(ITeamExplorer)));
	var wiQueriesExt = teamExplorer.CurrentPage.GetService<IWorkItemQueriesExt>();
	teamExplorer.NavigateToPage(
	    new Guid(WorkItemSearchReplacePage.PageId),
	    wiQueriesExt.SelectedQueryItems.FirstOrDefault());
}
```

The `NavigateToPage` method accepts an `object` for its second parameter, which is passed to the `Initialize` method of the page target of the navigation. I took this opportunity to set up a ViewModel for my page. The ViewModel will handle all of the data binding and commanding for the extension. In the `WorkItemSearchReplacePage`, I created an instance of my new `WorkItemSearchReplaceViewModel` class and relayed the selected `QueryItem`, as well as the current `ITeamFoundationContext` to its constructor. Then I set the `DataContext` of my `WorkItemSearchReplaceView` user control to the ViewModel.

I added some controls to the user control for displaying the selected query, and called the TFS API to get the number of work items returned by the query. The Search and Replace term TextBoxes and the Preview and Execute buttons were all bound to the ViewModel.

The relevant code that hooks up the commands and gets the query count information via the TFS API:

```
public WorkItemSearchReplaceViewModel(QueryItem queryItem, ITeamFoundationContext context)
{
    _queryItem = queryItem;
    _context = context;

    _previewCommand = new DelegateCommand(Preview, CanPreview);
    _executeCommand = new DelegateCommand(Execute, CanExecute);

    QueryName = _queryItem.Name;

    var store = _context.TeamProjectCollection.GetService<WorkItemStore>();
    var query = store.GetQueryDefinition(_queryItem.Id);
    QueryWorkItemCount = store.QueryCount(
        query.QueryText, 
        new Dictionary<string,string>{{"project",_context.TeamProjectName}});
}
```

### Previewing the Work Item Changes

At this point I needed to implement the behavior of the Preview command. It will scan through the work items returned by the selected query and provide a list of work items and where the search term was found, and list the fields that contained the search term. The Preview command is bound to its button on the View using the handy [`DelegateCommand` class](http://wpftutorial.net/DelegateCommand.html).

The code for the Preview command follows:

```
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
```

Several things to note about the `Preview` method:

* It is an `async` method, because it does some long-running work and should not block the user interface thread. The initial `IsBusy` state and preview fields are set, then a `Task` is run asynchronously to do the work of connecting to TFS and searching the work items in the selected query. Once that is complete, back on the UI thread, the results of the search can be displayed.

* The search method used is very inefficient when the query returns many work items. All of the fields of every work item returned by the query are scanned, which results in several round trips to the server. In a final implementation, it would be more efficient to first interrogate the Team Project for the text-backed work item fields and then dynamically add `CONTAINS` clauses to the text of the selected query.

* Most work item queries include an embedded `@project` macro which scopes the results to the current project. The second parameter of the `store.Query` call supplies the current project from the `ITeamFoundationContext` instance supplied to the ViewModel from the Page.

* The Page subscribes to `PropertyChanged` from the ViewModel and sets its own `IsBusy` property to match the ViewModel's. This hooks into Team Explorer's extensibility model and causes an indefinite progress bar to be displayed at the top of the pane while work is being done in the background.

I added a couple of list boxes to the `WorkItemSearchReplaceView` user control and bound them to the `PreviewWorkItems` and `PreviewFields` collections.

### Executing the Work Item Changes

Once the user reviews previewed work items and fields, he or she can invoke the Execute command to apply the replacement. I have cached the set of work items and fields matched by the search term, so now it is a matter of iterating them, making the replacement, and saving the changes. The code for the Execute command follows:

```
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
```

Several things to note about the `Execute` method:

* Like `Preview`, it is an `async` method, to avoid blocking the UI while doing work. It sets the `IsBusy` indicator, performs the replacement on a background thread, and then displays the result.

* The `workItem.Open()` method must be called before applying changes to a Work Item's fields.

* The `store.BatchSave(_workItemMatches.ToArray())` method provides a way to minimize server round trips when saving changes to several work items. Calling save on each individual work item would take much longer.

Once the replacement is applied, a status message is displayed and the Preview lists are cleared, making the page ready to perform a new search. The functionality of my extension was complete.

## Packaging and Deployment

With the extension complete and working in my development environment, I needed to package it up and make it available to the public. The Visual Studio Package template automatically outputs a `.VSIX` when built. I wanted to make my extension available to the public, so I went to the Visual Studio Gallery site to find out how.

On the [main gallery page](http://visualstudiogallery.msdn.microsoft.com), there is a link to Upload, which takes you through the process of uploading your extension. Before uploading my package, I opened up the manifest editor and supplied a license, icon, and preview image. I then re-built my extension with all of the metadata included.

After entering some basic information, I uploaded my `vsix` file, and entered a description of the tool. A preview of the listing page was displayed. After making some tweaks to the description, I clicked Publish, and my extension was available to the public!

## Future Plans

This extension has several areas with room for improvement. The two biggest are the search implementation and preview interaction. For search, I would like to implement the dynamic query idea I described above, to avoid scanning every work item field. In the Preview, it would be useful to allow the user to expand each work item and examine which field(s) were matched, as well as target the replacement to a subset of fields by selecting them from the field preview list. Another small drawback is that the Search and Replace conext menu command is available when multiple queries are selected, and when a query folder is selected. There is a dynamic command mechanism that I should leverage to enable the command only when a single work item query is selected. If the extension catches on, I will deliver these improvements in a future version.

The extension is available on the Visual Studio Gallery here: [Work Item Search and Replace](http://visualstudiogallery.msdn.microsoft.com/ef02938d-7fb7-44ef-9040-798668773306), and its code is in my GitHub repository: [github.com/ajryan/WorkItemSearchReplace](http://github.com/ajryan/WorkItemSearchReplace).