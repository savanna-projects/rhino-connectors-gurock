[Home](../Home.md 'Home') 

# Gurock Connector - Overview
11/01/2020 - 1 minutes to read

## In This Article
* [Connector Capabilities](./docs/basics/ConnectorCapabilities.md 'ConnectorCapabilities')
* [TestRail Configuration](./docs/basics/TestRailConfiguration.md 'TestRailConfiguration')  

Rhino API connectors for using with [TestRail](https://www.gurock.com/testrail/) product. The bugs management tool using by this connector is Jira (both cloud and server are supported).

## Known Issues
* When adding more than one result to a test run, the latest result is automatically assigned. If running a data driven test and the last iteration pass, even if other iterations failed, the run will be marked as passed. This is a known [TestRail issue](https://discuss.gurock.com/t/test-results-all-steps-failed-but-overall-result-passed-cant-edit/605/2) which is not yet fixed. TestRail does not have an API for changing run results directly and therefore Rhino cannot change it.
* Atlassian is shifting to "Cloud First" and therefore [removing the future support and licenses sales for Jira Server](https://www.atlassian.com/migration/faqs#server). If this is your first use with Jira, we recommend to take Jira Cloud.

## See Also
* [TestRail User Guide](https://www.gurock.com/testrail/docs/user-guide)