import * as ed from '@noble/ed25519'

// @ts-ignore
import { sha512 } from '@noble/hashes/sha512'
import cbor from 'cbor'

ed.etc.sha512Sync = (...m) => sha512(ed.etc.concatBytes(...m))



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

function decodeCoseKey (coseKeyBase64: string): { x: Uint8Array, y: Uint8Array } {
  // Convert Base64 to ArrayBuffer
  const keyBuffer = base64ToArrayBuffer(coseKeyBase64)

  // Decode the CBOR data
  const decoded = cbor.decode(new Uint8Array(keyBuffer))

  // Extract the x and y coordinates from the CBOR object
  // Note: The indices 3 and -2 correspond to the x and y coordinates in the COSE Key format
  const x = decoded.get(-2) // x-coordinate
  const y = decoded.get(-3) // y-coordinate

  return { x, y }
}
function compressEcdsaKey (x: Uint8Array, y: Uint8Array): Uint8Array {
  // Determine the sign (prefix)
  // @ts-ignore
  const sign = (y[y.length - 1] & 1) === 1 ? 0x03 : 0x02

  // Combine the sign and the x coordinate
  return new Uint8Array([sign, ...x])
}

export function compressECDSACoseKey (keyB64: string): Uint8Array {
  const { x, y } = decodeCoseKey(keyB64)
  const compressedKey = compressEcdsaKey(x, y)
  return compressedKey
}

function base64ToArrayBuffer (base64: string): ArrayBuffer {
  const binaryString = window.atob(base64)
  const length = binaryString.length
  const bytes = new Uint8Array(length)
  for (let i = 0; i < length; i++) {
    bytes[i] = binaryString.charCodeAt(i)
  }
  return bytes.buffer
}
