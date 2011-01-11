﻿using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSBuild.XCode.Helpers;

namespace MSBuild.XCode
{
    /// <summary>
    /// </summary>
    public class CsProject
    {
        private bool mAllowRemoval;
        private XmlDocument mXmlDocMain;

        /// <summary>
        /// Copy a node from a source XmlDocument to a target XmlDocument
        /// </summary>
        /// <param name="domTarget">The XmlDocument to which we want to copy</param>
        /// <param name="node">The node we want to copy</param>
        private XmlNode CopyTo(XmlDocument xmlDoc, XmlNode xmlDocNode, XmlNode nodeToCopy)
        {
            XmlNode copy = xmlDoc.ImportNode(nodeToCopy, true);
            if (xmlDocNode != null)
                xmlDocNode.AppendChild(copy);
            else
                xmlDoc.AppendChild(copy);
            return copy;
        }

        public CsProject()
        {
            mXmlDocMain = new XmlDocument();
        }

        public CsProject(XmlNodeList nodes)
        {
            mXmlDocMain = new XmlDocument();
            foreach(XmlNode node in nodes)
                CopyTo(mXmlDocMain, null, node);
        }

        public bool Load(string filename)
        {
            if (File.Exists(filename))
            {
                mXmlDocMain = new XmlDocument();
                mXmlDocMain.Load(filename);
                return true;
            }
            return false;
        }

        public void RemovePlatform(string platform)
        {
            XmlDocument result = new XmlDocument();
            mAllowRemoval = true;
            Merge(result, mXmlDocMain,
                delegate(XmlNode node)
                {
                    return !HasCondition(node, platform, string.Empty);
                },
                delegate(XmlNode main, XmlNode other)
                {
                });

            mXmlDocMain = result;
            mAllowRemoval = false;
        }

        public void RemoveConfigForPlatform(string config, string platform)
        {
            XmlDocument result = new XmlDocument();
            mAllowRemoval = true; 
            Merge(result, mXmlDocMain,
                delegate(XmlNode node)
                {
                    return !HasCondition(node, platform, config);
                },
                delegate(XmlNode main, XmlNode other)
                {
                });

            mXmlDocMain = result;
            mAllowRemoval = false;
        }

        public void RemoveAllBut(Dictionary<string, StringItems> platformConfigs)
        {
            XmlDocument result = new XmlDocument();
            mAllowRemoval = true;
            string platform, config;
            Merge(result, mXmlDocMain,
                delegate(XmlNode node)
                {
                    if (GetPlatformConfig(node, out platform, out config))
                    {
                        StringItems items;
                        if (platformConfigs.TryGetValue(platform, out items))
                        {
                            return (items.Contains(config));
                        }
                        return false;
                    }
                    return true;
                },
                delegate(XmlNode main, XmlNode other)
                {
                });

            mXmlDocMain = result;
            mAllowRemoval = false;
        }

        public bool FilterItems(string[] to_remove, string[] to_keep)
        {
            Merge(mXmlDocMain, mXmlDocMain,
                delegate(XmlNode node)
                {
                    return true;
                },
                delegate(XmlNode main, XmlNode other)
                {
                    if (main.ParentNode.Name == "DefineConstants")
                    {
                        StringItems items = new StringItems();
                        items.Add(main.Value, true);
                        items.Filter(to_remove, to_keep);
                        main.Value = items.ToString();
                    }
                });

            return true;
        }

        /// if action == "Compile" and filename.endswith(".cs")
        ///    if (filename.endswith(".designer.cs"))
        ///        basename = filename.replace(".designer.cs", ".cs")
        ///        if (files.has(basename))
        ///            return ["dependency", basename]
        ///        endif
        ///        basename = basename.replace(".cs", ".resx")
        ///        if (files.has(basename))
        ///            return ["AutoGen", basename]
        ///        endif
        ///    else
        ///        basename = filename.replace(".cs", ".designer.cs")
        ///        if (files.has(basename))
        ///            return "SubTypeForm"
        ///        endif
        ///    endif
        /// endif
        ///
        ///
        /// if action == "EmbeddedResource" and filename.endswith(".resx")
        ///    basename = filename.replace(".resx", ".cs")
        ///    if (files.has(basename))
        ///        testname = basename.replace(".cs", ".designer.cs")
        ///        if (files.has(testname))
        ///            return ["DesignerType", basename]
        ///        else
        ///            return ["Dependency", testname]
        ///        endif
        ///    else
        ///        testname = basename.replace(".cs", ".designer.cs")
        ///        if (files.has(testname))
        ///            return "AutoGenerated"
        ///        endif
        ///    endif
        /// endif
        /// 
        /// return "None"
        /// 

