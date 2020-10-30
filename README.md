# Rhino Connectors: Gurock
Rhino API connectors for using with [Test Rail](https://www.gurock.com/testrail/) product.

## Automation Provider Capabilites
The list of optional capabilities which you can pass with Rhino ProviderConfiguration.  

The options must be passed under `<connector_name>:options` key, as follow:

```js
...
"capabilities": {
	"connector_test_rail:options": {
		"dryRun": false,
		"bucketSize": 15,
		...
	}
}
...
```

|Name              |Type   |Description                                                                                                   |
|------------------|-------|--------------------------------------------------------------------------------------------------------------|
|dryRun            |boolean|Holds a boolean value rather or not to create Test Execution entity when running tests.                       |
|bucketSize        |number |How many parallel requests can be sent to Jira/XRay API when executing a large number of tests. Default is 4. |
|bugType           |string |Bug issue type capability, if not set "Bug" is the default.                                                   |