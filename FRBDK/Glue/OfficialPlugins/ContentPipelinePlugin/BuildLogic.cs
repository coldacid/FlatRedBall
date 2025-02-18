﻿using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Elements;

namespace OfficialPlugins.MonoGameContent
{
    public class BuildLogic : Singleton<BuildLogic>
    {
        static IGlueState GlueState => EditorObjects.IoC.Container.Get<IGlueState>();
        static IGlueCommands GlueCommands => EditorObjects.IoC.Container.Get<IGlueCommands>();


        static FilePath commandLineBuildExe =
            @"C:\Program Files (x86)\MSBuild\MonoGame\v3.0\Tools\MGCB.exe";

        public void RefreshBuiltFilesFor(VisualStudioProject project, bool forcePngsToContentPipeline)
        {


            /////////////Early Out///////////////////
            bool builderExists = commandLineBuildExe.Exists();
            if (builderExists == false)
            {
                // error? output?
                return;
            }

            ////////////End Early Out////////////////

            var allReferencedFileSaves = GlueState.CurrentGlueProject.GetAllReferencedFiles();

            allReferencedFileSaves = allReferencedFileSaves.Distinct((a, b) => a.Name == b.Name).ToList();

            bool needsMonoGameFilesBuilt = GetIfNeedsMonoGameFilesBuilt(project);

            if (needsMonoGameFilesBuilt)
            {
                // Actually files could be removed from the content pipeline, so we should 
                // consider all files:
                //var filesToBeBuilt = allReferencedFileSaves.Where(item => IsBuiltByContentPipeline(item, forcePngsToContentPipeline)).ToList();

                foreach (var fileToBeBuilt in allReferencedFileSaves)
                {
                    UpdateFileMembershipAndBuildReferencedFile(project, fileToBeBuilt, forcePngsToContentPipeline);
                }
            }

            //foreach (var file in GlueState.Self.CurrentGlueProject.GetAllReferencedFiles())
        }

        public static bool GetIfNeedsMonoGameFilesBuilt(ProjectBase project)
        {
            return project is DesktopGlProject ||
                project is AndroidProject ||
                project is UwpProject
                // todo: need to support iOS
                ;

        }


        public void UpdateFileMembershipAndBuildReferencedFile(VisualStudioProject project, ReferencedFileSave referencedFile, bool forcePngsToContentPipeline)
        {
            bool isBuilt = IsBuiltByContentPipeline(referencedFile, forcePngsToContentPipeline);

            if(isBuilt)
            {
                TryAddXnbReferencesAndBuild(referencedFile, project, save:true);
            }
            else
            {
                TryRemoveXnbReferences(project, referencedFile);

            }
        }

        public void TryHandleReferencedFile(VisualStudioProject project, string absoluteFile, bool forcePngsToContentPipeline)
        {
            bool isBuilt = IsBuiltByContentPipeline(absoluteFile, false, forcePngsToContentPipeline);

            if (isBuilt)
            {
                TryAddXnbReferencesAndBuild(absoluteFile, project, saveProjectAfterAdd: true);
            }
            else
            {
                TryRemoveXnbReferences(project, absoluteFile, save: true);

            }
        }


        public static void TryRemoveXnbReferences(VisualStudioProject project, ReferencedFileSave referencedFile, bool save = true)
        {
            var fullFileName = GlueCommands.FileCommands.GetFullFileName(referencedFile);
            TryRemoveXnbReferences(project, fullFileName, save);
        }

