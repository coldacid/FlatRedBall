﻿using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Managers;
using Gum.DataTypes;
using GumPlugin.DataGeneration;
using GumPlugin.Managers;
using GumPluginCore.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GumPlugin.CodeGeneration
{
    public class GueRuntimeTypeAssociationGenerator : Singleton<GueRuntimeTypeAssociationGenerator>
    {



        public string GetRuntimeRegistrationPartialClassContents(bool registerFormsAssociations)
        {
            CodeBlockBase codeBlock = new CodeBlockBase(null);

            ICodeBlock currentBlock = codeBlock.Namespace("FlatRedBall.Gum");

            currentBlock = currentBlock.Class("public ", "GumIdbExtensions", "");

            currentBlock = currentBlock.Function("public static void", "RegisterTypes", "");
            {
                AddAssignmentFunctionContents(currentBlock);

                if(registerFormsAssociations)
                {
                    currentBlock._();
                    AddFormsAssociations(currentBlock);
                }

            }


            return codeBlock.ToString();
        }
        void AddAssignmentFunctionContents(ICodeBlock codeBlock)
        {
            foreach (var element in AppState.Self.AllLoadedElements)
            {
                if (GueDerivingClassCodeGenerator.Self.ShouldGenerateRuntimeFor(element))
                {
                    AddRegisterCode(codeBlock, element);
                }
            }
        }

        class AssociationFulfillment
        {
            public ElementSave Element { get; set; }
            public bool IsCompletelyFulfilled { get; set; }
            public string ControlType { get; set; }

            public override string ToString()
            {
                return $"{ControlType} by {Element}";
            }
        }

        private void AddFormsAssociations(ICodeBlock currentBlock)
        {
            List<AssociationFulfillment> associationFulfillments = new List<AssociationFulfillment>();

            var loadedElements = AppState.Self.AllLoadedElements.ToList();

            foreach (var element in loadedElements)
            {
                var elementAsComponent = element as ComponentSave;

                if(elementAsComponent != null)
                {
                    foreach(var behavior in elementAsComponent.Behaviors)
                    {
                        var formsControlInfo = FormsControlInfo.AllControls
                            .First(item => item.BehaviorName == behavior.BehaviorName);

                        string controlType = formsControlInfo.ControlName;

                        AssociationFulfillment matchingFulfillment = null;

                        if (controlType != null)
                        {
                            matchingFulfillment = associationFulfillments.FirstOrDefault(item => item.ControlType == controlType);
                        }

                        // Here we try to get the "most fulfilled" version of an object to set it as the default.
                        // For example, Button text is optional, and two Gum objects may have the Button behavior.
                        // If one of them has text properties then we should favor that over the one that doens't.
                        // Of coruse, the user can still change the defaults at runtime or manually create the visual
                        // for a form if they don't want the default, but this will hopefully give the "best fit"
                        // default.
                        if(matchingFulfillment == null || matchingFulfillment.IsCompletelyFulfilled == false)
                        {
                            bool isCompleteFulfillment = GetIfIsCompleteFulfillment(element, controlType);

                            if(matchingFulfillment == null)
                            {
                                var newFulfillment = new AssociationFulfillment();
                                newFulfillment.Element = element;
                                newFulfillment.IsCompletelyFulfilled = isCompleteFulfillment;
                                newFulfillment.ControlType = controlType;

                                associationFulfillments.Add(newFulfillment);
                            }
                            else if(isCompleteFulfillment)
                            {
                                matchingFulfillment.Element = element;
                                matchingFulfillment.IsCompletelyFulfilled = isCompleteFulfillment;
                                matchingFulfillment.ControlType = controlType;
                            }
                            
                        }
                    }
                }
            }

            foreach(var component in AppState.Self.GumProjectSave.Components)
            {
                // Is this component a forms control, but not a default forms control?
                if(FormsClassCodeGenerator.Self.GetIfShouldGenerate(component))
                {
                    // associate them:
                    var newFulfillment = new AssociationFulfillment();
                    newFulfillment.Element = component;
                    newFulfillment.IsCompletelyFulfilled = true;
                    newFulfillment.ControlType = FormsClassCodeGenerator.Self.GetFullRuntimeNamespaceFor(component) + 
                        "." + FormsClassCodeGenerator.Self.GetUnqualifiedRuntimeTypeFor(component);

                    associationFulfillments.Add(newFulfillment);
                }
            }

            foreach(var fulfillment in associationFulfillments)
            {
                string qualifiedControlType;

                if(fulfillment.ControlType.Contains("."))
                {
                    qualifiedControlType = fulfillment.ControlType;
                }
                else
                {
                    qualifiedControlType = "FlatRedBall.Forms.Controls." + fulfillment.ControlType;

                }

                var gumRuntimeType = 
                    GueDerivingClassCodeGenerator.Self.GetQualifiedRuntimeTypeFor(fulfillment.Element);

                var line =
                    $"FlatRedBall.Forms.Controls.FrameworkElement.DefaultFormsComponents[typeof({qualifiedControlType})] = typeof({gumRuntimeType});";

                currentBlock.Line(line);
            }
        }

        private bool GetIfIsCompleteFulfillment(ElementSave element, string controlType)
        {
            switch(controlType)
            {
                // some controls are automatically completely fulfilled:
                case "ComboBox":
                case "ListBox":
                case "PasswordBox":
                case "ScrollBar":
                case "ScrollViewer":
                case "Slider":
                case "TextBox":
                case "UserControl":
                case "TreeViewItem":
                case "TreeView":
                case "FlatRedBall.Forms.Controls.Games.OnScreenKeyboard":
                    return true;
                    // These require a Text object
                case "Button": 
                case "CheckBox":
                case "Label":
                case "ListBoxItem":
                case "RadioButton":
                case "ToggleButton":
                case "Toast":
                case "FlatRedBall.Forms.Controls.Popups.Toast":
                case "FlatRedBall.Forms.Controls.Games.DialogBox":
                    return element.Instances.Any(item => item.Name == "TextInstance" && item.BaseType == "Text");

                default:
                    throw new NotImplementedException($"Need to handle {controlType} in {nameof(GetIfIsCompleteFulfillment)}");
            }

        }
        
        private void GenerateAssociation(string controlType, string gumRuntimeType)
        {

        }

        private static void AddRegisterCode(ICodeBlock codeBlock, Gum.DataTypes.ElementSave element)
        {
            string elementNameString = element.Name.Replace("\\", "\\\\") ;

            var qualifiedName = GueDerivingClassCodeGenerator.Self.GetQualifiedRuntimeTypeFor(element);

            codeBlock.Line(
                "GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType(\"" +
                elementNameString +
                "\", typeof(" +
                qualifiedName +
                "));");

            var needsGeneric = element is StandardElementSave && element.Name == "Container";

            if(needsGeneric)
            {
                codeBlock.Line(
                    "GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType(\"" +
                    elementNameString + "<T>" +
                    "\", typeof(" +
                    qualifiedName + "<>" +
                    "));");
            }

        }
    }
}
