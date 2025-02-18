﻿using System;
using System.Linq;
using System.Windows.Forms;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Glue;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    public class RefreshCommands : IRefreshCommands
    {
        // The error manager is in a plugin. We could put it
        // here but it contains UI and we are trying ot move UI
        // out of Glue, not in, so instead we're going to go through
        public static Action RefreshErrorsAction { get; set; }

        public void RefreshUiForSelectedElement()
        {
            if (EditorLogic.CurrentElementTreeNode != null)
                MainGlueWindow.Self.BeginInvoke(
                    new EventHandler(delegate { EditorLogic.CurrentElementTreeNode.RefreshTreeNodes(); }));
            
        }

        public void RefreshTreeNodes()
        {
            if(TaskManager.Self.IsInTask())
            {
                TaskManager.Self.OnUiThread(RefreshTreeNodes);
            }
            else
            {
                var project = GlueState.Self.CurrentGlueProject;
                var entities = project.Entities.ToArray();
                var screens = project.Screens.ToArray();

                foreach(var entity in entities)
                {
                    RefreshTreeNodeFor(entity);
                }

                foreach(var screen in screens)
                {
                    RefreshTreeNodeFor(screen);
                }
                GlueCommands.Self.DoOnUiThread(() =>

                    ElementViewWindow.ScreensTreeNode.Nodes.SortByTextConsideringDirectories()
                );

                RefreshGlobalContent();
            }
        }

        public void RefreshTreeNodeFor(IElement element)
        {
            if (ProjectManager.ProjectBase != null)
            {
                GlueCommands.Self.DoOnUiThread(() =>
                {
                    var elementTreeNode = GlueState.Self.Find.ElementTreeNode(element);

                    if(elementTreeNode == null)
                    {
                        if(!element.IsHiddenInTreeView)
                        {
                            if(element is ScreenSave screen)
                            {
                                elementTreeNode = AddScreenInternal(screen);
                            }
                            else if(element is EntitySave entitySave)
                            {
                                elementTreeNode = ElementViewWindow.AddEntity(entitySave);
                            }
                            elementTreeNode?.RefreshTreeNodes();
                        }
                    }
                    else
                    {
                        if(element.IsHiddenInTreeView)
                        {
                            // remove it!
                            if (element is ScreenSave screen)
                            {
                                ElementViewWindow.RemoveScreen(screen);
                            }
                            else if (element is EntitySave entitySave)
                            {
                                ElementViewWindow.RemoveEntity(entitySave);
                            }
                        }
                        else
                        { 
                            elementTreeNode?.RefreshTreeNodes();
                        }
                    }

                });
            }
        }

        BaseElementTreeNode AddScreenInternal(ScreenSave screenSave)
        {
            string screenFileName = screenSave.Name + ".cs";
            string screenFileWithoutExtension = FileManager.RemoveExtension(screenFileName);

            var screenTreeNode = new ScreenTreeNode(FileManager.RemovePath(screenFileWithoutExtension));
            screenTreeNode.CodeFile = screenFileName;

            ElementViewWindow.ScreensTreeNode.Nodes.Add(screenTreeNode);
            ElementViewWindow.ScreensTreeNode.Nodes.SortByTextConsideringDirectories();

            string generatedFile = screenFileWithoutExtension + ".Generated.cs";
            screenTreeNode.GeneratedCodeFile = generatedFile;

            screenTreeNode.SaveObject = screenSave;

            return screenTreeNode;
        }


        public void RefreshUi(StateSaveCategory category)
        {
            BaseElementTreeNode treeNode = null;
            if (ProjectManager.ProjectBase != null)
            {
                var element = Elements.ObjectFinder.Self.GetElementContaining(category);

                if(element != null)
                {
                    treeNode = GlueState.Self.Find.ElementTreeNode(element);

                }



            }

            if(treeNode != null)
            {
                MainGlueWindow.Self.BeginInvoke(new EventHandler(delegate { treeNode.RefreshStateCategoryUi(category); }));
            }
        }

        public void RefreshGlobalContent()
        {
            ElementViewWindow.UpdateGlobalContentTreeNodes(false);
        }

        public void RefreshPropertyGrid()
        {
            MainGlueWindow.Self.BeginInvoke(new EventHandler(delegate { MainGlueWindow.Self.PropertyGrid.Refresh(); }));
            PropertyGridHelper.UpdateDisplayedPropertyGridProperties();

        }

        public void RefreshVariables()
        {
            PluginManager.CallPluginMethod("Main Property Grid Plugin", "RefreshVariables");
        }


        public void RefreshSelection()
        {
            if (!ProjectManager.WantsToClose)
            {
                MainGlueWindow.Self.BeginInvoke(new EventHandler(RefreshSelectionInternal));
            }

        }

        private void RefreshSelectionInternal(object sender, EventArgs e)
        {
            // During a reload the CurrentElement may no longer be valid:
            var element = EditorLogic.CurrentElement;
            if (element != null)
            {
                if (EditorLogic.CurrentCustomVariable != null)
                {
                    EditorLogic.CurrentCustomVariable = element.GetCustomVariable(EditorLogic.CurrentCustomVariable.Name);
                }
                else if (EditorLogic.CurrentReferencedFile != null)
                {
                    EditorLogic.CurrentReferencedFile = element.GetReferencedFileSave(EditorLogic.CurrentReferencedFile.Name);
                }
            }
        }

        public void RefreshErrors()
        {
            TaskManager.Self.AddOrRunIfTasked(() => RefreshErrorsAction?.Invoke(),
                "Refreshing Errors",
                TaskExecutionPreference.AddOrMoveToEnd);
        }

        public void RefreshDirectoryTreeNodes()
        {
            ElementViewWindow.AddDirectoryNodes();
        }
    }
}