        public static void TryRemoveXnbReferences(VisualStudioProject project, string fullFileName, bool save = true)
        {
            string destinationDirectory = GetDestinationDirectory(fullFileName, project);

            ContentItem contentItem = GetContentItem(fullFileName, project, createEvenIfProjectTypeNotSupported: true);

            string absoluteToAddNoExtension = destinationDirectory +
                FileManager.RemovePath(FileManager.RemoveExtension(fullFileName));

            if (contentItem != null)
            {


                TaskManager.Self.Add(() =>
                {

                    bool didRemove = false;


                    foreach (string extension in contentItem.GetBuiltExtensions())
                    {
                        var absoluteFile = absoluteToAddNoExtension + "." + extension;

                        // remove any built file from the project if referenced - cleanup in case the user switched between content pipeline or not.
                        var item = project.GetItem(absoluteFile, true);

                        if (item != null)
                        {
                            project.RemoveItem(item);
                            didRemove = true;
                        }
                    }

                    if (didRemove && save)
                    {
                        GlueCommands.TryMultipleTimes(project.Save, 5);
                    }
                }, $"Removing XNB references for {fullFileName}");
            }
        }

        public static void TryDeleteBuiltXnbFor(ProjectBase project, ReferencedFileSave referencedFileSave, bool forcePngsToContentPipeline)
        {
            bool isBuilt = IsBuiltByContentPipeline(referencedFileSave, forcePngsToContentPipeline);

            ContentItem contentItem = null;

            if(isBuilt)
            {
                contentItem = GetContentItem(referencedFileSave, project, createEvenIfProjectTypeNotSupported: false);
            }

            if(contentItem != null)
            {
                
                foreach(var extension in contentItem.GetBuiltExtensions())
                {
                    var fileToDelete = $"{contentItem.OutputDirectory}{contentItem.OutputFileNoExtension}.{extension}";

                    if(System.IO.File.Exists(fileToDelete))
                    { 
                        GlueCommands.TryMultipleTimes(()=>System.IO.File.Delete(fileToDelete), 5);
                    }
                }
            }
        }

        private static ContentItem GetContentItem(ReferencedFileSave referencedFileSave, ProjectBase project, bool createEvenIfProjectTypeNotSupported)
        {
            var fullFileName = GlueCommands.FileCommands.GetFullFileName(referencedFileSave);
            return GetContentItem(fullFileName, project, createEvenIfProjectTypeNotSupported);
        }

        private static ContentItem GetContentItem(string fullFileName, ProjectBase project, bool createEvenIfProjectTypeNotSupported)
        {
            var contentDirectory = GlueState.ContentDirectory;

            var relativeToContent = FileManager.MakeRelative(fullFileName, contentDirectory);

            string extension = FileManager.GetExtension(fullFileName);

            ContentItem contentItem = null;

            if (extension == "mp3" || extension == "ogg")
            {
                contentItem = ContentItem.CreateMp3Build();
            }
            else if(extension == "wma")
            {
                contentItem = ContentItem.CreateWmaBuild();
            }
            else if (extension == "wav")
            {
                contentItem = ContentItem.CreateWavBuild();
            }
            else if (extension == "png")
            {
                contentItem = ContentItem.CreateTextureBuild();
            }
            else if (extension == "fx")
            {
                contentItem = ContentItem.CreateEffectBuild();
            }

            string platform = GetPipelinePlatformNameFor(project);

            if (platform == null && createEvenIfProjectTypeNotSupported == false)
            {
                contentItem = null;
            }
            else if (contentItem != null)
            {
                contentItem.Platform = platform;
            }


            if (contentItem != null)
            {
                string projectDirectory = GlueState.CurrentGlueProjectDirectory;
                // The user may have multiple monogame projects synced. If so we need to build to different
                // folders so that one platform doesn't override the other:
                string builtXnbRoot = FileManager.RemoveDotDotSlash($"{projectDirectory}../BuiltXnbs/{contentItem.Platform}/");
                contentItem.BuildFileName = fullFileName;

                // remove the trailing slash:
                contentItem.OutputDirectory = builtXnbRoot;
                contentItem.OutputFileNoExtension = FileManager.RemoveExtension(relativeToContent);

                contentItem.IntermediateDirectory = builtXnbRoot + "obj/" +
                    FileManager.RemoveExtension(relativeToContent);
            }
            return contentItem;
        }

