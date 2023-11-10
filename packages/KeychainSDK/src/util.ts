import { type DWT } from '@gradian/keychain-auth-server-client'
import * as ed from '@noble/ed25519'

export function isNullOrUndefined (value: any): boolean {
  return value === undefined || value === null
}

/**
 * Check if the user agent indicates a mobile device.
 * @returns {boolean} true if the user agent belongs to a mobile device; otherwise, false.
 */
export function isMobile (): boolean {
  return /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent)
}

/**
 * Check if DWT we receive from assertion options contains our csPK and username.
 * @returns {boolean} true if the DWT contains our csPK and username; otherwise, false.
 */
export function verifyAssertionOptionsDWT (dwt: DWT, clientSessionPKB64: string, username: string): boolean {
  return dwt.user === username && dwt.cspk === clientSessionPKB64
}

/**
 * Determine if the current window or iframe is sandboxed.
 * It attempts to run scripts and access parent window properties.
 * If any of these attempts fail due to security restrictions, it's likely that the context is sandboxed.
 *
 * @returns {boolean} - True if the environment is sandboxed, otherwise false.
 */
export function getIsSandboxed (): boolean {
  try {
    // Attempt to create and execute a script
    const script = document.createElement('script')
    script.innerHTML = 'var test = 1;'
    document.body.appendChild(script)
    document.body.removeChild(script)
  } catch (e) {
    // If an error is thrown, then the iframe might be sandboxed
    return true
  }
  try {
    // Attempt to access a property of the parent window
    const topWindowUrl: string | undefined = window?.top?.location?.href
    if (isNullOrUndefined(topWindowUrl)) {
      return false
    }
  } catch (e) {
    // If an error is thrown, then the iframe might be sandboxed
    return true
  }

  // If none of the above checks failed, the page is likely not sandboxed
  return false
}

/**
 * Sign given base64 encoded message with given base64 encoded private key (Ed25519).
 * @param {string} messageB64 - The message to sign.
 * @param {string} privateKeyB64 - The private key to sign with.
 * @returns {string} Base64 Encoded signature.
 */
export async function ed25519Sign (messageB64: string, privateKeyB64: string): Promise<string> {
  const message = Buffer.from(messageB64, 'base64')
  const privateKey = Buffer.from(privateKeyB64, 'base64')
  const signature = await ed.signAsync(message, privateKey)
  return Buffer.from(signature).toString('base64')
}

/**
 * Verifies the signature of a message using the ed25519 algorithm.
 * @param {string} messageB64 The message to verify, encoded in base64.
 * @param {string} signatureB64 The signature to verify, encoded in base64.
 * @param {string} publicKeyB64 The public key to use for verification, encoded in base64.
 * @returns {boolean} A Promise that resolves to a boolean indicating whether the signature is valid.
 */
export async function ed25519Verify (messageB64: string, signatureB64: string, publicKeyB64: string): Promise<boolean> {
  const message = Buffer.from(messageB64, 'base64')
  const signature = Buffer.from(signatureB64, 'base64')
  const publicKey = Buffer.from(publicKeyB64, 'base64')
  return await ed.verifyAsync(signature, message, publicKey)
}
