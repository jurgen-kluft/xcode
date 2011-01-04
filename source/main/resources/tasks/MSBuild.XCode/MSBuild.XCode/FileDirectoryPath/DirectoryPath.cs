using System.IO;
using System;
using System.Collections.Generic;

namespace FileDirectoryPath
{
    public abstract class DirectoryPath : BasePath
    {
        protected DirectoryPath() { }  // Special for empty Path
        protected DirectoryPath(string path, bool isAbsolute)
            : base(path, isAbsolute)
        {
        }

        public override bool IsDirectoryPath { get { return true; } }
        public override bool IsFilePath { get { return false; } }



        //
        //  DirectoryName
        //
        public string DirectoryName { get { return InternalStringHelper.GetLastName(this.Path); } }
        public bool HasParentDir { get { return InternalStringHelper.HasParentDir(this.Path); } }
    }
}
