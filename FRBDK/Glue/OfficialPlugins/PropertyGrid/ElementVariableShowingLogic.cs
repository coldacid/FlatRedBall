﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows.Media;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Utilities;
using WpfDataUi;
using WpfDataUi.DataTypes;
using FlatRedBall.Glue.SetVariable;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Elements;
using GlueFormsCore.Controls;

namespace OfficialPlugins.VariableDisplay
{
    class ElementVariableShowingLogic
    {
        public static void UpdateShownVariables(DataUiGrid grid, IElement element)
        {
            grid.Categories.Clear();

            List<MemberCategory> categories = new List<MemberCategory>();
            var categoryName = "Variables";

            CreateAndAddCategory(categories, categoryName);
            CreateInstanceMembersForVariables(element, categories);

            var dictionary = MainPanelControl.ResourceDictionary;
            const byte brightness = 227;
            var color = Color.FromRgb(brightness, brightness, brightness);
            if (dictionary.Contains("BlackSelected"))
            {
                color = (Color)MainPanelControl.ResourceDictionary["BlackSelected"];
            }

            foreach (var category in categories)
            {
                category.SetAlternatingColors(
                    new SolidColorBrush(color), 
                    Brushes.Transparent);

                grid.Categories.Add(category);
            }

            grid.Refresh();

        }

        private static MemberCategory CreateAndAddCategory(List<MemberCategory> categories, string categoryName)
        {
            var defaultCategory = new MemberCategory(categoryName);
            defaultCategory.FontSize = 14;
            categories.Add(defaultCategory);
            return defaultCategory;
        }

