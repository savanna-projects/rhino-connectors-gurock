/*
 * CHANGE LOG - keep only last 5 threads
 * 
 * RESOURCES
 * https://www.gurock.com/testrail/docs/api/reference/priorities
 */
using Gravity.Abstraction.Logging;
using Rhino.Connectors.GurockClients.Contracts;
using Rhino.Connectors.GurockClients.Internal;
using System;
using System.Collections.Generic;

namespace Rhino.Connectors.GurockClients.Clients
{
    /// <summary>
    /// Use the following API methods to request details about priorities.
    /// </summary>
    public class TestRailPrioritiesClient : TestRailClient
    {
        #region *** constructors     ***
        /// <summary>
        /// Creates a new instance of this client
        /// </summary>
        /// <param name="testRailServer">Test-Rail server to create a client against</param>
        /// <param name="user">Test-Rail user name (account email)</param>
        /// <param name="password">Test-Rail password</param>
        public TestRailPrioritiesClient(string testRailServer, string user, string password)
            : base(testRailServer, user, password) { }

        /// <summary>
        /// Creates a new instance of this client
        /// </summary>
        /// <param name="testRailServer">Test-Rail server to create a client against</param>
        /// <param name="user">Test-Rail user name (account email)</param>
        /// <param name="password">Test-Rail password</param>
        /// <param name="logger">Logger implementation for this client</param>
        public TestRailPrioritiesClient(Uri testRailServer, string user, string password, ILogger logger)
            : base(testRailServer, user, password, logger) { }
        #endregion

        #region *** pipeline: get    ***
        /// <summary>
        /// Returns a list of available priorities.
        /// </summary>
        /// <returns>Success, the available priorities are returned as part of the response.</returns>
        public IEnumerable<TestRailPriority> GetPriorities()
        {
            return ExecuteGet<TestRailPriority[]>(ApiCommands.GET_PRIORITIES);
        }
        #endregion
    }
}