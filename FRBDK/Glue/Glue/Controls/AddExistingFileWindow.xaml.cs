﻿using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.ViewModels;
using FlatRedBall.IO;
using Glue;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ToolsUtilitiesStandard.Network;

namespace FlatRedBall.Glue.Controls
{
    /// <summary>
    /// Interaction logic for AddExistingFileControl.xaml
    /// </summary>
    public partial class AddExistingFileWindow : Window
    {
        #region Fields/Properties

        private AddExistingFileViewModel ViewModel
        {
            get
            {
                return DataContext as AddExistingFileViewModel;
            }
        }

        #endregion

        public AddExistingFileWindow()
        {
            InitializeComponent();

            Left = MainGlueWindow.MousePosition.X - this.Width / 2;
            Top = MainGlueWindow.MousePosition.Y - Height / 2;

            SearchTextBox.Focus();
        }

        private void HandleBrowseClicked(object sender, RoutedEventArgs e)
        {
            // add externally built file, add external file, add built file
            if (ProjectManager.StatusCheck() == ProjectManager.CheckResult.Passed)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();

                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var element = GlueState.Self.CurrentElement;
                    string directoryOfTreeNode = EditorLogic.CurrentTreeNode.GetRelativePath();

                    ViewModel.Files.Clear();
                    ViewModel.Files.AddRange(openFileDialog.FileNames.Select(item => new FilePath(item)));
                    this.DialogResult = true;
                }
            }
        }

        private async void OkButtonClicked(object sender, RoutedEventArgs e)
        {
            if(ViewModel.FileLocationType == FileLocationType.Local)
            {
                DoAcceptLogic();
            }
            else
            {
                await HandleRemoteDownload();
            }
        }

        #region Download

        private async Task HandleRemoteDownload()
        {

            FilePath destinationFolder;
            var currentElement = GlueState.Self.CurrentElement;
            if(currentElement != null)
            {
                destinationFolder = GlueCommands.Self.FileCommands.GetContentFolder(currentElement);
            }
            else
            {
                destinationFolder = GlueCommands.Self.FileCommands.GetGlobalContentFolder();
            }

            FilePath destination = destinationFolder + FileManager.RemovePath(ViewModel.DownloadUrl);

            var shouldDownload = true;
            if(destination.Exists())
            {
                DialogResult result =
                    System.Windows.Forms.MessageBox.Show("Do you want to download this file? It will ovewrite:\n" +
                    destination.FullPath,
                    "Download and Ovewrite?",
                    MessageBoxButtons.YesNo);

                shouldDownload = result == System.Windows.Forms.DialogResult.Yes;
            }

            var innerVm = new IndividualFileAddDownloadViewModel();
            innerVm.Url = ViewModel.DownloadUrl;
            ViewModel.DownloadedFilesList.Add(innerVm);
            Action<long?, long> progressChanged = (a, b) => { innerVm.TotalLength = a; innerVm.DownloadedBytes = b; };

            using var _httpClient = new HttpClient { Timeout = TimeSpan.FromDays(1), };
            var downloadResponse = await NetworkManager.Self.DownloadWithProgress(
                _httpClient, ViewModel.DownloadUrl, destination.FullPath, progressChanged);

            innerVm.DownloadResponse = downloadResponse;
            if(downloadResponse.Succeeded)
            {
                await DownloadReferencedFilesRecursively(_httpClient, destination, ViewModel.DownloadUrl);
            }

            if(downloadResponse.Succeeded == false || ViewModel.DownloadedFilesList.Any(item => item.DownloadResponse?.Succeeded == false))
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox("Error downloading files");
            }
            else
            {
                ViewModel.Files.Clear();
                ViewModel.Files.Add(destination);
                this.DialogResult = true;
            }


        }

        private async Task DownloadReferencedFilesRecursively(HttpClient httpClient, FilePath destination, string urlForParentFile)
        {
            var referencedFiles = FileReferenceManager.Self.GetFilesReferencedBy(destination, EditorObjects.Parsing.TopLevelOrRecursive.TopLevel);

            var destinationFolder = destination.GetDirectoryContainingThis();

            foreach(var file in referencedFiles)
            {
                var relative = FileManager.MakeRelative(file.FullPath, destinationFolder.FullPath);
                var fileToDownload = FileManager.GetDirectory(urlForParentFile) + relative;

                var innerVm = new IndividualFileAddDownloadViewModel();
                innerVm.Url = fileToDownload;
                ViewModel.DownloadedFilesList.Add(innerVm);
                Action<long?, long> progressChanged = (a, b) => { innerVm.TotalLength = a; innerVm.DownloadedBytes = b; };

                var innerDownloadResult = await NetworkManager.Self.DownloadWithProgress(
                    httpClient, fileToDownload, file.FullPath, progressChanged);
                innerVm.DownloadResponse = innerDownloadResult;
                if(innerDownloadResult.Succeeded)
                {
                    await DownloadReferencedFilesRecursively(httpClient, file, fileToDownload);
                }
            }
        }


        #endregion

        private void DoAcceptLogic()
        {
            var selectedItem = ViewModel.SelectedListBoxItem;

            if (!string.IsNullOrEmpty(selectedItem))
            {
                ViewModel.Files.Clear();
                ViewModel.Files.Add(ViewModel.ContentFolder + ViewModel.SelectedListBoxItem);
                this.DialogResult = true;
            }
            else
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox("Select a file or click the Browse button");
            }
        }

        private void CancelButtonClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.Enter:
                    DoAcceptLogic();
                    break;
                case Key.Escape:
                    this.DialogResult = false;
                    break;
            }
        }

        private void TextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Down:
                    e.Handled = true;
                    {
                        var index = ViewModel.FilteredFileList.IndexOf(ViewModel.SelectedListBoxItem);

                        if (index < ViewModel.FilteredFileList.Count - 1)
                        {
                            ViewModel.SelectedListBoxItem = ViewModel.FilteredFileList[index + 1];
                        }
                    }
                    break;
                case Key.Up:
                    e.Handled = true;
                    {
                        var index = ViewModel.FilteredFileList.IndexOf(ViewModel.SelectedListBoxItem);

                        if (index > 0)
                        {
                            ViewModel.SelectedListBoxItem = ViewModel.FilteredFileList[index - 1];
                        }
                    }
                    break;
            }
        }

        private void ListBoxInstance_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBoxInstance.ScrollIntoView(ViewModel.SelectedListBoxItem);

        }
    }
}
