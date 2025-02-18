﻿using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.CollisionPlugin.Controllers;
using OfficialPlugins.CollisionPlugin.Managers;
using OfficialPlugins.CollisionPlugin.ViewModels;
using OfficialPlugins.CollisionPlugin.Views;
using OfficialPluginsCore.CollisionPlugin.Errors;
using OfficialPluginsCore.CollisionPlugin.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OfficialPlugins.CollisionPlugin
{
    [Export(typeof(PluginBase))]
    public class MainCollisionPlugin : PluginBase
    {
        #region Fields/Properties

        CollisionRelationshipViewModel relationshipViewModel;
        CollisionRelationshipView relationshipControl;
        PluginTab relationshipPluginTab;

        CollidableNamedObjectRelationshipDisplay collidableDisplay;
        CollidableNamedObjectRelationshipViewModel collidableViewModel;
        PluginTab collidableTab;

        public override string FriendlyName => "Collision Plugin";

        // 1.0
        //  - Initial release
        // 1.1
        //  - CollisionRelationships now have their Name set
        // 1.2
        //  - Added ability to mark a collision as inactive
        public override Version Version => new Version(1, 2);

        #endregion

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            UnregisterAllCodeGenerators();

            AvailableAssetTypes.Self.RemoveAssetType(
                AssetTypeInfoManager.Self.CollisionRelationshipAti);
            return true;
        }

        public override void StartUp()
        {
            relationshipViewModel = CollisionRelationshipViewModelController.CreateViewModel();

            collidableViewModel = new CollidableNamedObjectRelationshipViewModel();
            CollidableNamedObjectController.RegisterViewModel(collidableViewModel);

            var collisionCodeGenerator = new CollisionCodeGenerator();

            AvailableAssetTypes.Self.AddAssetType(
                AssetTypeInfoManager.Self.CollisionRelationshipAti);

            RegisterCodeGenerator(collisionCodeGenerator);

            AssignEvents();

            AddErrorReporter(new CollisionErrorReporter());
        }

        private void AssignEvents()
        {
            this.ReactToLoadedGluxEarly += HandleGluxLoad;

            this.ReactToItemSelectHandler += HandleTreeViewItemSelected;

            this.AddEventsForObject += HandleAddEventsForObject;

            this.GetEventSignatureArgs += GetEventSignatureAndArgs;

            this.ReactToObjectRemoved += HandleObjectRemoved;

            this.ReactToChangedPropertyHandler += CollisionRelationshipViewModelController.HandleGlueObjectPropertyChanged;

            this.ReactToCreateCollisionRelationshipsBetween += (NamedObjectSave first, NamedObjectSave second) =>
            {
                CollidableNamedObjectController.CreateCollisionRelationshipBetweenObjects(first.InstanceName, second.InstanceName, first.GetContainer());
            };
        }

        private void HandleObjectRemoved(IElement element, NamedObjectSave namedObject)
        {
            if(namedObject.IsCollisionRelationship())
            {
                GlueCommands.Self.RefreshCommands.RefreshErrors();
            }
            else if(element.AllNamedObjects.Any(item => item.IsCollisionRelationship() &&
                (item.GetFirstCollidableObjectName() == namedObject.InstanceName) ||
                (item.GetSecondCollidableObjectName() == namedObject.InstanceName)))
            {
                GlueCommands.Self.RefreshCommands.RefreshErrors();
            }
        }

        private void HandleGluxLoad()
        {
            foreach(var screen in GlueState.Self.CurrentGlueProject.Screens)
            {
                foreach(var nos in screen.AllNamedObjects)
                {
                    CollisionRelationshipViewModelController.TryFixSourceClassType(nos);
                }
            }

            // entities probably won't have collisisons but...what if they do? Might as well be prepared:
            foreach (var entity in GlueState.Self.CurrentGlueProject.Entities)
            {
                foreach (var nos in entity.AllNamedObjects)
                {
                    CollisionRelationshipViewModelController.TryFixSourceClassType(nos);
                }
            }

        }

        private void GetEventSignatureAndArgs(NamedObjectSave namedObjectSave, EventResponseSave eventResponseSave, out string type, out string signatureArgs)
        {
            if(namedObjectSave == null)
            {
                throw new ArgumentNullException(nameof(namedObjectSave));
            }

            if (namedObjectSave.GetAssetTypeInfo() == AssetTypeInfoManager.Self.CollisionRelationshipAti &&
                eventResponseSave.SourceObjectEvent == "CollisionOccurred")
            {
                bool firstThrowaway;
                bool secondThrowaway;

                var firstType = AssetTypeInfoManager.GetFirstGenericType(namedObjectSave, out firstThrowaway);
                var secondType = AssetTypeInfoManager.GetSecondGenericType(namedObjectSave, out secondThrowaway);

                type = $"System.Action<{firstType}, {secondType}>";
                signatureArgs = $"{firstType} first, {secondType} second";
            }
            else
            {
                type = null;
                signatureArgs = null;
            }
        }

        private void HandleAddEventsForObject(NamedObjectSave namedObject, List<ExposableEvent> listToAddTo)
        {
            if(namedObject.GetAssetTypeInfo() == AssetTypeInfoManager.Self.CollisionRelationshipAti)
            {
                var newEvent = new ExposableEvent("CollisionOccurred");
                listToAddTo.Add(newEvent);
            }
        }

        private void HandleTreeViewItemSelected(TreeNode selectedTreeNode)
        {
            var selectedNos = GlueState.Self.CurrentNamedObjectSave;

            var element = GlueState.Self.CurrentElement;

            if (selectedNos != null)
            {
                CollisionRelationshipViewModelController.TryFixSourceClassType(selectedNos);
            }

            TryHandleSelectedCollisionRelationship(selectedNos);

            TryHandleSelectedCollidable(element, selectedNos);
        }

        private void TryHandleSelectedCollisionRelationship(NamedObjectSave selectedNos)
        {
            var shouldShowControl = false;
            if (selectedNos?.GetAssetTypeInfo() == AssetTypeInfoManager.Self.CollisionRelationshipAti)
            {
                RefreshViewModelTo(selectedNos);

                shouldShowControl = true;
            }

            if (shouldShowControl)
            {
                if (relationshipControl == null)
                {
                    relationshipControl = new CollisionRelationshipView();
                    relationshipPluginTab = this.CreateTab(relationshipControl, "Collision");
                    relationshipControl.DataContext = relationshipViewModel;
                }
                relationshipPluginTab.Show();
            }
            else
            {
                relationshipPluginTab?.Hide();
            }
        }

        private void TryHandleSelectedCollidable(IElement element, NamedObjectSave selectedNos)
        {
            var shouldShowControl = selectedNos != null &&
                CollisionRelationshipViewModelController
                .GetIfCanBeReferencedByRelationship(selectedNos);

            if(shouldShowControl)
            {
                RefreshCollidableViewModelTo(element, selectedNos);

                if (collidableDisplay == null)
                {
                    collidableDisplay = new CollidableNamedObjectRelationshipDisplay();
                    collidableTab = this.CreateTab(collidableDisplay, "Collision");
                    collidableDisplay.DataContext = collidableViewModel;
                }
                collidableTab.Show();

                // not sure why this is required:
                collidableDisplay.DataContext = null;
                collidableDisplay.DataContext = collidableViewModel;
            }
            else
            {
                collidableTab?.Hide();
            }
        }

        private void RefreshViewModelTo(NamedObjectSave selectedNos)
        {
            // show UId

            if (relationshipControl != null)
            {
                relationshipControl.DataContext = null;
            }

            CollisionRelationshipViewModelController.RefreshViewModelTo(relationshipViewModel, selectedNos);

            if (relationshipControl != null)
            {
                relationshipControl.DataContext = relationshipViewModel;
            }
        }

        private void RefreshCollidableViewModelTo(IElement element, NamedObjectSave selectedNos)
        {
            CollidableNamedObjectController.RefreshViewModelTo(element, selectedNos, collidableViewModel);
        }

        public void FixNamedObjectCollisionType(NamedObjectSave selectedNos)
        {
            CollisionRelationshipViewModelController.TryFixSourceClassType(selectedNos);
        }
    }
}
