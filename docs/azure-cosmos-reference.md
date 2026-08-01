# REFERENCE IMPLEMENTATION: COSMOS DB STARTUP INITIALIZATION

```csharp
// Step 1: Best-effort ARM Client Control-Plane Provisioning
try 
{
    var managedIdentityClientId = Environment.GetEnvironmentVariable("AZURE_MANAGED_IDENTITY_CLIENT_ID") ?? string.Empty;
    var credential = string.IsNullOrEmpty(managedIdentityClientId)
        ? new DefaultAzureCredential()
        : new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = managedIdentityClientId });

    var armClient = new ArmClient(credential);
    var subscription = await armClient.GetDefaultSubscriptionAsync();
    var resourceGroupResource = await subscription.GetResourceGroupAsync(resourceGroup);
    var cosmosAccount = await resourceGroupResource.Value.GetCosmosDBAccountAsync(accountName);

    var sqlDbContent = new CosmosDBSqlDatabaseCreateOrUpdateContent(new AzureLocation(region), new CosmosDBSqlDatabaseResourceInfo(databaseName));
    await cosmosAccount.Value.GetCosmosDBSqlDatabases().CreateOrUpdateAsync(WaitUntil.Completed, databaseName, sqlDbContent);

    var containerInfo = new CosmosDBSqlContainerResourceInfo(containerName) { PartitionKey = new CosmosDBContainerPartitionKey { Paths = { "/id" } } };
    var sqlContainerContent = new CosmosDBSqlContainerCreateOrUpdateContent(new AzureLocation(region), containerInfo);
    var sqlDbResource = await cosmosAccount.Value.GetCosmosDBSqlDatabases().GetAsync(databaseName);
    await sqlDbResource.Value.GetCosmosDBSqlContainers().CreateOrUpdateAsync(WaitUntil.Completed, containerName, sqlContainerContent);
} 
catch (Exception ex) 
{
    Console.WriteLine(\$"[STARTUP WARNING] ARM Provisioning skipped/failed: {ex.Message}");
    // Proceed seamlessly to required token-authenticated Data-Plane verification step below
}

// Step 2: Mandatory Token-Authenticated Data-Plane Fallback
try 
{
    var managedIdentityClientId = Environment.GetEnvironmentVariable("AZURE_MANAGED_IDENTITY_CLIENT_ID") ?? string.Empty;
    var credential = string.IsNullOrEmpty(managedIdentityClientId)
        ? new DefaultAzureCredential()
        : new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = managedIdentityClientId });

    var client = new Microsoft.Azure.Cosmos.CosmosClient(uri, credential);
    var dbResponse = await client.CreateDatabaseIfNotExistsAsync(databaseName);
    var containerResponse = await dbResponse.Database.CreateContainerIfNotExistsAsync(containerName, "/id");
} 
catch (Exception ex) 
{
    // Step 3: Terminal Exit on Failure
    throw new InvalidOperationException("CRITICAL: Token-authenticated data-plane initialization failed. Terminating application.", ex);
}
```
