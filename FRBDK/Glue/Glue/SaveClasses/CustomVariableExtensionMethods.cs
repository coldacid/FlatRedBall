﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Elements;
using System.ComponentModel;
using System.Drawing;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Instructions;
using FlatRedBall.Glue.GuiDisplay.Facades;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using Microsoft.Xna.Framework;

namespace FlatRedBall.Glue.SaveClasses
{
    public static partial class CustomVariableExtensionMethods
    {
        static IGlueState GlueState => EditorObjects.IoC.Container.Get<IGlueState>();
        static IGlueCommands GlueCommands => EditorObjects.IoC.Container.Get<IGlueCommands>();


        public static bool GetIsCsv(this CustomVariable customVariable)
        {
            if (customVariable.Type == null)
            {
                return false;
            }
            if (customVariable.Type.EndsWith(".csv"))
            {
                return true;
            }
            else if (customVariable.Type.EndsWith(".txt"))
            {
                //ReferencedFileSave rfs = ObjectFinder.Self.GetReferencedFileSaveFromFile(customVariable.Type);
                throw new NotImplementedException("Need to implement checking if a custom variable is CSV from Text");

            }
            else if (GlueState.GetAllReferencedFiles().Any(item =>
                    item.IsCsvOrTreatedAsCsv && item.GetTypeForCsvFile() == customVariable.Type))
            {
                return true;
            }
            return false;
        }

        public static bool GetIsListCsv(this CustomVariable customVariable)
        {
            if (customVariable.GetIsCsv())
            {
                string fullFileName = FacadeContainer.Self.ProjectValues.ContentDirectory + customVariable.Type;
                ReferencedFileSave foundRfs = GlueCommands.FileCommands.GetReferencedFile(fullFileName);

                if (foundRfs != null)
                {
                    return foundRfs.CreatesDictionary == false;
                }
            }

            return false;
        }

        public static bool GetIsVariableState(this CustomVariable customVariable, IElement containingElement = null)
        {

            if(customVariable.Type == null)
            {
                throw new NullReferenceException(
                    $"The custom variable with name {customVariable.Name} has a Type that is null. This is not allowed");
            }
            bool returnValue = false;

            if (customVariable.DefinedByBase)
            {
                // If this is DefinedByBase, it may represent a variable that is tunneling, but it
                // doesn't know it - we have to get the variable from the base to know for sure.
                if (containingElement == null)
                {
                    containingElement = ObjectFinder.Self.GetElementContaining(customVariable);
                }
                if (containingElement != null && !string.IsNullOrEmpty(containingElement.BaseElement))
                {
                    IElement baseElement = GlueState.CurrentGlueProject.GetElement( containingElement.BaseElement);
                    if (baseElement != null)
                    {
                        CustomVariable customVariableInBase = baseElement.GetCustomVariableRecursively(customVariable.Name);
                        if (customVariableInBase != null)
                        {
                            returnValue = customVariableInBase.GetIsVariableState();
                        }
                    }
                }
            }
            else
            {

                bool isTunneled = !string.IsNullOrEmpty(customVariable.SourceObject) &&
                    !string.IsNullOrEmpty(customVariable.SourceObjectProperty);

                bool isOnThis = string.IsNullOrEmpty(customVariable.SourceObject) &&
                        string.IsNullOrEmpty(customVariable.SourceObjectProperty);


                if (isTunneled)
                {
                    string property = customVariable.SourceObjectProperty;
                    return !string.IsNullOrEmpty(property) && property.StartsWith("Current") &&
                        property.EndsWith("State");
                }
                else
                {
                    if (containingElement == null)
                    {
                        containingElement = ObjectFinder.Self.GetElementContaining(customVariable);
                    }

                    if (containingElement != null)
                    {
                        returnValue = customVariable.Type == "VariableState" ||
                            containingElement.GetStateCategory(customVariable.Type) != null;
                    }
                }
            }

            if(!returnValue && customVariable.Type.StartsWith("Entities."))
            {
                // It may still be a state, so let's see the entity:
                var entityName = customVariable.GetEntityNameDefiningThisTypeCategory();

                var entity = ObjectFinder.Self.GetEntitySave(entityName);

                if(entity != null)
                {
                    var lastPeriod = customVariable.Type.LastIndexOf('.');
                    var startIndex = lastPeriod + 1;
                    var stateCategory = customVariable.Type.Substring(startIndex);
                    var category = entity.GetStateCategory(stateCategory);

                    if(category != null)
                    {
                        returnValue = true;
                    }
                }
            }

            return returnValue;

        }

