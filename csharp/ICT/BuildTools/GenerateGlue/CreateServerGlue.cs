﻿//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       timop
//
// Copyright 2004-2013 by OM International
//
// This file is part of OpenPetra.org.
//
// OpenPetra.org is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OpenPetra.org is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OpenPetra.org.  If not, see <http://www.gnu.org/licenses/>.
//
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Reflection;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory;
using NamespaceHierarchy;
using Ict.Common;
using Ict.Common.IO;
using Ict.Tools.CodeGeneration;
using GenerateSharedCode;

namespace GenerateGlue
{
/// <summary>
/// generate the code for the server
/// </summary>
public class GenerateServerGlue
{
    static private string FTemplateDir = string.Empty;
    static SortedList <string, string>FUsingNamespaces = null;

    static private ProcessTemplate CreateModuleAccessPermissionCheck(ProcessTemplate ATemplate,
        string AConnectorClassWithNamespace,
        MethodDeclaration m)
    {
        if (m.Attributes != null)
        {
            foreach (AttributeSection attrSection in m.Attributes)
            {
                foreach (ICSharpCode.NRefactory.Ast.Attribute attr in attrSection.Attributes)
                {
                    if (attr.Name == "RequireModulePermission")
                    {
                        ProcessTemplate snippet = ATemplate.GetSnippet("CHECKUSERMODULEPERMISSIONS");
                        snippet.SetCodelet("METHODNAME", m.Name);
                        snippet.SetCodelet("CONNECTORWITHNAMESPACE", AConnectorClassWithNamespace);
                        snippet.SetCodelet("LEDGERNUMBER", "");

                        string ParameterTypes = ";";

                        foreach (ParameterDeclarationExpression p in m.Parameters)
                        {
                            if (p.ParameterName == "ALedgerNumber")
                            {
                                snippet.SetCodelet("LEDGERNUMBER", ", ALedgerNumber");
                            }

                            string ParameterType = p.TypeReference.Type.Replace("&", "").Replace("System.", String.Empty);

                            if (ParameterType == "List")
                            {
                                ParameterType = ParameterType.Replace("List", "List[" + p.TypeReference.GenericTypes[0].ToString() + "]");
                                ParameterType = ParameterType.Replace("System.", String.Empty);
                            }

                            if (ParameterType == "Dictionary")
                            {
                                ParameterType = ParameterType.Replace("Dictionary", "Dictionary[" +
                                    p.TypeReference.GenericTypes[0].ToString() + "," +
                                    p.TypeReference.GenericTypes[1].ToString() + "]");
                                ParameterType = ParameterType.Replace("System.", String.Empty);
                            }

                            if (ParameterType.Contains("."))
                            {
                                ParameterType = ParameterType.Substring(ParameterType.LastIndexOf(".") + 1);
                            }

                            if (p.TypeReference.Type == "System.Nullable")
                            {
                                ParameterType = ParameterType.Replace("Nullable", "Nullable[" + p.TypeReference.GenericTypes[0].ToString() + "]");
                            }

                            if (p.TypeReference.IsArrayType)
                            {
                                ParameterType += ".ARRAY";
                            }

                            ParameterType = ParameterType.Replace("Boolean", "bool");
                            ParameterType = ParameterType.Replace("Int32", "int");
                            ParameterType = ParameterType.Replace("Int64", "long");

                            ParameterTypes += ParameterType + ";";
                        }

                        ParameterTypes = ParameterTypes.ToUpper();
                        snippet.SetCodelet("PARAMETERTYPES", ParameterTypes);
                        return snippet;
                    }
                }
            }
        }

        TLogging.Log("Warning !!! Missing module access permissions for " + AConnectorClassWithNamespace + "::" + m.Name);

        return new ProcessTemplate();
    }

