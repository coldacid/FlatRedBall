﻿using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.SaveClasses;
using Gum.DataTypes.Behaviors;
using GumPlugin.DataGeneration;
using GumPlugin.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GumPluginCore.Managers
{
    class FormsAddManager
    {
        public static void GenerateBehaviors()
        {
            TaskManager.Self.Add(() =>
            {
                bool didAdd = false;

                foreach (var control in FormsControlInfo.AllControls)
                {
                    if (AddIfDoesntHave(CreateBehaviorSaveFrom(control)))
                    {
                        didAdd = true;
                    }
                }

                if (didAdd)
                {
                    AppCommands.Self.SaveGumx();
                }
            }, "Adding Gum Forms Behaviors");
        }

        public static bool AddIfDoesntHave(BehaviorSave behaviorSave)
        {
            var project = AppState.Self.GumProjectSave;

            bool doesProjectAlreadyHaveBehavior =
                project.Behaviors.Any(item => item.Name == behaviorSave.Name);

            if (!doesProjectAlreadyHaveBehavior)
            {
                AppCommands.Self.AddBehavior(behaviorSave);
            }
            // in case it's changed, or in case the user has somehow corrupted their behavior, force save it
            AppCommands.Self.SaveBehavior(behaviorSave);

            return doesProjectAlreadyHaveBehavior == false;
        }

        public static BehaviorSave CreateBehaviorSaveFrom(FormsControlInfo controlInfo)
        {
            BehaviorSave toReturn = new BehaviorSave();
            toReturn.Name = controlInfo.BehaviorName;

            foreach (var gumStateCategory in controlInfo.GumStateCategory)
            {
                var category = new Gum.DataTypes.Variables.StateSaveCategory();
                toReturn.Categories.Add(category);
                category.Name = gumStateCategory.Name;

                if (gumStateCategory.States != null)
                {
                    foreach (var stateName in gumStateCategory.States)
                    {
                        category.States.Add(new Gum.DataTypes.Variables.StateSave { Name = stateName });
                    }
                }
            }

            toReturn.RequiredInstances.AddRange(controlInfo.RequiredInstances);

            return toReturn;
        }
    }
}
