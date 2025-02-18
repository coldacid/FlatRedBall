﻿using EditorObjects.SaveClasses;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.ViewModels;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlatRedBall.Glue.Managers
{
    public class AddExistingFileManager : Singleton<AddExistingFileManager>
    {

        public void AddExistingFileClick()
        {
            var viewModel = new AddExistingFileViewModel();
            FillWithFiles(viewModel);


            var window = new AddExistingFileWindow();
            window.DataContext = viewModel;

            var result = window.ShowDialog();

            if(result == true)
            {
                var element = GlueState.Self.CurrentElement;
                string directoryOfTreeNode = EditorLogic.CurrentTreeNode.GetRelativePath();
                bool userCancelled = false;

                foreach(var file in viewModel.Files)
                {
                    // If there is already an RFS for this file, no need to add it again.
                    ReferencedFileSave existingFile = null;
                    if(element != null)
                    {
                        existingFile = element.GetAllReferencedFileSavesRecursively()
                            .FirstOrDefault(item =>
                                GlueCommands.Self.FileCommands.GetFullFileName(item) == file);
                    }
                    else
                    {
                        // global content?
                        existingFile = GlueState.Self.CurrentGlueProject.GlobalFiles
                            .FirstOrDefault(item =>
                                GlueCommands.Self.FileCommands.GetFullFileName(item) == file);
                    }

                    if(existingFile == null)
                    {
                        TaskManager.Self.Add(() =>
                        {
                            AddSingleFile(file, ref userCancelled, element, directoryOfTreeNode);

                        }, $"Adding file {file}");
                    }
                    else
                    {
                        GlueState.Self.CurrentReferencedFileSave = existingFile;
                    }
                }
            }
        }

        private void FillWithFiles(AddExistingFileViewModel viewModel)
        {
            var contentFolder = GlueState.Self.ContentDirectory;

            var files = FileManager.GetAllFilesInDirectory(contentFolder, null, int.MaxValue);

            foreach(var file in files)
            {

                // make this thing relative:
                var relativeFile = FileManager.MakeRelative(file, contentFolder);

                // Feb 22, 2019
                // Initially I thought
                // I'd only show files that
                // aren't already added to the
                // current element, but files can
                // be added multiple times to an element
                // using different converters. Even though
                // it's rare, we want to still support it so
                // don't filter out files that are already added.

                viewModel.UnfilteredFileList.Add(relativeFile);
            }
            viewModel.ContentFolder = contentFolder;
            viewModel.RefreshFilteredList();
        }

        public ReferencedFileSave AddSingleFile(FilePath fileName, ref bool userCancelled, object options = null, IElement elementToAddTo = null)
        {
            var element = elementToAddTo ?? GlueState.Self.CurrentElement;

            return AddSingleFile(fileName, ref userCancelled, element, null, options);
        }

        public ReferencedFileSave AddSingleFile(FilePath fileName, ref bool userCancelled, IElement element, 
            string directoryOfTreeNode, object options = null)
        {
            ReferencedFileSave toReturn = null;

            #region Find the BuildToolAssociation for the selected file

            string rfsName = fileName.NoPathNoExtension;
            string extraCommandLineArguments = null;

            BuildToolAssociation buildToolAssociation = null;
            bool isBuiltFile = BuildToolAssociationManager.Self.GetIfIsBuiltFile(fileName.FullPath);
            bool userPickedNone = false;

            if (isBuiltFile)
            {
                buildToolAssociation = BuildToolAssociationManager.Self.GetBuildToolAssocationAndNameFor(fileName.FullPath, out userCancelled, out userPickedNone, out rfsName, out extraCommandLineArguments);
            }

            #endregion

            string sourceExtension = fileName.Extension;

            if (userPickedNone)
            {
                isBuiltFile = false;
            }

            if (isBuiltFile && buildToolAssociation == null && !userPickedNone)
            {
                GlueCommands.Self.PrintOutput("Couldn't find a tool for the file extension " + sourceExtension);
            }

            else if (!userCancelled)
            {

                toReturn = GlueCommands.Self.GluxCommands.AddSingleFileTo(fileName.FullPath, rfsName, extraCommandLineArguments, buildToolAssociation,
                    isBuiltFile, options, element, directoryOfTreeNode);
            }



            return toReturn;

        }

    }
}
