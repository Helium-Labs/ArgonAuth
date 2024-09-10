import { type DWT } from './api/RelyingPartyClient'
import format from 'ecdsa-sig-formatter'

export function isNullOrUndefined(value: any): boolean {
  return value === undefined || value === null
}

export function Assert(value: boolean, message: string): asserts value is true {
  if (!value) {
    throw new Error(message)
  }
}
export function AssertDefined<T>(value: T | undefined | null, message: string): asserts value is T {
  if (value === undefined || value === null) {
    throw new Error(message)
  }
}

/**
 * Check if the user agent indicates a mobile device.
 * @returns {boolean} true if the user agent belongs to a mobile device; otherwise, false.
 */
export function isMobile(): boolean {
  return /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent)
}

/**
 * Check if DWT we receive from assertion options contains our csPK and username.
 * @returns {boolean} true if the DWT contains our csPK and username; otherwise, false.
 */
export function verifyAssertionOptionsDWT(dwt: DWT, clientSessionPKB64: string, username: string): boolean {
  return dwt.user === username && dwt.cspk === clientSessionPKB64
}

export function base64urlToBase64(base64url: string): string {
  let base64 = base64url
    .replace(/-/g, '+')
    .replace(/_/g, '/')

  while (base64.length % 4 !== 0) {
    base64 += '='
  }

  return base64
}

export function base64ToBase64url(base64: string): string {
  return base64
    .replace(/\+/g, '-') // Replace '+' with '-'
    .replace(/\//g, '_') // Replace '/' with '_'
    .replace(/=+$/, '') // Remove any trailing '=' padding characters
}

export function bufferToBase64url(buffer: Uint8Array): string {
  return base64ToBase64url(Buffer.from(buffer).toString('base64'))
}
export function arrayBufferToBase64Url(buffer: ArrayBuffer): string {
  return base64ToBase64url(Buffer.from(buffer).toString('base64'))
}

// Helper function to concatenate Uint8Arrays
export function concatUint8Arrays(...arrays: Uint8Array[]): Uint8Array {
  const totalLength = arrays.reduce((acc, value) => acc + value.length, 0)
  const result = new Uint8Array(totalLength)
  let offset = 0

  arrays.forEach((array) => {
    result.set(array, offset)
    offset += array.length
  })

  return result
}
export function GetRSFromASN1DEREncodedSignature(derSignature: Uint8Array): { r: Uint8Array, s: Uint8Array } {
  const derSignatureB64: string = Buffer.from(derSignature).toString('base64')
  // Convert the DER signature to JOSE format
  const joseSignature = format.derToJose(derSignatureB64, 'ES256')
  // Decode the JOSE-style signature (Base64Url) to get R and S components
  const decodedJoseSignature = Buffer.from(joseSignature, 'base64')
  const r = Buffer.alloc(32) // Buffer for R component
  const s = Buffer.alloc(32) // Buffer for S component

  decodedJoseSignature.copy(r, 0, 0, 32) // Copy first 32 bytes for R
  decodedJoseSignature.copy(s, 0, 32, 64) // Copy next 32 bytes for S+
  return { r, s }
}

interface DecodedJWT { header: any, claims: any, signature: string }
export function getJWTParts(jwt: string): DecodedJWT {
  const [header, claims, signature] = jwt.split('.')
  if (isNullOrUndefined(header) || isNullOrUndefined(claims) || isNullOrUndefined(signature)) {
    throw new Error('JWT is not in correct format.')
  }
  AssertDefined(header, 'header must be defined')
  AssertDefined(signature, 'header must be defined')
  AssertDefined(claims, 'claims must be defined')
  let decoded: DecodedJWT
  try {
    // decode header and claims into JSON
    const decodedHeader = Buffer.from(base64urlToBase64(header), 'base64').toString('utf8')
    const decodedClaims = Buffer.from(base64urlToBase64(claims), 'base64').toString('utf8')
    decoded = {
      header: JSON.parse(decodedHeader),
      claims: JSON.parse(decodedClaims),
      signature
    }
  } catch (e) {
    throw new Error('JWT is not in correct format.')
  }
  return decoded
}

export function generateRandomBuffer(stateLength: number): Uint8Array {
  // Generate a random value using a cryptographic source
  const array = new Uint8Array(stateLength)
  window.crypto.getRandomValues(array)
  return array
}
export async function sha256(buffer: ArrayBuffer): Promise<ArrayBuffer> {
  return await crypto.subtle.digest('SHA-256', buffer)
}
// Utility function to extract IPFS CIDs from TXT records
const extractIPFSCIDs = (answers: any[]): any[] => {
  return answers
    .filter(answer => answer.type === 16) // TXT record type
    .map(answer => answer.data.match(/dnslink=\/ipfs\/(.+)/))
    .map(match => match[1])
}

export async function getAllIPFSCIDViaDNSLink(origin: string, ipfs: string): Promise<boolean> {
  const dnsLinkDomain = `_dnslink.${origin}`
  const dohEndpoint = `https://cloudflare-dns.com/dns-query?name=${dnsLinkDomain}&type=TXT`

  try {
    const response = await fetch(dohEndpoint, {
      headers: { accept: 'application/dns-json' }
    })
    const data = await response.json()

    if (data.Status !== 0 || isNullOrUndefined(data.Answer)) {
      console.error(`Failed to retrieve TXT records. Status: ${data.Status}`)
      return false
    }

    const hashes = extractIPFSCIDs(data.Answer)
    const ipfsMatches = hashes.length <= 1 && hashes.includes(ipfs)

    return ipfsMatches
  } catch (error) {
    console.error('Error fetching DNSLink TXT record:', error)
    return false
  }
}

export async function checkDNSSECEnabled(origin: string): Promise<boolean> {
  const dnsLinkDomain = `_dnslink.${origin}`
  const dohEndpoint = `https://cloudflare-dns.com/dns-query?name=${dnsLinkDomain}&type=TXT`

  try {
    const response = await fetch(dohEndpoint, {
      headers: { accept: 'application/dns-json' }
    })
    const json = await response.json()

    // In DoH responses, the 'AD' (Authenticated Data) flag indicates if DNSSEC is enabled
    return json.AD === true
  } catch (error) {
    console.error('Error while checking DNSSEC:', error)
    return false
  }
}

export async function originHasSoundIntegrity(origin: string, expectedIPFS: string, disableDNSSECCheck: boolean = true): Promise<boolean> {
  const hashesPromise = getAllIPFSCIDViaDNSLink(origin, expectedIPFS)
  const dnssecEnabledPromise = checkDNSSECEnabled(origin)
  const promises = [hashesPromise, dnssecEnabledPromise]
  const [hashes, dnssecEnabled] = await Promise.all(promises)
  return !!hashes && (dnssecEnabled || disableDNSSECCheck)
}
