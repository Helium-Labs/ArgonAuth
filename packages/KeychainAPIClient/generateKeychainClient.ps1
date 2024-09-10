# Download the JSON file
Invoke-WebRequest -Uri "https://localhost:5001/swagger/v1/swagger.json" -OutFile "swagger.json"
# Generate the client
java -jar .\swagger-codegen-cli.jar generate -i .\swagger.json -l typescript-axios -o ./src/
# Delete the client's package.json
Remove-Item .\src\package.json
