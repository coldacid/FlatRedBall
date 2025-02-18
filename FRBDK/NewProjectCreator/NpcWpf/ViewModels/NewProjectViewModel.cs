﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Npc.ViewModels
{
    public class NewProjectViewModel : ViewModel
    {
        char[] invalidNamespaceCharacters = new char[]
        {
                '~', '`', '!', '@', '#', '$', '%', '^', '&', '*',
                '(', ')', '-', '=', '+', ';', '\'', ':', '"', '<',
                ',', '>', '.', '/', '\\', '?', '[', '{', ']', '}',
                '|',
        // Spaces are handled separately
        //    ' ' 
        };

        public bool UseLocalCopy
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(UseLocalCopy))]
        public bool IsOnlineTemplatesChecked => UseLocalCopy == false;

        public bool OpenSlnFolderAfterCreation
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string ProjectLocation
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ProjectName
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsDifferentNamespaceChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string DifferentNamespace
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsCreateProjectDirectoryChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsDifferentNamespaceChecked))]
        public Visibility DifferentNamespaceTextBoxVisibility
        {
            get => IsDifferentNamespaceChecked ? Visibility.Visible : Visibility.Collapsed;
        }

        [DependsOn(nameof(ProjectLocation))]
        [DependsOn(nameof(ProjectName))]
        public string CombinedProjectDirectory
        {
            get
            {
                if (!ProjectLocation.EndsWith("\\") && !ProjectLocation.EndsWith("/"))
                {
                    return ProjectLocation + "\\" + ProjectName;
                }
                else
                {
                    return ProjectLocation + ProjectName;

                }
            }
        }

        [DependsOn(nameof(IsCreateProjectDirectoryChecked))]
        [DependsOn(nameof(CombinedProjectDirectory))]
        [DependsOn(nameof(ProjectLocation))]
        public string FinalDirectory
        {
            get
            {
                if(IsCreateProjectDirectoryChecked)
                {
                    return CombinedProjectDirectory;
                }
                else
                {
                    return ProjectLocation;
                }
            }
        }

        [DependsOn(nameof(FinalDirectory))]
        public SolidColorBrush FinalDirectoryForeground
        {
            get
            {
                if(System.IO.Directory.Exists(FinalDirectory))
                {
                    return Brushes.Red;
                }
                else
                {
                    return Brushes.Black;
                }
            }
        }

        public ObservableCollection<PlatformProjectInfo> AvailableProjects
        {
            get;
            private set;
        } = new ObservableCollection<PlatformProjectInfo>();

        public PlatformProjectInfo SelectedProject
        {
            get => Get<PlatformProjectInfo>();
            set => Set(value);
        }

        public NewProjectViewModel()
        {
            ProjectName = "MyProject";
            UseLocalCopy = false;
        }

        internal string GetWhyIsntValid()
        {
            string whyIsntValid = null;
            if (IsDifferentNamespaceChecked)
            {
                if (string.IsNullOrEmpty(DifferentNamespace))
                {
                    whyIsntValid = "You must enter a non-empty namespace if using a different namespace";
                }
                else if (char.IsDigit(DifferentNamespace[0]))
                {
                    whyIsntValid = "Namespace can't start with a number.";
                }
                else if (DifferentNamespace.Contains(" "))
                {
                    whyIsntValid = "The namespace can't have any spaces.";
                }
                else if (DifferentNamespace.IndexOfAny(invalidNamespaceCharacters) != -1)
                {
                    whyIsntValid = "The namespace can't contain invalid character " + DifferentNamespace[DifferentNamespace.IndexOfAny(invalidNamespaceCharacters)];
                }
            }

            if (string.IsNullOrEmpty(whyIsntValid))
            {
                whyIsntValid = ProjectCreationHelper.GetWhyProjectNameIsntValid(ProjectName);
            }


            return whyIsntValid;
        }
    }
}
