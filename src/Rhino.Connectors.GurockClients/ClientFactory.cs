/*
 * CHANGE LOG - keep only last 5 threads
 * 
 * RESOURCES
 */
using Gravity.Abstraction.Logging;
using Rhino.Connectors.GurockClients.Clients;
using System;

namespace Rhino.Connectors.GurockClients
{
    public class ClientFactory
    {
        // members: state
        internal readonly Uri testRailServer;

        private readonly ILogger logger;
        private readonly string user;
        private readonly string password;

        /// <summary>
        /// Creates a new instance of ClientFactory.
        /// </summary>
        /// <param name="testRailServer">Test-Rail server to create a client against</param>
        /// <param name="user">Test-Rail user name (account email)</param>
        /// <param name="password">Test-Rail password</param>
        public ClientFactory(string testRailServer, string user, string password)
            : this(testRailServer, user, password, logger: default)
        { }

        /// <summary>
        /// Creates a new instance of ClientFactory.
        /// </summary>
        /// <param name="testRailServer">Test-Rail server to create a client against</param>
        /// <param name="user">Test-Rail user name (account email)</param>
        /// <param name="password">Test-Rail password</param>
        /// <param name="logger">Logger implementation for this factory.</param>
        public ClientFactory(string testRailServer, string user, string password, ILogger logger)
        {
            this.testRailServer = new Uri(testRailServer);
            this.user = user;
            this.password = password;
            this.logger = logger;
        }

        /// <summary>
        /// Creates a TestRailClient implementation.
        /// </summary>
        /// <typeparam name="T">TestRailClient type to create.</typeparam>
        /// <returns>A new instance of TestRailClient</returns>
        public T Create<T>() where T : TestRailClient
        {
            return (T)Activator.CreateInstance(typeof(T), new object[] { testRailServer, user, password, logger });
        }
    }
}
