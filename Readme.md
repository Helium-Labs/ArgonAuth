# RelyingParty - FIDO2 WebAuthn API

This project is an implementation of a FIDO2 Relying Party, utilizing the FIDO2 WebAuthn API for secure user authentication. The API supports registration, authentication, and credential management.

## Endpoints

- **POST /makeCredentialOptions**: Generate credential options for registration
- **POST /makeCredential**: Register a new credential
- **POST /assertionOptions**: Generate assertion options for authentication
- **POST /makeAssertion**: Authenticate a user

## OpenAPI Client Generation

Generate the client in the language of choice, with the openapi-generator.
```sh
java -jar openapi-generator-cli.jar generate -i openapi.yaml -g typescript-axios -o ./TypescriptAxios 
```

## Getting Started

### Prerequisites

- .NET Core 6.x
- An AWS account with the AWS CLI configured

### Setup

1. Clone the repository.
2. Open the `RelyingParty` directory.
3. Run `dotnet restore` to restore the necessary packages.
4. Configure your AWS credentials and region by running `aws configure`.

### Test Serverless Endpoints

1. Debug build `dotnet build -c Debug`
2. Launch Serverless Offline UI Test Dashboard `dotnet lambda-test-tool-6.0`
3. You can send HTTP(S) requests to the indicated localhost endpoint


### Deployment

1. Publish the project with `dotnet publish -c Release`.
2. Deploy the application to AWS Lambda with `aws lambda create-function` or update an existing Lambda function with `aws lambda update-function-code`.

### Usage

To use the API, send requests to the deployed AWS Lambda function.

## Structure

- `Startup.cs`: Contains the configuration of the services and request pipeline.
- `Fido2Controller.cs`: Contains the implementation of the FIDO2 WebAuthn API endpoints.

### Libraries

This project uses the following libraries:

- Fido2NetLib: A .NET library for FIDO2 / WebAuthn Attestation and Assertion using .NET Core.
- ASP.NET Core: A cross-platform, high-performance, open-source framework for building modern, cloud-based, Internet-connected applications.


### Configuring for API Gateway HTTP API ###

API Gateway supports the original REST API and the new HTTP API. In addition HTTP API supports 2 different
payload formats. When using the 2.0 format the base class of `LambdaEntryPoint` must be `Amazon.Lambda.AspNetCoreServer.APIGatewayHttpApiV2ProxyFunction`.
For the 1.0 payload format the base class is the same as REST API which is `Amazon.Lambda.AspNetCoreServer.APIGatewayProxyFunction`.
**Note:** when using the `AWS::Serverless::Function` CloudFormation resource with an event type of `HttpApi` the default payload
format is 2.0 so the base class of `LambdaEntryPoint` must be `Amazon.Lambda.AspNetCoreServer.APIGatewayHttpApiV2ProxyFunction`.


### Configuring for Application Load Balancer ###

To configure this project to handle requests from an Application Load Balancer instead of API Gateway change
the base class of `LambdaEntryPoint` from `Amazon.Lambda.AspNetCoreServer.APIGatewayProxyFunction` to 
`Amazon.Lambda.AspNetCoreServer.ApplicationLoadBalancerFunction`.

### Project Files ###

* serverless.template - an AWS CloudFormation Serverless Application Model template file for declaring your Serverless functions and other AWS resources
* aws-lambda-tools-defaults.json - default argument settings for use with Visual Studio and command line deployment tools for AWS
* LambdaEntryPoint.cs - class that derives from **Amazon.Lambda.AspNetCoreServer.APIGatewayProxyFunction**. The code in 
this file bootstraps the ASP.NET Core hosting framework. The Lambda function is defined in the base class.
Change the base class to **Amazon.Lambda.AspNetCoreServer.ApplicationLoadBalancerFunction** when using an 
Application Load Balancer.
* LocalEntryPoint.cs - for local development this contains the executable Main function which bootstraps the ASP.NET Core hosting framework with Kestrel, as for typical ASP.NET Core applications.
* Startup.cs - usual ASP.NET Core Startup class used to configure the services ASP.NET Core will use.
* appsettings.json - used for local development.
* Controllers\ValuesController - example Web API controller

You may also have a test project depending on the options selected.

## Here are some steps to follow from Visual Studio:

To deploy your Serverless application, right click the project in Solution Explorer and select *Publish to AWS Lambda*.

To view your deployed application open the Stack View window by double-clicking the stack name shown beneath the AWS CloudFormation node in the AWS Explorer tree. The Stack View also displays the root URL to your published application.

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
