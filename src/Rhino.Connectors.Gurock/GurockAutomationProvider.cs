﻿/*
 * CHANGE LOG - keep only last 5 threads
 * 
 * RESOURCES
 * http://docs.gurock.com/testrail-api2/reference-attachments
 */
using Gravity.Abstraction.Logging;

using Rhino.Api;
using Rhino.Api.Contracts.AutomationProvider;
using Rhino.Api.Contracts.Configuration;
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
using System.Text.Json;
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
        private readonly JiraClient jiraClient;
        private readonly JiraBugsManager bugsManager;

        // members: test rail clients
        private readonly ClientFactory clientFactory;
        private readonly TestRailSuitesClient suitesClient;
        private readonly TestRailCasesClient casesClient;
        private readonly TestRailConfiguraionsClient configuraionsClient;
        private readonly TestRailMilestoneClient milestoneClient;
        private readonly TestRailPlansClient plansClient;
        private readonly TestRailUsersClient usersClient;
        private readonly TestRailPrioritiesClient prioritiesClient;
        private readonly TestRailTemplatesClient templatesClient;

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
            BucketSize = configuration.GetCapability(ProviderCapability.BucketSize, 15);

            // jira
            var jiraAuth = GetJiraAuthentication(configuration.Capabilities);
            if (!string.IsNullOrEmpty(jiraAuth.Collection))
            {
                jiraClient = new JiraClient(jiraAuth);
                bugsManager = new JiraBugsManager(jiraClient);
            }

            // integration
            clientFactory = new ClientFactory(
                configuration.ConnectorConfiguration.Collection,
                configuration.ConnectorConfiguration.Username,
                configuration.ConnectorConfiguration.Password,
                logger);

            suitesClient = clientFactory.Create<TestRailSuitesClient>();
            casesClient = clientFactory.Create<TestRailCasesClient>();
            configuraionsClient = clientFactory.Create<TestRailConfiguraionsClient>();
            milestoneClient = clientFactory.Create<TestRailMilestoneClient>();
            plansClient = clientFactory.Create<TestRailPlansClient>();
            usersClient = clientFactory.Create<TestRailUsersClient>();
            prioritiesClient = clientFactory.Create<TestRailPrioritiesClient>();
            templatesClient = clientFactory.Create<TestRailTemplatesClient>();

            // meta data
            project = suitesClient.Projects.FirstOrDefault(i => i.Name.Equals(configuration.ConnectorConfiguration.Project, Compare));
            user = usersClient.GetUserByEmail(configuration.ConnectorConfiguration.Username);
        }

        private static JiraAuthentication GetJiraAuthentication(IDictionary<string, object> capabilities) => new()
        {
            AsOsUser = capabilities.GetCapability(RhinoConnectors.TestRail, "jiraAsOsUser", false),
            Capabilities = capabilities.GetCapability(RhinoConnectors.TestRail, "jiraCapabilities", new Dictionary<string, object>()),
            Collection = capabilities.GetCapability(RhinoConnectors.TestRail, "jiraCollection", string.Empty),
            Password = capabilities.GetCapability(RhinoConnectors.TestRail, "jiraPassword", string.Empty),
            Project = capabilities.GetCapability(RhinoConnectors.TestRail, "jiraProject", string.Empty),
            Username = capabilities.GetCapability(RhinoConnectors.TestRail, "jiraUsername", string.Empty)
        };
        #endregion

        #region *** Get: Test Cases   ***
        /// <summary>
        /// Returns a list of test cases for a project.
        /// </summary>
        /// <param name="ids">A list of test ids to get test cases by.</param>
        /// <returns>A collection of Rhino.Api.Contracts.AutomationProvider.RhinoTestCase</returns>
        protected override IEnumerable<RhinoTestCase> OnGetTestCases(params string[] ids)
        {
            // constants: logging
            const string M1 = "tests-repository parsed into integers";
            const string M2 = "total of [{0}] distinct items found (test or test-suite)";

            // parse as int
            var idList = new List<(string schema, int id)>();
            foreach (var id in Configuration.TestsRepository)
            {
                idList.Add(GetSchema(id));
            }
            logger?.Debug(M1);

            // normalize
            var cases = idList.Where(i => i.schema.Equals("C", Compare)).Select(i => i.id).Distinct();
            var suits = idList.Where(i => !i.schema.Equals("C", Compare)).Select(i => i.id).Distinct();
            logger?.DebugFormat(M2, idList.Count);

            // get all test-cases
            var bySuites = suits.SelectMany(i => casesClient.GetCases(project.Id, i)).Where(i => i != null) ?? Array.Empty<TestRailCase>();
            var byCases = cases.Select(i => casesClient.GetCase(i)).Where(i => i != null) ?? Array.Empty<TestRailCase>();
            return Gravity
                .Extensions
                .CollectionExtensions
                .DistinctBy(bySuites.Concat(byCases), i => i.Id)
                .Select(ToConnectorTestCase);
        }

        private static (string Schema, int Id) GetSchema(string id)
        {
            // setup
            var isCase = Regex.IsMatch(id.Trim(), @"(?i)C\d+");

            // build
            var schema = isCase ? "C" : "S";
            _ = int.TryParse(Regex.Match(id.Trim(), @"\d+").Value, out int idOut);

            // get
            return (schema, idOut);
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
            testCase.Context["projectKey"] = string.IsNullOrEmpty($"{jiraClient?.Authentication?.Project}")
                ? "-1"
                : jiraClient.Authentication.Project;

            // get
            return testCase;
        }
        #endregion

        #region *** Create: Test Case ***
        /// <summary>
        /// Creates a new test case under the specified automation provider.
        /// </summary>
        /// <param name="testCase">Rhino.Api.Contracts.AutomationProvider.RhinoTestCase by which to create automation provider test case.</param>
        /// <returns>The ID of the newly created entity.</returns>
        protected override string OnCreateTestCase(RhinoTestCase testCase)
        {
            // shortcuts
            var onProject = Configuration.ConnectorConfiguration.Project;
            testCase.Context[ContextEntry.Configuration] = Configuration;

            // get section id
            var master = suitesClient.GetSuites(project.Id).FirstOrDefault(i => i?.IsMaster == true);
            _ = int.TryParse(testCase.TestSuites.FirstOrDefault(), out int suite);
            var id = suite == 0 ? master.Id : suite;
            testCase.TestSuites = new[] { $"{id}" };

            // get template
            var templateName = Configuration.Capabilities.GetCapability(RhinoConnectors.TestRail, "template", "Test Case (Steps)");
            var template = templatesClient.GetTemplates(project.Id).FirstOrDefault(i => i.Name.Equals(templateName, Compare));
            if(template == default)
            {
                return string.Empty;
            }

            // setup request body
            var testRailCase = testCase.ToTestRailCase();
            testRailCase.TemplateId = template.Id;

            // add test case
            testRailCase = casesClient.AddCase(sectionId: id, data: testRailCase);

            // success
            Logger?.InfoFormat($"Create-Test -Project [{onProject}] -Set [{string.Join(",", testCase?.TestSuites)}] = true");

            // results
            return JsonSerializer.Serialize(testRailCase, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        #endregion

        #region *** Configuration     ***
        /// <summary>
        /// Implements a mechanism of setting a testing configuration for an automation provider.
        /// </summary>
        /// <remarks>Use this method for <see cref="SetConfiguration"/> customization.</remarks>
        protected override void OnSetConfiguration(RhinoConfiguration configuration)
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
        protected override RhinoTestRun OnCreateTestRun(RhinoTestRun testRun)
        {
            // constants: logging
            const string M1 = "test-run [{0}] create under [{1}] project";

            // constants
            const string C1 = "Automatically generated by Rhino engine";

            // shortcuts
            var M = Configuration.Capabilities.GetCapability(RhinoConnectors.TestRail, "milestone", string.Empty);

            // collect all suites used in this run
            var suites = GetSuites(testRun.TestCases).Distinct();

            // compose information about suits & cases for this run
            var entires = Get(suites, testRun.TestCases);
            var planEntries = Get(entires);

            // get milestone
            _ = int.TryParse(M, out int msOut);
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
        private static IEnumerable<int> GetSuites(IEnumerable<RhinoTestCase> testCases)
        {
            var suites = new List<int>();
            foreach (var testCase in testCases)
            {
                _ = int.TryParse(testCase.TestSuites?.FirstOrDefault(), out int suiteOut);
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
        private static IEnumerable<int> Get(IEnumerable<RhinoTestCase> testCases)
        {
            var cases = new List<int>();
            foreach (var testCase in testCases)
            {
                _ = int.TryParse(testCase.Key, out int caseOut);
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

        /// <summary>
        /// Completes automation provider test run results, if any were missed or bypassed.
        /// </summary>
        /// <param name="testRun">Rhino.Api.Contracts.AutomationProvider.RhinoTestRun results object to complete by.</param>
        protected override void OnRunTeardown(RhinoTestRun testRun)
        {
            // get test plan
            var isPlan = testRun.Context.ContainsKey(nameof(TestRailPlan));
            var isPlanNull = isPlan && testRun.Context[nameof(TestRailPlan)] == default;

            if (isPlanNull)
            {
                return;
            }

            var plan = testRun.Context[nameof(TestRailPlan)] as TestRailPlan;

            // close test plan
            plansClient.ClosePlan(plan.Id);
        }
        #endregion

        #region *** Bugs & Defects    ***
        /// <summary>
        /// Gets a list of open bugs.
        /// </summary>
        /// <param name="testCase">RhinoTestCase by which to find bugs.</param>
        /// <returns>A list of bugs (can be JSON or ID for instance).</returns>
        protected override IEnumerable<string> OnGetBugs(RhinoTestCase testCase)
        {
            return bugsManager.GetBugs(testCase);
        }

        /// <summary>
        /// Asserts if the RhinoTestCase has already an open bug.
        /// </summary>
        /// <param name="testCase">RhinoTestCase by which to assert against match bugs.</param>
        /// <returns>An open bug.</returns>
        protected override string OnGetOpenBug(RhinoTestCase testCase)
        {
            // setup
            _ = int.TryParse(testCase.Key, out int caseId);

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
        protected override string OnCreateBug(RhinoTestCase testCase)
        {
            return bugsManager.OnCreateBug(testCase);
        }

        /// <summary>
        /// Executes a routine of post bug creation.
        /// </summary>
        /// <param name="testCase">RhinoTestCase to execute routine on.</param>
        protected override void OnCreateBugTeardown(RhinoTestCase testCase)
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
        protected override string OnUpdateBug(RhinoTestCase testCase)
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
        protected override IEnumerable<string> OnCloseBugs(RhinoTestCase testCase)
        {
            // setup
            _ = int.TryParse(testCase.Key, out int id);

            // get bugs
            var testRailCase = casesClient.GetCase(id);
            if (string.IsNullOrEmpty(testRailCase.Refs))
            {
                return Array.Empty<string>();
            }

            // parse bugs
            var bugs = testRailCase.Refs.Split(',').Select(i => i.Trim());
            return bugsManager.OnCloseBugs(testCase, "Done", string.Empty, bugs);
        }

        /// <summary>
        /// Close all existing bugs.
        /// </summary>
        /// <param name="testCase">Rhino.Api.Contracts.AutomationProvider.RhinoTestCase by which to close automation provider bugs.</param>
        protected override string OnCloseBug(RhinoTestCase testCase)
        {
            return bugsManager.OnCloseBug(testCase, "Done", string.Empty);
        }
        #endregion
    }
}
