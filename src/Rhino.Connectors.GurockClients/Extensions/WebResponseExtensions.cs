﻿/*
 * CHANGE LOG - keep only last 5 threads
 * 
 * RESOURCES
 */
using System.IO;
using System.Net;

namespace Rhino.Connectors.GurockClients.Extensions
{
    internal static class WebResponseExtensions
    {
        /// <summary>
        /// Reads the body of this WebResponse instance
        /// </summary>
        /// <param name="response">This WebResponse instance</param>
        /// <returns>WebResponse content as string</returns>
        public static string ReadBody(this WebResponse response)
        {
            using (response)
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
