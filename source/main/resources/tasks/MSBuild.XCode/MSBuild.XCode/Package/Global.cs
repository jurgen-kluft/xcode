﻿using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSBuild.XCode.Helpers;

namespace MSBuild.XCode
{
    public static class Global
    {
        public static bool IsInitialized { get; set; }

        public static string CacheRepoDir { get; set; }
        public static string RemoteRepoDir { get; set; }
        public static string TemplateDir { get; set; }
        public static string RootDir { get; set; }

        public static PackageRepository CacheRepo { get; set; }
        public static PackageRepository RemoteRepo { get; set; }

        public static CppProject CppTemplateProject { get; set; }
        public static CsProject CsTemplateProject { get; set; }

        static Global()
        {
            IsInitialized = false;
        }

        public static bool Initialize()
        {
            Loggy.ToConsole = true;
            Loggy.TaskLogger = null;
            Loggy.Indentor = "\t";

            if (!String.IsNullOrEmpty(CacheRepoDir))
            {
                if (!Directory.Exists(CacheRepoDir))
                {
                    Loggy.Error(String.Format("Error: Initialization of Global failed since cache repo {0} doesn't exist", CacheRepoDir));
                    return false;
                }
            }
            if (!String.IsNullOrEmpty(RemoteRepoDir))
            {
                if (!Directory.Exists(RemoteRepoDir))
                {
                    Loggy.Error(String.Format("Error: Initialization of Global failed since remote repo {0} doesn't exist", RemoteRepoDir));
                    return false;
                }
            }
            if (!String.IsNullOrEmpty(TemplateDir))
            {
                if (!Directory.Exists(TemplateDir))
                {
                    Loggy.Error(String.Format("Error: Initialization of Global failed since template dir {0} doesn't exist", TemplateDir));
                    return false;
                }
            }

            RemoteRepo = new PackageRepository(RemoteRepoDir, ELocation.Remote);
            CacheRepo = new PackageRepository(CacheRepoDir, ELocation.Cache);

            if (!String.IsNullOrEmpty(TemplateDir))
            {
                // For C++
                CppTemplateProject = new CppProject();
                if (!CppTemplateProject.Load(TemplateDir + "main.vcxproj"))
                {
                    Loggy.Error(String.Format("Error: Initialization of Global failed in due to failure in loading {0}", TemplateDir + "main.vcxproj"));
                    return false;
                }

                // For C#
                // CsTemplateProject = new CsProject();
                // CsTemplateProject.Load(TemplateDir + "main.vcxproj");
            }

            IsInitialized = true;
            return true;
        }

    }
}