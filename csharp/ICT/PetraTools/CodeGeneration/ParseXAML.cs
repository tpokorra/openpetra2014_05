﻿/*************************************************************************
 *
 * DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
 *
 * @Authors:
 *       timop
 *
 * Copyright 2004-2009 by OM International
 *
 * This file is part of OpenPetra.org.
 *
 * OpenPetra.org is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * OpenPetra.org is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with OpenPetra.org.  If not, see <http://www.gnu.org/licenses/>.
 *
 ************************************************************************/
using System;
using DDW;
using System.Xml;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using Ict.Common.IO;
using Ict.Common;

namespace Ict.Tools.CodeGeneration
{
    /*
     * This class generally parses an XAML file independent of the kind of winform (report, etc)
     * The code is stored in TCodeModel
     * @see http://ict.om.org/PetraWiki/current/index.php/ICTYaml
     */
    public class TParseXAML
    {
        public TCodeStorage FCodeStorage = null;
        public TParseXAML(ref TCodeStorage ACodeStorage)
        {
            this.FCodeStorage = ACodeStorage;
        }

        public void LoadTemplateParameters(XmlNode AParameters)
        {
            FCodeStorage.FTemplateParameters = AParameters;
        }

        // parse basic things: BaseClass, FormTitle, Namespace
        public void LoadFormProperties(XmlNode formNode)
        {
            FCodeStorage.FBaseClass = TYml2Xml.GetAttribute(formNode, "BaseClass");
            FCodeStorage.FInterfaceName = TYml2Xml.GetAttribute(formNode, "InterfaceName");
            FCodeStorage.FUtilObjectClass = TYml2Xml.GetAttribute(formNode, "UtilObjectClass");
            FCodeStorage.FFormTitle = TYml2Xml.GetAttribute(formNode, "FormTitle");
            FCodeStorage.FNamespace = TYml2Xml.GetAttribute(formNode, "Namespace");
            FCodeStorage.FClassName = "TFrm" + System.IO.Path.GetFileNameWithoutExtension(FCodeStorage.FFilename);

            if (TYml2Xml.HasAttribute(formNode, "WindowHeight"))
            {
                FCodeStorage.FHeight = Convert.ToInt32(TYml2Xml.GetAttribute(formNode, "WindowHeight"));
            }

            if (TYml2Xml.HasAttribute(formNode, "WindowWidth"))
            {
                FCodeStorage.FWidth = Convert.ToInt32(TYml2Xml.GetAttribute(formNode, "WindowWidth"));
            }

            if (TYml2Xml.HasAttribute(formNode, "ClassName"))
            {
                FCodeStorage.FClassName = TXMLParser.GetAttribute(formNode, "ClassName");
            }
        }

        public void LoadLayout(XmlNode ALayoutNode)
        {
            if (ALayoutNode != null)
            {
                List <XmlNode>children = TYml2Xml.GetChildren(ALayoutNode, true);

                if ((children.Count > 0) && (children[0].Name == "Tabs"))
                {
                    foreach (XmlNode curNode in children)
                    {
                        AddTabPage(curNode);
                    }
                }
            }
        }

        // access permissions etc
        public void LoadSecurity(XmlNode ASecurityNode)
        {
            // todo
        }

        public Boolean LoadRecursively(string AXamlFilename)
        {
            return LoadRecursively(AXamlFilename, 0);
        }

        /**
         * this loads the contents of the yaml file
         * it supports inheritance, base elements are overwritten
         * @param depth 0 is the last file that is derived from all base files
         */
        protected Boolean LoadRecursively(string AXamlFilename,
            Int32 depth)
        {
            string baseyaml;

            if (!TYml2Xml.ReadHeader(AXamlFilename, out baseyaml))
            {
                throw new Exception("This is not an OpenPetra Yaml file");
            }

            if ((baseyaml.Length > 0) && baseyaml.EndsWith(".yaml"))
            {
                LoadRecursively(System.IO.Path.GetDirectoryName(AXamlFilename) +
                    System.IO.Path.DirectorySeparatorChar +
                    baseyaml,
                    depth - 1);
            }

            Console.WriteLine("Loading " + AXamlFilename + "...");

            if ((depth == 0) && (FCodeStorage.FXmlNodes != null))
            {
                // apply the tag, so that we know which things have been changed by the last yml file
                TYml2Xml.Tag((XmlNode)FCodeStorage.FXmlNodes["RootNode"]);
            }

            TYml2Xml yml2xml = new TYml2Xml(AXamlFilename);
            yml2xml.ParseYML2XML(FCodeStorage.FXmlDocument, depth);

            // for debugging:
            FCodeStorage.FXmlDocument.Save(AXamlFilename + ".xml");

            FCodeStorage.FXmlNodes = TYml2Xml.ReferenceNodes(FCodeStorage.FXmlDocument);
            FCodeStorage.FRootNode = (XmlNode)FCodeStorage.FXmlNodes["RootNode"];

            if (baseyaml.Length == 0)
            {
                if (FCodeStorage.FXmlNodes["RootNode"] == null)
                {
                    throw new Exception("TParseXAML.LoadRecursively: YML Document could not be properly parsed");
                }

                if (TXMLParser.GetAttribute((XmlNode)FCodeStorage.FXmlNodes["RootNode"], "BaseYaml").Length > 0)
                {
                    throw new Exception("The BaseYaml attribute must come first!");
                }
            }

            if (depth == 0)
            {
                FCodeStorage.FFilename = AXamlFilename;
                LoadData(FCodeStorage.FXmlNodes);
            }

            return true;
        }

