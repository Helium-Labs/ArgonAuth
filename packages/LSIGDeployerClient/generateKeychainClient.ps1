# Download the JSON file
Invoke-WebRequest -Uri "https://zklsig.cloudflare-gradian.workers.dev/openapi.json" -OutFile "swagger.json"
# Generate the client
java -jar .\swagger-codegen-cli.jar generate -i .\swagger.json -l typescript-axios -o ./src/client
# Delete the client's package.json
Remove-Item .\src\client\package.json
