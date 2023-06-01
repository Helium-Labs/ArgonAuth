# Research, Development & Due-Diligence around Account Access gated by Webauthn 



## Rekeying a smart-signature to a multi-sig account
Relevant when wanting to add another factor of authenticator to signing requests. Ideally want a way to only require signing by the second factor for certain kinds of TX.
Rekeying a Smart-Signature, and accounts more generally, to include a second factor of authentication (another key):

### With Javascript
```javascript
const txn = //...
const signerAddrs: string[] = []
signerAccounts.push(acc.addr)
signerAccounts.push(smartSig.address())

// multiSigParams is used when creating the address and when signing transactions
const multiSigParams = {
  version: 1,
  threshold: 2,
  addrs: signerAddrs,
}
// First signature uses signMultisigTransaction
const msigWithFirstSig = algosdk.signMultisigTransaction(
  txn,
  multiSigParams,
  acc.sk
).blob
const signedDelegatedTxn = algosdk.signLogicSigTransactionObject(
  txn,
  smartSig
)
const msigWithAllSigs = algosdk.appendSignRawMultisigSignature(
  msigWithFirstSig,
  smartSig.address(),
  signedDelegatedTxn 
)
// submit...
```

Sources:
- https://algorand.github.io/js-algorand-sdk/functions/appendSignRawMultisigSignature.html
- https://developer.algorand.org/docs/get-details/transactions/signatures/#multisignatures
- https://developer.algorand.org/docs/get-details/dapps/smart-contracts/frontend/smartsigs/

### With CS
```csharp
// make the unsigned tx
PaymentTransaction transaction = new PaymentTransaction() 
{
    Fee=fee,
    FirstValid = fv,
    LastValid = lv,
    GenesisHash = new Digest(gh),
    Receiver = new Address(to),
    CloseRemainderTo = new Address(close),
    Amount = amt,
    GenesisID = gen,
    Note = Convert.FromBase64String(note)
};

// create an MSIG with which we'll sign it
var version = 1;
var threshold = 2;
MultisigSignature mSig = new MultisigSignature(version, threshold);

// add subsigs (at least threshold count worth)
// sign with the account
Account signingAccount = //... init
SignedTransaction accTxSig = transaction.sign(signingAccount);
mSig.Subsigs.Add(new MultisigSubsig(signingAccount.KeyPair.PublicKey, txSig.Sig));
// sign with the lsig
LogicsigSignature lsig = //... init
SignedTransaction lsigTxSig = transaction.sign(lsig);
mSig.Subsigs.Add(new MultisigSubsig(lsig.Address, lsigTxSig));

// sign original tx with the msig we just created. NOTE: there's no transaction.sign(msig) :(
SignedTransaction msigTxSig = new SignedTransaction(transaction, null, mSig, null, null);
// submit...
```

Sources:
- https://frankszendzielarz.github.io/dotnet-algorand-sdk/api/Algorand.MultisigSignature.html
- https://frankszendzielarz.github.io/dotnet-algorand-sdk/api/Algorand.MultisigSubsig.html
- https://frankszendzielarz.github.io/dotnet-algorand-sdk/api/Algorand.LogicsigSignature.html
