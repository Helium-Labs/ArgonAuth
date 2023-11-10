import { algorandFixture } from '@algorandfoundation/algokit-utils/testing'
import { describe, beforeEach, it } from '@jest/globals'
import algosdk, { tealSignFromProgram } from 'algosdk'
import * as algokit from '@algorandfoundation/algokit-utils'
import getZKWebauthnLSIGInstance from './client'
import { generateSecp256r1Key, signMessage } from '../../crypto'
import { sha256 } from '@noble/hashes/sha256'

describe('Webauthn LSIG', () => {
  const fixture = algorandFixture()
  beforeEach(fixture.beforeEach, 10_000)
  it('can sign', async () => {
    const { algod, kmd } = fixture.context
    // Use test values
    // generate secp256r1/ES256 public key, in XY encoding
    const { pk: publicKey, sk: privateKey } = generateSecp256r1Key()
    const compiledTeal = await getZKWebauthnLSIGInstance(publicKey.toString('base64'))
    const sender = algosdk.generateAccount()
    await algokit.ensureFunded(
      {
        accountToFund: sender.addr,
        minSpendingBalance: algokit.microAlgos(304_000),
        suppressLog: true
      },
      algod,
      kmd
    )
    const getWebauthnLSIGAddress = async (compiledTeal: string): Promise<string> => {
      const b64CompiledTeal = Buffer.from(compiledTeal, 'base64')
      const smartSig: algosdk.LogicSigAccount = new algosdk.LogicSigAccount(b64CompiledTeal)
      return smartSig.address()
    }
    const getWebauthnLSIGAccount = async (
      compiledTeal: string,
      txID: Buffer
    ): Promise<algosdk.LogicSigAccount> => {
      const compiledTealBuffer = Buffer.from(compiledTeal, 'base64')

      const csk = algosdk.generateAccount()
      const cspk: Uint8Array = algosdk.decodeAddress(csk.addr).publicKey
      const exp: Uint8Array = algosdk.encodeUint64(1e9)
      const user: Uint8Array = Buffer.from('testUser', 'utf8')
      const rand: Uint8Array = Buffer.from('testRand', 'utf8')
      const concatArgs: Uint8Array = Buffer.concat([cspk, exp, user, rand])
      const H: Uint8Array = sha256(concatArgs)
      const credSig: any = signMessage(H, privateKey)
      const credSigR: Uint8Array = credSig.r
      const credSigS: Uint8Array = credSig.s

      // Verify the ed25519 signature of the concatenation ("ProgData" + hash_of_current_program + data).
      const delgSig: Uint8Array = tealSignFromProgram(csk.sk, txID, compiledTealBuffer)
      const args: Uint8Array[] = [
        cspk,
        exp,
        user,
        rand,
        H,
        credSigR,
        credSigS,
        delgSig
      ]

      const smartSig: algosdk.LogicSigAccount = new algosdk.LogicSigAccount(compiledTealBuffer, args)
      return smartSig
    }

    const smartSigAddress = await getWebauthnLSIGAddress(compiledTeal)
    const suggestedParams = await algod.getTransactionParams().do()
    const smartSigTxn = algosdk.makePaymentTxnWithSuggestedParamsFromObject({
      from: smartSigAddress,
      to: smartSigAddress,
      amount: 0,
      suggestedParams
    })
    const smartSig = await getWebauthnLSIGAccount(compiledTeal, smartSigTxn.rawTxID())
    await algokit.ensureFunded({
      accountToFund: smartSig.address(),
      minSpendingBalance: algokit.microAlgos(304_000),
      suppressLog: true
    }, algod, kmd)

    const signedSmartSigTxn = algosdk.signLogicSigTransactionObject(
      smartSigTxn,
      smartSig
    )
    await algod.sendRawTransaction(signedSmartSigTxn.blob).do()
    await algosdk.waitForConfirmation(algod, signedSmartSigTxn.txID, 3)
  })
})