    /// <summary>
    /// insert a method call
    /// </summary>
    private static void InsertMethodCall(ProcessTemplate snippet, TypeDeclaration connectorClass, MethodDeclaration m, ref List <string>AMethodNames)
    {
        string ParameterDefinition = string.Empty;
        string ActualParameters = string.Empty;

        snippet.SetCodelet("LOCALVARIABLES", string.Empty);
        string returnCode = string.Empty;

        foreach (ParameterDeclarationExpression p in m.Parameters)
        {
            string parametertype = p.TypeReference.ToString();

            // check if the parametertype is not a generic type, eg. dictionary or list
            if (!parametertype.Contains("<"))
            {
                parametertype = parametertype == "string" || parametertype == "String" ? "System.String" : parametertype;
                parametertype = parametertype == "bool" || parametertype == "Boolean" ? "System.Boolean" : parametertype;
                parametertype = parametertype.Contains("Int32") || parametertype == "int" ? "System.Int32" : parametertype;
                parametertype = parametertype.Contains("Int16") || parametertype == "short" ? "System.Int16" : parametertype;
                parametertype = parametertype.Contains("Int64") || parametertype == "long" ? "System.Int64" : parametertype;
            }

            bool BinaryParameter = !((parametertype == "System.Int64") || (parametertype == "System.Int32") || (parametertype == "System.Int16")
                                     || (parametertype == "System.String") || (parametertype == "System.Boolean"));

            if (ActualParameters.Length > 0)
            {
                ActualParameters += ", ";
            }

            // ignore out parameters in the web service method definition
            if ((ParameterModifiers.Out & p.ParamModifier) == 0)
            {
                if (ParameterDefinition.Length > 0)
                {
                    ParameterDefinition += ", ";
                }

                if (!BinaryParameter)
                {
                    ParameterDefinition += parametertype + " " + p.ParameterName;
                }
                else
                {
                    ParameterDefinition += "string " + p.ParameterName;
                }
            }

            if ((ParameterModifiers.Out & p.ParamModifier) != 0)
            {
                snippet.AddToCodelet("LOCALVARIABLES", parametertype + " " + p.ParameterName + ";" + Environment.NewLine);
                ActualParameters += "out " + p.ParameterName;
            }
            else if ((ParameterModifiers.Ref & p.ParamModifier) != 0)
            {
                if (BinaryParameter)
                {
                    snippet.AddToCodelet("LOCALVARIABLES", parametertype + " Local" + p.ParameterName + " = " +
                        " (" + parametertype + ")THttpBinarySerializer.DeserializeObject(" + p.ParameterName + ",\"binary\");" +
                        Environment.NewLine);
                    ActualParameters += "ref Local" + p.ParameterName;
                }
                else
                {
                    ActualParameters += "ref " + p.ParameterName;
                }
            }
            else
            {
                if (BinaryParameter)
                {
                    ActualParameters += "(" + parametertype + ")THttpBinarySerializer.DeserializeObject(" + p.ParameterName + ",\"binary\")";
                }
                else
                {
                    ActualParameters += p.ParameterName;
                }
            }

            if (((ParameterModifiers.Ref & p.ParamModifier) > 0) || ((ParameterModifiers.Out & p.ParamModifier) > 0))
            {
                returnCode +=
                    (returnCode.Length > 0 ? "+\",\"+" : string.Empty) + "THttpBinarySerializer.SerializeObjectWithType(" + p.ParameterName + ")";
            }
        }

        string returntype = AutoGenerationTools.TypeToString(m.TypeReference, "");

        if (returnCode.Length > 0)
        {
            if (returntype != "void")
            {
                returnCode += (returnCode.Length > 0 ? "+\",\"+" : string.Empty) + "THttpBinarySerializer.SerializeObjectWithType(Result)";
            }

            returntype = "string";
        }
        else if (!((returntype == "System.Int64") || (returntype == "System.Int32") || (returntype == "System.Int16")
                   || (returntype == "System.String") || (returntype == "System.Boolean")) && (returntype != "void"))
        {
            returntype = "string";
            returnCode = "THttpBinarySerializer.SerializeObject(Result)";
        }

        string localreturn = AutoGenerationTools.TypeToString(m.TypeReference, "");

        if (localreturn == "void")
        {
            localreturn = string.Empty;
        }
        else if (returnCode.Length > 0)
        {
            localreturn += " Result = ";
        }
        else
        {
            localreturn = "return ";
        }

        snippet.SetCodelet("RETURN", string.Empty);

        if (returnCode.Length > 0)
        {
            snippet.SetCodelet("RETURN", returntype != "void" ? "return " + returnCode + ";" : string.Empty);
        }

        // avoid duplicate names for webservice methods
        string methodname = m.Name;
        int methodcounter = 1;

        while (AMethodNames.Contains(methodname))
        {
            methodcounter++;
            methodname = m.Name + methodcounter.ToString();
        }

        AMethodNames.Add(methodname);

        snippet.SetCodelet("UNIQUEMETHODNAME", methodname);
        snippet.SetCodelet("METHODNAME", m.Name);
        snippet.SetCodelet("PARAMETERDEFINITION", ParameterDefinition);
        snippet.SetCodelet("RETURNTYPE", returntype);
        snippet.SetCodelet("LOCALRETURN", localreturn);
        snippet.SetCodelet("WEBCONNECTORCLASS", connectorClass.Name);
        snippet.SetCodelet("ACTUALPARAMETERS", ActualParameters);
        snippet.InsertSnippet("CHECKUSERMODULEPERMISSIONS",
            CreateModuleAccessPermissionCheck(
                snippet,
                connectorClass.Name,
                m));
    }