        public static string GetEntityNameDefiningThisTypeCategory(this CustomVariable customVariable)
        {
            if(customVariable.Type.StartsWith("Entities."))
            {
                var lastPeriod = customVariable.Type.LastIndexOf('.');
                var entityNameWithPeriods = customVariable.Type.Substring(0, lastPeriod);
                var entityName = entityNameWithPeriods.Replace('.', '\\');

                return entityName;
            }
            else
            {
                return null;
            }
        }

        public static void SetDefaultValueAccordingToType(this CustomVariable customVariable, string typeAsString)
        {
            Type type = TypeManager.GetTypeFromString(typeAsString);

            if (type == typeof(string))
            {
                customVariable.DefaultValue = "";
            }
            else if (type == null && customVariable.Type == "VariableState")
            {
                customVariable.DefaultValue = "";
            }
                // We used to check just Texture2D, but we want to check all file types since we're expanding that
            //else if (type == typeof(Microsoft.Xna.Framework.Graphics.Texture2D))
            else if (customVariable.GetIsFile())
            {
                customVariable.DefaultValue = "";
            }
            else if (type == typeof(Microsoft.Xna.Framework.Color))
            {
                customVariable.DefaultValue = "";
            }
            else if (type == typeof(byte))
            {
                customVariable.DefaultValue = (byte)0;
            }
            else if (type == typeof(short))
            {
                customVariable.DefaultValue = (short)0;
            }
            else if (type == typeof(int))
            {
                customVariable.DefaultValue = (int)0;
            }
            else if (type == typeof(long))
            {
                customVariable.DefaultValue = (long)0;
            }
            else if (type == typeof(char))
            {
                customVariable.DefaultValue = ' ';
            }
            else if (type == typeof(float))
            {
                customVariable.DefaultValue = 0.0f;
            }
            else if (type == typeof(double))
            {
                customVariable.DefaultValue = 0.0;
            }
            else
            {
                // This will be things like types defined in CSV values
                customVariable.DefaultValue = "";
            }

            customVariable.FixEnumerationTypes();
        }

