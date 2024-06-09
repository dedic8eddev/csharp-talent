# Introduction  
This is a main product API for Fruity Parsnips.

It is authenticated via short-lived Bearer Tokens issued by IdentityServer.

There is an async processing mechanism using an Azure Function app.

# Getting Started
1. Install Visual Studio 2019
2. Install [Azure Storage Emulator](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator)
3. Install [Cosmos Db Emulator](https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator)
4. The Azure Functions CLI should automatically be [installed as part of VS 2019](https://docs.microsoft.com/en-us/azure/azure-functions/functions-develop-local)
5. Install the [Azure Functions Core Tools](https://www.npmjs.com/package/azure-functions-core-tools) (required only for running Functions from command line)

# Run (IDE)
- Open `Ikiru.Parsnips.sln` in Visual Studio/VS Code and build all.
- Run the `Ikiru.Parnsips.Api` project for the API.
- Additionally run the `Ikiru.Parsnips.Functions` project if you need async processing (e.g. import PDF)

# Run (Command Line)
- Ensure your machine trusts the NetCore Dev Cert: `dotnet dev-certs https --trust`
- Navigate to `\Ikiru.Parnsips.Api\` folder and execute `dotnet run` for the API
- Or check it builds ok first with `dotnet build`
- For Functions, ensure you have installed the Azure Functions Core Tools (see Getting Started)
- Navigate to `\Ikiru.Parnsips.Functions\` folder and execute: `func start --build`
- Note: If you are running both the API and the Functions, avoid shared file locking by building the API with `dotnet build` first, then run the functions and finally run the API with `dotnet run`.

# Test
- As we are doing TDD, everything is unit tested.
- There are also Integration Tests for the API which is sometimes a good starting point for checking your environment is setup correctly. 

# Functions and Credentials (appsettings.Development.json)
- The Import function will not work for PDF/Text as there are no Sovren Credentials (if you really need them then we can get them for you but you must not commit the changes to the server as they are live creds)
- The Geolocation in the PersonLocationChanged function will not work as there are no Azure Maps credentials.  If you need this then create an Azure Maps instance in Azure Portal in your MSDN Account (not in the Parsnips Dev)
- For Emails, if you need them to work then use MailTrap.io (see below)

# MailTrap.io
1. Go to https://mailtrap.io/register/signup
2. Sign Up using Office 365 Account - login with your @ikirupeople.com account and approve permissions for MailTrap
3. Go to Demo Inbox in Inboxes and click Configure (Cog)
4. Copy SMTP details
5. Paste SMTP details into appsettings.development.json:

>
    "smtpSendingSettings": {
      "host": "smtp.mailtrap.io",
      "port": 587,
      "username": "<username>",
      "password": "<password>",
      "fromAddress": "local-dev@talentis.global"
    },

6. Emails will then appearin the Demo Inbox.  
7. Be aware that there is a limit to the number of messages on the free account, so maybe remove creds if not using it.

# Rocket Reach Api
- Note. ApiKey used in dev and test is the only apikey as of (10/09/20).
- There is not a sandbox environment.  
- Product ApiKey might need to be created.
- Integration tests use a Mocked RocketReachAPI

# Running the API with external access
1. Locate the `applicationhost.config` if the \.vs\ folder of the solution you have open.  This will be `(Directory where you opened the solution)\.vs\(solution name - important!)\config\` (note .vs is a hidden folder)
2. Within the `applicationhost.config` find the `Ikiru.Parsnips.Api` with bindings for port 58261 and 44385
3. Edit the bindings like this:

>
    <binding protocol="http" bindingInformation=":58261:" />
    <binding protocol="https" bindingInformation=":44385:" />
    <binding protocol="http" bindingInformation=":58261:desktop-rt" />
    <binding protocol="https" bindingInformation=":44385:desktop-rt" />

Replacing `desktop-rt` with your machine name. (run `hostname` at a command prompt to get this)

4. Save and make sure that IISExpress is not running in your System Tray (check for iisexpress.exe in task manager if unsure)
5. Run the following in an elevated command prompt or powershell:

>
    netsh http add urlacl url=https://*:44385/ user=Interactive listen=yes

    netsh advfirewall firewall add rule name="Fruity Parsnips API Port" dir=in action=allow protocol=TCP localport=44385

6. Run the API project.  You should be able to view, for example, `https://<machinename>:44385/api/persons/?profileUrl=https://uk.linkedin.com/in/abc123` (will get 404 with json content)
7. Any problems:
    - Try running Visual Studio as Admin
    - In the binding entries, use * instead of blanks - e.g. `*:44385:*`
    - Delete folder `%userprofile%\Documents\IISExpress` (VS will create it again)
    - Clean and Rebuild project
    - Ensure IIS Express is not running.
    - When running, check that the IISExpress Tray > Show All Applications lists 4 entries for the 4 above bindings. If not, ensure you are editing the correct `applicationhost.config` file.