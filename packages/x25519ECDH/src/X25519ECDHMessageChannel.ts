import { x25519 } from '@noble/curves/ed25519'
import {
  type ECDHPair,
  type EncryptedMessage,
  type EncryptedPayload,
  type UnencryptedPayload,
  type X25519Pair
} from './types'

/**
 * X25519ECDHMessageChannel is a class that provides a simple API for encrypting and decrypting messages between two parties with X25519 ECDH.
 */
export default class X25519ECDHMessageChannel {
  private readonly myKeyPair: X25519Pair
  private readonly theirPublicKey: Uint8Array
  /**
     * Generate a new X25519ECDHMessageChannel: a simple API for encrypting and decrypting messages between two parties with X25519 ECDH.
     * @param {Uint8Array} theirPublicKey - the public key of the other party
     * @param {X25519Pair} myKeyPair - optional X25519 key pair. If not provided, a key pair will be generated.
     * @returns {X25519ECDHMessageChannel} X25519ECDHMessageChannel
     */
  constructor (theirPublicKey: Uint8Array, myKeyPair: X25519Pair) {
    this.myKeyPair = myKeyPair
    this.theirPublicKey = theirPublicKey
  }

  /**
     * Generate a ECDH derived shared secret from a provided public key. See https://github.com/paulmillr/noble-curves.
     * @param {string} theirPub
     * @returns {ECDHPair} ECDHPair
     */
  private generateSharedSecret (): ECDHPair {
    const myPriv = this.myKeyPair.priv
    const myPub = this.myKeyPair.pub
    const theirPub = this.theirPublicKey
    const sharedSecret = x25519.getSharedSecret(myPriv, theirPub)
    return { myPub, sharedSecret }
  }

  /**
     * Import a raw key into the crypto.subtle API for use in encryption and decryption.
     * See https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/importKey.
     * @param {ArrayBuffer} rawKey - raw key to import
     * @returns {Promise<CryptoKey>} CryptoKey
     */
  private async importSecretKey (rawKey: ArrayBuffer): Promise<CryptoKey> {
    return await crypto.subtle.importKey('raw', rawKey, 'AES-GCM', true, ['encrypt', 'decrypt'])
  }

  /**
     * Encrypt a message with AES-GCM, using ECDH derived shared secret as the key.
     * See https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/encrypt.
     * Authentication tag is appended to the cipher text for ensuring confidentiality and integrity.
     * @param {string} plainText - plain text to encrypt
     * @param {Uint8Array} secret - secret to encrypt with
     * @returns {EncryptedMessage} EncryptedMessage
     */
  private async encryptMessage (plainText: string, secret: Uint8Array): Promise<EncryptedMessage> {
    const enc = new TextEncoder()
    const encoded = enc.encode(plainText)
    const secretKey = await this.importSecretKey(secret.buffer)
    // iv will be needed for decryption
    const iv = crypto.getRandomValues(new Uint8Array(12))
    const cipherText = await crypto.subtle.encrypt({ name: 'AES-GCM', iv }, secretKey, encoded)
    return { cipherText, iv }
  }

  /**
     * Encrypt an unencrypted payload with AES-GCM, using ECDH derived shared secret as the key.
     * @param {UnencryptedPayload} unencryptedPayload - unencrypted payload to encrypt
     * @returns {Promise<EncryptedPayload>} EncryptedPayload
     */
  async encrypt (unencryptedData: any): Promise<EncryptedPayload> {
    // Derive ECDHSharedSecret
    const echdPair: ECDHPair = this.generateSharedSecret()
    const jsonString: string = JSON.stringify(unencryptedData)
    const cipherTextIVJSON: EncryptedMessage = await this.encryptMessage(jsonString, echdPair.sharedSecret)
    const encryptedPayload: EncryptedPayload = {
      iv: cipherTextIVJSON.iv,
      encryptedData: cipherTextIVJSON.cipherText,
      senderPublicKey: this.getMyPub()
    }
    return encryptedPayload
  }

  /**
     * Decrypt cipherText with AES-GCM, with the secret and iv.
     * See https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/decrypt.
     * Authentication tag is appended to the cipher text, and used for ensuring confidentiality and integrity.
     * @param {ArrayBuffer} cipherText - encrypted text
     * @param {Uint8Array} iv - initialization vector
     * @param {Uint8Array} secret - secret to decrypt with
     * @returns {Promise<string>} - decrypted text
     */
  private async decryptMessage (cipherText: ArrayBuffer, iv: Uint8Array, secret: Uint8Array): Promise<string> {
    const secretKey = await this.importSecretKey(secret.buffer)
    const decrypted = await crypto.subtle.decrypt({ name: 'AES-GCM', iv }, secretKey, cipherText)
    const dec = new TextDecoder()
    return dec.decode(decrypted)
  }

  /**
     * Decrypt an encrypted payload with AES-GCM, using ECDH derived shared secret as the key.
     * @param {EncryptedPayload} encryptedPayload - encrypted payload to decrypt
     * @returns {Promise<UnencryptedPayload>} UnencryptedPayload
     */
  async decrypt (encryptedPayload: EncryptedPayload): Promise<UnencryptedPayload> {
    // Derive ECDHSharedSecret
    const echdPair: ECDHPair = this.generateSharedSecret()
    const cipherText: ArrayBuffer = encryptedPayload.encryptedData
    const iv: Uint8Array = encryptedPayload.iv
    const decrypted: string = await this.decryptMessage(cipherText, iv, echdPair.sharedSecret)
    const decryptedJSON: any = JSON.parse(decrypted)
    return { unencryptedData: decryptedJSON, senderPublicKey: encryptedPayload.senderPublicKey }
  }

  /**
     * Get my public key.
     * @returns {Uint8Array} - my public key
     */
  getMyPub (): Uint8Array {
    return this.myKeyPair.pub
  }
}
