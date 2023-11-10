export type Subscription = Record<string, Array<(data: any) => void>>

export interface EncryptedPayload {
  encryptedData: ArrayBuffer
  iv: Uint8Array
  senderPublicKey: Uint8Array
}
export interface UnencryptedPayload {
  unencryptedData: any
  senderPublicKey: Uint8Array
}

export interface ECDHPair {
  myPub: Uint8Array
  sharedSecret: Uint8Array
}

export interface EncryptedMessage {
  cipherText: ArrayBuffer
  iv: Uint8Array
}

export interface X25519Pair {
  pub: Uint8Array
  priv: Uint8Array
}
