﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Plugins.Interfaces;
using System.Windows.Forms;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Instructions.Reflection;
using System.ComponentModel;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.CodeGeneration.Game1;
using FlatRedBall.IO;
using System.Windows;
using System.Collections.ObjectModel;
using GlueFormsCore.Controls;
using GeneralResponse = ToolsUtilities.GeneralResponse;

namespace FlatRedBall.Glue.Plugins
{
    #region Tab Location Enum

    public enum TabLocation
    {
        Top,
        Left,
        Center,
        Right,
        Bottom
    }

    #endregion

    #region PluginTab
    public class PluginTab
    {
        public string Title
        {
            get => Page.Title;
            set => Page.Title = value;
        }

        public TabLocation SuggestedLocation
        {
            get; set;
        } = TabLocation.Center;

        PluginTabPage page;
        internal PluginTabPage Page
        {
            get => page;
            set
            {
                if (page != value)
                {
                    page = value;
                    page.TabSelected = RaiseTabShown;
                }
            }
        }

        public void RaiseTabShown() => TabShown?.Invoke();

        public event Action TabShown;

        public void Hide()
        {
            var items = Page.ParentTabControl as ObservableCollection<PluginTabPage>;
            items?.Remove(Page);
            Page.ParentTabControl = null;

        }

        public void Show()
        {
            if(Page.ParentTabControl == null)
            {
                var items = PluginBase.GetTabContainerFromLocation(SuggestedLocation);
                items.Add(Page);
                Page.ParentTabControl = items;
            }
        }

        public void Focus()
        {
            Page.Focus();
            Page.LastTimeClicked = DateTime.Now;
        }

        public bool CanClose
        {
            get => Page.DrawX;
            set => Page.DrawX = value;
        }

        public void ForceLocation(TabLocation tabLocation)
        {
            var desiredTabControl = PluginBase.GetTabContainerFromLocation(SuggestedLocation);
            var parentTabControl = Page.ParentTabControl as ObservableCollection<PluginTabPage>;

            if(desiredTabControl != parentTabControl)
            {
                if(parentTabControl != null)
                {
                    desiredTabControl.Remove(Page);
                }

                parentTabControl.Add(Page);
                Page.ParentTabControl = desiredTabControl;
            }
        }
    }
    #endregion

    public abstract class PluginBase : IPlugin
    {
        Dictionary<ToolStripMenuItem, ToolStripMenuItem> toolStripItemsAndParents = new Dictionary<ToolStripMenuItem, ToolStripMenuItem>();


        #region Properties

        public string PluginFolder
        {
            get;
            set;
        }

        public abstract string FriendlyName { get; }
        public abstract Version Version { get; }


        protected PluginTabPage PluginTab { get; private set; } // This is the tab that will hold our control


        #endregion

        #region Delegates

        public AddNewFileOptionsDelegate AddNewFileOptionsHandler { get; protected set; }
        public CreateNewFileDelegate CreateNewFileHandler { get; protected set; }

        public InitializeMenuDelegate InitializeMenuHandler { get; protected set; }

        /// <summary>
        /// Action raised when a new Glue screen is created.
        /// </summary>
        public Action<ScreenSave> NewScreenCreated { get; protected set; }
        public Action<EntitySave> NewEntityCreated { get; protected set; }

        public Action<ScreenSave, AddScreenWindow> NewScreenCreatedWithUi { get; protected set; }
        public Action<EntitySave, AddEntityWindow> NewEntityCreatedWithUi { get; protected set; }

        public OnErrorOutputDelegate OnErrorOutputHandler { get; protected set; }
        public OnOutputDelegate OnOutputHandler { get; protected set; }

        /// <summary>
        /// Raised when the user clicks the menu item to open a project.  This allows plugins to handle opening projects in other
        /// IDEs (like Eclipse).
        /// </summary>
        public OpenProjectDelegate OpenProjectHandler { get; protected set; }
        public OpenSolutionDelegate OpenSolutionHandler { get; protected set; }

        /// <summary>
        /// Delegate raised whenever a property on either a variable or an element has changed. Implementations
        /// should check the current object to handle this properly.
        /// </summary>
        public ReactToChangedPropertyDelegate ReactToChangedPropertyHandler { get; protected set; }
        public ReactToFileChangeDelegate ReactToFileChangeHandler { get; protected set; }
        public ReactToFileChangeDelegate ReactToBuiltFileChangeHandler { get; protected set; }
        public Action ReactToChangedStartupScreen { get; protected set; }
        public Action<FilePath> ReactToCodeFileChange { get; protected set; }
        public ReactToItemSelectDelegate ReactToItemSelectHandler { get; protected set; }
        public ReactToNamedObjectChangedValueDelegate ReactToNamedObjectChangedValue { get; protected set; }

