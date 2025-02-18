﻿using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using Microsoft.Build.Evaluation;
using System.Collections.Generic;


namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IProjectCommands
    {
        /// <summary>
        /// Saves a project immediately if run from an existing task. Adds a task if not.
        /// </summary>
        void SaveProjects();

        void SaveProjectsImmediately();


        void AddNugetIfNotAdded(string packageName, string versionNumber);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        /// <param name="partialName"></param>
        /// <param name="code"></param>
        void CreateAndAddPartialFile(IElement element, string partialName, string code);

        /// <summary>
        /// Creates an empty code file (if it doesn't already exist), and adds it to the main project. If the file already exists, then it 
        /// is not modified on disk, but is still added to the code project. The code project is saved if added.
        /// </summary>
        /// <param name="relativeFileName">The file name.</param>
        void CreateAndAddCodeFile(string relativeFileName);

        /// <summary>
        /// Creates an empty code file (if it doesn't already exist), and adds it to the main project. If the file already exists, then it 
        /// is not modified on disk, but is still added to the code project. The code project is saved if added.
        /// </summary>
        /// <param name="filePath">The file path to save.</param>
        void CreateAndAddCodeFile(FilePath filePath);
        ProjectItem CreateAndAddCodeFile(FilePath filePath, bool save);


        void AddContentFileToProject(string absoluteFileName, bool saveProjects = true);

        /// <summary>
        /// Adds the argument file to the current project if it is not already part of the project. 
        /// </summary>
        /// <param name="codeFilePath">The FilePath to the file.</param>
        void TryAddCodeFileToProject(FilePath codeFilePath, bool saveOnAdd = false);

        void CopyToBuildFolder(ReferencedFileSave rfs);
        void CopyToBuildFolder(string absoluteSource);

        void AddDirectory(string folderName, System.Windows.Forms.TreeNode treeNodeToAddTo);

        string MakeAbsolute(string relativeFileName, bool forceAsContent);

        void MakeGeneratedCodeItemsNested();

        /// <summary>
        /// Removes the argument filePath from all currently-loaded files, and saves the projects.
        /// </summary>
        /// <param name="filePath">The file path to remove</param>
        void RemoveFromProjects(FilePath filePath, bool saveAfterRemoving = true);
        void RemoveFromProjects(string absoluteFileName);
        void RemoveFromProjectsTask(FilePath absoluteFileName, bool saveAfterRemoving = true);

        /// <summary>
        /// Verifies that the passed ReferencedFileSave is part of the project, and if not, adds it.
        /// This recurisvely adds all files referenced by the argument.
        /// </summary>
        /// <param name="referencedFileSave"></param>
        /// <returns>Whether anything was added</returns>
        bool UpdateFileMembershipInProject(ReferencedFileSave referencedFileSave);

        /// <summary>
        /// Updates the argument fileName's membership to the argument project.
        /// </summary>
        /// <param name="project">The project (main, does not have to be a content project if XNA)</param>
        /// <param name="fileName">The file name, which can be relative to the project or which can be absolute.</param>
        /// <param name="useContentPipeline">Whether to force the file to use the content pipeline.</param>
        /// <param name="shouldLink"></param>
        /// <param name="parentFile"></param>
        /// <returns></returns>
        bool UpdateFileMembershipInProject(VisualStudioProject project, string fileName, bool useContentPipeline, bool shouldLink, string parentFile = null, bool recursive = true, List<string> alreadyReferencedFiles = null);

        void CreateNewProject();
    }
}