        public static void FixEnumerationTypes(this CustomVariable customVariable)
        {
            if (customVariable.GetIsEnumeration() && customVariable.DefaultValue != null && customVariable.DefaultValue.GetType() == typeof(int))
            {
                Type runtimeType = customVariable.GetRuntimeType();
                Array array = Enum.GetValues(runtimeType);
                int valueAsInt = (int)customVariable.DefaultValue;


                try
                {
                    string name = Enum.GetName(runtimeType, valueAsInt);

                    foreach (object enumValue in array)
                    {
                        if (name == enumValue.ToString())
                        {
                            customVariable.DefaultValue = enumValue;
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("Could not set the integer value" + valueAsInt + "to an enumeration of type " + runtimeType.FullName);
                }
                    
            }
        }

        public static void ConvertEnumerationValuesToInts(this CustomVariable customVariable)
        {
            if(customVariable.DefaultValue?.GetType()?.IsEnum == true)
            {
                customVariable.DefaultValue = (int)customVariable.DefaultValue;
            }
        }


        public static bool GetIsEnumeration(this CustomVariable customVariable)
        {

            if (string.IsNullOrEmpty(customVariable.Type))
            {
                return false;
            }

            Type type = TypeManager.GetTypeFromString(customVariable.Type);

            if (type == null)
            {
                return false;
            }
            else
            {
                return type.IsEnum;
            }
        }

        public static bool GetIsAnimationChain(this CustomVariable variable)
        {
            Type runtimeType = variable.GetRuntimeType();

            return runtimeType != null &&
                runtimeType == typeof(string) && variable.SourceObjectProperty == "CurrentChainName";
        }

        public static bool GetIsFile(this CustomVariable customVariable)
        {
            string typeString = null;

            if (string.IsNullOrEmpty(customVariable.OverridingPropertyType))
            {
                typeString = customVariable.Type;
            }
            else
            {
                typeString = customVariable.OverridingPropertyType;
            }
            
            // this can be expanded to support setting more file-based variables in Glue
            return GetIsFile(typeString);
        }

        public static bool GetIsFile(Type runtimeType)
        {
            return runtimeType != null && GetIsFile(runtimeType.Name);

                //runtimeType == typeof(Microsoft.Xna.Framework.Graphics.Texture2D) ||
                //runtimeType == typeof(FlatRedBall.Graphics.Animation.AnimationChainList) ||
                //runtimeType == typeof(FlatRedBall.Graphics.BitmapFont)
                //;
        }

        public static bool GetIsFile(string typeName)
        {
            /////////////////Early Out//////////////////////////
            // I was able to get
            // Glue to generate really
            // bad code by adding a type
            // to a CSV that deserialized
            // to a string.  In this case, every
            // string member was being treated as
            // if it was a file, and having its quotes
            // removed when code was generated for it.  We
            // don't want this to happen so we're going to always
            // treat strings as non-files...for now at least.
            if (typeName != null && typeName.ToLower() == "string")
            {
                return false;
            }
            ////////////// End Early out ///////////////////////




            bool isDefaultFileType = 
                typeName == "Texture2D" ||
                (typeName != null && typeName.EndsWith(".Texture2D")) ||
                typeName == "AnimationChainList" || 
                typeName == "BitmapFont" ||
                typeName == "ShapeCollection" ||
                typeName == "Scene"
                ;
            bool isFileTypeDefinedInCsv = false;

            bool isFileTypeFromAti = false;
            if (!isDefaultFileType && typeName != null)
            {
                foreach (var assetType in AvailableAssetTypes.Self.AllAssetTypes)
                {
                    if (!string.IsNullOrEmpty(assetType.Extension) && assetType.RuntimeTypeName == typeName)
                    {
                        isFileTypeDefinedInCsv = true;
                        break;
                    }

                    if(assetType.QualifiedRuntimeTypeName.QualifiedType == typeName &&
                        !string.IsNullOrEmpty(assetType.Extension ))
                    {
                        isFileTypeFromAti = true;
                        break;
                    }

                }
            }
            

            return isDefaultFileType || isFileTypeDefinedInCsv || isFileTypeFromAti;

        }

        public static bool GetIsObjectType(this CustomVariable customVariable)
        {
            string typeString = null;

            if (string.IsNullOrEmpty(customVariable.OverridingPropertyType))
            {
                typeString = customVariable.Type;
            }
            else
            {
                typeString = customVariable.OverridingPropertyType;
            }

            return GetIsObjectType(typeString);
        }

        public static bool GetIsObjectType(string typeString)
        {
            if (typeString != null)
            {
                return AvailableAssetTypes.Self.AllAssetTypes.Any(item =>
                {
                    return item.CanBeObject && (item.RuntimeTypeName == typeString || item.QualifiedRuntimeTypeName.QualifiedType == typeString);
                });
            }

            return false;
        }

        public static Type GetRuntimeType(this CustomVariable customVariable)
        {
            if (customVariable.GetIsVariableState())
            {
                return null;
            }
            else if (string.IsNullOrEmpty(customVariable.OverridingPropertyType))
            {
                var type = TypeManager.GetTypeFromString(customVariable.Type);
                return type;
            }
            else
            {
                return TypeManager.GetTypeFromString(customVariable.OverridingPropertyType);
            }
        }

        /// <summary>
        /// If a CustomVariable has its SetByDerived to true, then any Elements that inherit from the Element
        /// that has this CustomVariable will also have CustomVariables with matching name that have their
        /// DefinedByBase set to true.  The variable with DefinedByBase often has incomplete information (like
        /// whether it is tunneling or not) so sometimes we need the defining variable for information like state types.
        /// </summary>
        /// <param name="customVariable">The variable to get the defining variable for.</param>
        /// <returns>The defining variable.  If an error occurs in finding base types, null is returned.</returns>
        public static CustomVariable GetDefiningCustomVariable(this CustomVariable customVariable)
        {
            if(customVariable == null)
            {
                throw new ArgumentNullException("customVariable");
            }
            if (!customVariable.DefinedByBase)
            {
                return customVariable;
            }
            else
            {
                IElement container = ObjectFinder.Self.GetElementContaining(customVariable);

                if (container != null && !string.IsNullOrEmpty(container.BaseElement))
                {
                    IElement baseElement = GlueState.CurrentGlueProject.GetElement(container.BaseElement);
                    if (baseElement != null)
                    {
                        CustomVariable customVariableInBase = baseElement.GetCustomVariableRecursively(customVariable.Name);

                        if (customVariableInBase != null)
                        {
                            return customVariableInBase.GetDefiningCustomVariable();
                        }
                    }
                }
                return null;
            }

        }

        public static bool GetIsExposingVariable(this CustomVariable customVariable, IElement container)
        {
            bool isExposedExistingMember = false;

            if (container is EntitySave)
            {
                isExposedExistingMember =
                    ExposedVariableManager.IsMemberDefinedByEntity(customVariable.Name, container as EntitySave);
            }
            else if (container is ScreenSave)
            {
                isExposedExistingMember = customVariable.Name == "CurrentState";
            }

            return isExposedExistingMember;
        }

        public static bool GetIsTunneling(this CustomVariable customVariable)
        {
            return !string.IsNullOrEmpty(customVariable.SourceObject) && !string.IsNullOrEmpty(customVariable.SourceObjectProperty);

        }

        public static bool GetIsNewVariable(this CustomVariable customVariable, IElement container)
        {
            return customVariable.GetIsTunneling() == false && customVariable.GetIsExposingVariable(container) == false;
        }

        public static string CustomVariableToString(CustomVariable cv)
        {
            IElement container = ObjectFinder.Self.GetElementContaining(cv);
            string containerName = " (Uncontained)";
            if (container != null)
            {
                containerName = " in " + container.ToString();
            }

            if (string.IsNullOrEmpty(cv.SourceObject))
            {
                return "" + cv.Type + " " + cv.Name + " = " + cv.DefaultValue + containerName;

            }
            else
            {
                return "" + cv.Type + " " + cv.SourceObject + "." + cv.Name + " = " + cv.DefaultValue + containerName;
            }

        }

        public static bool HasAccompanyingVelocityConsideringTunneling(this CustomVariable variable, IElement container, int maxDepth = 0)
        {
            if (variable.HasAccompanyingVelocityProperty)
            {
                return true;
            }
            else if (!string.IsNullOrEmpty(variable.SourceObject) && !string.IsNullOrEmpty(variable.SourceObjectProperty) && maxDepth > 0)
            {
                NamedObjectSave nos = container.GetNamedObjectRecursively(variable.SourceObject);

                if (nos != null)
                {
                    // If it's a FRB 
                    if (nos.SourceType == SourceType.FlatRedBallType || nos.SourceType == SourceType.File)
                    {
                        return !string.IsNullOrEmpty(InstructionManager.GetVelocityForState(variable.SourceObjectProperty));
                    }
                    else if(nos.SourceType == SourceType.Entity)
                    {
                        EntitySave entity = GlueState.CurrentGlueProject.GetEntitySave(nos.SourceClassType);

                        if (entity != null)
                        {
                            CustomVariable variableInEntity = entity.GetCustomVariable(variable.SourceObjectProperty);

                            if (variableInEntity != null)
                            {
                                if (!string.IsNullOrEmpty(InstructionManager.GetVelocityForState(variableInEntity.Name)))
                                {
                                    return true;
                                }
                                else
                                {

                                    return variableInEntity.HasAccompanyingVelocityConsideringTunneling(entity, maxDepth - 1);
                                }
                            }
                            else
                            {
                                // There's no variable for this, so let's see if it's a variable that has velocity in FRB
                                return !string.IsNullOrEmpty(InstructionManager.GetVelocityForState(variable.SourceObjectProperty));

                            }
                        }
                    }
                }
            }
            return false;

        }

        public static bool GetIsSourceFile(this CustomVariable customVariable, IElement container)
        {
            NamedObjectSave referencedNos = null;
            if(!string.IsNullOrEmpty(customVariable.SourceObject) )
            {
                referencedNos = container.GetNamedObjectRecursively(customVariable.SourceObject);
            }

            return referencedNos != null && customVariable.SourceObjectProperty == "SourceFile" && referencedNos.SourceType == SourceType.FlatRedBallType;


        }
    }
    

}
