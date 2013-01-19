// Auto generated by nant generateGlue
// From a template at inc\template\src\ClientServerGlue\ClientGlue.Connector.cs
//
// Do not modify this file manually!
//
{#GPLFILEHEADER}

using System;
using System.IO;
using System.Threading;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Ict.Common;
using Ict.Common.Remoting.Shared;
{#USINGNAMESPACES}

namespace Ict.Common.Remoting.Client
{
    /// generated code because the standalone application will link to the server,
    /// but the client application should not contain all the server dlls
    public class TConnectionHelper
    {
        /// connect to the server
        static public IClientManagerInterface Connect()
        {
            {#CONNECTOR}
        }
    }
}

{##CONNECTORSTANDALONE}
// this is code for the standalone openpetra, there is no server, only one single application
TServerManager TheServerManager = new TServerManager();

//
// Connect to main Database
//
try
{
    TheServerManager.EstablishDBConnection();
}
catch (FileNotFoundException ex)
{
    TLogging.Log(ex.Message);
    TLogging.Log("Please check your OpenPetra.build.config file ...");
    TLogging.Log("Maybe a nant initConfigFile helps ...");
    throw new ApplicationException();
}
catch (Exception)
{
    throw;
}

return new TClientManager();

{##CONNECTORCLIENTSERVER}
return new TClientManager();