        /// <summary>
        /// Delegate called when the user creates a new ReferencedFileSave (adds a new file to the Glue project)
        /// </summary>
        public ReactToNewFileDelegate ReactToNewFileHandler { get; protected set; }
        public ReactToNewObjectDelegate ReactToNewObjectHandler { get; protected set; }
        public Action<IElement, NamedObjectSave> ReactToObjectRemoved { get; protected set; }

        /// <summary>
        /// Delegate raised when right-clicking on the property grid.
        /// </summary>
        public ReactToRightClickDelegate ReactToRightClickHandler { get; protected set; }


        public ReactToTreeViewRightClickDelegate ReactToTreeViewRightClickHandler { get; protected set; }
        public ReactToStateNameChangeDelegate ReactToStateNameChangeHandler { get; protected set; }
        public ReactToStateRemovedDelegate ReactToStateRemovedHandler { get; protected set; }

        public Action<IElement, ReferencedFileSave> ReactToFileRemoved { get; protected set; }

        /// <summary>
        /// Delegate raised whenever an entity is going to be removed. The first argument
        /// (EntitySave) is the entity to remove. The string list argument is 
        /// a list of to-be-removed files. Entities can add addiitonal files.
        /// </summary>
        public Action<EntitySave, List<string>> ReactToEntityRemoved { get; protected set; }

        /// <summary>
        /// Delegate raised whenever a Screen is removed. The first argument is the screen
        /// which is being removed. The second argument is a list of files to remove. Plugins
        /// can optionally add additional files to-be-removed when a Screen is removed.
        /// </summary>
        public Action<ScreenSave, List<string>> ReactToScreenRemoved { get; protected set; }

        public Action<IElement, EventResponseSave> ReactToEventRemoved { get; protected set; }

        /// <summary>
        /// Action raised when a variable changes. The IElement is the container of the variable, the CustomVariable is the changed variable.
        /// </summary>
        public Action<IElement, CustomVariable> ReactToElementVariableChange { get; protected set; }

        /// <summary>
        /// Raised whenever an element (screen or entity) is renamed. First parameter is the
        /// renamed element, the second is the old name. The element will already have its new
        /// name assigned.
        /// </summary>
        public Action<IElement, string> ReactToElementRenamed { get; protected set; }

        public Action<string> SelectItemInCurrentFile { get; protected set; }

        public Action ReactToLoadedGluxEarly { get; protected set; }

        /// <summary>
        /// Delegate raised after a project is loaded, but before any code has been generated.
        /// </summary>
        public Action ReactToLoadedGlux { get; protected set; }

        public Action<AddEntityWindow> ModifyAddEntityWindow { get; protected set; }
        public Action<AddScreenWindow> ModifyAddScreenWindow { get; protected set; }

        /// <summary>
        /// Raised whenever a project is unloaded. Glue will still report the project as loaded, so that plugins can
        /// react to a specific project unloading (such as by saving content).
        /// </summary>
        public Action ReactToUnloadedGlux { get; protected set; }
        public TryHandleCopyFileDelegate TryHandleCopyFile { get; protected set; }

        /// <summary>
        /// Raised when an object references a file and needs to know the contained objects.  
        /// Returned values contain the name of the object followed by the type of the object in 
        /// parenthesis.  Example of returned file:  "UntexturedSprite (Sprite)"
        /// </summary>
        public TryAddContainedObjectsDelegate TryAddContainedObjects { get; protected set; }

        public AdjustDisplayedScreenDelegate AdjustDisplayedScreen { get; protected set; }

        public AdjustDisplayedEntityDelegate AdjustDisplayedEntity { get; protected set; }

        [Obsolete("Use FillWithReferencedFiles instead", error:true)]
        public Action<string, EditorObjects.Parsing.TopLevelOrRecursive, List<string>> GetFilesReferencedBy { get; protected set; }

        public Func<FilePath, List<FilePath>, GeneralResponse> FillWithReferencedFiles { get; protected set; }
        public Action<FilePath, GeneralResponse> ReactToFileReadError { get; protected set; }

        public Action<string, List<FilePath>> GetFilesNeededOnDiskBy { get; protected set; }

        public Action ResolutionChanged { get; protected set; }

        /// <summary>
        /// Responsible for returning whether the argument file can return content.  The file shouldn't be opened
        /// here, only the extension should be investigated to see if the file can potentially reference content.
        /// </summary>
        /// <remarks>
        /// The plugin should not open the file here (if possible) as this event will be raised a lot, and it should
        /// be very fast and not hit the disk.
        /// </remarks>
        public Func<string, bool> CanFileReferenceContent { get; protected set; }