    static private void WriteWebConnector(string connectorname, TypeDeclaration connectorClass, ProcessTemplate Template)
    {
        List <string>MethodNames = new List <string>();

        foreach (MethodDeclaration m in CSParser.GetMethods(connectorClass))
        {
            if (TCollectConnectorInterfaces.IgnoreMethod(m.Attributes, m.Modifier))
            {
                continue;
            }

            string connectorNamespaceName = ((NamespaceDeclaration)connectorClass.Parent).Name;

            if (!FUsingNamespaces.ContainsKey(connectorNamespaceName))
            {
                FUsingNamespaces.Add(connectorNamespaceName, connectorNamespaceName);
            }

            ProcessTemplate snippet = Template.GetSnippet("WEBCONNECTOR");

            InsertMethodCall(snippet, connectorClass, m, ref MethodNames);

            Template.InsertSnippet("WEBCONNECTORS", snippet);
        }
    }

    static private void CreateServerGlue(TNamespace tn, SortedList <string, TypeDeclaration>connectors, string AOutputPath)
    {
        String OutputFile = AOutputPath + Path.DirectorySeparatorChar + "ServerGlue.M" + tn.Name +
                            "-generated.cs";

        Console.WriteLine("working on " + OutputFile);

        ProcessTemplate Template = new ProcessTemplate(FTemplateDir + Path.DirectorySeparatorChar +
            "ClientServerGlue" + Path.DirectorySeparatorChar +
            "ServerGlue.cs");

        // load default header with license and copyright
        Template.SetCodelet("GPLFILEHEADER", ProcessTemplate.LoadEmptyFileComment(FTemplateDir));

        Template.SetCodelet("TOPLEVELMODULE", tn.Name);
        Template.SetCodelet("WEBCONNECTORS", string.Empty);

        string InterfacePath = Path.GetFullPath(AOutputPath).Replace(Path.DirectorySeparatorChar, '/');
        InterfacePath = InterfacePath.Substring(0, InterfacePath.IndexOf("csharp/ICT/Petra")) + "csharp/ICT/Petra/Shared/lib/Interfaces";
        Template.AddToCodelet("USINGNAMESPACES", CreateInterfaces.AddNamespacesFromYmlFile(InterfacePath, tn.Name));

        FUsingNamespaces = new SortedList <string, string>();

        foreach (string connector in connectors.Keys)
        {
            if (connector.Contains(":"))
            {
                // TODO UIConnector
            }
            else
            {
                WriteWebConnector(connector, connectors[connector], Template);
            }
        }

        foreach (string usingNamespace in FUsingNamespaces.Keys)
        {
            Template.AddToCodelet("USINGNAMESPACES", "using " + usingNamespace + ";" + Environment.NewLine);
        }

        Template.FinishWriting(OutputFile, ".cs", true);
    }

    /// <summary>
    /// write the server code
    /// </summary>
    static public void GenerateCode(TNamespace ANamespaces, String AOutputPath, String ATemplateDir)
    {
        if (TAppSettingsManager.GetValue("compileForStandalone", "no", false) == "yes")
        {
            // this code is not needed for standalone
            return;
        }

        FTemplateDir = ATemplateDir;

        foreach (TNamespace tn in ANamespaces.Children.Values)
        {
            string module = TAppSettingsManager.GetValue("module", "all");

            if ((module == "all") || (tn.Name == module))
            {
                SortedList <string, TypeDeclaration>connectors = TCollectConnectorInterfaces.GetConnectors(tn.Name);
                CreateServerGlue(tn, connectors, AOutputPath);
            }
        }
    }
}
}