        protected void LoadData(SortedList nodes)
        {
            LoadFormProperties((XmlNode)nodes["RootNode"]);
            LoadTemplateParameters((XmlNode)nodes["TemplateParameters"]);
            LoadSecurity((XmlNode)nodes["Security"]);
            LoadControls((XmlNode)nodes["Controls"]);
            LoadLayout((XmlNode)nodes["Layout"]);

            // todo: what about popup menus?; can contain menu items from the main menu
            if (FCodeStorage.HasRootControl("mnu"))
            {
                LoadMenu(FCodeStorage.GetRootControl("mnu").controlName, (XmlNode)nodes["Menu"]);
            }
            if (FCodeStorage.HasRootControl("tbr"))
            {
                LoadToolbar(FCodeStorage.GetRootControl("tbr").controlName, (XmlNode)nodes["Toolbar"]);
            }

            LoadActions((XmlNode)nodes["Actions"]);
            LoadEvents((XmlNode)nodes["Events"]);
        }

        Int32 FMenuSeparatorCount = 0;
        public Boolean LoadMenu(string parentName,
            XmlNode curNode)
        {
            string menuName = curNode.Name;

            if (menuName == "mniSeparator")
            {
                // UniqueName is not stored to yml again; just used temporary
                TYml2Xml.SetAttribute(curNode, "UniqueName", menuName + FMenuSeparatorCount.ToString());
                FMenuSeparatorCount++;
            }

            if (curNode.ParentNode.Name == "RootNode")
            {
                // add each menu, but obviously not the "Menu" tag
                XmlNode menuNode = curNode;
                List <XmlNode>children = TYml2Xml.GetChildren(menuNode, true);

                foreach (XmlNode childNode in children)
                {
                    LoadMenu(parentName, childNode);

                    // attach the menu to the appropriate root control
                    XmlNode rootMenu = FCodeStorage.GetRootControl("mnu").xmlNode;
                    rootMenu.AppendChild(childNode);
                }

                return true;
            }

            TControlDef menuItem = FCodeStorage.AddControl(curNode);
            menuItem.parentName = parentName;
            List <XmlNode>children2 = TYml2Xml.GetChildren(curNode, true);

            foreach (XmlNode childNode in children2)
            {
                // the check for mni works around problems with elements list, shortcutkeys
                if (childNode.Name.StartsWith("mni"))
                {
                    LoadMenu(menuName, childNode);
                }
            }

            return true;
        }

        Int32 FToolbarSeparatorCount = 0;
        public Boolean LoadToolbar(string parentName,
            XmlNode curNode)
        {
            List <XmlNode>children = TYml2Xml.GetChildren(curNode, true);

            XmlNode rootBarNode = FCodeStorage.GetRootControl("tbr").xmlNode;
            bool UsePreviousNode = false;

            foreach (XmlNode childNode in children)
            {
                // the check for tbb works around problems with elements list, shortcutkeys
                XmlNode prevNode = childNode;

                if (childNode.Name.StartsWith("tbb"))
                {
                    UsePreviousNode = true;
                    string tbbName = childNode.Name;

                    if (tbbName == "tbbSeparator")
                    {
                        // UniqueName is not stored to yml again; just used temporary
                        TYml2Xml.SetAttribute(childNode, "UniqueName", tbbName + FToolbarSeparatorCount.ToString());
                        FToolbarSeparatorCount++;
                    }

                    TControlDef tbbItem = FCodeStorage.AddControl(childNode);
                    tbbItem.parentName = parentName;
                }

                if (UsePreviousNode)
                {
                    rootBarNode.AppendChild(prevNode);
                }

                UsePreviousNode = false;
            }

            return true;
        }

        protected void LoadControls(XmlNode curNode)
        {
            if (curNode != null)
            {
                List <XmlNode>children = TYml2Xml.GetChildren(curNode, true);

                foreach (XmlNode childNode in children)
                {
                    TControlDef ctrl = FCodeStorage.AddControl(childNode);
                }
            }
        }

        protected void LoadActions(XmlNode curNode)
        {
            if (curNode != null)
            {
                List <XmlNode>children = TYml2Xml.GetChildren(curNode, true);

                foreach (XmlNode childNode in children)
                {
                    TActionHandler ctrl = FCodeStorage.AddAction(childNode);
                }
            }
        }

        protected void LoadEvents(XmlNode curNode)
        {
            if (curNode != null)
            {
                List <XmlNode>children = TYml2Xml.GetChildren(curNode, true);

                foreach (XmlNode childNode in children)
                {
                    TEventHandler TempEvent = FCodeStorage.AddEvent(childNode);
                }
            }
        }

        public Boolean AddTabPage(XmlNode curNode)
        {
            // name of tabpage
            TControlDef tabPage = FCodeStorage.AddControl(curNode);

            tabPage.parentName = FCodeStorage.GetRootControl("tab").controlName;
            curNode = TXMLParser.NextNotBlank(curNode.FirstChild);

            if (curNode != null)
            {
                if (curNode.Name == "Controls")
                {
                    // one control per row, align labels
                    StringCollection controls = TYml2Xml.GetElements(curNode);

                    foreach (string ctrlName in controls)
                    {
                        TControlDef ctrl = FCodeStorage.GetControl(ctrlName);

                        if (ctrl != null)
                        {
                            ctrl.parentName = tabPage.controlName;
                        }
                    }
                }
            }

            return true;
        }
    }
}