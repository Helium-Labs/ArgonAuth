# Research, Development & Due-Diligence around Account Access gated by Webauthn 

## LSIG Contract Account with Access Gated by Webauthn

A contract account that approves any TX from the account if a valid DIDT and Webauthn is provided.

### Spec

```
immutable state:
 - secp256r1 public key for the authenticator

assert(DIDT hasn't expired)
assert(DIDT signed with secp256r1 public key)
assert(DIDT contains PK_sess)
assert(TX hash signed with PK_sess)
assert(lease)
assert(default checks)
```


## Signature Mode 

Logic signatures (lsigs) in Algorand refer to programs that execute in a constrained environment. These programs are stateless and are essentially digital signatures with embedded logic. They have no direct access to the majority of the blockchain state, except for the transaction they're validating and a small selection of global properties.

### Contract Account

If the SHA512_256 hash of the program (prefixed by "Program") is equal to authorizer address of the transaction sender then this is a contract account wholly controlled by the program. No other signature is necessary or possible. The only way to execute a transaction against the contract account is for the program to approve it.
*Meaning you can't use a multisignature to add another layer of security to a contract account*.

### Delegated Access

If the account has signed the program (by providing a valid ed25519 signature or valid multisignature for the authorizer address on the string "Program" concatenated with the program bytecode) then: if the program returns true the transaction is authorized as if the account had signed it. This allows an account to hand out a signed program so that other users can carry out delegated actions which are approved by the program. Note that Smart Signature Args are not signed.

Sources: 
- https://developer.algorand.org/docs/get-details/dapps/avm/teal/specification/#execution-environment-for-smart-signatures


## Rekeying a smart-signature to a multi-sig account for MFA delegated lsig access 

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
