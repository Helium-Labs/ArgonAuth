import { describe, it } from '@jest/globals'
import algosdk from 'algosdk'
import getZKWebauthnLSIGInstance from '../src/contracts/zkWebauthn/client'
import { generateSecp256r1Key, generateX25519KeyPair, signMessage, signWithEd25519 } from './crypto'
import { sha256 } from '@noble/hashes/sha256'
import { getAlgokitTestkit, fundAccount } from 'algokit-testkit'

const generateTestAuthenticatorData = (): Uint8Array => {
  const rpIdHash = Buffer.alloc(32, 0x00)
  const flags = Buffer.alloc(1, 0x05)
  flags[0] = flags[0] | 0x05
  const signCount = Buffer.alloc(4, 0x00)
  const authenticatorData = Buffer.concat([rpIdHash, flags, signCount])
  const authDataAsUint8Array = new Uint8Array(authenticatorData)
  return authDataAsUint8Array
}

const generateTestClientDataJSON = (challenge: Uint8Array): Uint8Array => {
  const clientDataJSON = Buffer.from(JSON.stringify({
    type: 'webauthn.get',
    challenge: Buffer.from(challenge).toString('base64url'),
    origin: 'http://localhost:3000'
  }), 'utf8')
  const clientDataJSONAsUint8Array = new Uint8Array(clientDataJSON)
  return clientDataJSONAsUint8Array
}
const getWebauthnLSIGAccount = async (
  compiledTeal: string,
  txID: Buffer,
  credpk: Uint8Array,
  credsk: Uint8Array
): Promise<algosdk.LogicSigAccount> => {
  const compiledTealBuffer = Buffer.from(compiledTeal, 'base64')

  // const csk = algosdk.generateAccount()
  const csk = generateX25519KeyPair()
  const cspk: Uint8Array = csk.pk
  const exp: Uint8Array = algosdk.encodeUint64(1e9)
  const user: Uint8Array = Buffer.from('testUser', 'utf8')
  const rand: Uint8Array = Buffer.from('testRand', 'utf8')
  const concatArgs: Uint8Array = Buffer.concat([cspk, exp, user, rand])
  const H: Uint8Array = sha256(concatArgs)
  const delgSig: Uint8Array = signWithEd25519(txID, csk.sk)

  const authData: Uint8Array = generateTestAuthenticatorData()
  const clientDataJSON: Uint8Array = generateTestClientDataJSON(H)

  const signedPayload = sha256(Buffer.concat([authData, sha256(clientDataJSON)]))

  // HERE WE USE THE RECOVERY FACTOR TO GAIN ACCESS
  const credSig: any = signMessage(signedPayload, credsk)
  const credSigR: Uint8Array = credSig.r
  const credSigS: Uint8Array = credSig.s

  const args: Uint8Array[] = [
    cspk,
    exp,
    user,
    rand,
    authData,
    clientDataJSON,
    credSigR,
    credSigS,
    delgSig,
    credpk
  ]

  const smartSig: algosdk.LogicSigAccount = new algosdk.LogicSigAccount(compiledTealBuffer, args)
  return smartSig
}

