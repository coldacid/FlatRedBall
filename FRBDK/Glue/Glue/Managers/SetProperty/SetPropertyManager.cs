﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses.Helpers;
using Glue;
using FlatRedBall.Glue.SaveClasses;
using EditorObjects.IoC;

namespace FlatRedBall.Glue.SetVariable
{
    /// <summary>
    /// Initial responder to when an object property (not variable) get changed.  This handles determing the type 
    /// of change that occurred and calling methods on object-based handlers - like 
    /// NamedObjectSave vs. ReferencedFileSave.
    /// </summary>
    public class SetPropertyManager
    {

        public void PropertyValueChanged(PropertyValueChangedEventArgs e, System.Windows.Forms.PropertyGrid mPropertyGrid)
        {
            UnreferencedFilesManager.Self.ProcessRefreshOfUnreferencedFiles();

            #region Check for Errors

            if (mPropertyGrid == null)
            {
                System.Windows.Forms.MessageBox.Show("There has been an internal error in Glue related to updating the PropertyGrid.  This likely happens if there has been an earlier error in Glue.  You should probably restart Glue.");
                MainGlueWindow.Self.HasErrorOccurred = true;
            }

            #endregion

            string changedMember = e.ChangedItem.PropertyDescriptor.Name;
            object oldValue = e.OldValue;
            string variableName = e.ChangedItem.Label;
            string parentGridItemName = null;
            if (e.ChangedItem != null && e.ChangedItem.Parent != null)
            {
                parentGridItemName = e.ChangedItem.Parent.Label;
            }

            ReactToPropertyChanged(changedMember, oldValue, variableName, parentGridItemName);
        }

        public void ReactToPropertyChanged(string variableNameAsDisplayed, object oldValue, 
            string variableName, string parentGridItemName)
        {
            var mPropertyGrid = MainGlueWindow.Self.PropertyGrid;


            bool updateTreeView = true;

            #region EventResponseSave
            if (EditorLogic.CurrentEventResponseSave != null)
            {
                Container.Get<EventResponseSaveSetVariableLogic>().ReactToChange(
                    variableNameAsDisplayed, oldValue, GlueState.Self.CurrentEventResponseSave, GlueState.Self.CurrentElement);
            }

            #endregion

            #region State

            else if (EditorLogic.CurrentStateSave != null)
            {
                Container.Get<StateSaveSetVariableLogic>().ReactToStateSaveChangedValue(
                    EditorLogic.CurrentStateSave, EditorLogic.CurrentStateSaveCategory, variableNameAsDisplayed, oldValue,
                    GlueState.Self.CurrentElement, ref updateTreeView);


            }

            #endregion

            #region StateCategory

            else if (EditorLogic.CurrentStateSaveCategory != null)
            {
                Container.Get<StateSaveCategorySetVariableLogic>().ReactToStateSaveCategoryChangedValue(
                    EditorLogic.CurrentStateSaveCategory, variableNameAsDisplayed, oldValue,
                    GlueState.Self.CurrentElement, ref updateTreeView);

            }

            #endregion

            #region NamedObject

            else if (GlueState.Self.CurrentNamedObjectSave != null)
            {
                Container.Get<NamedObjectSetVariableLogic>().ReactToNamedObjectChangedValue(
                    variableNameAsDisplayed, oldValue, parentGridItemName);
            }

            #endregion

            #region ReferencedFile

            else if (EditorLogic.CurrentReferencedFile != null)
            {
                Container.Get<ReferencedFileSaveSetPropertyManager>().ReactToChangedReferencedFile(
                    variableNameAsDisplayed, oldValue, ref updateTreeView);
            }

            #endregion

            #region CustomVariable

            else if (EditorLogic.CurrentCustomVariable != null)
            {
                Container.Get<CustomVariableSaveSetPropertyLogic>().ReactToCustomVariableChangedValue(
                    variableNameAsDisplayed, EditorLogic.CurrentCustomVariable, oldValue);
            }
            else if (mPropertyGrid.SelectedObject != null && mPropertyGrid.SelectedObject is PropertyGridDisplayer &&
                GlueState.Self.CurrentElement != null && GlueState.Self.CurrentElement.GetCustomVariableRecursively(variableName) != null)
            {
                Container.Get<CustomVariableSaveSetPropertyLogic>().ReactToCustomVariableChangedValue(
                    variableName, GlueState.Self.CurrentElement.GetCustomVariableRecursively(variableName), oldValue);
            }
            #endregion

            // Check Entities and Screens after checking variables and objects
            #region Entity
            else if (EditorLogic.CurrentEntitySave != null)
            {
                Container.Get<EntitySaveSetVariableLogic>().ReactToEntityChangedProperty(variableNameAsDisplayed, oldValue);
            }

            #endregion

            #region ScreenSave

            else if (EditorLogic.CurrentScreenSave != null)
            {
                Container.Get<ScreenSaveSetVariableLogic>().ReactToScreenChangedValue(variableNameAsDisplayed, oldValue);
            }

            #endregion

            #region Global content container node

            else if (EditorLogic.CurrentTreeNode.Root().IsGlobalContentContainerNode())
            {
                Container.Get<GlobalContentSetVariableLogic>().ReactToGlobalContentChangedValue(
                    variableNameAsDisplayed, oldValue, ref updateTreeView);
            }

            #endregion


            PluginManager.ReactToChangedProperty(variableNameAsDisplayed, oldValue);

            if (GlueState.Self.CurrentElement != null)
            {
                GlueCommands.Self.GenerateCodeCommands
                    .GenerateElementAndReferencedObjectCodeTask(GlueState.Self.CurrentElement);
            }
            else if (EditorLogic.CurrentReferencedFile != null)
            {
                GlobalContentCodeGenerator.UpdateLoadGlobalContentCode();
            }

            // UpdateCurrentObjectReferencedTreeNodes
            // kicks off a save by default.  Therefore
            // we don't need to call SaveProjects if UpdateCurrentObjectReferencedTreeNodes
            // is called.
            if (updateTreeView)
            {
                ElementViewWindow.UpdateCurrentObjectReferencedTreeNodes();
            }
            else
            {
                GlueCommands.Self.ProjectCommands.SaveProjects();
            }

            mPropertyGrid.Refresh();

            GluxCommands.Self.SaveGlux();

            // Vic says:  This was intented to refresh the variables at one point
            // but this is a messy feature.  I think we should just refresh the entire
            // glux whenever a change is made now that it's async
            //RemotingManager.RefreshVariables(false); 
        }

    }
}