        private static string GetPipelinePlatformNameFor(ProjectBase project)
        {
            string platform = null;
            // According to docs, the following are available:
            // Windows
            // iOS
            // Android
            // DesktopGL
            // MacOSX
            // WindowsStoreApp
            // NativeClient
            // PlayStation4
            // WindowsPhone8
            // RaspberryPi
            // PSVita
            // XboxOne
            // Switch
            if (project is DesktopGlProject)
            {
                platform = "DesktopGL";
            }
            else if (project is AndroidProject)
            {
                platform = "Android";
            }
            else if (project is UwpProject)
            {
                platform = "WindowsStoreApp";
            }

            return platform;
        }

        private static string GetDestinationDirectory(string fullFileName, ProjectBase project)
        {
            string platform = GetPipelinePlatformNameFor(project);

            string contentDirectory = GlueState.ContentDirectory;
            string projectDirectory = GlueState.CurrentGlueProjectDirectory;
            string destinationDirectory = FileManager.GetDirectory(fullFileName);
            destinationDirectory = FileManager.MakeRelative(destinationDirectory, contentDirectory);
            destinationDirectory = FileManager.RemoveDotDotSlash($"{projectDirectory}../BuiltXnbs/{platform}/{destinationDirectory}");

            return destinationDirectory;
        }

        private void TryAddXnbReferencesAndBuild(ReferencedFileSave referencedFile, VisualStudioProject project, bool save)
        {
            var fullFileName = GlueCommands.FileCommands.GetFullFileName(referencedFile);

            TryAddXnbReferencesAndBuild(fullFileName, project, save);

        }

        public void TryAddXnbReferencesAndBuild(FilePath rfsFilePath, VisualStudioProject project, bool saveProjectAfterAdd, bool rebuild = false)
        {
            //////////////EARLY OUT////////////////////////
            if(!commandLineBuildExe.Exists())
            {
                GlueCommands.PrintError($"Could not find Monogame Builder Tool. Looking for {commandLineBuildExe.FullPath}.\n\nTry installing MonoGame");

                var viewModel = new DelegateBasedErrorViewModel();
                viewModel.UniqueId = "Missing Monogame Builder for " + rfsFilePath.Standardized;
                viewModel.Details = 
                    $"Could not build {rfsFilePath.FullPath} because the Monogame Builder Tool could not be found at {commandLineBuildExe.FullPath}";
                // double click command?
                viewModel.IfIsFixedDelegate = () =>
                {
                    return rfsFilePath.Exists() == false ||
                        commandLineBuildExe.Exists() == true ||
                        GlueCommands.GluxCommands.GetReferencedFileSaveFromFile(rfsFilePath.FullPath) == null;
                };

                EditorObjects.IoC.Container.Get<GlueErrorManager>().Add(viewModel);

                return;
            }
            ////////////END EARLY OUT//////////////////////


            var contentDirectory = GlueState.ContentDirectory;

            var relativeToContent = FileManager.MakeRelative(rfsFilePath.FullPath, contentDirectory);

            ContentItem contentItem;
            contentItem = GetContentItem(rfsFilePath.FullPath, project, createEvenIfProjectTypeNotSupported: false);

            string destinationDirectory = GetDestinationDirectory(rfsFilePath.FullPath, project);

            // The monogame content builder seems to be doing an incremental build - 
            // it's being told to do so through the command line params, and it's not replacing
            // files unless they're actually built, but it's SLOW!!! We can put our own check here
            // and make it much faster
            if (contentItem != null)
            {
                TaskManager.Self.Add(() =>
                {
                    // If the user closes the project while the startup is happening, just skip the task - no need to build
                    if (GlueState.CurrentGlueProject != null)
                    {

                        if (contentItem.GetIfNeedsBuild(destinationDirectory) || rebuild)
                        {
                            PerformBuild(contentItem, rebuild);
                        }

                        string relativeToAddNoExtension =
                            FileManager.RemoveExtension(relativeToContent);

                        string absoluteToAddNoExtension = destinationDirectory +
                            FileManager.RemovePath(FileManager.RemoveExtension(relativeToContent));

                        foreach (var extension in contentItem.GetBuiltExtensions())
                        {
                            AddFileToProject(project,
                                absoluteToAddNoExtension + "." + extension,
                                relativeToAddNoExtension + "." + extension,
                                saveProjectAfterAdd);

                        }
                    }
                },
                "Building MonoGame Content " + rfsFilePath);
            }
        }

