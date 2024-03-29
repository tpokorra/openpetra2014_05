/* **********************************************************************************
*
* Copyright (c) Microsoft Corporation. All rights reserved.
*
* This source code is subject to terms and conditions of the Microsoft Public
* License (Ms-PL). A copy of the license can be found in the license.htm file
* included in this distribution.
*
* You must not remove this notice, or any other, from this software.
*
* **********************************************************************************/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Threading;
using System.Web;
using System.Web.Hosting;

namespace Ict.Tools.OpenPetraRuntimeHost
{
    /// <summary>
    /// Server class
    /// </summary>
    public class Server : MarshalByRefObject
    {
        int _port;
        string _virtualPath;
        string _physicalPath;
        string _defaultPage;
        bool _allowRemoteConnection;

        bool _shutdownInProgress;
        Socket _socket;
        Host _host;

        /// <summary>
        /// Main server constructor
        /// </summary>
        public Server(int port, string virtualPath, string physicalPath, string defaultPage, bool allowRemoteConnection)
        {
            _port = port;
            _virtualPath = virtualPath;
            _physicalPath = physicalPath.EndsWith("\\", StringComparison.Ordinal) ? physicalPath : physicalPath + "\\";
            _defaultPage = defaultPage;
            _allowRemoteConnection = allowRemoteConnection;
        }

        /// <summary>
        /// Override for InitializeLifetimeService
        /// </summary>
        /// <returns></returns>
        public override object InitializeLifetimeService()
        {
            // never expire the license
            return null;
        }

        /// <summary>
        /// Returns the Virtual path
        /// </summary>
        public string VirtualPath {
            get
            {
                return _virtualPath;
            }
        }

        /// <summary>
        /// Returns the physical path
        /// </summary>
        public string PhysicalPath {
            get
            {
                return _physicalPath;
            }
        }

        /// <summary>
        /// Returns the port number
        /// </summary>
        public int Port {
            get
            {
                return _port;
            }
        }

        /// <summary>
        /// Returns the root url
        /// </summary>
        public string RootUrl {
            get
            {
                if (_port != 80)
                {
                    return "http://localhost:" + _port + _virtualPath;
                }
                else
                {
                    return "http://localhost" + _virtualPath;
                }
            }
        }

        //
        // Socket listening
        //

        static Socket CreateSocketBindAndListen(AddressFamily family, IPAddress address, int port)
        {
            var socket = new Socket(family, SocketType.Stream, ProtocolType.Tcp);

            socket.Bind(new IPEndPoint(address, port));
            socket.Listen((int)SocketOptionName.MaxConnections);
            return socket;
        }

        /// <summary>
        /// Starts the server
        /// </summary>
        public void Start()
        {
            try
            {
                _socket = CreateSocketBindAndListen(AddressFamily.InterNetwork, IPAddress.Loopback, _port);
            }
            catch
            {
                _socket = CreateSocketBindAndListen(AddressFamily.InterNetworkV6, IPAddress.IPv6Loopback, _port);
            }

            ThreadPool.QueueUserWorkItem(delegate {
                    while (!_shutdownInProgress)
                    {
                        try
                        {
                            Socket acceptedSocket = _socket.Accept();

                            ThreadPool.QueueUserWorkItem(delegate {
                                    if (!_shutdownInProgress)
                                    {
                                        var conn = new Connection(this, acceptedSocket);

                                        // wait for at least some input
                                        if (conn.WaitForRequestBytes() == 0)
                                        {
                                            conn.WriteErrorAndClose(400);
                                            return;
                                        }

                                        // find or create host
                                        Host host = GetHost();

                                        if (host == null)
                                        {
                                            conn.WriteErrorAndClose(500);
                                            return;
                                        }

                                        // process request in worker app domain
                                        host.ProcessRequest(conn);
                                    }
                                });
                        }
                        catch
                        {
                            Thread.Sleep(100);
                        }
                    }
                });
        }

        /// <summary>
        /// Stops the server
        /// </summary>
        public void Stop()
        {
            _shutdownInProgress = true;

            try
            {
                if (_socket != null)
                {
                    _socket.Close();
                }
            }
            catch
            {
            }
            finally
            {
                _socket = null;
            }

            try
            {
                if (_host != null)
                {
                    _host.Shutdown();
                }

                while (_host != null)
                {
                    Thread.Sleep(100);
                }
            }
            catch
            {
            }
            finally
            {
                _host = null;
            }
        }

        /// <summary>
        /// Called at the end of request processing to disconnect the remoting proxy for Connection object
        /// and allow GC to pick it up
        /// </summary>
        public void OnRequestEnd(Connection conn)
        {
            RemotingServices.Disconnect(conn);
        }

        /// <summary>
        /// Sets the host object to null
        /// </summary>
        public void HostStopped()
        {
            _host = null;
        }

        Host GetHost()
        {
            if (_shutdownInProgress)
            {
                return null;
            }

            Host host = _host;

            if (host == null)
            {
                lock (this) {
                    host = _host;

                    if (host == null)
                    {
                        host = (Host)CreateWorkerAppDomainWithHost(_virtualPath, _physicalPath, typeof(Host));
                        host.Configure(this, _port, _virtualPath, _physicalPath, _defaultPage, _allowRemoteConnection);
                        _host = host;
                    }
                }
            }

            return host;
        }

        static object CreateWorkerAppDomainWithHost(string virtualPath, string physicalPath, Type hostType)
        {
            // this creates worker app domain in a way that host doesn't need to be in GAC or bin
            // using BuildManagerHost via private reflection
            string uniqueAppString = string.Concat(virtualPath, physicalPath).ToLowerInvariant();
            string appId = (uniqueAppString.GetHashCode()).ToString("x", CultureInfo.InvariantCulture);

            // create BuildManagerHost in the worker app domain
            var appManager = ApplicationManager.GetApplicationManager();
            var buildManagerHostType = typeof(HttpRuntime).Assembly.GetType("System.Web.Compilation.BuildManagerHost");
            var buildManagerHost = appManager.CreateObject(appId, buildManagerHostType, virtualPath, physicalPath, false);

            // call BuildManagerHost.RegisterAssembly to make Host type loadable in the worker app domain
            buildManagerHostType.InvokeMember(
                "RegisterAssembly",
                BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic,
                null,
                buildManagerHost,
                new object[2] { hostType.Assembly.FullName, hostType.Assembly.Location });

            // create Host in the worker app domain
            IRegisteredObject ret = appManager.CreateObject(appId, hostType, virtualPath, physicalPath, false);
            return ret;
        }
    }
}