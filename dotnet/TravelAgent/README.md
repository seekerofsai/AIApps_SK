
# AI Travel Agent 

## Prerequisites
    Semantic Kernel 

#### Pipeline

![Pipeline](./Images/TravelAgent.png)


## Prerequisites

Adding secrets

```powershell
dotnet user-secrets set "AzureOpenAI:ApiKey" "<>" --id "aitravelagent" </item>
dotnet user-secrets set "AzureOpenAI:DeploymentChatName" "<>"--id "aitravelagent" </item>
dotnet user-secrets set "AzureOpenAI:Endpoint" "<>" --id "aitravelagent" </item>

```powershell
dotnet build
dotnet run
```

