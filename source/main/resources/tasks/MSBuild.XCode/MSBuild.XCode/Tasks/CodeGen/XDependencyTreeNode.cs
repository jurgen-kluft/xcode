﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSBuild.XCode
{
    public class XDepNode
    {
        public string Name { get; set; }
        public bool Done { get; set; }
        public int Depth { get; set; }
        public XDependency Dependency { get; set; }
        public XVersion Version { get; set; }
        public Dictionary<string, XDepNode> Children { get; set; }
        public XPackage Package { get; set; }

        public XDepNode(XDependency dep, int depth)
        {
            Name = dep.Name;
            Done = false;
            Depth = depth;
            Version = null;
            Dependency = dep;
            Children = null;
            Package = null;
        }

        public List<XDepNode> Build(Dictionary<string, XDepNode> dependencyFlatMap, string Platform)
        {
            // Sync remote repo to local repo which will cache the best version in our local repo
            // Obtain the package from the local repo of the best version
            // Get the dependencies of that package and add them as children
            // - Some dependencies already have been processed, maybe resulting in a different best version due to a different branch of version range

            if (!Done)
            {
                List<XDepNode> newDepNodes = new List<XDepNode>();

                Package = new XPackage();
                Package.Name = Dependency.Name;
                Package.Group = Dependency.Group;
                Package.Branch = Dependency.GetBranch(Platform);
                Package.Platform = Platform;
                Package.Remote = true;

                if (XGlobal.RemoteRepo.Checkout(Package, Dependency.GetVersionRange(Package.Platform)))
                {
                    XGlobal.LocalRepo.Checkin(Package);
                    Package.LoadPom();

                    Children = new Dictionary<string, XDepNode>();
                    Dictionary<string, XDepNode> dependencyTreeMap = Children;
                    foreach (XDependency d in Package.Pom.Dependencies)
                    {
                        XDepNode depNode;
                        if (!dependencyFlatMap.TryGetValue(d.Name, out depNode))
                        {
                            depNode = new XDepNode(d, Depth + 1);
                            newDepNodes.Add(depNode);
                            dependencyTreeMap.Add(depNode.Name, depNode);
                            dependencyFlatMap.Add(depNode.Name, depNode);
                        }
                        else
                        {
                            // Check if we need to process it again, the criteria are:
                            // - If ((Depth + 1) < depNode.Depth)
                            //   - Replace Dependency with this one
                            // - If ((Depth + 1) == depNode.Depth)
                            //   - prefer default branch
                            //   - prefer latest version
                            if (depNode.Depth > (Depth + 1))
                            {
                                // Take this dependency
                                if (depNode.ReplaceDependency(d, Depth + 1))
                                {
                                    // Dependency is modified, we have to process it again
                                    newDepNodes.Add(depNode);
                                }
                            }
                            else if (depNode.Depth == (Depth + 1))
                            {
                                // If merging these dependencies results in a modified dependency then we have to build it again
                                if (depNode.Dependency.Merge(d))
                                {
                                    // Name is still the same
                                    depNode.Depth = Depth + 1;
                                    depNode.Children = null;
                                    depNode.Done = false;

                                    // For now register it as a new DepNode
                                    newDepNodes.Add(depNode);
                                }
                            }
                            else
                            {
                                dependencyTreeMap.Add(depNode.Name, depNode);
                            }
                        }
                    }
                }
                else
                {
                    // Error, Dependency Tree : Build, Node=Name, unable to load dependency package for Name/Group/Branch/Platform/Version
                }
                Done = true;
                return newDepNodes;
            }
            return null;
        }

        public bool ReplaceDependency(XDependency dependency, int depth)
        {
            Dependency = dependency;
            if (!Dependency.IsEqual(dependency))
            {
                bool processAgain = Done;
                Depth = depth;
                Children = null;
                Done = false;
                return processAgain;
            }
            return false;
        }

        public void Print(string indent)
        {
            if (String.IsNullOrEmpty(indent)) indent = "+";
            else if (indent == "+") indent = "|----+";
            else indent = "     " + indent;

            Console.WriteLine(String.Format("{0} {1}, version={2}, type={3}", indent, Name, Version.ToString(), Dependency.Type));

            if (Children != null)
            {
                foreach (XDepNode child in Children.Values)
                    child.Print(indent);
            }
        }

    }
}
