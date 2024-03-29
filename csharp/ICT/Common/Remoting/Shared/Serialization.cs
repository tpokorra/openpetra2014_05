﻿//
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
using System.Threading;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using Ict.Common;
using Ict.Common.IO;

namespace Ict.Common.Remoting.Shared
{
    /// serialize and deserialize complex types in binary
    public class THttpBinarySerializer
    {
        /// <summary>
        /// serialize any object. if it is a complex type, use Base64
        /// </summary>
        static public string SerializeObject(object o, bool binary)
        {
            if (!binary)
            {
                if (o.GetType() == typeof(bool))
                {
                    return o.ToString().ToLower();
                }

                return o.ToString();
            }

            if (o == null)
            {
                return "null";
            }

            MemoryStream memoryStream = new MemoryStream();
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            try
            {
                binaryFormatter.Serialize(memoryStream, o);
            }
            catch (Exception e)
            {
                TLogging.Log("cannot serialize object of type " + o.GetType().ToString());

                TLogging.Log(e.ToString());
            }
            string encoded = Convert.ToBase64String(memoryStream.ToArray()) + ":binary";

            if (encoded.Length > 1024 * 1024 * 4)
            {
                TLogging.Log("Warning: THttpBinarySerializer.SerializeObject: Binary parameter is too long: " + encoded.Length.ToString());
            }

            return encoded;
        }

        static private bool ShouldParameterEncodeBinary(object o)
        {
            if (o.GetType() == typeof(string))
            {
                if (((string)o).IndexOfAny(new char[] { ',', ':', '<', '>' }) > -1)
                {
                    // Avoid System.Web.HttpRequestValidationException: A potentially dangerous Request.Form value was detected from the client
                    // when sending for example an html template.
                    // see also http://stackoverflow.com/questions/81991/a-potentially-dangerous-request-form-value-was-detected-from-the-client
                    return true;
                }

                return false;
            }

            return !(
                       o.GetType() == typeof(Int16)
//                       || o.GetType() == typeof(Int32[])
                       || o.GetType() == typeof(Int32)
//                       || o.GetType() == typeof(Int32[])
                       || o.GetType() == typeof(Int64)
//                       || o.GetType() == typeof(Int64[])
                       || o.GetType() == typeof(bool)
//                       || o.GetType() == typeof(bool[])
                       );
        }

        /// serialize any object. depending on the type of the object, it will be serialized in binary format
        static public string SerializeObject(object o)
        {
            if (o == null)
            {
                return "null";
            }

            return SerializeObject(o, ShouldParameterEncodeBinary(o));
        }

        /// serialize any object. depending on the type of the object, it will be serialized in binary format
        static public string SerializeObjectWithType(object o)
        {
            if (o == null)
            {
                return "null:void";
            }

            bool binary = ShouldParameterEncodeBinary(o);

            string result = SerializeObject(o, binary);

            if (result.EndsWith(":binary"))
            {
                return result;
            }
            else
            {
                return result + ":" + (binary ? "binary" : o.GetType().ToString());
            }
        }

        /// <summary>
        /// reverse of SerializeObject
        /// </summary>
        static public object DeserializeObject(string s, string type)
        {
            if (s == "null")
            {
                return null;
            }

            if (s.EndsWith(":binary"))
            {
                type = "binary";
                s = s.Substring(0, s.Length - ":binary".Length);
            }

            if (type == "System.Int64")
            {
                return Convert.ToInt64(s);
            }
            else if (type == "System.Int32")
            {
                return Convert.ToInt32(s);
            }
            else if (type == "System.Int16")
            {
                return Convert.ToInt16(s);
            }
            else if (type == "System.Boolean")
            {
                return Convert.ToBoolean(s);
            }
            else if (type == "System.String")
            {
                return s;
            }
            else if (type.EndsWith("Enum"))
            {
                Type t = Type.GetType(type);

                if (t == null)
                {
                    foreach (Assembly a in System.AppDomain.CurrentDomain.GetAssemblies())
                    {
                        t = a.GetType(type);

                        if (t != null)
                        {
                            break;
                        }
                    }
                }

                if (t != null)
                {
                    return Enum.Parse(t, s);
                }

                throw new Exception("THttpBinarySerializer.DeserializeObject: unknown enum " + type);
            }
            else // if (type == "binary" || true)
            {
                MemoryStream memoryStream = new MemoryStream(Convert.FromBase64String(s));
                memoryStream.Seek(0, SeekOrigin.Begin);
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                return binaryFormatter.Deserialize(memoryStream);
            }
        }
    }
}