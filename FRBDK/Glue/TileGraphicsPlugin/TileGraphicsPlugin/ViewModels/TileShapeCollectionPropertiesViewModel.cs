﻿using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TileGraphicsPlugin.ViewModels
{
    #region Enumerations

    public enum CollisionCreationOptions
    {
        Empty = 0,
        FillCompletely,
        BorderOutline,
        FromProperties,
        FromType,
        FromLayer
    }

    public enum BorderOutlineType
    {
        NumberOfTiles,
        InnerSize
    }

    #endregion

    public class TileShapeCollectionPropertiesViewModel : PropertyListContainerViewModel
    {
        #region Empty

        [SyncedProperty]
        [DefaultValue((int)CollisionCreationOptions.Empty)]
        public CollisionCreationOptions CollisionCreationOptions
        {
            get => (CollisionCreationOptions)Get<int>(); 
            set => SetAndPersist((int)value); 
        }

        [DependsOn(nameof(CollisionCreationOptions))]
        public bool IsEmptyChecked
        {
            get => CollisionCreationOptions == CollisionCreationOptions.Empty;
            set
            {
                if (value)
                {
                    CollisionCreationOptions = CollisionCreationOptions.Empty;
                }
            }
        }

        #endregion

        #region Fill

        [DependsOn(nameof(CollisionCreationOptions))]
        public bool IsFillCompletelyChecked
        {
            get => CollisionCreationOptions == CollisionCreationOptions.FillCompletely;
            set
            {
                if (value)
                {
                    CollisionCreationOptions = CollisionCreationOptions.FillCompletely;
                }
            }
        }

        [DependsOn(nameof(CollisionCreationOptions))]
        public Visibility FillDimensionsVisibility => CollisionCreationOptions == CollisionCreationOptions.FillCompletely ?
                  Visibility.Visible :
                  Visibility.Collapsed;

        [SyncedProperty]
        [DefaultValue(16.0f)]
        public float CollisionTileSize
        {
            get => Get<float>();
            set => SetAndPersist(value);
        }

        [SyncedProperty]
        public float CollisionFillLeft
        {
            get => Get<float>();
            set => SetAndPersist(value);
        }

        [SyncedProperty]
        public float CollisionFillTop
        {
            get => Get<float>();
            set => SetAndPersist(value);
        }

        [SyncedProperty]
        [DefaultValue(32)]
        public int CollisionFillWidth
        {
            get => Get<int>();
            set => SetAndPersist(value);
        }

        [SyncedProperty]
        [DefaultValue(1)]
        public int CollisionFillHeight
        {
            get => Get<int>();
            set => SetAndPersist(value);
        }

        #endregion

        #region Border Outline

        [DependsOn(nameof(CollisionCreationOptions))]
        public bool IsBorderChecked
        {
            get => CollisionCreationOptions == CollisionCreationOptions.BorderOutline; 
            set
            {
                if (value)
                {
                    CollisionCreationOptions = CollisionCreationOptions.BorderOutline;
                }
            }
        }

        [DependsOn(nameof(CollisionCreationOptions))]
        public Visibility BorderOutlineVisibility
        {
            get
            {
                return CollisionCreationOptions == CollisionCreationOptions.BorderOutline 
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        // outline uses fill properties if using # of tiles

        [SyncedProperty]
        public BorderOutlineType BorderOutlineType
        {
            get => (BorderOutlineType)Get<int>();
            set => SetAndPersist((int)value);
        }

        [DependsOn(nameof(BorderOutlineType))]
        public bool IsNumberOfTilesBorderOutlineChecked
        {
            get => BorderOutlineType == BorderOutlineType.NumberOfTiles;
            set
            {
                if(value)
                {
                    BorderOutlineType = BorderOutlineType.NumberOfTiles;
                }
            }
        }

        [DependsOn(nameof(BorderOutlineType))]
        public bool IsInnerSizeBorderOutlineChecked
        {
            get => BorderOutlineType == BorderOutlineType.InnerSize;
            set
            {
                if(value)
                {
                    BorderOutlineType = BorderOutlineType.InnerSize;
                }
            }
        }

        [DependsOn(nameof(IsNumberOfTilesBorderOutlineChecked))]
        public Visibility BorderNumberOfTilesUiVisibility => IsNumberOfTilesBorderOutlineChecked.ToVisibility();

        [DependsOn(nameof(IsInnerSizeBorderOutlineChecked))]
        public Visibility BorderInnerSizeUiVisibility => IsInnerSizeBorderOutlineChecked.ToVisibility();

        [SyncedProperty]
        [DefaultValue(800f)]
        public float InnerSizeWidth
        {
            get => Get<float>();
            set => SetAndPersist(value);
        }

        [SyncedProperty]
        [DefaultValue(600f)]
        public float InnerSizeHeight
        {
            get => Get<float>();
            set => SetAndPersist(value);
        }
        #endregion

        #region From Properties

        [DependsOn(nameof(CollisionCreationOptions))]
        public bool IsFromPropertiesChecked
        {
            get => CollisionCreationOptions == CollisionCreationOptions.FromProperties; 
            set
            {
                if (value)
                {
                    CollisionCreationOptions = CollisionCreationOptions.FromProperties;
                }
            }
        }

        [DependsOn(nameof(CollisionCreationOptions))]
        public Visibility FromPropertiesVisibility => CollisionCreationOptions == CollisionCreationOptions.FromProperties ?
                      Visibility.Visible :
                      Visibility.Collapsed;

        // for now a single string, eventually a list?
        [SyncedProperty]
        public string CollisionPropertyName
        {
            get => Get<string>(); 
            set => SetAndPersist(value); 
        }

        #endregion

        #region From Type

        [DependsOn(nameof(CollisionCreationOptions))]
        public bool IsFromTypeChecked
        {
            get => CollisionCreationOptions == CollisionCreationOptions.FromType; 
            set
            {
                if (value)
                {
                    CollisionCreationOptions = CollisionCreationOptions.FromType;
                }
            }
        }

        [DependsOn(nameof(CollisionCreationOptions))]
        public Visibility FromTypeVisibility => CollisionCreationOptions == CollisionCreationOptions.FromType ?
                    Visibility.Visible :
                    Visibility.Collapsed;

        public ObservableCollection<string> AvailableTypes
        {
            get => Get<ObservableCollection<string>>(); 
            set => Set(value); 
        } 

        #endregion

        #region From Layer

        [DependsOn(nameof(CollisionCreationOptions))]
        public bool IsFromLayerChecked
        {
            get => CollisionCreationOptions == CollisionCreationOptions.FromLayer; 
            set
            {
                if(value)
                {
                    CollisionCreationOptions = CollisionCreationOptions.FromLayer;
                }
            }
        }

        [DependsOn(nameof(CollisionCreationOptions))]
        public Visibility FromLayerVisibility => CollisionCreationOptions == CollisionCreationOptions.FromLayer ?
                  Visibility.Visible :
                  Visibility.Collapsed;

        [SyncedProperty]
        public string CollisionLayerName
        {
            get => Get<string>();
            set => SetAndPersist(value); 
        }

        [SyncedProperty]
        public string CollisionLayerTileType
        {
            get => Get<string>(); 
            set => SetAndPersist(value); 
        }


        #endregion

        [SyncedProperty]
        public bool IsCollisionMerged
        {
            get => Get<bool>(); 
            set => SetAndPersist(value); 
        }

        public ObservableCollection<string> TmxObjectNames
        {
            get => Get<ObservableCollection<string>>(); 
            set => Set(value);
        }

        [SyncedProperty]
        public string SourceTmxName
        {
            get => Get<string>(); 
            set => SetAndPersist(value); 
        }


        [SyncedProperty]
        public string CollisionTileTypeName
        {
            get => Get<string>(); 
            set => SetAndPersist(value); 
        }

        [SyncedProperty]
        public bool RemoveTilesAfterCreatingCollision
        {
            get => Get<bool>();
            set => SetAndPersist(value);
        }

        public bool IsEntireViewEnabled
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        public TileShapeCollectionPropertiesViewModel()
        {
            AvailableTypes = new ObservableCollection<string>();

        }
    }
}
