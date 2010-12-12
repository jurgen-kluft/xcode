﻿using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;
using System.Text;
using System.Runtime;
using Ionic.Zip;
using Ionic.Zlib;
using MSBuild.XCode.Helpers;

namespace MSBuild.XCode
{
    public class PackageCreate : Task
    {
        public string Path { get; set; }
        public string Platform { get; set; }
        public string Branch { get; set; }

        [Output]
        public string Filename { get; set; }

        public override bool Execute()
        {
            bool success = false;

            if (!Path.EndsWith("\\"))
                Path = Path + "\\";

            XPackage package = new XPackage();
            package.Load(Path + "package.xml");

            /// 1) Create zip file
            /// 2) For every file create an MD5 and gather them into a sfv file
            /// 3) Remove root from every source file
            /// 4) Set the work directory
            /// 5) Add files to zip
            /// 6) Close
            /// 
            xDirname dir = new xDirname(Path + "target\\" + package.Name + "\\" + Platform);
            DirectoryScanner scanner = new DirectoryScanner(dir);
            scanner.scanSubDirs = true;
            scanner.collect(new xDirname(""), "*.*", DirectoryScanner.EmptyFilterDelegate);

            Environment.CurrentDirectory = dir.ToString();

            MD5CryptoServiceProvider md5_provider = new MD5CryptoServiceProvider();
            Dictionary<string, byte[]> crcDict = new Dictionary<string, byte[]>();
            List<KeyValuePair<string, string>> sourceFilenames = new List<KeyValuePair<string, string>>();
            foreach (xFilename filename in scanner.filenames)
            {
                string src_filename = package.Name + "\\" + Platform + "\\" + filename;

                FileStream fs = new FileStream(Path + "target\\" + src_filename, FileMode.Open, FileAccess.Read);
                byte[] md5 = md5_provider.ComputeHash(fs);
                fs.Close();
                crcDict.Add(src_filename, md5);

                string zip_filename = src_filename;
                sourceFilenames.Add(new KeyValuePair<string, string>(src_filename, System.IO.Path.GetDirectoryName(src_filename)));
            }

            Environment.CurrentDirectory = Path + "target\\";

            string sfv_filename = package.Name + ".md5";
            using (FileStream wfs = new FileStream(sfv_filename, FileMode.Create))
            { 
                StreamWriter writer = new StreamWriter(wfs);
                writer.WriteLine("; Generated by MSBuild.XCode");
                foreach (KeyValuePair<string, byte[]> k in crcDict)
                {
                    writer.WriteLine("{0} *{1}", k.Key, StringTools.MD5ToString(k.Value));
                }
                writer.Close();
                wfs.Close();

                sourceFilenames.Add(new KeyValuePair<string, string>(sfv_filename, ""));
            }

            // Versioning:
            // - Get package.Version
            // - Build = (DateTime.Year).(DateTime.Month).(DateTime.Day).(DateTime.Hour).(DateTime.Minute).(DateTime.Second)

            // VCS:
            // - Get revision information and write it into a file
            // - Add that file to the source file list to include it into the zip package
            // @TODO

            DateTime t = DateTime.Now;
            string version = package.Version.ToString() + String.Format(".{0}.{1}.{2}.{3}.{4}.{5}", t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second);

            Filename = package.Name + "-" + version + "-" + Branch + "-" + Platform + ".zip";
            if (File.Exists(Filename))
            {
                try { File.Delete(Filename); }
                catch(Exception) {}
            }

            using (ZipFile zip = new ZipFile(Filename))
            {
                foreach (KeyValuePair<string, string> p in sourceFilenames)
                    zip.AddFile(Path + "target\\" + p.Key, p.Value);

                zip.Save();
                success = true;
            }
            return success;
        }
    }
}
