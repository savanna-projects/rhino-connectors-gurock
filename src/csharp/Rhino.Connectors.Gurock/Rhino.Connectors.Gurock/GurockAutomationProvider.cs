/*
 * CHANGE LOG - keep only last 5 threads
 * 
 * RESOURCES
 * http://docs.gurock.com/testrail-api2/reference-attachments
 */
using Gravity.Abstraction.Logging;
using Gravity.Extensions;

using Rhino.Api;
using Rhino.Api.Contracts.AutomationProvider;
using Rhino.Api.Contracts.Configuration;
using Rhino.Api.Contracts.Extensions;
using Rhino.Api.Extensions;

using Rhino.Connectors.AtlassianClients;
using Rhino.Connectors.AtlassianClients.Contracts;
using Rhino.Connectors.AtlassianClients.Extensions;
using Rhino.Connectors.AtlassianClients.Framework;
using Rhino.Connectors.Gurock.Extensions;
using Rhino.Connectors.GurockClients;
using Rhino.Connectors.GurockClients.Clients;
using Rhino.Connectors.GurockClients.Contracts;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Utilities = Rhino.Api.Extensions.Utilities;

namespace Rhino.Connectors.Gurock
{
    public class GurockAutomationProvider : ProviderManager
    {
        // members: constants
        private const StringComparison Compare = StringComparison.OrdinalIgnoreCase;

        // state: global parameters
        private readonly ILogger logger;
        private readonly IDictionary<string, object> capabilities;
        private readonly ClientFactory clientFactory;
        private readonly JiraClient jiraClient;
        private readonly JiraBugsManager bugsManager;

        // members: test rail clients
        private readonly TestRailSuitesClient suitesClient;
        private readonly TestRailCasesClient casesClient;
        private readonly TestRailConfiguraionsClient configuraionsClient;
        private readonly TestRailMilestoneClient milestoneClient;
        private readonly TestRailPlansClient plansClient;
        private readonly TestRailUsersClient usersClient;
        private readonly TestRailPrioritiesClient prioritiesClient;

        // members: state
        private readonly TestRailProject project;
        private readonly TestRailUser user;

        #region *** Constructors      ***
        /// <summary>
        /// Creates a new instance of this Rhino.Api.Simulator.Framework.AutomationProvider.
        /// </summary>
        /// <param name="configuration">Rhino.Api.Contracts.Configuration.RhinoConfiguration to use with this provider.</param>
        public GurockAutomationProvider(RhinoConfiguration configuration)
            : this(configuration, Utilities.Types)
        { }

        /// <summary>
        /// Creates a new instance of this Rhino.Api.Simulator.Framework.AutomationProvider.
        /// </summary>
        /// <param name="configuration">Rhino.Api.Contracts.Configuration.RhinoConfiguration to use with this provider.</param>
        /// <param name="types">A collection of <see cref="Type"/> to load for this repository.</param>
        public GurockAutomationProvider(RhinoConfiguration configuration, IEnumerable<Type> types)
            : this(configuration, types, Utilities.CreateDefaultLogger(configuration))
        { }

        /// <summary>
        /// Creates a new instance of this Rhino.Api.Simulator.Framework.AutomationProvider.
        /// </summary>
        /// <param name="configuration">Rhino.Api.Contracts.Configuration.RhinoConfiguration to use with this provider.</param>
        /// <param name="types">A collection of <see cref="Type"/> to load for this repository.</param>
        /// <param name="logger">Gravity.Abstraction.Logging.ILogger implementation for this provider.</param>
        public GurockAutomationProvider(RhinoConfiguration configuration, IEnumerable<Type> types, ILogger logger)
            : base(configuration, types, logger)
        {
            // setup
            this.logger = logger?.Setup(loggerName: nameof(GurockAutomationProvider));

            // capabilities
            BucketSize = configuration.GetBucketSize();
            configuration.PutDefaultCapabilities();
            capabilities = configuration.Capabilities.ContainsKey($"{Connector.TestRail}:options")
                ? configuration.Capabilities[$"{Connector.TestRail}:options"] as IDictionary<string, object>
                : new Dictionary<string, object>();

            // setup
            var jiraAuth = new JiraAuthentication
            {
                Collection = capabilities["jiraCollection"].ToString(),
                Password = capabilities["jiraPassword"].ToString(),
                Project = capabilities["jiraProject"].ToString(),
                UserName = capabilities["jiraUser"].ToString()
            };
            jiraClient = new JiraClient(jiraAuth);

            // integration
            bugsManager = new JiraBugsManager(jiraClient);
            clientFactory = new ClientFactory(
                configuration.ConnectorConfiguration.Collection,
                configuration.ConnectorConfiguration.UserName,
                configuration.ConnectorConfiguration.Password,
                logger);

            suitesClient = clientFactory.Create<TestRailSuitesClient>();
            casesClient = clientFactory.Create<TestRailCasesClient>();
            configuraionsClient = clientFactory.Create<TestRailConfiguraionsClient>();
            milestoneClient = clientFactory.Create<TestRailMilestoneClient>();
            plansClient = clientFactory.Create<TestRailPlansClient>();
            usersClient = clientFactory.Create<TestRailUsersClient>();
            prioritiesClient = clientFactory.Create<TestRailPrioritiesClient>();

            // meta data
            project = suitesClient.Projects.FirstOrDefault(i => i.Name.Equals(configuration.ConnectorConfiguration.Project, Compare));
            user = usersClient.GetUserByEmail(configuration.ConnectorConfiguration.UserName);
        }
        #endregion