        private static void PerformBuild(ContentItem contentItem, bool rebuild = false)
        {
            string contentDirectory = GlueState.ContentDirectory;
            string workingDirectory = contentDirectory;

            string commandLine = contentItem.GenerateCommandLine(rebuild);
            var process = new Process();

            process.StartInfo.Arguments = commandLine;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.FileName = commandLineBuildExe.FullPath;
            process.StartInfo.WorkingDirectory = contentDirectory;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;

            process.StartInfo.CreateNoWindow = true;

            process.Start();

            GlueCommands.PrintOutput($"Building: {commandLineBuildExe} {commandLine}");

            while (process.HasExited == false)
            {
                System.Threading.Thread.Sleep(100);

                while (!process.StandardOutput.EndOfStream)
                {
                    var line = process.StandardOutput.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        GlueCommands.PrintOutput(line);
                    }
                }
            }

            bool builtCorrectly = process.ExitCode == 0;

            string str;
            while ((str = process.StandardOutput.ReadLine()) != null)
            {
                // Currently the content pipeline prints errors as normal output instead of error.
                // We can look at the error code to see if it's an error or not.
                if(builtCorrectly)
                {
                    GlueCommands.PrintOutput(str);
                }
                else
                {
                    GlueCommands.PrintError(str);
                }
            }

            while ((str = process.StandardError.ReadLine()) != null)
            {
                GlueCommands.PrintError(str);
            }




        }


        public static bool IsBuiltByContentPipeline(ReferencedFileSave file, bool forcePngsToContentPipeline)
        {
            var ati = file.GetAssetTypeInfo();

            return IsBuiltByContentPipeline(file.Name, file.UseContentPipeline || ati?.MustBeAddedToContentPipeline == true, forcePngsToContentPipeline);
        }

        private static bool IsBuiltByContentPipeline(string fileName, bool rfsUseContentPipeline, bool forcePngsToContentPipeline)
        {
            string extension = FileManager.GetExtension(fileName);

            bool isRequired = 
                extension == "wma" ||

                // OGG and WAV no longer force content pipeline now that we support other audio engines
                // like NAudio
                //extension == "ogg" ||
                //extension == "wav" ||
                // extension == "mp3" ||

                extension == "fx";

            if (isRequired)
            {
                return true;
            }
            else if(rfsUseContentPipeline && 
                (extension == "ogg" ||
                 extension == "wav" ||
                 extension == "mp3"))
            {
                return true;
            }
            else if (rfsUseContentPipeline || forcePngsToContentPipeline)
            {
                return extension == "png";
            }
            return false;
        }

        private void AddFileToProject(VisualStudioProject project, string absoluteFile, string link, bool saveProjectAfterAdd)
        {
            //project.AddContentBuildItem(file, SyncedProjectRelativeType.Linked, false);


            var item = project.GetItem(absoluteFile, true);
            if (item == null)
            {
                item = project.AddContentBuildItem(absoluteFile, SyncedProjectRelativeType.Linked, false);

                link = "Content\\" + link;

                if (project is AndroidProject)
                {
                    link = "Assets\\" + link;
                }
                link = project.ProcessLink(link);
                item.SetLink(link.Replace("/", "\\"));

                if(saveProjectAfterAdd)
                {
                    GlueCommands.TryMultipleTimes(project.Save, 5);
                }
            }

        }
    }
}
