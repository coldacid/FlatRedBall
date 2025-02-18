﻿using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Gum.DataTypes;
using GumPlugin.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumPlugin.CodeGeneration
{
    class GumLayerAssociationCodeGenerator : ElementComponentCodeGenerator
    {
        const string UnderEverythingLayerPrefix = "UnderEverythingLayer";

        bool ShouldGenerate
        {
            get
            {
                return AppState.Self.GumProjectSave != null;
            }
        }

        public override FlatRedBall.Glue.Plugins.Interfaces.CodeLocation CodeLocation
        {
            get
            {
                // This needs to be before so that all layers are created and associated before
                // any components try to access them
                return FlatRedBall.Glue.Plugins.Interfaces.CodeLocation.BeforeStandardGenerated;
            }
        }

        string[] GetUsedFrbLayerNames(IElement element)
        {
            var list = element.AllNamedObjects.Where(item => item.IsLayer &&
                NamedObjectSaveCodeGenerator.GetFieldCodeGenerationType(item) == CodeGenerationType.Full)
                .Select(item => item.InstanceName)
                .ToList();

            bool anyOnUnderAllLayer = element.NamedObjects
                .Any(item => item.LayerOn == AvailableLayersTypeConverter.UnderEverythingLayerName);

            if(anyOnUnderAllLayer)
            {
                list.Add(UnderEverythingLayerPrefix);
            }

            bool anyOnAboveAllLayer = element.NamedObjects
                    .Any(item => item.LayerOn == AvailableLayersTypeConverter.TopLayerName);

            if(anyOnAboveAllLayer)
            {
                list.Add("TopLayer");
            }


            return list.ToArray();
        }


        public override ICodeBlock GenerateAddToManagers(ICodeBlock codeBlock, IElement element)
        {
            if (ShouldGenerate)
            {

                var rfs = GetScreenRfsIn(element);
                var idbName = rfs?.GetInstanceName();
                var rfsAssetTpe = rfs?.GetAssetTypeInfo();
                var isIdb = rfsAssetTpe == AssetTypeInfoManager.Self.ScreenIdbAti;
                if (string.IsNullOrEmpty(idbName) && element is FlatRedBall.Glue.SaveClasses.ScreenSave)
                {
                    idbName = "gumIdb";
                }
                else if(rfs != null && isIdb == false)
                {
                    idbName = "FlatRedBall.Gum.GumIdb.Self";
                }

                
                var frbLayerNames = GetUsedFrbLayerNames(element);
                // Creates Gum layers for every FRB layer, so that objects can be moved between layers at runtime, and so code gen
                // can use these for objects that are placed on layers in Glue.
                foreach (var layerPrefix in frbLayerNames)
                {

                    if(idbName != null)
                    {
                        codeBlock.Line(layerPrefix + "Gum = RenderingLibrary.SystemManagers.Default.Renderer.AddLayer();");
                        codeBlock.Line(layerPrefix + "Gum.Name = \"" + layerPrefix + "Gum\";");

                        string frbLayerName = layerPrefix;

                        if(frbLayerName == UnderEverythingLayerPrefix)
                        {
                            frbLayerName = "global::FlatRedBall.SpriteManager.UnderAllDrawnLayer";
                        }

                        codeBlock.Line(idbName + ".AddGumLayerToFrbLayer(" + layerPrefix + "Gum, " + frbLayerName + ");");
                    }
                }

            }
            return base.GenerateAddToManagers(codeBlock, element);
        }

        private ReferencedFileSave GetScreenRfsIn(IElement element)
        {
            return element.ReferencedFiles.FirstOrDefault(item =>
                FileManager.GetExtension(item.Name) == GumProjectSave.ScreenExtension);
        }
    }
}