        #region *** Get: Test Cases   ***
        /// <summary>
        /// Returns a list of test cases for a project.
        /// </summary>
        /// <param name="ids">A list of test ids to get test cases by.</param>
        /// <returns>A collection of Rhino.Api.Contracts.AutomationProvider.RhinoTestCase</returns>
        public override IEnumerable<RhinoTestCase> OnGetTestCases(params string[] ids)
        {
            // constants: logging
            const string M1 = "tests-repository parsed into integers";
            const string M2 = "total of [{0}] distinct items found (test or test-suite)";

            // parse as int
            var idList = new List<int>();
            foreach (var testRepository in Configuration.TestsRepository)
            {
                int.TryParse(testRepository, out int repositoryOut);
                idList.Add(repositoryOut);
            }
            logger?.Debug(M1);

            // normalize
            idList = idList.Where(i => i != 0).Distinct().ToList();
            logger?.DebugFormat(M2, idList.Count);

            // get all suites
            var suites = suitesClient.GetSuites(project.Id).Where(i => idList.Contains(i.Id));

            // get all test-cases
            var bySuites = suites.SelectMany(i => casesClient.GetCases(project.Id, i.Id)).Where(i => i != null) ?? new TestRailCase[0];
            var byCases = idList.Select(i => casesClient.GetCase(i)).Where(i => i != null) ?? new TestRailCase[0];
            return bySuites.Concat(byCases).DistinctBy(i => i.Id).Select(ToConnectorTestCase);
        }

        private RhinoTestCase ToConnectorTestCase(TestRailCase testRailCase)
        {
            // get priority
            var priority = prioritiesClient
                .GetPriorities()
                .FirstOrDefault(i => i.Id.Equals(testRailCase.PriorityId));

            // set
            var testCase = testRailCase.ToConnectorTestCase();
            testCase.Priority = $"{priority.Priority} - {priority.Name}";
            testCase.Context["projectKey"] = capabilities.ContainsKey("jiraProject")
                ? capabilities["jiraProject"]
                : string.Empty;

            // get
            return testCase;
        }
        #endregion

        #region *** Configuration     ***
        /// <summary>
        /// Implements a mechanism of setting a testing configuration for an automation provider.
        /// </summary>
        /// <remarks>Use this method for <see cref="SetConfiguration"/> customization.</remarks>
        public override void OnSetConfiguration()
        {
            // constants: logging
            const string M0 = "searching for [{0}] configuration-group under [{1}] project";
            const string M1 = "configuration-group [{0}] was not found under [{1}]";
            const string M2 = "configuration-group [{0}] successfully created under [{1}] project";
            const string M3 = "configuration [{0}] successfully created under [{1}] configuration-group under [{2}] project";

            // shortcuts
            var P = project.Id;
            var C = Configuration.Name;
            const string G = "Automation Configurations";
            const StringComparison COMPARE = StringComparison.OrdinalIgnoreCase;

            // validating: configuration group
            logger?.DebugFormat(M0, G, P);
            var configurationGroup = configuraionsClient.GetConfigs(P);
            if (!configurationGroup.Any())
            {
                logger?.DebugFormat(M1, G, P);
                configuraionsClient.AddConfigGroup(P, new TestRailConfigurationGroup { Name = G });
                logger?.DebugFormat(M2, G, P);
                configurationGroup = configuraionsClient.GetConfigs(P);
            }

            // set configuration
            var config = configurationGroup.SelectMany(i => i.Configs).FirstOrDefault(i => i.Name.Equals(C, COMPARE));
            if (config != default)
            {
                return;
            }
            configuraionsClient.AddConfig(P, G, new TestRailConfiguration { Name = C });
            logger?.DebugFormat(M3, C, G, P);
        }
        #endregion

