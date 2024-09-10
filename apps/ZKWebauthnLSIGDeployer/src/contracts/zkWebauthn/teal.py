from pyteal import *
import sys
import os
from algosdk.v2client import algod

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
    tmplCredpk = Tmpl.Bytes("TMPL_C")
    tmplOrigin = Tmpl.Bytes("TMPL_O")
    credpkLengthBounded = Len(tmplCredpk) <= Int(128)
    originLengthBounded = Len(tmplOrigin) <= Int(128)
    boundedCond = And(credpkLengthBounded, originLengthBounded)

    # Usual safety checks
    safetyCond = And(
        Txn.close_remainder_to() == Global.zero_address(),
        Txn.rekey_to() == Global.zero_address(),
    )

    # Extract Args
    cspk = Arg(0)
    exp = Arg(1)
    user = Arg(2)
    rand = Arg(3)
    authenticatorData = Arg(4)
    clientDataJSON = Arg(5)
    credSigR = Arg(6)
    credSigS = Arg(7)
    delgSig = Arg(8)

    challengeB64Url = JsonRef.as_string(clientDataJSON, Bytes("challenge"))
    challenge = Base64Decode.url(challengeB64Url)
    origin = JsonRef.as_string(clientDataJSON, Bytes("origin"))
    type = JsonRef.as_string(clientDataJSON, Bytes("type"))

    # Check it's a webauthn.get (Assertion)
    type = BytesEq(type, Bytes("webauthn.get"))

    # Check it's coming from an approved origin
    originCond = BytesEq(origin, tmplOrigin)

    # Check challenge matches the hash of the parameters in the dwt
    params = Concat(
        cspk,
        exp,
        user,
        rand
    )
    challengeMatchesParamsHashCond = BytesEq(challenge, Sha256(params))

    # Verify the authenticatorData
    rpIdHash = Substring(authenticatorData, Int(0), Int(32))
    flags = Substring(authenticatorData, Int(32), Int(33))
    signCount = Substring(authenticatorData, Int(33), Int(37))
    attestedCredentialDataAndExts = Substring(authenticatorData, Int(37), Len(authenticatorData))

    # Assert user present
    userPresent = GetBit(flags, Int(7))
    userVerified = GetBit(flags, Int(5))
    userLivenessCond = And(userPresent, userVerified)

    # Credential signs (authenticatorData + sha256(clientDataJSON))
    credpk = EcdsaDecompress(
        EcdsaCurve.Secp256r1,
        tmplCredpk
    )
    authClientPayload = Concat(authenticatorData, Sha256(clientDataJSON))
    challengeAuthenticationCond = EcdsaVerify(
        EcdsaCurve.Secp256r1,
        Sha256(authClientPayload),
        credSigR,
        credSigS,
        credpk
    )
    challengeCond = And(
        challengeMatchesParamsHashCond,
        challengeAuthenticationCond,
        originCond,
        userLivenessCond
    )
    # Assert delegated client session key signed transaction hash.
    cspkApprovedTxCond = Ed25519Verify_Bare(
        Txn.tx_id(),
        delgSig,
        cspk
    )

    # Assert not expired
    expRound = Btoi(exp)
    unexpiredCondFv = expRound > Txn.first_valid()
    unexpiredCondLV = expRound > Txn.last_valid()
    unexpiredCond = And(unexpiredCondFv, unexpiredCondLV)

    return And(
        safetyCond,
        boundedCond,
        challengeCond,
        unexpiredCond,
        cspkApprovedTxCond
    )

def algod_client():
	algod_address= "http://localhost:4001"
	algod_token= "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
	return algod.AlgodClient(algod_token, algod_address)

if __name__ == "__main__":
    teal = compileTeal(zkpassless(), mode=Mode.Signature, version=9)
    # Get name of this file, without the extension
    filename = os.path.splitext(os.path.basename(__file__))[0]
    with open(os.path.join(sys.path[0], f"{filename}.ts"), "w+") as f:
        sourceCode = f"// version {lsig_version}\nexport default `{teal}`\n"
        f.write(sourceCode)
