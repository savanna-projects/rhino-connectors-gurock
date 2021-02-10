using Gravity.Services.DataContracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Rhino.Api.Contracts.AutomationProvider;
using Rhino.Api.Contracts.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rhino.Connectors.Gurock.UnitTests
{
    [TestClass]
    public class OnGoing
    {
        [TestMethod]
        public void Test()
        {
            var configu = new RhinoConfiguration
            {
                Name = "For Integration Testing",
                TestsRepository = new[]
                {
                    "C1"
                },
                Authentication = new Authentication
                {
                    UserName = "automation@rhino.api",
                    Password = "Aa123456!"
                },
                ConnectorConfiguration = new RhinoConnectorConfiguration
                {
                    Collection = "https://rhinoapi2.testrail.io",
                    Password = "Aa123456!",
                    UserName = "rhino.api@gmail.com",
                    Project = "Rhino Demo",
                    BugManager = false,
                    DryRun = true,
                    Connector = Connector.TestRail,
                },
                DriverParameters = new[]
                {
                    new Dictionary<string, object>
                    {
                        ["driver"] = "ChromeDriver",
                        ["driverBinaries"] = @"D:\automation_env\web_drivers"
                    }
                },
                ScreenshotsConfiguration = new RhinoScreenshotsConfiguration
                {
                    KeepOriginal = true,
                    ReturnScreenshots = true
                },
                EngineConfiguration = new RhinoEngineConfiguration
                {
                    MaxParallel = 5
                },
                Capabilities = new Dictionary<string, object>
                {
                    //[$"{Connector.TestRail}:options"] = new Dictionary<string, object>
                    //{
                    //    ["bugType"] = "Bug",
                    //    ["milestone"] = "Demo Milestone",
                    //    ["jiraCollection"] = "https://rhinoapi.atlassian.net",
                    //    ["jiraProject"] = "RA",
                    //    ["jiraUserName"] = "rhino.api@gmail.com",
                    //    ["jiraPassword"] = "0hshf1gBkfZqsoABp9oO173D"
                    //},
                    ["bucketSize"] = 10,
                }
            };

            var testCase = new RhinoTestCase
            {
                Scenario = "Craeted by Rhino",
                Steps = new[]
                {
                    new RhinoTestStep { Action = "Action 1", Expected = "Expected 1" },
                    new RhinoTestStep { Action = "Action 2", Expected = "Expected 2" },
                    new RhinoTestStep { Action = "Action 3" },
                }
            };

            var connector = new GurockConnector(configu);
            var r = connector.Execute();
            var a = "";
        }
    }
}