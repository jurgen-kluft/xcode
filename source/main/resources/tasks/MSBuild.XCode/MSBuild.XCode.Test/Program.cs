﻿using System;
using MSBuild.XCode.Helpers;

namespace MSBuild.XCode.Test
{
     static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static string RemoteRepoDir; 
        private static string CacheRepoDir;
        private static string RootDir;
        private static string XCodeRepoDir;
        private static string TemplateDir;


        [STAThread]
        static void Main()
        {
            string name = "xunittest";

            RemoteRepoDir = @"db::server=127.0.0.1;port=3309;database=xcode_cpp;uid=root;password=p1|ftp::ip=127.0.0.1,username=admin,password=p1,port=1414,base=.storage\";
            CacheRepoDir = @"k:\Dev.C++.Packages\PACKAGE_REPO\";
            RootDir = @"k:\Dev.C++.Packages\" + name + "\\";
            XCodeRepoDir = @"k:\Dev.C++.Packages\PACKAGE_REPO\com\virtuos\xcode\publish\";
            TemplateDir = XCodeRepoDir + @"templates\";

            Loggy.ToConsole = true;

            PackageInstance.TemplateDir = TemplateDir;
            if (!PackageInstance.Initialize(RemoteRepoDir, CacheRepoDir, RootDir))
                return;

            if (Update("1.1.0.0"))
            {
                string platform = "WII";
                Construct(name, platform);
                Create(name, platform);
                Install(name, platform);
                Deploy(name, platform);
            }
        }

        public static bool Update(string version)
        {
            XCodeUpdate cmd = new XCodeUpdate();
            cmd.CacheRepoDir = CacheRepoDir;
            cmd.XCodeRepoDir = XCodeRepoDir;
            cmd.XCodeVersion = version;
            return cmd.Execute();
        }

        public static void Create(string name, string platform)
        {
            if (platform == "*")
                platform = "Win32";

            PackageCreate cmd = new PackageCreate();
            cmd.RemoteRepoDir = RemoteRepoDir;
            cmd.CacheRepoDir = CacheRepoDir;
            cmd.RootDir = @"k:\Dev.C++.Packages\" + name + "\\";
            cmd.Platform = platform;
            cmd.IncrementBuild = true;
            cmd.Execute();
        }

        public static void Install(string name, string platform)
        {
            if (platform == "*")
                platform = "Win32";

            PackageInstall cmd = new PackageInstall();
            cmd.RemoteRepoDir = RemoteRepoDir;
            cmd.CacheRepoDir = CacheRepoDir;
            cmd.RootDir = @"k:\Dev.C++.Packages\" + name + "\\";
            cmd.Platform = platform;
            cmd.Execute();
        }

        public static void Deploy(string name, string platform)
        {
            if (platform == "*")
                platform = "Win32";

            PackageDeploy cmd = new PackageDeploy();
            cmd.RemoteRepoDir = RemoteRepoDir;
            cmd.CacheRepoDir = CacheRepoDir;
            cmd.RootDir = @"k:\Dev.C++.Packages\" + name + "\\";
            cmd.Platform = platform;
            cmd.Execute();
        }

        public static void Construct(string name, string platform)
        {
            PackageConstruct construct = new PackageConstruct();
            construct.Name = name;
            construct.RootDir = @"k:\Dev.C++.Packages\";
            construct.CacheRepoDir = CacheRepoDir;
            construct.RemoteRepoDir = RemoteRepoDir;
            construct.TemplateDir = TemplateDir;
            construct.Language = "C++";
            //construct.Action = "init";
            //construct.Execute();
            construct.RootDir = construct.RootDir + construct.Name + "\\";
            //construct.Action = "dir";
            //construct.Execute();
            construct.Language = "C++";
            construct.Platform = platform;
            construct.Action = "vs2010";
            construct.Execute();
        }
    }
}
