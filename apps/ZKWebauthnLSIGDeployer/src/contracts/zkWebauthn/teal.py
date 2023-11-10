from pyteal import *
import sys
import os

lsig_version = 1
"""
ARGs:
- 0: cspk (ED25519 public key)
- 1: exp (as round)
- 2: user
- 3: rand
- 4: H(cspk, exp, user, rand)
- 5: credSigR(ARG0, credpk)
- 6: credSigS(ARG0, credpk)
- 7: delgSig(ARG0, cspk)
Assumes client integrity. Credentials must be accessed from the same domain.
"""

def zkpassless():
    # identification
    # credpk in (secp256r1/ES256 public key, in XY encoding)
    tmpl_credpk = Tmpl.Bytes("TMPL_CREDPK")
    credpk_len = Len(tmpl_credpk)
    credpk_cond = credpk_len <= Int(64)
    bounded_cond = credpk_cond

    # Usual safety checks
    safety_cond = And(
        Txn.close_remainder_to() == Global.zero_address(),
        Txn.rekey_to() == Global.zero_address(),
    )

    # Extract Args
    cspk = Arg(0)
    exp = Arg(1)
    user = Arg(2)
    rand = Arg(3)
    hash = Arg(4)
    credSigR = Arg(5)
    credSigS = Arg(6)
    delgSig = Arg(7)

    # Verify data hasn't been tampered with
    dat = Concat(
        cspk,
        exp,
        user,
        rand
    )
    hash_dat = Sha256(dat)
    data_integrity_cond = BytesEq(hash_dat, hash)

    # Verify hash is signed by the bound credential public key
    credpk = EcdsaDecompress(
        EcdsaCurve.Secp256r1,
        tmpl_credpk
    )
    data_authentication_cond = EcdsaVerify(
        EcdsaCurve.Secp256r1,
        hash,
        credSigR,
        credSigS,
        credpk
    )

    # Assert delegated client session key signed transaction hash.
    cspk_approved_tx_cond = Ed25519Verify(
        Txn.tx_id(),
        delgSig,
        cspk
    )

    # Assert not expired
    exp_round = Btoi(exp)
    unexpired_cond_fv = exp_round > Txn.first_valid() 
    unexpired_cond_lv = exp_round > Txn.last_valid()
    unexpired_cond = And(unexpired_cond_fv, unexpired_cond_lv)
    return And(
        safety_cond,
        bounded_cond,
        data_integrity_cond,
        data_authentication_cond,
        unexpired_cond,
        cspk_approved_tx_cond
    )

if __name__ == "__main__":
    teal = compileTeal(zkpassless(), mode=Mode.Signature, version=8)
    with open(os.path.join(sys.path[0], "teal.ts"), "w+") as f:
        sourceCode = f"// version {lsig_version}\nexport default `{teal}`"
        f.write(sourceCode)
