﻿using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.CollisionPlugin.ViewModels
{
    public class NamedObjectPairRelationshipViewModel : ViewModel
    {
        public string OtherObjectName
        {
            get => Get<string>();
            set => Set(value); 
        }

        [DependsOn(nameof(OtherObjectName))]
        public string ObjectObjectDisplayName
        {
            get => OtherObjectName ?? CollisionRelationshipViewModel.AlwaysColliding;
        }

        public string SelectedNamedObjectName
        {
            get => Get<string>();
            set => Set(value); 
        }

        public ObservableCollection<RelationshipListCellViewModel> Relationships
        {
            get => Get<ObservableCollection<RelationshipListCellViewModel>>(); 
            set => Set(value); 
        }

        public event EventHandler AddObjectClicked;

        public NamedObjectPairRelationshipViewModel()
        {
            Relationships = new ObservableCollection<RelationshipListCellViewModel>();
        }

        public void AddNewRelationship()
        {
            AddObjectClicked(this, null);
        }
    }
}