describe('Webauthn LSIG', () => {
  it('can sign', async () => {
    const { algod } = await getAlgokitTestkit()
    // Use test values
    // generate secp256r1/ES256 public key, in XY encoding
    const { pk: publicKey, sk: privateKey } = generateSecp256r1Key()
    const origin = Buffer.from('http://localhost:3000', 'utf8').toString('base64')
    const compiledTealSrcB64 = await getZKWebauthnLSIGInstance(publicKey.toString('base64'), origin)
    const compiledTealSrc = Buffer.from(compiledTealSrcB64, 'base64').toString('utf8')
    const compiledTealUint8 = await algod.compile(compiledTealSrc).do()
    const compiledTeal = compiledTealUint8.result
    const otherAcc = algosdk.generateAccount()
    const getWebauthnLSIGAddress = async (compiledTeal: string): Promise<string> => {
      const b64CompiledTeal = Buffer.from(compiledTeal, 'base64')
      const smartSig: algosdk.LogicSigAccount = new algosdk.LogicSigAccount(b64CompiledTeal)
      return smartSig.address()
    }
    const smartSigAddress = await getWebauthnLSIGAddress(compiledTeal)
    const suggestedParams = await algod.getTransactionParams().do()
    const smartSigTxn = algosdk.makePaymentTxnWithSuggestedParamsFromObject({
      from: smartSigAddress,
      to: otherAcc.addr,
      amount: 0,
      suggestedParams
    })
    const smartSig = await getWebauthnLSIGAccount(compiledTeal, smartSigTxn.rawTxID(), publicKey, privateKey)

    await fundAccount(smartSig.address(), 304_000)

    const signedSmartSigTxn = algosdk.signLogicSigTransactionObject(
      smartSigTxn,
      smartSig
    )
    await algod.sendRawTransaction(signedSmartSigTxn.blob).do()
    await algosdk.waitForConfirmation(algod, signedSmartSigTxn.txID, 3)
  })

  it('can sign with a recovery factor', async () => {
    const { algod } = await getAlgokitTestkit()
    // Use test values
    // generate secp256r1/ES256 public key, in XY encoding
    const { pk: publicKey } = generateSecp256r1Key()
    const { pk: recoveryFactorPublicKey, sk: recoveryFactorPrivateKey } = generateSecp256r1Key()
    const origin = Buffer.from('http://localhost:3000', 'utf8').toString('base64')

    const publicKeys = Buffer.concat([publicKey, recoveryFactorPublicKey])
    const compiledTealSrcB64 = await getZKWebauthnLSIGInstance(publicKeys.toString('base64'), origin)
    const compiledTealSrc = Buffer.from(compiledTealSrcB64, 'base64').toString('utf8')
    const compiledTealUint8 = await algod.compile(compiledTealSrc).do()
    const compiledTeal = compiledTealUint8.result
    const getWebauthnLSIGAddress = async (compiledTeal: string): Promise<string> => {
      const b64CompiledTeal = Buffer.from(compiledTeal, 'base64')
      const smartSig: algosdk.LogicSigAccount = new algosdk.LogicSigAccount(b64CompiledTeal)
      return smartSig.address()
    }

    const smartSigAddress = await getWebauthnLSIGAddress(compiledTeal)
    const suggestedParams = await algod.getTransactionParams().do()
    const smartSigTxn = algosdk.makePaymentTxnWithSuggestedParamsFromObject({
      from: smartSigAddress,
      to: smartSigAddress,
      amount: 0,
      suggestedParams
    })
    const smartSig = await getWebauthnLSIGAccount(compiledTeal, smartSigTxn.rawTxID(), recoveryFactorPublicKey, recoveryFactorPrivateKey)

    await fundAccount(smartSig.address(), 304_000)

    const signedSmartSigTxn = algosdk.signLogicSigTransactionObject(
      smartSigTxn,
      smartSig
    )
    await algod.sendRawTransaction(signedSmartSigTxn.blob).do()
    await algosdk.waitForConfirmation(algod, signedSmartSigTxn.txID, 3)
  })

  it('can rekey to add another recovery factor', async () => {
    const { algod } = await getAlgokitTestkit()
    // Use test values
    // generate secp256r1/ES256 public key, in XY encoding
    const { pk: publicKey, sk: privateKey } = generateSecp256r1Key()
    const { pk: recoveryFactorPublicKey, sk: recoveryFactorPrivateKey } = generateSecp256r1Key()
    const origin = Buffer.from('http://localhost:3000', 'utf8').toString('base64')

    const publicKeys = Buffer.concat([publicKey, recoveryFactorPublicKey])

    const getLSIG = async (publicKeys: Buffer): Promise<string> => {
      const compiledTealSrcB64 = await getZKWebauthnLSIGInstance(publicKeys.toString('base64'), origin)
      const compiledTealSrc = Buffer.from(compiledTealSrcB64, 'base64').toString('utf8')
      const compiledTealUint8 = await algod.compile(compiledTealSrc).do()
      const compiledTeal = compiledTealUint8.result
      return compiledTeal
    }

    const compiledTeal = await getLSIG(publicKey)
    const compiledTealWithRecoveryFactors = await getLSIG(publicKeys)

    const getWebauthnLSIGAddress = async (compiledTeal: string): Promise<string> => {
      const b64CompiledTeal = Buffer.from(compiledTeal, 'base64')
      const smartSig: algosdk.LogicSigAccount = new algosdk.LogicSigAccount(b64CompiledTeal)
      return smartSig.address()
    }
    // REKEY IT
    const smartSigAddress = await getWebauthnLSIGAddress(compiledTeal)
    const newSmartSigAddress = await getWebauthnLSIGAddress(compiledTealWithRecoveryFactors)
    const suggestedParams = await algod.getTransactionParams().do()
    const smartSigTxn = algosdk.makePaymentTxnWithSuggestedParamsFromObject({
      from: smartSigAddress,
      to: smartSigAddress,
      amount: 0,
      suggestedParams,
      rekeyTo: newSmartSigAddress
    })
    const smartSig = await getWebauthnLSIGAccount(compiledTeal, smartSigTxn.rawTxID(), publicKey, privateKey)

    await fundAccount(smartSig.address(), 304_000)

    const signedSmartSigTxn = algosdk.signLogicSigTransactionObject(
      smartSigTxn,
      smartSig
    )
    await algod.sendRawTransaction(signedSmartSigTxn.blob).do()
    await algosdk.waitForConfirmation(algod, signedSmartSigTxn.txID, 3)

    // TEST THE REKEYED ADDR
    const smartSigTxn2 = algosdk.makePaymentTxnWithSuggestedParamsFromObject({
      from: newSmartSigAddress,
      to: newSmartSigAddress,
      amount: 0,
      suggestedParams
    })
    const smartSigWithRecoveryFactors = await getWebauthnLSIGAccount(compiledTealWithRecoveryFactors, smartSigTxn2.rawTxID(), recoveryFactorPublicKey, recoveryFactorPrivateKey)

    await fundAccount(smartSigWithRecoveryFactors.address(), 304_000)

    const signedSmartSigTxn2 = algosdk.signLogicSigTransactionObject(
      smartSigTxn2,
      smartSigWithRecoveryFactors
    )
    await algod.sendRawTransaction(signedSmartSigTxn2.blob).do()
    await algosdk.waitForConfirmation(algod, signedSmartSigTxn2.txID, 3)
  })
})
