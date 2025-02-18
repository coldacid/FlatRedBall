﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Utilities;

namespace FlatRedBall.Glue.ViewModels
{
    #region Selected Item Wrapper class

    public class SelectedItemWrapper
    {
        public object BackingObject { get; set; }

        public event Action StrongSelect;

        public string MainText
        {
            get
            {
                if (BackingObject is string) return BackingObject.ToString();
                else if (BackingObject is AssetTypeInfo ati) return ati.FriendlyName;
                //else if (BackingObject is EntitySave entity) return entity.Name.Substring("Entities/".Length);
                else if (BackingObject is EntitySave entity) return entity.Name;
                else if (BackingObject is ReferencedFileSave rfs) return rfs.Name;
                else return null;
            }
        }

        public string SubText
        {
            get
            {
                if (BackingObject is AssetTypeInfo ati) return $"({ati.QualifiedRuntimeTypeName.QualifiedType})";
                //else if (BackingObject is EntitySave entity) return entity.Name.Substring("Entities/".Length);
                //else if (BackingObject is ReferencedFileSave rfs) return rfs.Name;
                else return null;
            }
        }

        public Visibility SubtextVisibility => string.IsNullOrEmpty(SubText)
            ? Visibility.Collapsed
            : Visibility.Visible;

        public ICommand HandleStrongSelect { get; private set; }

        public SelectedItemWrapper()
        {
            HandleStrongSelect = new Command(
                () => StrongSelect());
        }

        public override string ToString()
        {
            return MainText;
        }
    }

    #endregion

    public class AddObjectViewModel : ViewModel
    {
        #region SourceType

        public SourceType SourceType 
        {
            get => Get<SourceType>();
            set
            {
                if (base.SetWithoutNotifying(value))
                {
                    ForceRefreshToSourceType();
                }
            }
        }

        public void ForceRefreshToSourceType()
        {
            RefreshAllSelectedItems();

            RefreshFilteredItems();

            NotifyPropertyChanged();

            SelectIfNoSelection();
        }

        public bool IsFlatRedBallType
        {
            get => SourceType == SourceType.FlatRedBallType;
            set
            {
                if(value)
                {
                    SourceType = SourceType.FlatRedBallType;
                }
            }
        }

        public bool IsEntityType
        {
            get => SourceType == SourceType.Entity;
            set
            {
                if (value)
                {
                    SourceType = SourceType.Entity;
                }
            }
        }

        public bool IsFromFileType
        {
            get => SourceType == SourceType.File;
            set
            {
                if (value)
                {
                    SourceType = SourceType.File;
                }
            }
        }

        #endregion

        #region Filtering

        public string FilterText
        {
            get => Get<string>();
            set
            {
                if (Set(value))
                {
                    RefreshFilteredItems();
                    SelectIfNoSelection();
                }
            }
        }

        List<T> Filter<T>(IEnumerable<T> allitems, Func<T, string> getStringForObject, string filterText)
        {
            var filterTextToLower = filterText?.ToLowerInvariant();
            List<T> filteredOrdered = allitems
                .Where(item => getStringForObject(item)?.ToLowerInvariant().Contains(filterTextToLower) == true)
                // first show exact matches
                .OrderBy(item => getStringForObject(item)?.ToLowerInvariant() != filterTextToLower)
                // then items that start with what was typed
                .ThenBy(item => getStringForObject(item)?.ToLowerInvariant().StartsWith(filterTextToLower) != true)
                .ToList();
            return filteredOrdered;
            // 
        }


        #endregion

        #region Selected Item

        public SelectedItemWrapper SelectedItem
        {
            get => Get<SelectedItemWrapper>();
            set
            {
                if (Set(value))
                {
                    SetDefaultObjectName();
                }
            }
        }

        public AssetTypeInfo SelectedAti
        {
            get => SelectedItem?.BackingObject as AssetTypeInfo;
            set => SelectedItem = AllSelectedItemWrappers.FirstOrDefault(item => item.BackingObject == value) ??
                MakeWrapper(value);
        }

        public EntitySave SelectedEntitySave
        {
            get => SelectedItem?.BackingObject as EntitySave;
            set => SelectedItem = AllSelectedItemWrappers.FirstOrDefault(item => item.BackingObject == value) ??
                MakeWrapper(value);
        }