        #region *** Test Run          ***
        /// <summary>
        /// Creates an automation provider test run entity. Use this method to implement the automation
        /// provider test run creation and to modify the loaded Rhino.Api.Contracts.AutomationProvider.RhinoTestRun.
        /// </summary>
        /// <param name="testRun">Rhino.Api.Contracts.AutomationProvider.RhinoTestRun object to modify before creating.</param>
        /// <returns>Rhino.Api.Contracts.AutomationProvider.RhinoTestRun based on provided test cases.</returns>
        public override RhinoTestRun OnCreateTestRun(RhinoTestRun testRun)
        {
            // constants: logging
            const string M1 = "test-run [{0}] create under [{1}] project";

            // constants
            const string C1 = "Automatically generated by Rhino engine";

            // shortcuts
            var M = capabilities["milestone"].ToString();

            // collect all suites used in this run
            var suites = GetSuites(testRun.TestCases).Distinct();

            // compose information about suits & cases for this run
            var entires = Get(suites, testRun.TestCases);
            var planEntries = Get(entires);

            // get milestone
            int.TryParse(M, out int msOut);
            var milestone = milestoneClient
                .GetMileStones(project.Id)
                .FirstOrDefault(i => i.Id == msOut || i.Name.Equals(M, StringComparison.OrdinalIgnoreCase));

            // create ALM test plan (test run)
            var addPlan = new TestRailPlan
            {
                Description = C1,
                Name = TestRun.Title,
                Entries = planEntries.ToArray(),
                MilestoneId = milestone == default ? null : (int?)milestone.Id
            };
            var plan = plansClient.AddPlan(project.Id, addPlan);
            logger?.DebugFormat(M1, plan.Id, project.Name);

            // update connector test-run
            TestRun.Key = $"{plan.Entries[0].Runs[0].Id}";
            TestRun.Context[nameof(TestRailPlan)] = plan;
            return testRun;
        }

        // parse & returns all valid suites
        private IEnumerable<int> GetSuites(IEnumerable<RhinoTestCase> testCases)
        {
            var suites = new List<int>();
            foreach (var testCase in testCases)
            {
                int.TryParse(testCase.TestSuites?.FirstOrDefault(), out int suiteOut);
                if (suiteOut == default)
                {
                    continue;
                }
                suites.Add(suiteOut);
            }
            return suites;
        }

        // build tuple of test-suite + entries
        private IEnumerable<(TestRailSuite suite, IEnumerable<int> cases)> Get(IEnumerable<int> suites, IEnumerable<RhinoTestCase> testCases)
        {
            var entires = new List<(TestRailSuite suite, IEnumerable<int> cases)>();
            foreach (var suite in suites)
            {
                var testSuite = suitesClient.GetSuite(suite);
                if (testSuite == default)
                {
                    continue;
                }
                var caseEntries = testCases.Where(i => i.TestSuites.FirstOrDefault()?.Equals($"{suite}", StringComparison.OrdinalIgnoreCase) ?? false);
                var entry = (suite: testSuite, cases: Get(caseEntries));
                entires.Add(entry);
            }
            return entires;
        }

        // parse & returns all valid cases
        private IEnumerable<int> Get(IEnumerable<RhinoTestCase> testCases)
        {
            var cases = new List<int>();
            foreach (var testCase in testCases)
            {
                int.TryParse(testCase.Key, out int caseOut);
                if (caseOut == default)
                {
                    continue;
                }
                cases.Add(caseOut);
            }
            return cases;
        }

        // get plan entries based on suites & test-cases
        private IEnumerable<TestRailPlanEntry> Get(IEnumerable<(TestRailSuite suite, IEnumerable<int> cases)> entires)
        {
            // shortcuts
            var P = Configuration.ConnectorConfiguration.Project;
            var C = configuraionsClient
                .GetConfigs(P)
                .SelectMany(i => i.Configs)
                .FirstOrDefault(i => i.Name.Equals(Configuration.Name, StringComparison.OrdinalIgnoreCase));

            var planEntries = new List<TestRailPlanEntry>();
            for (int i = 0; i < entires.Count(); i++)
            {
                var planEntry = new TestRailPlanEntry
                {
                    AssignedTo = user.Id,
                    ConfigIds = new[] { C.Id },
                    Description = "Automatically generated by the automation engine",
                    IncludeAll = true,
                    Name = $"Automation - Generated Entry [{i}]",
                    SuiteId = entires.ElementAt(i).suite.Id,
                    Runs = new[]
                    {
                        new TestRailRun
                        {
                            AssignedTo = user.Id,
                            IncludeAll = false,
                            ConfigIds = new[] { C.Id },
                            CaseIds = entires.ElementAt(i).cases.Distinct().ToArray()
                        }
                    }
                };
                planEntries.Add(planEntry);
            }
            return planEntries;
        }
        #endregion

