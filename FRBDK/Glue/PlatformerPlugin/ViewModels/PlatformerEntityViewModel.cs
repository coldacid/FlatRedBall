﻿using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.Specialized;
using System.ComponentModel;

namespace FlatRedBall.PlatformerPlugin.ViewModels
{
    public class PlatformerEntityViewModel : ViewModel
    {
        public ObservableCollection<PlatformerValuesViewModel> PlatformerValues { get; private set; }

        public bool IsPlatformer
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool InheritsFromPlatformer
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsPlatformer))]
        [DependsOn(nameof(InheritsFromPlatformer))]
        public bool IsEffectivelyPlatformer => IsPlatformer || InheritsFromPlatformer;

        [DependsOn(nameof(IsEffectivelyPlatformer))]
        public Visibility PlatformerUiVisibility => IsEffectivelyPlatformer.ToVisibility();

        [DependsOn(nameof(InheritsFromPlatformer))]
        public Visibility InheritanceLabelVisibility => InheritsFromPlatformer.ToVisibility();



        public PlatformerEntityViewModel()
        {
            PlatformerValues = new ObservableCollection<PlatformerValuesViewModel>();

            PlatformerValues.CollectionChanged += HandlePlatformerValuesChanged;
        }

        private void HandlePlatformerValuesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch(e.Action)
            {
                case NotifyCollectionChangedAction.Add:

                    foreach (PlatformerValuesViewModel newItem in e.NewItems)
                    {
                        newItem.PropertyChanged += HandlePlatformerValuePropertyChanged;
                    }

                    base.NotifyPropertyChanged(nameof(this.PlatformerValues));

                    break;
                case NotifyCollectionChangedAction.Remove:

                    base.NotifyPropertyChanged(nameof(this.PlatformerValues));

                    break;
            }
        }

        private void HandlePlatformerValuePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.NotifyPropertyChanged(nameof(this.PlatformerValues));
        }
    }
}
