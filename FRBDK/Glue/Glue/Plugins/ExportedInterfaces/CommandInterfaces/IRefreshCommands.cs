﻿using FlatRedBall.Glue.SaveClasses;
using System.Security.Cryptography;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IRefreshCommands
    {
        /// <summary>
        /// Refreshes everything for the selected TreeNode
        /// </summary>
        void RefreshUiForSelectedElement();

        void RefreshTreeNodes();

        /// <summary>
        /// Refreshes the tree node for the selected element.
        /// This may add or remove tree nodes depending on whether
        /// the tree node is already created, and if the element's
        /// IsHiddenInTreeView is set to true.
        /// </summary>
        /// <param name="element">IElement to update the tree node for</param>
        void RefreshTreeNodeFor(IElement element);

        void RefreshUi(StateSaveCategory category);

        /// <summary>
        /// Refreshes the UI for the Global Content tree node
        /// </summary>
        void RefreshGlobalContent();

        /// <summary>
        /// Refreshes all errors.
        /// </summary>
        void RefreshErrors();


        /// <summary>
        /// Refreshes the propertygrid so that the latest data will be shown.  This should be called whenever data
        /// shown in the property grid has changed because the propertygrid does not automatically reflect the change.
        /// </summary>
        void RefreshPropertyGrid();

        void RefreshVariables();

        void RefreshSelection();

        void RefreshDirectoryTreeNodes();
    }
}
