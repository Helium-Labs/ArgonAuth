# Research, Development & Due-Diligence around Account Access gated by Webauthn 

# Rekeying a smart-signature to a multi-sig account
Rekeying a Smart-Signature, and accounts more generally, to include a second factor of authentication (another key):

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
