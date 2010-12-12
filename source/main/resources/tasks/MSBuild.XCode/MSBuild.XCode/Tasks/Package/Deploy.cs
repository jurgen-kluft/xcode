﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using MSBuild.XCode.Helpers;

namespace MSBuild.XCode
{
    /// <summary>
    ///	Will copy a new package release to the remote-package-repository. 
    /// </summary>
    public class PackageDeploy : Task
    {
        public string Path { get; set; }
        public string Filename { get; set; }
        public string RepoPath { get; set; }

        public override bool Execute()
        {
            if (!Path.EndsWith("\\"))
                Path = Path + "\\";

            // if (!File.Exists(Path + "package.xml"))
            //    return false;

            // - Verify that there are no local changes 
            // - Verify that there are no outgoing changes
            // - Strip (Year).(Month).(Day).(Minute).(Second) from version of filename
            // - Commit version to remote package repository

            return false;
        }
    }
}
