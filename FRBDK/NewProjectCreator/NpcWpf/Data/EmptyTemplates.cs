﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Npc.Data
{
    static class EmptyTemplates
    {
        public static List<PlatformProjectInfo> Projects { get; private set; } = new List<PlatformProjectInfo>();

        static EmptyTemplates()
        {
            Add("Desktop GL (Windows, Mac, Linux)", "FlatRedBallDesktopGlTemplate", "FlatRedBallDesktopGL.zip", "http://files.flatredball.com/content/FrbXnaTemplates/DailyBuild/ZippedTemplates/FlatRedBallDesktopGlTemplate.zip", true);
            Add("Android (Phone, Tablet, Fire TV)", "FlatRedBallAndroidTemplate", "FlatRedBallAndroidTemplate.zip", "http://files.flatredball.com/content/FrbXnaTemplates/DailyBuild/ZippedTemplates/FlatRedBallAndroidTemplate.zip", true);
            Add("iOS (iPhone, iPad, iPod Touch)", "FlatRedBalliOSTemplate", "FlatRedBalliOSTemplate.zip", "http://files.flatredball.com/content/FrbXnaTemplates/DailyBuild/ZippedTemplates/FlatRedBalliOSTemplate.zip" ,true);
            Add("Winows UWP (Windows Desktop, Xbox One, Tablet, Windows Phone)", "FlatRedBallUwpTemplate", "FlatRedBallUwpTemplate.zip", "http://files.flatredball.com/content/FrbXnaTemplates/DailyBuild/ZippedTemplates/FlatRedBallUwpTemplate.zip" ,true);
            Add("Desktop XNA (Windows, requires XNA install)", "FlatRedBallXna4Template", "FlatRedBallXna4Template.zip" , "http://files.flatredball.com/content/FrbXnaTemplates/DailyBuild/ZippedTemplates/FlatRedBallXna4Template.zip", true);
        }

        static void Add(string friendlyName, string namespaceName, string zipName, string url, bool supportedInGlue)
        {
            var newItem = new PlatformProjectInfo();

            newItem.FriendlyName = friendlyName;
            newItem.Namespace = namespaceName;
            newItem.ZipName = zipName;
            newItem.Url = url;
            newItem.SupportedInGlue = supportedInGlue;

            Projects.Add(newItem);
        }
    }
}