        public AdjustDisplayedReferencedFileDelegate AdjustDisplayedReferencedFile { get; protected set; }
        public AdjustDisplayedCustomVariableDelegate AdjustDisplayedCustomVariable { get; protected set; }

        /// <summary>
        /// Adjusts the properties for the selected NamedObject (not Variables window)
        /// </summary>
        public AdjustDisplayedNamedObjectDelegate AdjustDisplayedNamedObject { get; protected set; }

        public Func<IElement, IEnumerable<VariableDefinition>> GetVariableDefinitionsForElement;

        public Action<NamedObjectSave, List<ExposableEvent>> AddEventsForObject { get; protected set; }
        public Action<ProjectBase> ReactToLoadedSyncedProject { get; protected set; }

        public GetEventTypeAndSignature GetEventSignatureArgs { get; protected set; }

        public Func<IElement, NamedObjectSave, TypedMemberBase, TypeConverter> GetTypeConverter { get; protected set; }
        public Action<NamedObjectSave, ICodeBlock, InstructionSave> WriteInstanceVariableAssignment { get; protected set; }

        /// <summary>
        /// Action to raise whenever a ReferencedFileSave value changes. 
        /// string - The name of the variable that changed
        /// object - The old value for the variable
        /// </summary>
        public Action<string, object> ReactToReferencedFileChangedValueHandler { get; protected set; }
        public Action<CustomVariable> ReactToVariableAdded { get; protected set; }
        public Action<CustomVariable> ReactToVariableRemoved { get; protected set; }


        public Func<string, bool> GetIfUsesContentPipeline { get; protected set; }

        /// <summary>
        /// Delegate used to return additional types used by the plugin. Currently this is only used to populate dropdowns, so plugins only need to return enumerations, but eventually
        /// this could be used for other functionality.
        /// </summary>
        public Func<List<Type>> GetUsedTypes { get; protected set; }

        public Func<ReferencedFileSave, List<AssetTypeInfo>> GetAvailableAssetTypes { get; protected set; }

        public Action<NamedObjectSave, NamedObjectSave> ReactToCreateCollisionRelationshipsBetween { get; protected set; }

        public Func<TreeNode, bool> TryHandleTreeNodeDoubleClicked { get; protected set; }

        public Action<ReferencedFileSave> ReactToFileBuildCommand { get; protected set; }

        public Action<GlueElement> ReactToImportedElement { get; protected set; }

        #endregion

        public abstract void StartUp();

        public abstract bool ShutDown(PluginShutDownReason shutDownReason);

        #region Menu items

        protected ToolStripMenuItem AddTopLevelMenuItem(string whatToAdd)
        {
            ToolStripMenuItem menuItem = new ToolStripMenuItem(whatToAdd);
            GlueGui.MenuStrip.Items.Add(menuItem);
            return menuItem;
        }

        protected ToolStripMenuItem AddMenuItemTo(string whatToAdd, EventHandler eventHandler, string container)
        {
            return AddMenuItemTo(whatToAdd, eventHandler, container, -1);
        }

        protected ToolStripMenuItem AddMenuItemTo(string whatToAdd, Action action, string container)
        {
            return AddMenuItemTo(whatToAdd, (not, used) => action?.Invoke(), container, -1);
        }

        protected ToolStripMenuItem AddMenuItemTo(string whatToAdd, EventHandler eventHandler, string container, int preferredIndex)
        {
            ToolStripMenuItem menuItem = new ToolStripMenuItem(whatToAdd, null, eventHandler);
            ToolStripMenuItem itemToAddTo = GetItem(container);
            toolStripItemsAndParents.Add(menuItem, itemToAddTo);


            if (preferredIndex == -1)
            {
                itemToAddTo.DropDownItems.Add(menuItem);
            }
            else
            {
                int indexToInsertAt = System.Math.Min(preferredIndex, itemToAddTo.DropDownItems.Count);

                itemToAddTo.DropDownItems.Insert(indexToInsertAt, menuItem);
            }

            return menuItem;
        }

        ToolStripMenuItem GetItem(string name)
        {
            foreach (ToolStripMenuItem item in GlueGui.MenuStrip.Items)
            {
                if (item.Text == name)
                {
                    return item;
                }
            }
            return null;
        }

        public void RemoveAllMenuItems()
        {
            foreach (var kvp in toolStripItemsAndParents)
            {
                // need to invoke this on the main thread:

                kvp.Value.DropDownItems.Remove(kvp.Key);
            }

        }

        #endregion

        #region Toolbar

