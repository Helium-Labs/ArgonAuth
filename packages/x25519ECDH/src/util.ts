import { type X25519Pair } from './types'
import * as ed from '@noble/ed25519'
import { sha512 } from '@noble/hashes/sha512'
ed.etc.sha512Sync = (...m) => sha512(ed.etc.concatBytes(...m))

export function generateX25519KeyPair (): X25519Pair {
  const priv = ed.utils.randomPrivateKey()
  const pub = ed.getPublicKey(priv)
  return { pub, priv }
}
