
# AI Travel Agent 

## Prerequisites
    Semantic Kernel 

#### Pipeline

![Pipeline](./Images/TravelAgent.png)


## Prerequisites

Adding secrets

```powershell
dotnet user-secrets set "AzureOpenAI:ApiKey" "<>" --id "aiapps" </item>
dotnet user-secrets set "AzureOpenAI:DeploymentChatName" "<>"--id "aiapps" </item>
dotnet user-secrets set "AzureOpenAI:Endpoint" "<>" --id "aiapps" </item>

```powershell
dotnet build
dotnet run
```

### Sample Prompts

how many usd is 2000 indian rupees

Agent:   
$2000 INR is approximately $23.99 in US Dollars (USD)

i am visiting Hyderabad, Inida, can you suggest some helpful phrases to converse

Agent: 
1. "Hello" - Namaste or Hello
2. "Thank you" - Dhanyavad (pronounced as dhan-ya-wad)
3. "How much does this cost?" - Yeh kitne ka hai? (pronounced as yeh kit-nay ka hai?)
4. "Where is the nearest restaurant?" - Sabse nazdeek restaurant kahan hai? (pronounced as sab-say naz-deek restaurant ka-han hai?)
5. "I need help" - Mujhe madad chahiye (pronounced as moo-jhe ma-dad cha-hee-ye)
6. "Where is the restroom?" - Shauchalay kahan hai? (pronounced as shau-cha-lay ka-han hai?)
7. "I would like to order..." - Main ... ordar karna chahunga/chahungi (for males/females) (pronounced as main ... order kar-na cha-hoon-ga/cha-hoon-gee)