        protected void AddToToolBar(System.Windows.Controls.UserControl control, string toolbarName)
        {
            var tray = PluginManager.ToolBarTray;

            var toAddTo = tray.ToolBars.FirstOrDefault(item => item.Name == toolbarName);

            if (toAddTo == null)
            {
                toAddTo = new System.Windows.Controls.ToolBar();

                toAddTo.Name = toolbarName;
                tray.ToolBars.Add(toAddTo);
            }

            control.Padding = new System.Windows.Thickness(3, 0, 3, 0);

            toAddTo.Items.Add(control);

        }

        protected bool RemoveFromToolbar(System.Windows.Controls.UserControl control, string toolbarName)
        {
            var tray = PluginManager.ToolBarTray;

            var toRemoveFrom = tray.ToolBars.FirstOrDefault(item => item.Name == toolbarName);

            bool wasRemoved = false;

            if (toRemoveFrom != null)
            {
                toRemoveFrom.Items.Remove(control);
                wasRemoved = true;
            }

            return wasRemoved;
        }

        #endregion

        #region Code Generation

        List<ElementComponentCodeGenerator> CodeGenerators
        {
            get;
            set;
        } = new List<ElementComponentCodeGenerator>();
        List<Game1CodeGenerator> GameCodeGenerators
        {
            get;
            set;
        } = new List<Game1CodeGenerator>();

        public void RegisterCodeGenerator(ElementComponentCodeGenerator codeGenerator)
        {
            CodeGenerators.Add(codeGenerator);
            CodeWriter.CodeGenerators.Add(codeGenerator);
        }

        public void RegisterCodeGenerator(Game1CodeGenerator gameCodeGenerator)
        {
            GameCodeGenerators.Add(gameCodeGenerator);

            Game1CodeGeneratorManager.Generators.Add(gameCodeGenerator);
        }

        public void UnregisterAllCodeGenerators()
        {
            CodeWriter.CodeGenerators.RemoveAll(item => CodeGenerators.Contains(item));
            Game1CodeGeneratorManager.Generators.RemoveAll(item => GameCodeGenerators.Contains(item));

            CodeGenerators.Clear();
            GameCodeGenerators.Clear();
        }

        #endregion

        protected void AddErrorReporter(IErrorReporter errorReporter)
        {
            EditorObjects.IoC.Container.Get<GlueErrorManager>().Add(errorReporter);
        }

        #region Tab Methods

        protected PluginTab CreateTab(System.Windows.FrameworkElement control, string tabName)
        {
            //System.Windows.Forms.Integration.ElementHost wpfHost;
            //wpfHost = new System.Windows.Forms.Integration.ElementHost();
            //wpfHost.Dock = DockStyle.Fill;
            //wpfHost.Child = control;

            //return CreateTab(wpfHost, tabName);
            var page = new PluginTabPage();
            page.Resources = MainPanelControl.ResourceDictionary;

            page.Title = tabName;
            page.Content = control;
            control.Resources = MainPanelControl.ResourceDictionary;

            PluginTab pluginTab = new PluginTab();
            pluginTab.Page = page;

            page.ClosedByUser += (sender) =>
            {
                pluginTab.Hide();
                //OnClosedByUser(sender);
            };

            return pluginTab;

        }

        protected PluginTab CreateTab(System.Windows.Forms.Control control, string tabName)
        {
            var host = new System.Windows.Forms.Integration.WindowsFormsHost();

            host.Child = control;

            return CreateTab(host, tabName);
        }

        public static ObservableCollection<PluginTabPage> GetTabContainerFromLocation(TabLocation tabLocation)
        {
            ObservableCollection<PluginTabPage> tabContainer = null;

            switch (tabLocation)
            {
                case TabLocation.Top: tabContainer = PluginManager.TabControlViewModel.TopTabItems; break;
                case TabLocation.Left: tabContainer = PluginManager.TabControlViewModel.LeftTabItems; break;
                case TabLocation.Center: tabContainer = PluginManager.TabControlViewModel.CenterTabItems; break;
                case TabLocation.Right: tabContainer = PluginManager.TabControlViewModel.RightTabItems; break;
                case TabLocation.Bottom: tabContainer = PluginManager.TabControlViewModel.BottomTabItems; break;
            }

            return tabContainer;
        }

        protected PluginTab CreateAndAddTab(System.Windows.Forms.Control control, string tabName, TabLocation tabLocation = TabLocation.Center)
        {
            var tab = CreateTab(control, tabName);
            tab.SuggestedLocation = tabLocation;
            tab.Show();
            return tab;
        }

        protected PluginTab CreateAndAddTab(System.Windows.Controls.UserControl control, string tabName, TabLocation tabLocation = TabLocation.Center)
        {
            var tab = CreateTab(control, tabName);
            tab.SuggestedLocation = tabLocation;
            tab.Show();
            return tab;
        }

        void OnClosedByUser(object sender)
        {
            PluginManager.ShutDownPlugin(this);
        }


        #endregion

    }
}
