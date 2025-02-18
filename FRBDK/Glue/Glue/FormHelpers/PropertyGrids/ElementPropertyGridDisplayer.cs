﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.FormHelpers.PropertyGrids
{
    public class ElementPropertyGridDisplayer : PropertyGridDisplayer
    {
        protected void SetClassName(object sender, MemberChangeArgs args)
        {
            ((IElement)Instance).RenameElement((string)args.Value);
        }

        protected void HandleMemberSet(object sender, MemberChangeArgs args)
        {
            string name = args.Member;
            object value = args.Value;

            (this.Instance as IElement).Properties.SetValue(name, value);
        }

        public PropertyGridMember IncludeCustomPropertyMember(string name, Type propertyType)
        {
            // Do we have to do this to make sure we don't pass the same string if this is called multiple times?
            string nameCopy = name;

            var newMember = IncludeMember(name, propertyType,
                HandleMemberSet,
                delegate()
                {
                    object toReturn = (this.Instance as IElement).Properties.GetValue(nameCopy);
                    if (toReturn == null)
                    {
                        toReturn = TypeManager.GetDefaultForTypeAsType(propertyType.Name);
                    }
                    return toReturn;
                }

                );

            return newMember;
        }

        protected string GetClassName()
        {
            return ((IElement)Instance).ClassName;
        }

        protected void UpdateIncludedAndExcludedBase(IElement element)
        {
            ResetToDefault();

            ExcludeMember("Properties");

            ExcludeMember("ClassName");

        }

        protected virtual void SetAfterMemberChangedEvents()
        {
            this.GetPropertyGridMember("UseGlobalContent").AfterMemberChange += HandleAfteruseGlobalContentChanged;
        }



        void HandleAfteruseGlobalContentChanged(object sender, MemberChangeArgs args)
        {
            IElement element = Instance as IElement;
                
            List<IElement> elementsToMakeGlobal = new List<IElement>();

            if (element.UseGlobalContent)
            {
                GetAllElementsReferencedThroughObjectsRecursively(element, elementsToMakeGlobal);

                if (elementsToMakeGlobal.Count != 0)
                {
                    string message = "Setting " + FileManager.RemovePath(element.Name) +
                        " to use a global content manager will result in the following Entities being " +
                        " loaded with a global content manager.  What would you like to do?";

                    var lbw = new ListBoxWindowWpf();
                    lbw.Message = message;

                    foreach (var item in elementsToMakeGlobal)
                    {
                        lbw.AddItem(item);
                    }

                    lbw.ClearButtons();
                    lbw.AddButton("Set all to use global content", System.Windows.Forms.DialogResult.Yes);
                    lbw.AddButton("Nothing - this may result in runtime errors", System.Windows.Forms.DialogResult.No);
                    var dialogResult = lbw.ShowDialog();

                    if(dialogResult == true)
                    {
                        var result = (System.Windows.Forms.DialogResult)lbw.ClickedOption;
                        if (result == System.Windows.Forms.DialogResult.Yes)
                        {
                            foreach (IElement toMakeGlobal in elementsToMakeGlobal)
                            {
                                toMakeGlobal.UseGlobalContent = true;
                                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(toMakeGlobal);
                            }
                        }

                    }
                }
            }
        }

        private static void GetAllElementsReferencedThroughObjectsRecursively(IElement element, List<IElement> elementsToMakeGlobal)
        {

            // loop through all NamedObjects 
            // and recursively add them to the
            // list of elements to make global

            foreach (NamedObjectSave nos in element.NamedObjects.Where(nos => nos.SourceType == SourceType.Entity))
            {
                IElement nosElement = ObjectFinder.Self.GetIElement(nos.SourceClassType);
                if (elementsToMakeGlobal.Contains(nosElement) == false)
                {
                    elementsToMakeGlobal.Add(nosElement);
                    GetAllElementsReferencedThroughObjectsRecursively(nosElement, elementsToMakeGlobal);
                }
            }

        }
    }
}
