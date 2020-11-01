[Home](../README.md 'README') 

# Connector Capabilities
11/01/2020 - 10 minutes to read

## In This Article
* [Automation Provider Capabilities](#automation-provider-capabilites)  

Each connector implements it's own integration capabilities and the behavior depends on the implementation. Please follow this post in order to perform a successful integration with TestRail.

## Automation Provider Capabilities
The list of optional capabilities which you can pass with Rhino Configuration.  

The options must be passed under `<connector_name>:options` key, as follow:

```js
...
"capabilities": {
  "connector_test_rail:options": {
    "bugType": "bug",
    "jiraCollection": "https://myjira.atlassian.net",
    ...
  }
}
...
```  

|Name          |Type   |Description                                                                                                                                                            |
|--------------|-------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------|
|jiraCollection|string |The server base address under which the application is hosted (i.e. https://myjira.atlassian.net).                                                                     |
|jiraPassword  |string |A valid password for your application.                                                                                                                                 |
|jiraUserName  |string |A valid user for your application. The user must have create permissions for **Bugs**.                                                                                 |
|jiraProject   |string |The project name or ID (depends on the connector implementation) under which to find and execute tests.                                                                |
|bugType       |string |Bug issue type capability, if not set "Bug" is the default.                                                                                                            |
|template      |string |The cases template name to use when creating cases. _**Template must have steps**_ - i.e. **Test Case (Steps)**. If not provided, **Test Case (Steps)** is the default.|