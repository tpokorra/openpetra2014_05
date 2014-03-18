//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       timop
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
using System.IO;
using System.Collections.Generic;
using System.Web;
using System.Threading;
using System.Web.SessionState;
using Ict.Common;

namespace Ict.Common.Remoting.Server
{
    /// <summary>
    /// Static class for storing sessions.
    /// we are using our own session handling,
    /// since the mono server cannot handle concurrent requests in one session
    /// see also http://serverfault.com/questions/324033/how-do-i-get-concurrent-asp-net-on-linux
    /// </summary>
    public class TSession
    {
        private static SortedList <string, SortedList <string, object>>FSessionObjects = new SortedList <string, SortedList <string, object>>();

        [ThreadStaticAttribute]
        private static string FSessionID;

        /// get the current session id. if it is not stored in the http context, check the thread
        private static string FindSessionID()
        {
            if ((HttpContext.Current != null) && (HttpContext.Current.Request.Cookies["OpenPetraSessionID"] != null))
            {
                return HttpContext.Current.Request.Cookies["OpenPetraSessionID"].Value;
            }

            string sessionId = FSessionID;

            if ((sessionId != null) && (sessionId.Length > 0))
            {
                return sessionId;
            }

            return string.Empty;
        }

        /// <summary>
        /// set the session id for this current thread
        /// </summary>
        /// <param name="ASessionID"></param>
        public static void InitThread(string ASessionID)
        {
            FSessionID = ASessionID;
        }

        /// <summary>
        /// gets the current session id, or creates a new session id if it does not exist yet
        /// </summary>
        public static string GetSessionID()
        {
            string sessionID = FindSessionID();

            if ((sessionID != string.Empty) && !FSessionObjects.ContainsKey(sessionID))
            {
                if (HttpContext.Current != null)
                {
                    HttpContext.Current.Request.Cookies.Remove("OpenPetraSessionID");
                }

                sessionID = FindSessionID();
            }

            if (sessionID == string.Empty)
            {
                sessionID = Guid.NewGuid().ToString();

                if (HttpContext.Current != null)
                {
                    HttpContext.Current.Request.Cookies.Add(new HttpCookie("OpenPetraSessionID", sessionID));
                    HttpContext.Current.Response.Cookies.Add(new HttpCookie("OpenPetraSessionID", sessionID));
                }

                FSessionObjects.Add(sessionID, new SortedList <string, object>());
                InitThread(sessionID);
            }

            return sessionID;
        }

        private static SortedList <string, object>GetSession()
        {
            string sessionID = GetSessionID();

            return FSessionObjects[sessionID];
        }

        /// <summary>
        /// set a session variable
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public static void SetVariable(string name, object value)
        {
            // HttpContext.Current.Session[name] = value;
            SortedList <string, object>session = GetSession();

            if (session.Keys.Contains(name))
            {
                session[name] = value;
            }
            else
            {
                session.Add(name, value);
            }
        }

        /// <summary>
        /// returns true if variable exists and is not null
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool HasVariable(string name)
        {
            SortedList <string, object>session = GetSession();

            if (session.Keys.Contains(name) && (GetSession()[name] != null))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// get a session variable
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static object GetVariable(string name)
        {
            // return HttpContext.Current.Session[name];
            SortedList <string, object>session = GetSession();

            if (session.Keys.Contains(name))
            {
                return GetSession()[name];
            }

            return null;
        }

        /// <summary>
        /// clear the current session
        /// </summary>
        static public void Clear()
        {
            // HttpContext.Current.Session.Clear();
            string sessionId = GetSessionID();

            if (sessionId.Length > 0)
            {
                FSessionObjects.Remove(sessionId);
                HttpContext.Current.Request.Cookies.Remove("OpenPetraSessionID");
            }
        }
    }
}