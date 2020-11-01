/*
 * CHANGE LOG - keep only last 5 threads
 * 
 * RESOURCES
 * https://www.gurock.com/testrail/docs/api/reference/templates
 */
using Gravity.Abstraction.Logging;

using Rhino.Connectors.GurockClients.Contracts;
using Rhino.Connectors.GurockClients.Internal;

using System;
using System.Collections.Generic;

namespace Rhino.Connectors.GurockClients.Clients
{
    /// <summary>
    /// Use the following API methods to request details about templates (field layouts for cases/results).
    /// </summary>
    public class TestRailTemplatesClient : TestRailClient
    {
        #region *** constructors  ***
        /// <summary>
        /// Creates a new instance of this client
        /// </summary>
        /// <param name="testRailServer">Test-Rail server to create a client against</param>
        /// <param name="user">Test-Rail user name (account email)</param>
        /// <param name="password">Test-Rail password</param>
        public TestRailTemplatesClient(string testRailServer, string user, string password)
            : base(testRailServer, user, password)
        { }

        /// <summary>
        /// Creates a new instance of this client
        /// </summary>
        /// <param name="testRailServer">Test-Rail server to create a client against</param>
        /// <param name="user">Test-Rail user name (account email)</param>
        /// <param name="password">Test-Rail password</param>
        /// <param name="logger">Logger implementation for this client</param>
        public TestRailTemplatesClient(Uri testRailServer, string user, string password, ILogger logger)
            : base(testRailServer, user, password, logger)
        { }
        #endregion

        /// <summary>
        /// Returns a list of available templates (requires TestRail 5.2 or later).
        /// </summary>
        /// <returns>Success, the templates are returned as part of the response.</returns>
        public IEnumerable<TestRailTemplate> GetTemplates(int projectId)
        {
            // create command
            var command = string.Format(ApiCommands.GET_TEMPLATES, projectId);

            // execute command
            return ExecuteGet<TestRailTemplate[]>(command);
        }
    }
}