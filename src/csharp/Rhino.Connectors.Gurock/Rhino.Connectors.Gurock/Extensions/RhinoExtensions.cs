using Gravity.Extensions;
using Rhino.Api.Contracts.AutomationProvider;
using Rhino.Connectors.GurockClients.Contracts;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Rhino.Connectors.Gurock.Extensions
{
    internal static class RhinoExtensions
    {
        /// <summary>
        /// converts test-management test-case interface into a connector test-case
        /// ready for automation
        /// </summary>
        /// <param name="testCase">test-cases to convert</param>
        /// <returns>connector test-case</returns>
        public static RhinoTestCase ToConnectorTestCase(this TestRailCase testCase)
        {
            var connectorTesetCase = new RhinoTestCase
            {
                Actual = false,
                Context = testCase.Context,
                Inconclusive = false,
                Key = $"{testCase.Id}",
                DataSource = DoGetLocalDataSource(testCase).ToDictionary(),
                Severity = $"{testCase.CustomSeverity}",
                Tolerance = testCase.CustomTolerance,
                Scenario = testCase.Title,
                Steps = testCase.GetConnectorSteps(),
                TestSuites = new[] { $"{testCase?.SuiteId}" }
            };
            connectorTesetCase.Severity = string.IsNullOrEmpty(connectorTesetCase.Severity) ? "Unavailable" : connectorTesetCase.Severity;
            connectorTesetCase.Iteration = 1;
            return connectorTesetCase;
        }

        /// <summary>
        /// gets this test-case local data-provider
        /// </summary>
        /// <param name="testCase">this test-case instance</param>
        /// <returns>local data-provider</returns>
        public static DataTable GetLocalDataSource(this TestRailCase testCase)
        {
            return DoGetLocalDataSource(testCase);
        }

        /// <summary>
        /// converts test-management test-actions into a connector test-actions
        /// </summary>
        /// <param name="testCase">test-case to convert steps from</param>
        /// <returns>test-steps collection</returns>
        public static IEnumerable<RhinoTestStep> GetConnectorSteps(this TestRailCase testCase)
        {
            return testCase.CustomStepsSeparated.Select(i => new RhinoTestStep
            {
                Action = i.Content,
                Expected = i.Expected
            });
        }

        /// <summary>
        /// converts test-management test-case interface into a connector test-case
        /// ready for automation
        /// </summary>
        /// <param name="testCase">test-cases to convert</param>
        /// <returns>connector test-case</returns>
        public static TestRailCase ToTestRailCase(this RhinoTestCase testCase)
        {
            // setup
            _ = int.TryParse(testCase.TestSuites.FirstOrDefault(), out int sectionId);

            // build
            return new TestRailCase
            {
                SectionId = sectionId,
                Title = testCase.Scenario,
                CustomStepsSeparated = testCase.Steps.Select(i => new CustomStep { Content = i.Action, Expected = i.Expected }).ToArray()
            };
        }

        // UTILITIES
        private static DataTable DoGetLocalDataSource(this TestRailCase testCase)
        {
            // exit conditions
            if (string.IsNullOrEmpty(testCase.CustomPreconds))
            {
                return new DataTable();
            }
            return new DataTable().FromMarkDown(testCase.CustomPreconds, default);
        }
    }
}
