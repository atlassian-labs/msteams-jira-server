# Jira Server

[![Atlassian license](https://img.shields.io/badge/license-Apache%202.0-blue.svg?style=flat-square)](LICENSE) [![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg?style=flat-square)](CONTRIBUTING.md)

Official plugin for Jira Server that integrates with [Microsoft Teams](https://www.microsoft.com/en-ww/microsoft-teams/group-chat-software).

## Usage

Jira Server for Microsoft Teams brings your Jira Server experience into your collaboration environment, letting you and your team stay focused, communicate on issues and backlog. Interact with Jira Server bot for Microsoft Teams to: create, assign, watch, edit issues, log working time. You may also interact with the bot from your team channel. With the messaging extension, you can quickly search for a specific issue and submit it to a channel or conversation. With the actionable message, you can quickly create a new issue, pre-populated with message text as the issue description, or save the message as a comment on one of your Jira Server issues. Also, you can add your project backlog to your channel as a tab, so that your team could easily track and work on the issues within the tab. 
Issue urls sent in a message to the group chat or team channel unfurl cards with context about the issue. The unfurl displays key information such as summary, status, priority, updated date, reporter, assignee and those values can be changed from the card directly. There are available 'Assigned to me’, ‘Reported by me’, ‘Watched by me’ and ‘My filters’ tabs by default but you also have a possibility to add a tab with a custom filter and save it.
 
**Important:** To use messaging extension, bot, or tabs you’ll need to install Microsoft Teams for Jira Server add-on to your Jira Server. Make sure you have admin permissions within your Jira Server to be able to install and configure the add-on. Once the add-on is installed, the add-on will generate and assign a unique Jira ID to your Jira Server instance. Share the generated Jira ID with the team so that your teammates could connect Microsoft Teams to Jira. Jira Server connector for Microsoft Teams can be set up independently and doesn’t require add-on installation. Connector is using webhooks and can be configured directly from the Teams channel. Please note, that the webhook set-up in Jira requires Jira admin permissions. 

## Installation

### Prerequisites
 1. Clone solution to local repository. To do that you can use [clone action](https://git-scm.com/docs/git-clone).
 1. Create or use your existing [Microsoft Teams](https://teams.microsoft.com) account for testing the application.
 1. Create [Bot Channels Registration](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-quickstart-registration?view=azure-bot-service-3.0).
    - Select the **New** button found on the upper left-hand corner of the Azure portal. Type **Bot Channels Registration** in the search box and press enter
    - On the **Bot Channels Registration** blade click the **Create** button to start the creation process.
    - On the **Bot Service** blade fill in all requested fields. _Messaging endpoint use your application base url `https://<MICROSOFT_BOT_APPLICATION_BASE_URL>/api/messages`.
    - Press **Create** button to create the service and register your bot's messaging endpoint. You can monitor the creation progess by checking the Notifications pane. The notifications will be changing from 'Deployment in progress...' to 'Deployment succeeded'. 
    - When deployment is finished go to **All resources** from the left pane and select the bot.
    - Open **Configuration** blade and click **Manage**. The link is placed near **Microsoft App ID** field. 
    - Click **New client secret** for generating a new password.
    - Save generated password as **__BOT_APP_SECRET__**.
    - Navigate to **Overview** section and save **Application (client) ID** as **__BOT_APP_ID__**.
    - Navigate to **Channels** section and select Microsoft Teams channel. Check 'I agree to the Microsoft Channel Publication Terms and the Microsoft Privacy Statements for my deployment to the Microsoft Teams channel.' and **Save** the configuration.
    - Navigate to **Authentication.** section.
    - In the **Redirect URIs**, add a redirect URL of type Web with a value  `https://<MICROSOFT_BOT_APPLICATION_BASE_URL>/loginResult.html`.
    - In the  **Implicit grant and hybrid flows** section, check **ID tokens** as this sample requires the [Implicit grant flow](https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-implicit-grant-flow) to be enabled to sign-in the user.
    - Also, in the **Redirect URIs**, add a redirect URL of type Web and value  `https://token.botframework.com/.auth/web/redirect`.
    - Select **Save**.
    - Navigate to **Expose an API**.
    - Click **Add a scope** button. Enter clear Scope name (e.g. App.Read), set "Admins and users" for the field _Who can consent?_, enter some description and save the scope.
    - Navigate to **API permissions**.
    - Click **Add a permission** button. Select just created API from **My APIs** tab. Select the Scope previously created above and save the changes. Please copy the value of just added permission (it's like `api://.../...`). It will be necessary later.
    - Go to **All resources** and select your created Bot Channels registration resource.
    - From the **Configuration** blade on bot resource, click **Add OAuth Connection Settings** button on the bottom of page.
    - Enter clear name for the new connection string.
    - In **Service Provider** selectbox choose **Azure Active Directory v2**. **Client id** is the **Application (client) ID** of AzureAD app registration. **Client secret** is the secret of AzureAD app registration. Set value of **Tennant ID** to `common`. **Scopes** should be a value of the added permission.
    - **Save** the changes
    - Open just created Connection string from Bot Channel Registration resource. Press **Test Connection**. 
    - If connection was tested successfully add Name of just created connection to the _appsettings.Development.json_ as a value of property **OAuthConnectionName**.
 1. Install locally or configure Mongo Db on Azure. Example for Azure:
    - Login into [Azure](https://portal.azure.com).
    - Click on All Resources -> Add Azure Cosmos DB
    - Fill all items. In my example ID is testingmsteams, API: Mongo DB. And use or create new resource group. Click Create.
    - Navigate to Azure Cosmos DB created resource. 
    - Add new database. Save its name as **__DATABASE_NAME__** (e.g. jiraintegrationdb).
    - Click on Connection String and copy Primary Connection string. Example: `mongodb://testingmsteams:azgsTNX2Jq.../?ssl=true&replicaSet=globaldb`
    - Copy and alter it by adding newly created database name after the last '/' in line: `mongodb://testingmsteams:azgsTNX2Jq.../__DATABASE_NAME__?ssl=true&replicaSet=globaldb`. Example : `mongodb://testingmsteams:azgsTNX2Jq.../jiraintegrationdb?ssl=true&replicaSet=globaldb`
    - Save it as **__DATABASE_URL__**.
 1. Create new [Azure Storage Account](https://docs.microsoft.com/en-us/azure/storage/common/storage-quickstart-create-account?toc=%2Fazure%2Fstorage%2Fblobs%2Ftoc.json&tabs=portal)
    - Save **Primary Connection string** as **__TABLE_BOT_DATA_STORE_CONNECTION_STRING__**.
 1. Create [Azure SignalR Service instance](https://docs.microsoft.com/en-us/azure/azure-signalr/signalr-quickstart-azure-functions-csharp)
    - Select the New button found on the upper left-hand corner of the Azure portal. In the New screen, type **SignalR Service** in the search box and press enter
    - Select **SignalR Service** from the search results, then select **Create**.
    - Configure the settings for your new instance.
    - Select **Create** to start deploying the SignalR Service instance.
    - Go to created resource > **Keys**.  Save connection string as **__SIGNALR_CONNECTION_STRING__**

### Configuring
 1. Download [ngrok](https://ngrok.com/download).Install it and run `ngrok http 5000 --host-header=localhost:5000` in the terminal. Note that ports can be different. Use yours. The ngrok will emulate traffic to your connection. The created link is your `<MICROSOFT_BOT_APPLICATION_BASE_URL>`.
 1. Run `npm install` command in the terminal from a root of ClienApp folder to install project dependencies.
 1. Open  _appsettings.Development.json_ for MicrosoftTeamsIntegration.Jira and put this json inside it:
  ```xml  
    {
      "BaseUrl": "https://<MICROSOFT_BOT_APPLICATION_BASE_URL>",
      "DatabaseUrl": "DATABASE_NAME",
      "MicrosoftAppId": "__BOT_APP_ID__",
      "MicrosoftAppPassword": "__BOT_APP_SECRET__",
      "OAuthConnectionName": "AAD_OAUTH_CONNECTION_NAME",
      "AddonKey": "microsoft-teams-jira-dev",
      "StorageConnectionString": "TABLE_BOT_DATA_STORE_CONNECTION_STRING",
      "BotDataStoreContainer": "DATA_STORE_CONTAINER_NAME",
      "CacheConnectionString": "CACHE_CONNECTION_STRING",
      "SignalRConnectionString": "SIGNALR_CONNECTION_STRING",
      "Logging": {
      "LogLevel": {
         "Default": "Debug",
         "System": "Information",
         "Microsoft": "Information"
         }
      }      
    }
  ```
4.  Build project and start it with any server (for example IIS Express).

### Install Jira Server app (add-on)
 1. Install standalone Jira server is installed on your machine or you have installed Atlassian SDK (https://developer.atlassian.com/server/framework/atlassian-sdk/install-the-atlassian-sdk-on-a-windows-system/).
 1. Create Jira Server add-on. See more details [here](https://dev.azure.com/msteams-atlassian/On-Premises%20Apps/_git/RefAppAddon?path=%2FREADME.md&version=GBmaster).
 1. Got to your local instance of Jira Server -> Settings -> Manage apps.
 1. Click **Upload app** and select *.jar file of your Jira Server addon created previously.
 1. Finish installation. Expand new app. All modules should be enabled.
 1. Press **Configure** and follow steps on the page.

### Debug integration locally
 1. Copy manifests\cloud\development or manifests\server\development folder to new folder in local desktop.
 1. Change local manifest.json. 
 1. Put **__BOT_APP_ID__** into all `botId` occurrences.
 1. Replace all occurrences of `https://msteamsdev.ngrok.io` to `https://<MICROSOFT_BOT_APPLICATION_BASE_URL>`.
 1. Create ZIP file of manifiest and two *.png files.
 1. Login to [Microsoft Teams](https://teams.microsoft.com).
 1. Click on Store -> Upload Custom App and put your ZIP there.

Side note - every time you will restart ngrok your microsoft bot application base url changes. After restart you have to change all `<MICROSOFT_BOT_APPLICATION_BASE_URL>` urls once again (in Portal Azure, User Secrets or appsettings.json, manifests, install Jira Sever\Cloud Add-on). Register free ngrok account to resolve this issue.

## Documentation

Please visit a [link](https://www.msteams-atlassian.com/JiraServer/) to get more details.

## Tests

All unit tests for Jira Server project are placed under MicrosoftTeamsIntegration.Jira.Tests. You can use Test Explorer in the Visual Studio IDE to run them and View test results. It displays the results in groups of Failed Tests, Passed Tests, Skipped Tests and Not Run Tests. The details pane at the bottom or side of the Test Explorer displays a summary of the test run.
Another way to execute all tests is to run `dotnet test` command in the terminal from the project root folder.   

## Contributions

Contributions to Jira Server are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for details. 

## License

Copyright (c) 2019 - 2021 Atlassian and others.
Apache 2.0 licensed, see [LICENSE](LICENSE) file.

<br/> 


[![With ❤️ from Atlassian](https://raw.githubusercontent.com/atlassian-internal/oss-assets/master/banner-with-thanks-light.png)](https://www.atlassian.com)