        private KeyValuePair<string, string> GetElements(HashSet<string> files, string action, string filename)
        {
            if (action == "Compile" && filename.EndsWith(".cs"))
            {
                if (filename.EndsWith(".designer.cs"))
                {
                    string basename = filename.Replace(".designer.cs", ".cs");
                    if (files.Contains(basename))
                        return new KeyValuePair<string, string>("Dependency", basename);

                    basename = basename.Replace(".cs", ".resx");
                    if (files.Contains(basename))
                        return new KeyValuePair<string, string>("AutoGen", basename);
                }
                else
                {
                    string basename = filename.Replace(".cs", ".designer.cs");
                    if (files.Contains(basename))
                        return new KeyValuePair<string, string>("SubTypeForm", string.Empty);
                }
            }
            else if (action == "EmbeddedResource" && filename.EndsWith(".resx"))
            {
                string basename = filename.Replace(".resx", ".cs");
                if (files.Contains(basename))
                {
                    string testname = basename.Replace(".cs", ".designer.cs");
                    if (files.Contains(testname))
                        return new KeyValuePair<string, string>("DesignerType", basename);
                    else
                        return new KeyValuePair<string, string>("Dependency", testname);
                }
                else
                {
                    string testname = basename.Replace(".cs", ".designer.cs");
                    if (files.Contains(testname))
                        return new KeyValuePair<string, string>("AutoGenerated", string.Empty);
                }
            }
            else if (action == "Content")
            {
                return new KeyValuePair<string, string>("CopyNewest", string.Empty);
            }

            return new KeyValuePair<string, string>("None", string.Empty);
        }

        class Comparer<T> : IEqualityComparer<T>
        {
            private readonly Func<T, T, bool> _comparer;
            public Comparer(Func<T, T, bool> comparer)
            {
                if (comparer == null) 
                    throw new ArgumentNullException("comparer");
                _comparer = comparer;
            } 
            public bool Equals(T x, T y) 
            {
                return _comparer(x, y);
            }
            public int GetHashCode(T obj)
            {
                return obj.ToString().ToLower().GetHashCode();
            }
        }

        public bool ExpandGlobs(string rootdir, string reldir)
        {
            List<XmlNode> removals = new List<XmlNode>();
            List<XmlNode> globs = new List<XmlNode>();

            Merge(mXmlDocMain, mXmlDocMain,
                delegate(XmlNode node)
                {
                    if (node.Name == "Compile" || node.Name == "EmbeddedResource" || node.Name == "Content" || node.Name == "None")
                    {
                        foreach (XmlAttribute a in node.Attributes)
                        {
                            if (a.Name == "Include")
                            {
                                if (a.Value.Contains('*'))
                                {
                                    globs.Add(node);
                                }
                                else if (String.IsNullOrEmpty(a.Value))
                                {
                                    removals.Add(node);
                                }
                            }
                        }

                    }
                    return true;
                },
                delegate(XmlNode main, XmlNode other)
                {
                });

            // Removal
            foreach (XmlNode node in removals)
            {
                XmlNode parent = node.ParentNode;
                parent.RemoveChild(node);
                if (parent.ChildNodes.Count == 0)
                {
                    XmlNode grandparent = parent.ParentNode;
                    grandparent.RemoveChild(parent);
                }
            }

            // Now do the globbing
            // First collect all files
            HashSet<string> allFiles = new HashSet<string>(new Comparer<string>((x, y) => String.Compare(x, y, true) == 0));
            List<HashSet<string>> filesPerNode = new List<HashSet<string>>();
            foreach (XmlNode node in globs)
            {
                string glob = node.Attributes[0].Value;
                int index = glob.IndexOf('*');

                HashSet<string> files = new HashSet<string>(new Comparer<string>((x, y) => String.Compare(x, y, true) == 0));
                List<string> globbedFiles = PathUtil.getFiles(rootdir + glob);
                foreach (string filename in globbedFiles)
                {
                    XmlNode newNode = node.CloneNode(false);
                    string filedir = PathUtil.RelativePathTo(reldir, Path.GetDirectoryName(filename));
                    if (!String.IsNullOrEmpty(filedir) && !filedir.EndsWith("\\"))
                        filedir += "\\";

                    string file = filedir + Path.GetFileName(filename);
                    if (!allFiles.Contains(file))
                    {
                        allFiles.Add(file);
                        if (!files.Contains(file))
                            files.Add(file);
                    }
                }
                filesPerNode.Add(files);
            }

            // Second update the xml nodes
            foreach (var nf in globs.Zip(filesPerNode, (n, f) => new { Node = n, Files = f }))
            //foreach (XmlNode node in globs)
            {
                XmlNode parent = nf.Node.ParentNode;
                parent.RemoveChild(nf.Node);

                foreach (string filename in nf.Files)
                {
                    XmlNode newNode = nf.Node.CloneNode(false);

                    KeyValuePair<string, string> element = GetElements(allFiles, nf.Node.Name, filename);
                    /// if element.Key == "None" then
                    /// 	_p('    <%s Include="%s" />', action, fname)
                    /// else
                    /// 	_p('    <%s Include="%s">', action, fname)
                    /// 	if element.Key == "AutoGen" then
                    /// 		_p('      <AutoGen>True</AutoGen>')
                    /// 	elseif element.Key == "AutoGenerated" then
                    /// 		_p('      <SubType>Designer</SubType>')
                    /// 		_p('      <Generator>ResXFileCodeGenerator</Generator>')
                    /// 		_p('      <LastGenOutput>%s.Designer.cs</LastGenOutput>', premake.esc(path.getbasename(fcfg.name)))
                    /// 	elseif element.Key == "SubTypeDesigner" then
                    /// 		_p('      <SubType>Designer</SubType>')
                    /// 	elseif element.Key == "SubTypeForm" then
                    /// 		_p('      <SubType>Form</SubType>')
                    /// 	elseif element.Key == "PreserveNewest" then
                    /// 		_p('      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>')
                    /// 	end
                    /// 	if (!String.IsNullOrEmpty(element.Value))
                    /// 		_p('      <DependentUpon>%s</DependentUpon>', path.translate(premake.esc(dependency), "\\"))
                    /// 	end
                    /// 	_p('    </%s>', action)
                    /// end

                    newNode.Attributes[0].Value = filename;
                    parent.AppendChild(newNode);
                }
            }

            return true;
        }