        [DependsOn(nameof(SelectedItem))]
        public string SourceClassType
        {
            get => SelectedItem?.ToString();
            set
            {
                SelectedItem = AllSelectedItemWrappers.FirstOrDefault(item => item.BackingObject == value) ??
                    MakeWrapper(value);
            }
        }

        public ReferencedFileSave SourceFile
        {
            get => SelectedItem?.BackingObject as ReferencedFileSave;
            set => SelectedItem = AllSelectedItemWrappers.FirstOrDefault(item => item.BackingObject == value) ??
                MakeWrapper(value);
        }

        public bool IsTypePredetermined
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsTypePredetermined))]
        public bool IsSelectionEnabled => !IsTypePredetermined;


        #endregion

        #region Object Name

        private void SetDefaultObjectName()
        {
            string nameToAssign = null;

            if (SelectedItem != null)
            {
                if (SourceType == SourceType.File)
                {
                    if(!string.IsNullOrWhiteSpace(SourceNameInFile))
                    {
                        nameToAssign = GetDefaultObjectInFileName();
                    }
                }
                else
                {
                    var classType = SourceClassType;

                    if(classType?.Contains("(") == true)
                    {
                        var first = classType.IndexOf("(");
                        var last = classType.IndexOf(")");

                        if(first > -1 && last > -1 && first < last)
                        {
                            classType = classType.Substring(0, first);
                        }
                    }

                    if (classType?.Contains(".") == true)
                    {
                        // un-qualify if it's something like "FlatRedBall.Sprite"
                        var lastIndex = classType.LastIndexOf(".");
                        classType = classType.Substring(lastIndex + 1);
                    }
                    nameToAssign = classType + "Instance";
                    if (nameToAssign.Contains("/") || nameToAssign.Contains("\\"))
                    {
                        nameToAssign = FileManager.RemovePath(nameToAssign);
                    }

                    nameToAssign = nameToAssign.Replace("<T>", "");
                    nameToAssign = nameToAssign.Replace(" ", "");
                }
            }

            if (!string.IsNullOrEmpty(nameToAssign))
            {
                if(EffectiveElement == null)
                {
                    throw new InvalidOperationException("The AddObjectViewModel must either have its ForcedElementToAddTo set, or there must be a selected element.");
                }
                // We need to make sure this is a unique name.
                nameToAssign = StringFunctions.MakeStringUnique(nameToAssign, EffectiveElement.AllNamedObjects);

                ObjectName = nameToAssign;
            }
        }

        public string ObjectName
        {
            get => Get<string>();
            set => Set(value);
        }

        #endregion

        /// <summary>
        /// The element to add to. If not set, the current element is used. This must be set first, as
        /// otherh properties (like setting the name or the SourceClassType) may adjust the name of the element.
        /// </summary>
        public IElement ForcedElementToAddTo
        {
            get => Get<IElement>();
            set => Set(value);
        }

        public IElement EffectiveElement => ForcedElementToAddTo ?? GlueState.Self.CurrentElement;

        public string SourceNameInFile
        {
            get => Get<string>();
            set
            {
                if (Set(value))
                {
                    SetDefaultObjectName();
                }
            }
        }
        public string SourceClassGenericType 
        {
            get => Get<string>();
            set => Set(value);
        }

        public List<string> AvailableListTypes { get; private set; } =
            new List<string>();

        [DependsOn(nameof(SelectedItem))]
        public List<string> AvailableFileSourceNames
        {
            get 
            {
                if((SelectedItem as SelectedItemWrapper)?.BackingObject is ReferencedFileSave selectedRfs)
                {
                    List<string> availableObjects = new List<string>();

                    AvailableNameablesStringConverter.FillListWithAvailableObjects(SourceClassType, availableObjects);

                    return availableObjects;
                }
                return new List<string>();
            }
        }

        public List<AssetTypeInfo> FlatRedBallAndCustomTypes
        { 
            get => Get<List<AssetTypeInfo>>();
            set => Set(value);
        }
        public List<EntitySave> AvailableEntities 
        { 
            get => Get<List<EntitySave>>();
            set
            {
                Set(value);
            }
        }
        
        public List<ReferencedFileSave> AvailableFiles 
        {
            get => Get<List<ReferencedFileSave>>();
            set
            {
                Set(value);
            }
        }



        [DependsOn(nameof(IsGenericType))]
        public Visibility ListTypeVisibility
        {
            get
            {
                if(IsGenericType)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        [DependsOn(nameof(SourceType))]
        public Visibility SourceNameVisibility
        {
            get
            {
                if(SourceType == SourceType.File)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        [DependsOn(nameof(SourceType))]
        [DependsOn(nameof(SourceClassType))]
        private bool IsGenericType
        {
            get
            {
                bool usesGenericTypes = this.SourceType == SaveClasses.SourceType.FlatRedBallType &&
                    (SourceClassType == "PositionedObjectList (Generic)" || SourceClassType == "PositionedObjectList<T>" || SourceClassType == "FlatRedBall.Math.PositionedObjectList<T>");
                return usesGenericTypes;
            }
        }

        List<SelectedItemWrapper> AllSelectedItemWrappers { get; set; } = new List<SelectedItemWrapper>();

        public event Action StrongSelect;

        SelectedItemWrapper MakeWrapper(object backingObject)
        {
            var toReturn = new SelectedItemWrapper();

            toReturn.BackingObject = backingObject;
            toReturn.StrongSelect += () => StrongSelect?.Invoke() ;

            return toReturn;
        }

        public void RefreshAllSelectedItems()
        {
            AllSelectedItemWrappers.Clear();
            switch (SourceType)
            {
                case SourceType.FlatRedBallType:
                    AllSelectedItemWrappers.AddRange(FlatRedBallAndCustomTypes.Select(item => MakeWrapper(item)));
                    break;
                case SourceType.Entity:
                    AllSelectedItemWrappers.AddRange(AvailableEntities.Select(item => MakeWrapper(item )));
                    break;
                case SourceType.File:
                    AllSelectedItemWrappers.AddRange(AvailableFiles.Select(item => MakeWrapper(item )));
                    break;
            }
        }

        void SelectIfNoSelection()
        {
            if(SelectedItem == null && FilteredItems.Count > 0)
            {
                SelectedItem = FilteredItems[0];
            }
        }

        ObservableCollection<SelectedItemWrapper> filteredItems = new ObservableCollection<SelectedItemWrapper>();
        [DependsOn(nameof(FilterText))]
        [DependsOn(nameof(SourceType))]
        public ObservableCollection<SelectedItemWrapper> FilteredItems
        {
            get => filteredItems;
        }

        public void RefreshFilteredItems()
        {
            filteredItems.Clear();
            if (string.IsNullOrWhiteSpace(FilterText))
            {
                foreach (var item in AllSelectedItemWrappers)
                {
                    filteredItems.Add(item);
                }
            }
            else
            {
                var toAdd = Filter(AllSelectedItemWrappers, (item) => item.ToString(), FilterText);

                foreach (var item in toAdd)
                {
                    filteredItems.Add(item);
                }
            }
        }


        public AddObjectViewModel()
        {
            FlatRedBallAndCustomTypes = new List<AssetTypeInfo>();
            AvailableEntities = new List<EntitySave>();
            AvailableFiles = new List<ReferencedFileSave>();
        }

        private string GetDefaultObjectInFileName()
        {
            string newName;
            var spaceParen = SourceNameInFile.IndexOf(" (");

            if (spaceParen != -1)
            {
                newName = SourceNameInFile.Substring(0, spaceParen);
            }
            else
            {
                newName = SourceNameInFile;
            }

            // If the user selected "Entire File" we want to make sure the space doesn't show up:
            newName = newName.Replace(" ", "");

            string throwaway;
            bool isInvalid = NameVerifier.IsNamedObjectNameValid(newName, out throwaway);

            if (!isInvalid)
            {
                // let's get the type:
                var split = SourceNameInFile.Split('(', ')');

                var last = split.LastOrDefault(item => !string.IsNullOrEmpty(item));

                if (last != null)
                {
                    var lastDot = last.LastIndexOf('.');

                    newName = last.Substring(lastDot + 1, last.Length - (lastDot + 1));
                }
            }

            return newName;
        }

    }
}
