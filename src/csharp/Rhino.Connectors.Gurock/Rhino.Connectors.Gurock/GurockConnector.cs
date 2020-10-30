/*
 * CHANGE LOG - keep only last 5 threads
 * 
 * RESOURCES
 * https://www.gurock.com/testrail/docs/api/reference/cases
 * https://www.gurock.com/testrail/docs/api/reference/results
 * https://www.gurock.com/testrail/docs/api/reference/tests
 */
using Gravity.Abstraction.Logging;

using Rhino.Api;
using Rhino.Api.Contracts.Attributes;
using Rhino.Api.Contracts.AutomationProvider;
using Rhino.Api.Contracts.Configuration;
using Rhino.Api.Extensions;
using Rhino.Connectors.GurockClients;
using Rhino.Connectors.GurockClients.Clients;
using Rhino.Connectors.GurockClients.Contracts;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rhino.Connectors.Gurock
{
    /// <summary>
    /// XRay connector for running XRay tests as Rhino Automation Specs.
    /// </summary>
    [Connector(
        value: Connector.TestRail,
        Name = "Connector - Gurock Test Rail & Jira",
        Description =
        "Allows to execute Rhino Specs from Gurock Test Rail cases and report back as Test Execution. " +
        "Jira (either cloud or server) will be used for bugs management.")]
    public class GurockConnector : RhinoConnector
    {
        // members: test rail clients
        private readonly TestRailCasesClient casesClient;
        private readonly TestRailResultsClient resultsClient;
        private readonly TestRailTestsClient testsClient;
        private readonly TestRailAttachmentsClient attachmentsClient;

        #region *** Constructors   ***
        /// <summary>
        /// Creates a new instance of this Rhino.Api.Components.RhinoConnector.
        /// </summary>
        /// <param name="configuration">Rhino.Api.Contracts.Configuration.RhinoConfiguration to use with this connector.</param>
        public GurockConnector(RhinoConfiguration configuration)
            : this(configuration, Utilities.Types)
        { }

        /// <summary>
        /// Creates a new instance of this Rhino.Api.Components.RhinoConnector.
        /// </summary>
        /// <param name="configuration">Rhino.Api.Contracts.Configuration.RhinoConfiguration to use with this connector.</param>
        /// <param name="types">A collection of <see cref="Type"/> to load for this repository.</param>
        public GurockConnector(RhinoConfiguration configuration, IEnumerable<Type> types)
            : this(configuration, types, Utilities.CreateDefaultLogger(configuration))
        { }

        /// <summary>
        /// Creates a new instance of this Rhino.Api.Components.RhinoConnector.
        /// </summary>
        /// <param name="configuration">Rhino.Api.Contracts.Configuration.RhinoConfiguration to use with this connector.</param>
        /// <param name="types">A collection of <see cref="Type"/> to load for this repository.</param>
        /// <param name="logger">Gravity.Abstraction.Logging.ILogger implementation for this connector.</param>
        public GurockConnector(RhinoConfiguration configuration, IEnumerable<Type> types, ILogger logger)
            : this(configuration, types, logger, connect: true)
        { }

        /// <summary>
        /// Creates a new instance of this Rhino.Api.Components.RhinoConnector.
        /// </summary>
        /// <param name="configuration">Rhino.Api.Contracts.Configuration.RhinoConfiguration to use with this connector.</param>
        /// <param name="types">A collection of <see cref="Type"/> to load for this repository.</param>
        /// <param name="logger">Gravity.Abstraction.Logging.ILogger implementation for this connector.</param>
        /// <param name="connect"><see cref="true"/> for immediately connect after construct <see cref="false"/> skip connection.</param>
        /// <remarks>If you skip connection you must explicitly call Connect method.</remarks>
        public GurockConnector(RhinoConfiguration configuration, IEnumerable<Type> types, ILogger logger, bool connect)
            : base(configuration, types, logger)
        {
            // setup connector type (double check)
            configuration.ConnectorConfiguration ??= new RhinoConnectorConfiguration();
            configuration.ConnectorConfiguration.Connector = Connector.TestRail;

            // setup provider manager
            ProviderManager = new GurockAutomationProvider(configuration, types, logger);

            // integration
            var clientFactory = new ClientFactory(
                testRailServer: configuration.ConnectorConfiguration.Collection,
                user: configuration.ConnectorConfiguration.UserName,
                password: configuration.ConnectorConfiguration.Password,
                logger);

            casesClient = clientFactory.Create<TestRailCasesClient>();
            testsClient = clientFactory.Create<TestRailTestsClient>();
            resultsClient = clientFactory.Create<TestRailResultsClient>();
            attachmentsClient = clientFactory.Create<TestRailAttachmentsClient>();

            // connect on constructing
            if (connect)
            {
                Connect();
            }
        }
        #endregion

        #region *** Per Test Clean ***
        /// <summary>
        /// Performed just after each test is called.
        /// </summary>
        /// <param name="testCase">The Rhino.Api.Contracts.AutomationProvider.RhinoTestCase which was executed.</param>
        /// <remarks>Use this method for PostTestExecute customization.</remarks>
        public override RhinoTestCase OnPostTestExecute(RhinoTestCase testCase)
        {
            // build steps
            var customSteps = GetCustomSteps(testCase);

            // build test-case outcome
            var elapsed = testCase.End - testCase.Start;
            var test = testsClient.GetTests(int.Parse(testCase.TestRunKey)).FirstOrDefault(i => i.CaseId == int.Parse(testCase.Key));
            var isFaild = testCase.Steps.Any(i => !i.Actual);
            var status = isFaild ? 5 : 1;
            if (testCase.Inconclusive)
            {
                status = 4;
            }

            // save results
            var defect = GetDefect(testCase);
            var result = new TestRailResult
            {
                AssignedTo = test.AssignedTo,
                Version = $"{DateTime.Now:yyyyMMdd.fff}",
                Elapsed = $"{elapsed.Hours}h {elapsed.Minutes}m {elapsed.Seconds}s",
                StatusId = status,
                CustomStepResults = customSteps.ToArray(),
                Comment = $"iteration {testCase.Iteration}"
            };
            if (!string.IsNullOrEmpty(defect))
            {
                result.Defects = defect;
            }

            // add results
            var results = resultsClient.AddResultForCase(int.Parse(testCase.TestRunKey), int.Parse(testCase.Key), result);
            AddAttachments(results.Id, testCase);
            if (!string.IsNullOrEmpty(defect))
            {
                AddToCaseRefs(testCase, defect);
            }

            // get
            return testCase;
        }

        private IEnumerable<CustomStep> GetCustomSteps(RhinoTestCase testCase)
        {
            // setup
            var customSteps = new List<CustomStep>();

            // apply
            foreach (var step in testCase.Steps)
            {
                // extract exception if possible
                var excption = step?.Exception?.InnerException != null ? step.Exception.InnerException : step.Exception;

                // build custom-step
                var customStep = new CustomStep { Content = step.Action, Expected = step.Expected };
                customStep.StatusId = step.Actual ? 1 : 5;
                customStep.Actual = string.IsNullOrEmpty($"{excption}") ? step.ReasonPhrase : $"{excption}";
                customSteps.Add(customStep);
            }

            // get
            return customSteps;
        }

        private string GetDefect(RhinoTestCase testCase) => !testCase.Context.ContainsKey("lastBugKey")
            ? string.Empty
            : $"{testCase.Context["lastBugKey"]}";

        private void AddToCaseRefs(RhinoTestCase testCase, string bugKey)
        {
            // exit conditions
            if (string.IsNullOrEmpty(bugKey))
            {
                return;
            }

            // setup
            int.TryParse(testCase.Key, out int caseId);

            // get
            var onTestCase = casesClient.GetCase(caseId);
            var refs = onTestCase.Refs == null
                ? new[] { bugKey }
                : onTestCase.Refs.Split(',').Concat(new[] { bugKey });

            // update
            onTestCase.Refs = refs.Any() ? string.Join(",", refs) : default;
            casesClient.UpdateCase(caseId, onTestCase);
        }

        // TODO: add parallel options
        private void AddAttachments(int resultId, RhinoTestCase testCase)
        {
            // setup            
            var files = testCase.GetScreenshots();
            var options = new ParallelOptions { MaxDegreeOfParallelism = 4 };

            // add attachments
            Parallel.ForEach(files, options, file
                => attachmentsClient.AddAttachmentToResult(resultId, fileToUpload: file));
        }
        #endregion
    }
}