        private bool GetPlatformConfig(XmlNode node, out string platform, out string config)
        {
            if (node.Attributes != null)
            {
                foreach (XmlAttribute a in node.Attributes)
                {
                    int begin = -1;
                    int end = -1;

                    if (a.Name == "Condition")
                    {
                        int cursor = a.Value.IndexOf("==");
                        if (cursor >= 0)
                        {
                            cursor += 2;
                            if ((cursor + 1) < a.Value.Length && a.Value[cursor] == '\'')
                                cursor += 1;
                            else
                                cursor = -1;
                        }
                        begin = cursor;
                        end = begin>=0 ? a.Value.IndexOf("'", begin) : begin;
                    }
                    else if (a.Name == "Include")
                    {
                        begin = 0;
                        end = a.Value.Length;
                    }

                    if (begin >= 0 && end > begin)
                    {
                        string configplatform = a.Value.Substring(begin, end - begin);
                        string[] items = configplatform.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                        if (items.Length == 2)
                        {
                            config = items[0];
                            platform = items[1];
                            return true;
                        }
                        break;
                    }
                }
            }
            config = null;
            platform = null;
            return false;
        }
        private bool HasCondition(XmlNode node, string platform, string config)
        {
            if (node.Attributes != null)
            {
                foreach (XmlAttribute a in node.Attributes)
                {
                    if (a.Name == "Condition" || a.Name == "Include")
                    {
                        if (a.Value.Contains(String.Format("{0}|{1}", config, platform)))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private string GetItem(string platform, string config, string itemName)
        {
            StringItems concat = new StringItems();

            Merge(mXmlDocMain, mXmlDocMain,
                delegate(XmlNode node)
                {
                    return HasCondition(node, platform, config);
                },
                delegate(XmlNode main, XmlNode other)
                {
                    if (main.ParentNode.Name == itemName)
                    {
                        concat.Add(main.Value, true);
                        concat.Add(other.Value, true);
                    }
                });
            return concat.Get();
        }

        public bool GetPreprocessorDefinitions(string platform, string config, out string defines)
        {
            defines = GetItem(platform, config, "PreprocessorDefinitions");
            return true;
        }
        public bool GetAdditionalIncludeDirectories(string platform, string config, out string includeDirectories)
        {
            includeDirectories = GetItem(platform, config, "AdditionalIncludeDirectories");
            return true;
        }
        public bool GetAdditionalLibraryDirectories(string platform, string config, out string libraryDirectories)
        {
            libraryDirectories = GetItem(platform, config, "AdditionalLibraryDirectories");
            return true;
        }
        public bool GetAdditionalDependencies(string platform, string config, out string libraryDependencies)
        {
            libraryDependencies = GetItem(platform, config, "AdditionalDependencies");
            return true;
        }
        public string[] GetPlatforms()
        {
            HashSet<string> platforms = new HashSet<string>();
            string platform, config;

            Merge(mXmlDocMain, mXmlDocMain,
                delegate(XmlNode node)
                {
                    if (GetPlatformConfig(node, out platform, out config))
                    {
                        if (!platforms.Contains(platform))
                            platforms.Add(platform);
                    }
                    return true;
                },
                delegate(XmlNode main, XmlNode other)
                {
                });
            return platforms.ToArray();
        }
        public string[] GetPlatformConfigs(string platform)
        {
            HashSet<string> configs = new HashSet<string>();
            string _platform, config;

            Merge(mXmlDocMain, mXmlDocMain,
                delegate(XmlNode node)
                {
                    if (GetPlatformConfig(node, out _platform, out config))
                    {
                        if (String.Compare(platform, _platform, true) == 0)
                        {
                            if (!configs.Contains(config))
                                configs.Add(config);
                        }
                    }
                    return true;
                },
                delegate(XmlNode main, XmlNode other)
                {
                });
            return configs.ToArray();
        }

        public bool SetItem(string platform, string config, string itemName, string itemValue)
        {
            StringItems concat = new StringItems();
            concat.Add(itemValue, true);

            Merge(mXmlDocMain, mXmlDocMain,
                delegate(XmlNode node)
                {
                    return HasCondition(node, platform, config);
                },
                delegate(XmlNode main, XmlNode other)
                {
                    if (main.ParentNode.Name == itemName)
                    {
                        concat.Add(main.Value, true);
                        concat.Add(other.Value, true);
                        main.Value = concat.Get();
                    }
                });
            return true;
        }
        public bool SetPreprocessorDefinitions(string platform, string config, string defines)
        {
            return SetItem(platform, config, "PreprocessorDefinitions", defines);
        }
        public bool SetAdditionalIncludeDirectories(string platform, string config, string includeDirectories)
        {
            return SetItem(platform, config, "AdditionalIncludeDirectories", includeDirectories);
        }
        public bool SetAdditionalLibraryDirectories(string platform, string config, string libraryDirectories)
        {
            return SetItem(platform, config, "AdditionalLibraryDirectories", libraryDirectories);
        }
        public bool SetAdditionalDependencies(string platform, string config, string libraryDependencies)
        {
            return SetItem(platform, config, "AdditionalDependencies", libraryDependencies);
        }

        public bool Save(string filename)
        {
            mXmlDocMain.Save(filename);
            return true;
        }

        public bool Merge(CsProject project)
        {
            Merge(mXmlDocMain, project.mXmlDocMain,
                delegate(XmlNode node)
                {
                    return true;
                },
                delegate(XmlNode main, XmlNode other)
                {
                    // Merge:
                    // - DefineConstants
                    if (main.ParentNode.Name == "DefineConstants")
                    {
                        StringItems items = new StringItems();
                        items.Add(other.Value, true);
                        items.Add(main.Value, true);
                        main.Value = items.ToString();
                    }
                    else
                    {
                        // Replace
                        main.Value = other.Value;
                    }
                });
            return false;
        }

        private bool HasSameAttributes(XmlNode a, XmlNode b)
        {
            if (a.Attributes == null && b.Attributes == null)
                return true;
            if (a.Attributes != null && b.Attributes != null)
            {
                bool the_same = true;
                int na = 0;
                foreach (XmlAttribute aa in a.Attributes)
                {
                    if (aa.Name == "Concat")
                        continue;
                    ++na;
                }
                int nb = 0;
                foreach (XmlAttribute ab in b.Attributes)
                {
                    if (ab.Name == "Concat")
                        continue;
                    ++nb;
                }

                the_same = (na == nb);
                if (the_same)
                {
                    foreach (XmlAttribute aa in a.Attributes)
                    {
                        if (aa.Name == "Concat")
                            continue;

                        bool found = false;
                        foreach (XmlAttribute ab in b.Attributes)
                        {
                            if (ab.Name == "Concat")
                                continue;

                            if (ab.Name == aa.Name)
                            {
                                if (ab.Value == aa.Value)
                                {
                                    found = true;
                                    break;
                                }
                            }
                        }
                        if (!found)
                        {
                            the_same = false;
                            break;
                        }
                    }
                }
                return the_same;
            }
            return false;
        }

        private XmlNode FindNode(XmlNode nodeToFind, XmlNodeList children)
        {
            // vcxproj has multiple <ItemGroup> nodes with different content, we
            // need to make sure we pick the right one

            XmlNode foundNode = null;
            foreach (XmlNode child in children)
            {
                // First, match by name
                if (child.Name == nodeToFind.Name)
                {
                    // Now see if the attributes match
                    if (HasSameAttributes(nodeToFind, child))
                    {
                        if (!nodeToFind.HasChildNodes && !child.HasChildNodes)
                        {
                            foundNode = child;
                            break;
                        }
                        else if (nodeToFind.HasChildNodes && child.HasChildNodes)
                        {
                            if (nodeToFind.Name == "ItemGroup" && nodeToFind.Attributes.Count==0)
                            {
                                if (nodeToFind.ChildNodes[0].Name == child.ChildNodes[0].Name)
                                {
                                    foundNode = child;
                                    break;
                                }
                            }
                            else
                            {
                                foundNode = child;
                                break;
                            }
                        }
                        
                    }
                }
            }
            return foundNode;
        }

        public delegate void NodeMergeDelegate(XmlNode main, XmlNode other);
        public delegate bool NodeConditionDelegate(XmlNode node);

        private void LockStep(XmlDocument mainXmlDoc, XmlDocument otherXmlDoc, Stack<XmlNode> mainPath, Stack<XmlNode> otherPath, NodeConditionDelegate nodeConditionDelegate, NodeMergeDelegate nodeMergeDelegate)
        {
            XmlNode mainNode = mainPath.Peek();
            XmlNode otherNode = otherPath.Peek();

            if (mainNode.NodeType == XmlNodeType.Comment)
            {
            }
            else if (mainNode.NodeType == XmlNodeType.Text)
            {
                nodeMergeDelegate(mainNode, otherNode);
            }
            else
            {
                foreach (XmlNode otherChildNode in otherNode.ChildNodes)
                {
                    XmlNode mainChildNode = FindNode(otherChildNode, mainNode.ChildNodes);
                    if (mainChildNode == null)
                    {
                        if (nodeConditionDelegate(otherChildNode))
                        {
                            mainChildNode = CopyTo(mainXmlDoc, mainNode, otherChildNode);

                            mainPath.Push(mainChildNode);
                            otherPath.Push(otherChildNode);
                            LockStep(mainXmlDoc, otherXmlDoc, mainPath, otherPath, nodeConditionDelegate, nodeMergeDelegate);
                        }
                    }
                    else
                    {
                        if (nodeConditionDelegate(mainChildNode))
                        {
                            mainPath.Push(mainChildNode);
                            otherPath.Push(otherChildNode);
                            LockStep(mainXmlDoc, otherXmlDoc, mainPath, otherPath, nodeConditionDelegate, nodeMergeDelegate);
                        }
                        else if (mAllowRemoval)
                        {
                            // Removal
                            mainNode.RemoveChild(mainChildNode);
                        }
                    }
                }
            }
        }

        private void Merge(XmlDocument mainXmlDoc, XmlDocument otherXmlDoc, NodeConditionDelegate nodeConditionDelegate, NodeMergeDelegate nodeMergeDelegate)
        {
            // Lock-Step Merge the xml tree
            // 1) When encountering a node which does not exist in the main doc, insert it
            Stack<XmlNode> mainPath = new Stack<XmlNode>();
            Stack<XmlNode> otherPath = new Stack<XmlNode>();
            foreach (XmlNode otherChildNode in otherXmlDoc)
            {
                XmlNode mainChildNode = FindNode(otherChildNode, mainXmlDoc.ChildNodes);
                if (mainChildNode == null)
                {
                    if (nodeConditionDelegate(otherChildNode))
                    {
                        mainChildNode = CopyTo(mainXmlDoc, null, otherChildNode);

                        mainPath.Push(mainChildNode);
                        otherPath.Push(otherChildNode);
                        LockStep(mainXmlDoc, otherXmlDoc, mainPath, otherPath, nodeConditionDelegate, nodeMergeDelegate);
                    }
                }
                else
                {
                    if (nodeConditionDelegate(mainChildNode))
                    {
                        mainPath.Push(mainChildNode);
                        otherPath.Push(otherChildNode);
                        LockStep(mainXmlDoc, otherXmlDoc, mainPath, otherPath, nodeConditionDelegate, nodeMergeDelegate);
                    }
                    else if (mAllowRemoval)
                    {
                        // Removal
                        mainXmlDoc.RemoveChild(mainChildNode);
                    }
                }
            }
        }
    }
}