        #region *** Bugs & Defects    ***
        /// <summary>
        /// Gets a list of open bugs.
        /// </summary>
        /// <param name="testCase">RhinoTestCase by which to find bugs.</param>
        /// <returns>A list of bugs (can be JSON or ID for instance).</returns>
        public override IEnumerable<string> GetBugs(RhinoTestCase testCase)
        {
            return bugsManager.GetBugs(testCase);
        }

        /// <summary>
        /// Asserts if the RhinoTestCase has already an open bug.
        /// </summary>
        /// <param name="testCase">RhinoTestCase by which to assert against match bugs.</param>
        /// <returns>An open bug.</returns>
        public override string GetOpenBug(RhinoTestCase testCase)
        {
            // setup
            int.TryParse(testCase.Key, out int caseId);

            // get references
            var onTestCase = casesClient.GetCase(caseId);
            var refs = onTestCase.Refs == default ? Array.Empty<string>() : onTestCase.Refs.Split(',');
            var bugType = testCase.GetCapability(capability: AtlassianCapabilities.BugType, defaultValue: "Bug");

            // get bugs from jira
            var issuesFromRefs = jiraClient.Get(refs).Where(i => $"{i.SelectToken("fields.issuetype.name")}".Equals(bugType, Compare));
            var onBugs = issuesFromRefs.Where(i => !Regex.IsMatch($"{i.SelectToken("fields.status.name")}", "(?i)(Done|Closed)"));
            var onBugsKeys = onBugs.Select(i => $"{i.SelectToken("key")}").Where(i => !string.IsNullOrEmpty(i));
            var openBug = jiraClient.Get(onBugsKeys).FirstOrDefault(i => testCase.IsBugMatch(bug: i, assertDataSource: true));

            // update context
            testCase.Context["bugs"] = onBugsKeys;
            testCase.Context["bugsData"] = onBugs;

            // get
            return openBug == default ? string.Empty : $"{openBug}";
        }

        /// <summary>
        /// Creates a new bug under the specified automation provider.
        /// </summary>
        /// <param name="testCase">Rhino.Api.Contracts.AutomationProvider.RhinoTestCase by which to create automation provider bug.</param>
        /// <returns>The ID of the newly created entity.</returns>
        public override string OnCreateBug(RhinoTestCase testCase)
        {
            return bugsManager.OnCreateBug(testCase);
        }

        /// <summary>
        /// Executes a routie of post bug creation.
        /// </summary>
        /// <param name="testCase">RhinoTestCase to execute routine on.</param>
        public override void PostCreateBug(RhinoTestCase testCase)
        {
            // exit conditions
            if (!testCase.Context.ContainsKey("lastBugKey"))
            {
                return;
            }

            // setup
            var format = Utilities.GetActionSignature(action: "created") + "\r\n";
            var code =
                "\r\n{code}" +
                $"curl {Configuration.ConnectorConfiguration.Collection}/index.php?/api/v2/get_case/{testCase.Key} " +
                "--user user:password " +
                "--header \"Content-Type: application/json\"" +
                "{code}";
            var data = new[]
            {
                new Dictionary<string, object>
                {
                    ["Test Rail Run ID"] = testCase.TestRunKey,
                    ["Test Rail Case ID"] = testCase.Key
                }
            };
            var comment = format + data.ToJiraMarkdown().Replace("\\r\\n", "\r\n") + code;
            var key = $"{testCase.Context["lastBugKey"]}";

            // put
            jiraClient.AddComment(idOrKey: key, comment);
        }

        /// <summary>
        /// Updates an existing bug (partial updates are supported, i.e. you can submit and update specific fields only).
        /// </summary>
        /// <param name="testCase">Rhino.Api.Contracts.AutomationProvider.RhinoTestCase by which to update automation provider bug.</param>
        public override string OnUpdateBug(RhinoTestCase testCase)
        {
            // setup
            var closeStatus = Regex.IsMatch(Configuration.ConnectorConfiguration.Collection, @"(?i)atlassian\.net")
                ? "Done"
                : "Closed";

            // put
            // status and resolution apply here only for duplicates.
            return bugsManager.OnUpdateBug(testCase, closeStatus, string.Empty);
        }

        /// <summary>
        /// Close all existing bugs.
        /// </summary>
        /// <param name="testCase">Rhino.Api.Contracts.AutomationProvider.RhinoTestCase by which to close automation provider bugs.</param>
        public override IEnumerable<string> OnCloseBugs(RhinoTestCase testCase)
        {
            return bugsManager.OnCloseBugs(testCase, "Done", string.Empty);
        }

        /// <summary>
        /// Close all existing bugs.
        /// </summary>
        /// <param name="testCase">Rhino.Api.Contracts.AutomationProvider.RhinoTestCase by which to close automation provider bugs.</param>
        public override string OnCloseBug(RhinoTestCase testCase)
        {
            return bugsManager.OnCloseBug(testCase, "Done", string.Empty);
        }
        #endregion
    }
}
