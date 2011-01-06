﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSBuild.XCode
{
    public interface IPackageFilename
    {
        string Name { get; set; }
        ComparableVersion Version { get; set; }
        DateTime? DateTime { get; set; }
        string Branch { get; set; }
        string Platform { get; set; }
        string Extension { get; set; }
    }

    public class PackageFilename : IPackageFilename
    {
        public PackageFilename()
        {
            Name = string.Empty;
            Version = new ComparableVersion("1.0.0");
            DateTime = null;
            Branch = "default";
            Platform = "Win32";
            Extension = ".zip";
        }

        public PackageFilename(IPackageFilename filename)
        {
            Name = filename.Name;
            Version = new ComparableVersion(filename.Version);
            if (filename.DateTime.HasValue)
                DateTime = new DateTime(filename.DateTime.Value.Ticks);
            else
                DateTime = null;
            Branch = filename.Branch;
            Platform = filename.Platform;
            Extension = filename.Extension;
        }
        public PackageFilename(string filename)
        {
            if (filename.EndsWith(".zip"))
                filename = System.IO.Path.GetFileNameWithoutExtension(filename);

            string[] parts = filename.Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
            Name = parts[0];
            Version = new ComparableVersion(parts.Length>1 ? parts[1] : "1.0.0");
            DateTime = null;
            Branch = parts.Length>2 ? parts[2] : "default";
            Platform = parts.Length>2 ? parts[3] : "Win32";
            Extension = ".zip";
        }
        public PackageFilename(string name, ComparableVersion version, string branch, string platform)
        {
            Name = name;
            Version = version;
            DateTime = null;
            Branch = branch;
            Platform = platform;
            Extension = ".zip";
        }
        public PackageFilename(string name, ComparableVersion version, DateTime dateTime, string branch, string platform)
            : this(name, version, branch, platform)
        {
            DateTime = dateTime;
        }

        public string Name { get; set; }
        public ComparableVersion Version { get; set; }
        public DateTime? DateTime { get; set; }
        public string Branch { get; set; }
        public string Platform { get; set; }
        public string Extension { get; set; }

        public override string ToString()
        {
            string datetime = string.Empty;
            if (DateTime.HasValue)
            {
                DateTime dt = DateTime.Value;
                datetime = String.Format(".{0:yyyy.M.d.H.m.s}", dt);
            }
            return String.Format("{0}+{1}{2}+{3}+{4}{5}", Name, Version.ToString(), datetime, Branch, Platform, Extension);
        }
    }
}
