# Argon Auth

⚠️ WARNING: This software is not production-ready and contains intellectual property that is patent-pending in some jurisdictions. Use at your own risk.

Argon Auth is a service that enables FIDO2 to authenticate a user's access to an Algorand contract account containing their assets. It is designed to be self-custodial, with the eventual goal of supporting account recovery and including an integrated on-ramp, allowing users to purchase crypto assets within a cohesive, low-friction authentication service.

Unfortunately, during development, it was discovered that the basic FIDO2 on-chain mechanism was patent pending, and no simple variation of the mechanism could be devised to circumvent this issue, which brought the project to a standstill. For this reason, the license is explicitly Apache v2, which includes a clause covering no liability for infringing IP.

There are no plans to continue the service or undertake further development. It reached the MVP stage and should be viewed as an early prototype. Additionally, you will notice a lack of documentation, although, for the most part, the code is self-explanatory.

This work was done in collaboration by Frank Szendzielarz and Winton Nathan-Roberts.

## Demo Video

Watch the demo video on YouTube:

[![Argon Demo Video](https://img.youtube.com/vi/0_M8aunqZyA/0.jpg)](https://youtu.be/0_M8aunqZyA)

Click the image above to watch the video.


## Usage & Testing

To run a demo of the service, please do the following:

1. Provide your secret keys: `apps/RelyingPartyServer/RelyingParty/appsettings.json` is a place to provide your database connection string `ConnectionStrings.Default`, and Algorand `serverAccount.mnemonic`.
2. Start Algorand localnet on the default ports (See [Algokit](https://algorand.co/algokit))
3. Start the `apps/RelyingPartyServer` backend: see "Locally running the service" of its [readme](./apps/RelyingPartyServer/Readme.md)
4. Start the `apps/ContractAccountLSIGDeployer` backend: see "Locally running the service" of its [readme](./apps/ContractAccountLSIGDeployer/README.MD)
5. Start the `apps/DemoFrontend` frontend: see "Development" of its [readme](./apps/DemoFrontend/README.md)

Finally, you can visit the frontend to see demo at [http://localhost:8123/](http://localhost:8123/).

## License & Attribution

The code is [licensed under Apache 2.0](./LICENSE). Please provide attribution to the respective authors Frank Szendzielarz and Winton Nathan-Roberts.

## Disclaimer

Users must assess the risks and determine whether they can legally use the software. Please read the attached [license](./LICENSE). 
