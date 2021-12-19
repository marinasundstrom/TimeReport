# Time Report

Time reporting tool.

Built with Blazor. Components are from MudBlazor. It is multi-platform.

## How to run

### Build Requirements

* .NET 6 SDK
* Tye CLI tools
* Docker Desktop

### Start app

Just start the app via Visual Studio or CLI.

### Start services

Given that you have the Tye CLI tools installed:

To start all the services, you run this command in he ```Backend``` project folder:

```sh
tye run
```

This starts all backend dependencies (SQL Server, Azurite, Nginx etc.)

#### Watch

To start with file watch:

```sh
tye run --watch
```

### Important 1 - Startup Issue

The Backend might not work properly when cold-started. This is because it fails to connect to SQL server due to it not having fully started yet.

If you are in *watch mode*, make change to a random file, like ```Program.cs```. Reverse it before it has been applied.

Then the Backend will restart, and everything will work.

### Important 2 - Certificates
Certificates should be placed in ```Backend/certs```. They are used by Nginx.

Requested file names:

```
localhost.crt
localhost.key
```

This is how you generate self-signed certificates (also used by ASP.NET Core) on macOS:

```
dotnet dev-certs https -ep aspnetapp.pfx -p crypticpassword
dotnet dev-certs https --trust
```

```
sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain <<certificate>>'
```

Extract private key

```
openssl pkcs12 -in aspnetapp.pfx -nocerts -out localhost.key
```


Extract certificate

```
openssl pkcs12 -in aspnetapp.pfx -clcerts -nokeys -out localhost.crt
```

Remove passphrase from key

```
cp localhost.key localhost.key.bak
openssl rsa -in localhost.key.bak -out localhost.key
```