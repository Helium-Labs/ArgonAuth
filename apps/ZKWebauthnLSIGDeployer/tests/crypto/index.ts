import * as ed from '@noble/ed25519'
import { sha512 } from '@noble/hashes/sha512'

import { ec as EC } from 'elliptic'
ed.etc.sha512Sync = (...m) => sha512(ed.etc.concatBytes(...m))
// Set up the elliptic curve for secp256r1
const ec = new EC('p256') // 'p256' is an alias for prime256v1

// Generate an EC key pair for secp256r1
export const generateSecp256r1Key = (): { pk: Buffer, sk: Buffer } => {
  const keyPair = ec.genKeyPair()

  // Export the private key in PEM format
  const privateKey = keyPair.getPrivate('hex')
  const privateKeyBuffer = Buffer.from(privateKey, 'hex')

  // Export the public key in compressed format and encode in base64
  const publicKey = keyPair.getPublic().encodeCompressed('hex')
  const publicKeyBuffer = Buffer.from(publicKey, 'hex') // Use Buffer.from() instead of new Buffer()

  return {
    pk: publicKeyBuffer,
    sk: privateKeyBuffer
  }
}
// Sign a message with the private key, returning the signature as base64 encoded R & S values in a JSON object
export const signMessage = (message: Uint8Array, privateKey: Uint8Array): { r: Buffer, s: Buffer } => {
  const keyPair = ec.keyFromPrivate(privateKey)
  const signatureObj = keyPair.sign(message)
  return {
    r: signatureObj.r.toBuffer(),
    s: signatureObj.s.toBuffer()
  }
}
// Key pair generation using Ed25519
export const generateX25519KeyPair = (): { sk: Uint8Array, pk: Uint8Array } => {
  const sk = ed.utils.randomPrivateKey()
  // The public key needs to be calculated asynchronously due to hashing
  const pk = ed.getPublicKey(sk)
  return {
    sk,
    pk
  }
}

// Sign message with Ed25519
export const signWithEd25519 = (message: Uint8Array, privateKey: Uint8Array): Uint8Array => {
  const signature = ed.sign(message, privateKey)
  return signature
}

// Verify with Ed25519
export const verifyWithEd25519 = (message: string, signature: string, publicKey: Uint8Array): boolean => {
  return ed.verify(signature, message, publicKey)
}
