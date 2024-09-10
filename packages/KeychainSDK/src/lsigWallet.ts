import {
  type DWT
} from '@gradian/keychain-auth-server-client'
import {
  AssertDefined,
  GetRSFromASN1DEREncodedSignature,
  base64ToBase64url,
  getJWTParts,
  isNullOrUndefined
} from './util'
import { Fido2Client, algod } from './api'
import { compressECDSACoseKey, signWithEd25519 } from './crypto'
import algosdk from 'algosdk'
import { isTestEnvirnoment } from './constants'
import { fundAccount } from 'algokit-testkit'
import { getWebauthnCompiledLSIG } from './lib'

export interface Keypair {
  sk: Uint8Array
  pk: Uint8Array
}

export async function getUsernameIsAvailable(username: string): Promise<boolean> {
  const isAvailableResponse = await Fido2Client.fidoUsernameIsAvailable(username)
  return isAvailableResponse.data
}

export default class WebauthnLSIGWallet {
  private compiledTeal?: string
  private sessionKey?: Keypair
  private dwt?: DWT
  private isInitalized = false

  public async initialize(jwt: string, csk: Keypair): Promise<void> {
    this.dwt = getJWTParts(jwt).claims
    AssertDefined(this.dwt, 'this.dwt must be defined')
    AssertDefined(this.dwt.credpk, 'this.dwt.credpk must be defined')
    const originB64Url = base64ToBase64url(Buffer.from(window.location.origin, 'utf8').toString('base64'))
    const compressedB64 = Buffer.from(compressECDSACoseKey(this.dwt.credpk)).toString('base64')
    const credpkB64Url = base64ToBase64url(compressedB64)
    this.compiledTeal = await getWebauthnCompiledLSIG(credpkB64Url, originB64Url)

    if (isNullOrUndefined(this.compiledTeal)) {
      throw new Error('Compiled teal is undefined.')
    }

    this.sessionKey = csk
    this.isInitalized = true
  }

  public async sign(tx: algosdk.Transaction): Promise<string> {
    if (!this.isInitalized) {
      throw new Error('WebauthnLSIGWallet not initialized.')
    }
    AssertDefined(this.dwt, 'this.dwt must be defined')
    AssertDefined(this.dwt.credSig, 'this.dwt.credSig must be defined')
    AssertDefined(this.dwt.exp, 'this.dwt.exp must be defined')
    AssertDefined(this.sessionKey, 'this.sessionKey must be defined')
    AssertDefined(this.compiledTeal, 'this.compiledTeal must be defined')

    const { r, s } = GetRSFromASN1DEREncodedSignature(Buffer.from(this.dwt.credSig, 'base64'))
    const delgSig = await signWithEd25519(tx.rawTxID(), this.sessionKey.sk)
    const args = [this.sessionKey.pk, algosdk.encodeUint64(this.dwt.exp), ...this.getTxnArguments(r, s, delgSig)]
    const smartSig = new algosdk.LogicSigAccount(Buffer.from(this.compiledTeal, 'base64'), args)

    const signedSmartSigTxn = algosdk.signLogicSigTransactionObject(tx, smartSig)
    await algod.sendRawTransaction(signedSmartSigTxn.blob).do()
    await algosdk.waitForConfirmation(algod, signedSmartSigTxn.txID, 3)
    return signedSmartSigTxn.txID
  }

  public async testTransaction(): Promise<void> {
    if (!isTestEnvirnoment) {
      throw new Error('Not in test environment.')
    }
    await fundAccount(this.getAddress(), 304_000)
    const suggestedParams = await algod.getTransactionParams().do()
    const txn = algosdk.makePaymentTxnWithSuggestedParamsFromObject({
      from: this.getAddress(),
      to: this.getAddress(),
      amount: 0,
      note: new Uint8Array(Buffer.from('Test transaction for WebauthnLSIGWallet')),
      suggestedParams
    })

    await this.sign(txn)
    console.log('TEST Transaction successfully signed.')
  }

  public getAddress(): string {
    if (!this.isInitalized) {
      throw new Error('WebauthnLSIGWallet not initialized.')
    }
    AssertDefined(this.compiledTeal, 'this.compiledTeal must be defined')
    const smartSig = new algosdk.LogicSigAccount(Buffer.from(this.compiledTeal, 'base64'))
    return smartSig.address()
  }

  private getTxnArguments(r: Uint8Array, s: Uint8Array, delgSig: Uint8Array): Uint8Array[] {
    AssertDefined(this.dwt, 'this.dwt.user must be defined')
    AssertDefined(this.dwt.user, 'this.dwt.user must be defined')
    AssertDefined(this.dwt.rand, 'this.dwt.rand must be defined')
    AssertDefined(this.dwt.authenticatorData, 'this.dwt.authenticatorData must be defined')
    AssertDefined(this.dwt.clientDataJSON, 'this.dwt.clientDataJSON must be defined')
    AssertDefined(this.dwt.credpk, 'this.dwt.clientDataJSON must be defined')

    return [
      Buffer.from(this.dwt.user, 'utf8'),
      Buffer.from(this.dwt.rand, 'base64'),
      Buffer.from(this.dwt.authenticatorData, 'base64'),
      Buffer.from(this.dwt.clientDataJSON, 'base64'),
      r,
      s,
      delgSig,
      compressECDSACoseKey(this.dwt.credpk)
    ]
  }
}
