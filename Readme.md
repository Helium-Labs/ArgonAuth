# RelyingParty - FIDO2 WebAuthn API

This project is an implementation of a FIDO2 Relying Party, utilizing the FIDO2 WebAuthn API for secure user authentication. The API supports registration, authentication, and credential management.

## Endpoints

- **POST /makeCredentialOptions**: Generate credential options for registration
- **POST /makeCredential**: Register a new credential
- **POST /assertionOptions**: Generate assertion options for authentication
- **POST /makeAssertion**: Authenticate a user

## OpenAPI.yaml schema generation

```sh
java -jar swagger-codegen-cli.jar generate -i swagger.json -l openapi-yaml -o ./RelyingParty/wwwroot
```

## OpenAPI Client Generation

```sh
java -jar openapi-generator-cli.jar generate -i ./RelyingParty/wwwroot/openapi.yaml -g typescript-axios -o ./tmp/KeypartyClient 
```

## Getting Started

### Prerequisites

- .NET Core 6.x
- An AWS account with the AWS CLI configured

### Setup

1. Clone the repository.
2. Open the `RelyingParty` directory.
3. Install the local lambda test tool with `dotnet tool install -g Amazon.Lambda.Tools` (optional).
4. Configure your AWS credentials and region by running `aws configure`. Or with the AWS CLI configuration file at `~/.aws/config`, or AWS Toolkit 2022 for VS.
5. Build. It'll also deploy. `./build.cmd`

### Test Serverless Endpoints

1. Debug build `dotnet build -c Debug`
2. Launch Serverless Offline UI Test Dashboard `dotnet lambda-test-tool-6.0`
3. You can send HTTP(S) requests to the indicated localhost endpoint


### Deployment

1. Publish the project with `dotnet publish -c Release`.
2. Run `./build.cmd`

## Structure

- `Startup.cs`: Contains the configuration of the services and request pipeline.
- `Fido2Controller.cs`: Contains the implementation of the FIDO2 WebAuthn API endpoints.
- `Data/PlanetScaleDatabase.cs` Contains the implementation of the PlanetScale database connection for storing user credentials.

### Libraries

This project uses the following libraries:

- Fido2NetLib: A .NET library for FIDO2 / WebAuthn Attestation and Assertion using .NET Core.
- ASP.NET Core: A cross-platform, high-performance, open-source framework for building modern, cloud-based, Internet-connected applications.

## Here are some steps to follow to get started from the command line:

Once you have edited your template and code you can deploy your application using the [Amazon.Lambda.Tools Global Tool](https://github.com/aws/aws-extensions-for-dotnet-cli#aws-lambda-amazonlambdatools) from the command line.

Install Amazon.Lambda.Tools Global Tools if not already installed.
```
    dotnet tool install -g Amazon.Lambda.Tools
```

If already installed check if new version is available.
```
    dotnet tool update -g Amazon.Lambda.Tools
```

Execute unit tests
```
    cd "RelyingParty/test/RelyingParty.Tests"
    dotnet test
```

Deploy application
```
    cd "RelyingParty/src/RelyingParty"
    dotnet lambda deploy-serverless
```
