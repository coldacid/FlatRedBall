﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.AnimationEditorForms;
using FlatRedBall.IO;
using PreviewProject.IO;
using FlatRedBall.AnimationEditor;
using ToolsUtilities;
using Newtonsoft.Json;
using FlatRedBall.AnimationEditor.Models;
using FilePath = global::ToolsUtilities.FilePath;
using FlatRedBall.AnimationEditorForms.CommandsAndState;

namespace PreviewProject
{
    public partial class Form1 : Form
    {
        #region Fields/Properties

        FlatRedBall.AnimationEditorForms.MainControl mMainControl;

        FilePath SettingsFilePath => System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\AESettings.json";

        AppSettingsModel appSettings;

        ToolStripMenuItem loadRecentToolStripItem;

        #endregion

        public Form1()
        {
            InitializeComponent();

            mMainControl = new FlatRedBall.AnimationEditorForms.MainControl();
            mMainControl.AnimationChainChange += HandleAnimationChainChange;
            // We want to make sure to always have a good animation chain:
            this.Controls.Add(mMainControl);
            mMainControl.Dock = DockStyle.Fill;

            // load the settings
            LoadSettingsFile();

            CreateToolStripMenuItems();

            mMainControl.XnaInitialize += new Action(HandleXnaInitialize);

            ApplicationEvents.Self.AchxLoaded += HandleAchxLoaded;

        }

        private void HandleAchxLoaded(string fileName)
        {
            mMainControl.LoadAnimationChain(fileName);

            appSettings.AddFile(fileName);

            SaveSettingsfile();

            RefreshRecentFiles();

            SetFormTextToLoadedFile();
        }

        private void LoadSettingsFile()
        {
            var settingsFile = SettingsFilePath;
            appSettings = null;
            try
            {
                if(settingsFile.Exists())
                {
                    var contents = System.IO.File.ReadAllText(settingsFile.FullPath);
                    appSettings = JsonConvert.DeserializeObject<AppSettingsModel>(contents);
                }
            }
            catch
            {
                // do nothing?
            }

            if(appSettings == null)
            {
                appSettings = new AppSettingsModel();
            }
        }

        private void SaveSettingsfile()
        {
            var settingsFile = SettingsFilePath;
            var contents = JsonConvert.SerializeObject(appSettings);
            System.IO.File.WriteAllText(settingsFile.FullPath, contents);
        }

        private void ProcessCommandLines(out bool wasAnimationLoaded)
        {
            string[] commandLineArgs = Environment.GetCommandLineArgs();

            wasAnimationLoaded = false;
            if (commandLineArgs.Length == 2)
            {
                mMainControl.LoadAnimationChain(commandLineArgs[1]);
                SetFormTextToLoadedFile();

                wasAnimationLoaded = true;
            }

        }

        private void SetFormTextToLoadedFile()
        {
            this.Text = "AnimationEditor - " + ProjectManager.Self.FileName;
        }

        private void CreateToolStripMenuItems()
        {
            ToolStripMenuItem newToolStripItem = new ToolStripMenuItem("New AnimationChain List", null, HandleNewClick);

            ToolStripMenuItem loadToolStripItem = new ToolStripMenuItem("Load...", null, HandleLoadClick);
            ToolStripMenuItem saveToolStripItem = new ToolStripMenuItem("Save", null, HandleSaveClick);
            ToolStripMenuItem saveAsToolStripItem = new ToolStripMenuItem("Save As...", null, HandleSaveAsClick);
            loadRecentToolStripItem = new ToolStripMenuItem("Load Recent", null);

            RefreshRecentFiles();

            mMainControl.AddToolStripMenuItem(newToolStripItem, "File");
            mMainControl.AddToolStripMenuItem(loadToolStripItem, "File");
            mMainControl.AddToolStripMenuItem(loadRecentToolStripItem, "File");
            mMainControl.AddToolStripMenuItem(saveToolStripItem, "File");
            mMainControl.AddToolStripMenuItem(saveAsToolStripItem, "File");


            var about = new ToolStripMenuItem("About", null, HandleAboutClicked);
            mMainControl.AddToolStripMenuItem(about, "Help");
        }

        private void RefreshRecentFiles()
        {
            loadRecentToolStripItem.DropDownItems.Clear();
            foreach (var file in this.appSettings.RecentFiles)
            {
                var filePath = new FilePath(file);
                ToolStripMenuItem loadSubItem = new ToolStripMenuItem(filePath.FullPath, null, (not, used) =>
                {
                    LoadAnimationFile(file);
                });

                loadRecentToolStripItem.DropDownItems.Add(loadSubItem);
            }
        }

        private void HandleAboutClicked(object sender, EventArgs e)
        {
            AboutWindow window = new AboutWindow();
            window.Show();
        }

        private void HandleNewClick(object sender, EventArgs e)
        {
            mMainControl.AnimationChainList = new FlatRedBall.Content.AnimationChain.AnimationChainListSave();
            ProjectManager.Self.FileName = null;
            HandleSaveAsClick(null, null);
        }

        private void HandleLoadClick(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Animation Chain (*.achx)|*.achx";
            string fileName = null;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                fileName = dialog.FileName;
            }
            LoadAnimationFile(fileName);
        }

        private void LoadAnimationFile(string fileName)
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                ApplicationEvents.Self.CallAchxLoaded(fileName);
            }

        }

        private void HandleSaveClick(object sender, EventArgs e)
        {
            if (mMainControl.AnimationChainList == null)
            {
                MessageBox.Show("No animation file loaded");
            }
            else if (string.IsNullOrEmpty(ProjectManager.Self.FileName))
            {
                HandleSaveAsClick(sender, e);
            }
            else
            {
                mMainControl.SaveCurrentAnimationChain();

                this.Text = "AnimationEditor - " + ProjectManager.Self.FileName;
            }
        }

        private void HandleSaveAsClick(object sender, EventArgs e)
        {
            AchxSaver.Self.InitateSaveProcess(ProjectManager.Self.FileName, mMainControl);
            this.Text = "AnimationEditor - " + ProjectManager.Self.FileName;
        }

        private void HandleAnimationChainChange(object sender, EventArgs e)
        {
            bool autosave = true;

            if(autosave && !string.IsNullOrEmpty(ProjectManager.Self.FileName))
            {
                mMainControl.SaveCurrentAnimationChain();

                this.Text = "AnimationEditor - " + ProjectManager.Self.FileName;

            }
        }

        void HandleXnaInitialize()
        {
            //string fileName =
            //    @"C:\FlatRedBallProjects\Games\Baron\NewBaron\NewBaronContent\Entities\GameScreen\CreepBase\AnimationChains\CrocBloc1\CrocBloc1Animations.achx";

            //mMainControl.LoadAnimationChain(fileName);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            bool wasAnimationLoaded;
            ProcessCommandLines(out wasAnimationLoaded);
            if (!wasAnimationLoaded)
            {
                mMainControl.AnimationChainList = new FlatRedBall.Content.AnimationChain.AnimationChainListSave();
            }
            WireframeManager.Self.RefreshAll();
        }
    }
}
