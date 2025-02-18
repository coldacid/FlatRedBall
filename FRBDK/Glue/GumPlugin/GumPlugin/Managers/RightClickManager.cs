﻿using FlatRedBall.Glue.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Gum.DataTypes;
using System.Windows.Forms;
using FlatRedBall.IO;
using GumPlugin.CodeGeneration;
using Gum.Managers;
using FlatRedBall.Glue.ViewModels;

namespace GumPlugin.Managers
{
    public class RightClickManager : Singleton<RightClickManager>
    {
        
        public void HandleTreeViewRightClick(System.Windows.Forms.TreeNode rightClickedTreeNode, System.Windows.Forms.ContextMenuStrip menuToModify)
        {
            TryAddAddGumScreenItem(rightClickedTreeNode, menuToModify);

            TryAddAddComponentForCurrentEntity(rightClickedTreeNode, menuToModify);

            TryAddAddNewScreenForCurrentScreen(rightClickedTreeNode, menuToModify);

            TryAddRegenerateGumElement(rightClickedTreeNode, menuToModify);
        }

        private void TryAddRegenerateGumElement(TreeNode rightClickedTreeNode, ContextMenuStrip menuToModify)
        {
            var file = rightClickedTreeNode.Tag as ReferencedFileSave;

            bool shouldShowRegenerateCodeMenu =
                file != null;

            if(shouldShowRegenerateCodeMenu)
            {
                var extension = FileManager.GetExtension(file.Name);

                shouldShowRegenerateCodeMenu =
                    extension == GumProjectSave.ComponentExtension ||
                    extension == GumProjectSave.ProjectExtension ||
                    extension == GumProjectSave.ScreenExtension ||
                    extension == GumProjectSave.StandardExtension;

            }

            if(file != null && shouldShowRegenerateCodeMenu)
            {

                var newMenu = new ToolStripMenuItem("Regenerate Gum Code");
                menuToModify.Items.Add(newMenu);
                newMenu.Click += delegate
                {
                    var fileName = GlueCommands.Self.GetAbsoluteFileName(file);
                    CodeGeneratorManager.Self.GenerateDueToFileChange(fileName);

                };
            }
        }

        private void TryAddAddNewScreenForCurrentScreen(TreeNode rightClickedTreeNode, ContextMenuStrip menuToModify)
        {
            var shouldContinue = true;
            if (!rightClickedTreeNode.IsScreenNode())
            {
                shouldContinue = false;
            }

            if (shouldContinue && AppState.Self.GumProjectSave == null)
            {
                shouldContinue = false;
            }

            var screen = rightClickedTreeNode.Tag as FlatRedBall.Glue.SaveClasses.ScreenSave;

            if(shouldContinue)
            {
                var alreadyHasScreen = screen.ReferencedFiles.Any(item => FileManager.GetExtension(item.Name) == "gusx");

                if(alreadyHasScreen)
                {
                    shouldContinue = false;
                }
            }

            if(shouldContinue)
            {
                var newMenuItem = new ToolStripMenuItem($"Create New Gum Screen for {FileManager.RemovePath(screen.Name)}");
                menuToModify.Items.Add(newMenuItem);
                newMenuItem.Click += (not, used) => AppCommands.Self.AddScreenForGlueScreen(screen);
            }
        }

        private void TryAddAddComponentForCurrentEntity(TreeNode rightClickedTreeNode, ContextMenuStrip menuToModify)
        {
            var shouldContinue = true;
            if(!rightClickedTreeNode.IsEntityNode())
            {
                shouldContinue = false;
            }

            if(shouldContinue && AppState.Self.GumProjectSave == null)
            {
                shouldContinue = false;
            }

            var entity = rightClickedTreeNode.Tag as EntitySave;
            string gumComponentName = null;
            if(shouldContinue)
            {
                gumComponentName = FileManager.RemovePath(entity.Name) + "Gum";
                bool exists = AppState.Self.GumProjectSave.Components.Any(item => item.Name == gumComponentName);

                if(exists)
                {
                    shouldContinue = false;
                }
            }

            if (shouldContinue)
            {

                var newMenuitem = new ToolStripMenuItem($"Add Gum Component to {FileManager.RemovePath(entity.Name)}");
                menuToModify.Items.Add(newMenuitem);
                newMenuitem.Click += (not, used) =>
                {
                    var gumComponent = new ComponentSave();
                    gumComponent.Initialize(StandardElementsManager.Self.GetDefaultStateFor("Component"));
                    gumComponent.BaseType = "Container";
                    gumComponent.Name = gumComponentName;

                    string gumProjectFileName = GumProjectManager.Self.GetGumProjectFileName();

                    AppCommands.Self.AddComponentToGumProject(gumComponent);

                    AppCommands.Self.SaveGumx(saveAllElements: false);

                    AppCommands.Self.SaveComponent(gumComponent);



                    AssetTypeInfoManager.Self.RefreshProjectSpecificAtis();

                    var ati = AssetTypeInfoManager.Self.GetAtiFor(gumComponent);

                    var addObjectViewModel = new AddObjectViewModel();
                    addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                    addObjectViewModel.SourceClassType = ati.QualifiedRuntimeTypeName.QualifiedType;
                    addObjectViewModel.ObjectName = "GumObject";

                    GlueCommands.Self.GluxCommands.AddNewNamedObjectTo(addObjectViewModel, entity);
                };
            }
        }

