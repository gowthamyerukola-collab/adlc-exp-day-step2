# Container Environment Variables

Environment variables injected into Azure Container App containers, sourced from `.azure/container-app.tmpl.yaml`.

## Frontend Container

| Key | Description | Value |
|---|---|---|
| `VITE_API_URL` | API base URL for the Vite frontend. Left empty because Nginx proxies `/api/*` to the backend on the same origin. | `""` (empty) |

## Backend Container

| Key | Description | Value |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | .NET runtime environment mode. | `"Production"` |
| `ASPNETCORE_URLS` | URLs the .NET backend binds to. | `"http://+:3000"` |
| `CURRENCY_API_BASE_URL` | Base URL of the external currency rate provider API. Optional; when unset the backend defaults to Frankfurter at `https://api.frankfurter.app`. | `https://api.frankfurter.app` |
| `COSMOS_DB_URI` | Cosmos DB account endpoint URI. Resolved at deploy time from the `COSMOSDB_NAME` GitHub variable. | `${COSMOS_DB_URI}` |
| `COSMOS_DB_DATABASE` | Cosmos DB database name. | `"currency-conversion-db"` |
| `COSMOS_DB_CONTAINER` | Cosmos DB container name. | `"currencyconversion"` |
| `COSMOS_DB_ACCOUNT_NAME` | Cosmos DB account name, used for managed identity authentication. | `${COSMOS_DB_ACCOUNT_NAME}` |
| `COSMOS_DB_RESOURCE_GROUP` | Azure resource group containing the Cosmos DB account. | `${COSMOS_DB_RESOURCE_GROUP}` |
| `COSMOS_DB_REGION` | Azure region where Cosmos DB is deployed. `Central India` as default value. | `"Central India"` |
| `AZURE_MANAGED_IDENTITY_CLIENT_ID` | Client ID of the user-assigned managed identity used to authenticate with Cosmos DB. | `${AZURE_MANAGED_IDENTITY_CLIENT_ID}` |
