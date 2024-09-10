dotnet restore
dotnet build
dotnet lambda package --configuration Release --framework net8.0 --output-package bin/Release/net8.0/RelyingParty.zip
serverless deploy