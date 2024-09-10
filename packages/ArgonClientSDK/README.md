# Argon Client SDK

## Regenerating the RelyingParty REST Client with OpenAPI

```sh
java -jar swagger-codegen-cli.jar generate -i https://localhost:5001/swagger/v1/swagger.json -l typescript-axios -o ./src/api/RelyingPartyClient
```

## Regenerating the LSIG Deployer REST Client with OpenAPI

```sh
java -jar swagger-codegen-cli.jar generate -i https://zklsig.cloudflare-gradian.workers.dev/openapi.json -l typescript-axios -o ./src/api/LSIGDeployerClient
```
