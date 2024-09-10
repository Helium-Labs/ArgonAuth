dotnet restore
dotnet build
dotnet lambda package --configuration Release --framework net6.0 --output-package bin/Release/net6.0/RelyingParty.zip
serverless deploy