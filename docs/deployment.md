# Deployment

Azure Container Apps. The pipeline is `.github/workflows/cicd.yml`, the infrastructure it declares is
`src/aspire/Aspire.AppHost/AppHost.cs`. This file records only what was set up by hand.

## `rg-shared`, Poland Central

| Resource | What | Chosen by hand |
| --- | --- | --- |
| `acrsrebrek` | container registry | Standard |
| `pg-srebrek` | Postgres flexible server | B1ms, 32 GB, v18, password auth |
| `pokermanager` | database on that server | plain `CREATE DATABASE` - nothing in the repo creates it |
| `ace-shared` | Container Apps environment | Aspire Dashboard enabled in *Monitoring -> Logging options* |
| `id-github-ci` | user-assigned identity | Federated Credential repo:srebrek/PokerManager:ref:refs/heads/main, Roles: `Contributor` and `Role Based Access Control Administrator` at subscription scope |

## GitHub

Secrets `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `DB_CONNECTION_STRING`; variables
`AZURE_LOCATION` = `polandcentral`, `AZURE_RESOURCE_GROUP` = `rg-poker-manager`.