        private bool TryAddAddGumScreenItem(TreeNode rightClickedTreeNode, ContextMenuStrip menuToModify)
        {
            bool shouldContinue = true;
            if (!rightClickedTreeNode.IsFilesContainerNode() || !rightClickedTreeNode.Parent.IsScreenNode())
            {
                shouldContinue = false;
            }

            ReferencedFileSave gumxRfs = null;

            if (shouldContinue)
            {
                // Let's get all the available Screens:
                gumxRfs = GumProjectManager.Self.GetRfsForGumProject();
                shouldContinue = gumxRfs != null;

            }

            if (shouldContinue)
            {
                string fullFileName = GlueCommands.Self.GetAbsoluteFileName(gumxRfs);

                if (System.IO.File.Exists(fullFileName))
                {
                    string error;

                    // Calling Load does a deep load.  We only want references, so we're
                    // going to do a shallow load for perf reasons.
                    //GumProjectSave gps = GumProjectSave.Load(fullFileName, out error);
                    GumProjectSave gps = FileManager.XmlDeserialize<GumProjectSave>(fullFileName);

                    if (gps.ScreenReferences.Count != 0)
                    {
                        var menuToAddScreensTo = new ToolStripMenuItem("Add Gum Screen");

                        menuToModify.Items.Add(menuToAddScreensTo);

                        foreach (var screen in gps.ScreenReferences)
                        {
                            var screenMenuItem = new ToolStripMenuItem(screen.Name);
                            screenMenuItem.Click += HandleScreenToAddClick;
                            menuToAddScreensTo.DropDownItems.Add(screenMenuItem);
                        }
                    }
                }
            }

            return shouldContinue;
        }

        private void HandleScreenToAddClick(object sender, EventArgs e)
        {
            string screenName = ((ToolStripMenuItem)sender).Text;

            AddScreenByName(screenName, GlueState.Self.CurrentScreenSave);

        }

        public void AddScreenByName(string screenName, FlatRedBall.Glue.SaveClasses.ScreenSave glueScreen)
        {
            string fullFileName = AppState.Self.GumProjectFolder + "Screens/" +
                screenName + "." + GumProjectSave.ScreenExtension;

            if (System.IO.File.Exists(fullFileName))
            {
                bool cancelled = false;

                var newRfs = FlatRedBall.Glue.FormHelpers.RightClickHelper.AddSingleFile(
                    fullFileName, ref cancelled, glueScreen);

                // prior to doing any codegen, need to refresh the project specific ATIs:
                AssetTypeInfoManager.Self.RefreshProjectSpecificAtis();


                var element = CodeGeneratorManager.GetElementFrom(newRfs);

                if(element != null)
                {
                    newRfs.RuntimeType =
                        GueDerivingClassCodeGenerator.Self.GetQualifiedRuntimeTypeFor(element);

                    GlueCommands.Self.GluxCommands.SaveGlux();
                    GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();

                }
            }
            else
            {
                var message = "Could not find the file for the Gum screen " + screenName + $"\nSearched in:\n{fullFileName}";

                if (AppState.Self.GumProjectSave == null)
                {
                    message += "\nThis is probably happening because the Gum project is null";
                }
                else if(string.IsNullOrWhiteSpace(AppState.Self.GumProjectFolder))
                {
                    message += "\nThe project does have a Gum project loaded, but it does not have an associated filename";
                }

                MessageBox.Show(message);
            }
        }
    }
}