        private static void CreateInstanceMembersForVariables(IElement element, List<MemberCategory> categories)
        {
            var variableDefinitions = PluginManager.GetVariableDefinitionsFor(element);
            foreach (var variableDefinition in variableDefinitions)
            {
                var customVariable = element.GetCustomVariable(variableDefinition.Name);

                if (customVariable == null)
                {
                    customVariable = new CustomVariable();
                    customVariable.DefaultValue = variableDefinition.DefaultValue;
                    customVariable.Name = variableDefinition.Name;
                    customVariable.Type = variableDefinition.Type;
                    // category?

                    element.CustomVariables.Add(customVariable);

                    GlueCommands.Self.GluxCommands.SaveGlux();
                }
            }


            foreach (CustomVariable variable in element.CustomVariables)
            {
                Type type = variable.GetRuntimeType();
                if (type == null)
                {
                    type = typeof(string);
                }

                string name = variable.Name;

                var instanceMember = new DataGridItem();
                instanceMember.CustomGetTypeEvent += (throwaway) => type;
                string displayName = StringFunctions.InsertSpacesInCamelCaseString(name);

                // Currently this only works on TextBox variables - eventually will expand
                instanceMember.DetailText = variable.Summary;
                
                instanceMember.DisplayName = displayName;
                instanceMember.UnmodifiedVariableName = name;

                TypeConverter converter = variable.GetTypeConverter(element);
                instanceMember.TypeConverter = converter;
                
                instanceMember.CustomSetEvent += (intance, value) =>
                {
                    instanceMember.IsDefault = false;

                    RefreshLogic.IgnoreNextRefresh();


                    var oldValue = variable.DefaultValue;

                    variable.DefaultValue = value;

                    EditorObjects.IoC.Container.Get<CustomVariableSaveSetPropertyLogic>().ReactToCustomVariableChangedValue(
                        "DefaultValue", variable, oldValue);



                    GlueCommands.Self.GluxCommands.SaveGlux();

                    GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();

                    GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
                };

                instanceMember.CustomGetEvent += (instance) =>
                {
                    return GetValueRecursively(element, name);

                };

                instanceMember.IsDefault = element.GetCustomVariable(name)?.DefaultValue == null;

                // Assing the IsDefaultSet event setting IsDefault *after* 
                instanceMember.IsDefaultSet += (owner, args) =>
                {
                    
                    element.GetCustomVariableRecursively(name).DefaultValue = null;


                    GlueCommands.Self.GluxCommands.SaveGlux();

                    GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();

                    GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();

                };

                instanceMember.SetValueError = (newValue) =>
                {
                    if (newValue is string && string.IsNullOrEmpty(newValue as string))
                    {
                        element.GetCustomVariableRecursively(name).DefaultValue = null;

                        GlueCommands.Self.GluxCommands.SaveGlux();

                        GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();

                        GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
                    }
                };

                instanceMember.ContextMenuEvents.Add("Variable Properties", (sender, args) => GlueState.Self.CurrentCustomVariable = variable);

                var categoryName = !string.IsNullOrWhiteSpace(variable.Category) ?
                    variable.Category : "Variables";

                var category = categories.FirstOrDefault(item => item.Name == categoryName);

                if(category == null)
                {
                    category = CreateAndAddCategory(categories, categoryName);
                }

                category.Members.Add(instanceMember);
            }


            foreach(var variable in variableDefinitions)
            {
                var type = FlatRedBall.Glue.Parsing.TypeManager.GetTypeFromString(variable.Type);

                if (type == null)
                {
                    type = typeof(string);
                }

                string name = variable.Name;

                var instanceMember = new DataGridItem();
                instanceMember.CustomGetTypeEvent += (throwaway) => type;
                string displayName = StringFunctions.InsertSpacesInCamelCaseString(name);

                // Currently this only works on TextBox variables - eventually will expand
                // we don't have this on variable definitions
                //instanceMember.DetailText = variable.Summary;

                instanceMember.DisplayName = displayName;
                instanceMember.UnmodifiedVariableName = name;

                // todo - figure out type converters?
                //TypeConverter converter = variable.GetTypeConverter(element);
                //instanceMember.TypeConverter = converter;

                instanceMember.CustomSetEvent += (intance, value) =>
                {
                    instanceMember.IsDefault = false;

                    RefreshLogic.IgnoreNextRefresh();

                    var customVariable = element.GetCustomVariable(name);
                    var oldValue = customVariable?.DefaultValue;
                    if(customVariable == null)
                    {
                        element.CustomVariables.Add(new CustomVariable() { Name = name });
                    }
                    element.Properties.SetValue(name, value);

                    // todo - do we need this?
                    //EditorObjects.IoC.Container.Get<CustomVariableSaveSetVariableLogic>().ReactToCustomVariableChangedValue(
                    //        "DefaultValue", variable, oldValue);



                    GlueCommands.Self.GluxCommands.SaveGlux();

                    GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();

                    GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
                };

                instanceMember.CustomGetEvent += (instance) =>
                {
                    var foundVariable = element.GetCustomVariableRecursively(name);
                    return foundVariable?.DefaultValue;
                };

                //instanceMember.IsDefaultSet += (owner, args) =>
                //{

                //    element.GetCustomVariableRecursively(name).DefaultValue = null;


                //    GlueCommands.Self.GluxCommands.SaveGlux();

                //    GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();

                //    GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();

                //};

                //instanceMember.SetValueError = (newValue) =>
                //{
                //    if (newValue is string && string.IsNullOrEmpty(newValue as string))
                //    {
                //        element.GetCustomVariableRecursively(name).DefaultValue = null;

                //        GlueCommands.Self.GluxCommands.SaveGlux();

                //        GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();

                //        GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
                //    }
                //};

                //instanceMember.ContextMenuEvents.Add("Variable Properties", (sender, args) => GlueState.Self.CurrentCustomVariable = variable);

                //category.Members.Add(instanceMember);
            }
        }

        private static object GetValueRecursively(IElement element, string name)
        {
            var variable = element.GetCustomVariable(name);

            if(variable?.DefaultValue != null)
            {
                return variable.DefaultValue;
            }
            else if(!string.IsNullOrEmpty( element.BaseElement ))
            {
                var baseElement = ObjectFinder.Self.GetBaseElement(element);

                if(baseElement == null)
                {
                    return null;
                }
                else
                {
                    return GetValueRecursively(baseElement, name);
                }
            }
            else
            {
                return null;
            }
        }
    }
}
