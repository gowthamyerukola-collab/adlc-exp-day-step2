using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.CosmosDB.Models;
using Azure.ResourceManager.Resources;
using Microsoft.Azure.Cosmos;
using OuterloopLabApi.Configuration;

namespace OuterloopLabApi.Services;

public interface ICosmosProvisioner
{
    Task EnsureInitializedAsync(ILogger logger, CancellationToken cancellationToken);
}

public sealed class CosmosProvisioner(ApplicationEnvironment environment, TokenCredential credential) : ICosmosProvisioner
{
    public async Task EnsureInitializedAsync(ILogger logger, CancellationToken cancellationToken)
    {
        if (!environment.HasCompleteCosmosConfiguration)
        {
            logger.LogWarning("Cosmos DB initialization skipped because one or more required environment variables are missing.");
            return;
        }

        await ProvisionControlPlaneAsync(logger, cancellationToken);
        await VerifyDataPlaneAsync(logger, cancellationToken);
    }

    private async Task ProvisionControlPlaneAsync(ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var armClient = new ArmClient(credential);
            var subscription = await armClient.GetDefaultSubscriptionAsync(cancellationToken);
            var resourceGroupResource = await subscription.GetResourceGroups().GetAsync(environment.CosmosDbResourceGroup!, cancellationToken);
            var cosmosAccount = await resourceGroupResource.Value.GetCosmosDBAccounts().GetAsync(environment.CosmosDbAccountName!, cancellationToken);

            var sqlDbContent = new CosmosDBSqlDatabaseCreateOrUpdateContent(
                new AzureLocation(environment.CosmosDbRegion!),
                new CosmosDBSqlDatabaseResourceInfo(environment.CosmosDbDatabase!));

            await cosmosAccount.Value.GetCosmosDBSqlDatabases()
                .CreateOrUpdateAsync(WaitUntil.Completed, environment.CosmosDbDatabase!, sqlDbContent, cancellationToken);

            var containerInfo = new CosmosDBSqlContainerResourceInfo(environment.CosmosDbContainer!)
            {
                PartitionKey = new CosmosDBContainerPartitionKey
                {
                    Paths = { "/id" }
                }
            };

            var sqlContainerContent = new CosmosDBSqlContainerCreateOrUpdateContent(new AzureLocation(environment.CosmosDbRegion!), containerInfo);
            var sqlDbResource = await cosmosAccount.Value.GetCosmosDBSqlDatabases().GetAsync(environment.CosmosDbDatabase!, cancellationToken);

            await sqlDbResource.Value.GetCosmosDBSqlContainers()
                .CreateOrUpdateAsync(WaitUntil.Completed, environment.CosmosDbContainer!, sqlContainerContent, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning("ARM Provisioning skipped/failed: {Message}", exception.Message);
        }
    }

    private async Task VerifyDataPlaneAsync(ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var client = new CosmosClient(environment.CosmosDbUri!, credential);
            var container = client.GetContainer(environment.CosmosDbDatabase!, environment.CosmosDbContainer!);
            await container.ReadContainerAsync(cancellationToken: cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning("Cosmos DB data-plane verification failed: {Message}", exception.Message);
        }
    }
}
