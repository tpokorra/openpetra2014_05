//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       alanp, timop
//
// Copyright 2004-2014 by OM International
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Ict.Common;

namespace Ict.Tools.OpenPetraWebServer
{
    class CommandLineArgs
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// The command line can be blank - in which case the full UI is used
        /// or it can have parameters as follows - in which case the simplified UI is shown with one instance per port
        ///
        /// "path-to-exe" -physicalPath:"fully-qualified-physical-path" -port:portNumber [-virtualPath:virtualPath] [-defaultPage:defaultPage] [-r:true] [-quiet:true]


        private string _physicalPath = string.Empty;
        private string _virtualPath = string.Empty;
        private int _port = -1;
        private bool _acceptRemoteConnection = false;
        private string _defaultPage = string.Empty;
        private bool _suppressStartUpMessages = false;
        private Int16 _maxRuntimeInMinutes = -1;

        public string PhysicalPath {
            get
            {
                return _physicalPath;
            }
        }
        public string VirtualPath {
            get
            {
                return _virtualPath;
            }
        }
        public int Port {
            get
            {
                return _port;
            }
        }
        public bool AcceptRemoteConnection {
            get
            {
                return _acceptRemoteConnection;
            }
        }
        public string DefaultPage {
            get
            {
                return _defaultPage;
            }
        }
        public bool SuppressStartupMessages {
            get
            {
                return _suppressStartUpMessages;
            }
        }
        public Int16 MaxRuntimeInMinutes {
            get
            {
                return _maxRuntimeInMinutes;
            }
        }

        public CommandLineArgs(string[] args)
        {
            _physicalPath = TAppSettingsManager.GetValue("physicalPath", string.Empty, false);;
            _virtualPath = TAppSettingsManager.GetValue("virtualPath", string.Empty, false);
            _defaultPage = TAppSettingsManager.GetValue("defaultPage", string.Empty, false);
            _port = TAppSettingsManager.GetInt16("port", 8080);
            _acceptRemoteConnection = TAppSettingsManager.GetBoolean("acceptRemoteConnection", false);
            _suppressStartUpMessages = TAppSettingsManager.GetBoolean("quiet", false);
            _maxRuntimeInMinutes = TAppSettingsManager.GetInt16("maxRuntimeInMinutes", -1);
        }

        public bool IsValid
        {
            get
            {
                bool isValid = (_physicalPath != string.Empty && _port >= 80
                                && Directory.Exists(_physicalPath));

                return isValid;
            }
        }
    }
}