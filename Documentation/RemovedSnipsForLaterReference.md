# Removed Snippets of Code for Later Reference


```csharp
    public async Task<VerifyAssertionResult> MakeAssertionAndDelegateAccess(
        [FromBody] AuthenticatorAssertionRawResponse clientResponse,
        string username,
        CancellationToken cancellationToken
    ) {
        // ...
        // DEMO: now let's test the lsig to prove that delegation worked (or not)
        byte[] serverSecret = options.Challenge;
        // ES256 Credential Public Key Extraction
        var decodedPubKey = (CborMap)CborObject.Decode(creds.PublicKey);
        // X and Y values represent the coordinates of a point on the elliptic curve, constituting the public key
        byte[] pubkeyX = (byte[])decodedPubKey.GetValue(-2);
        byte[] pubkeyY = (byte[])decodedPubKey.GetValue(-3);
        var lsig = new AccountGameWallet(pubkeyX, pubkeyY);
        var compiledSig = await lsig.Compile((DefaultApi)_algodApi);
        // ...
    }
    
    // Make Assertion ...
    
    // byte[] didt = options.Challenge;
    // byte[] signature = clientResponse.Response.Signature;
    // await _db.UpsertDidt(clientResponse.Id, didt, signature);
```