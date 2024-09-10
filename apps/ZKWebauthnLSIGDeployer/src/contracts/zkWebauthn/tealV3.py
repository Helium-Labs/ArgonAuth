from pyteal import *
import sys
import os

lsig_version = 1

@Subroutine(TealType.uint64)
def listContainsValue(list: Expr, value: Expr, size: Expr):
    i = ScratchVar(TealType.uint64)
    return Seq([
        For(i.store(Int(0)), i.load() < size, i.store(i.load() + Int(1))).Do(
            If(BytesEq(Extract(list, Mul(i.load(), size), size), value)).Then(
                Return(Int(1))
            )
        ),
        Return(Int(0))
    ])

def zkwebauthn():
    # Conventional ED25519 recovery factor
    # identification
    tmplCredpks = Tmpl.Bytes("TMPL_C")
    tmplOrigin = Tmpl.Bytes("TMPL_O")
    credpkLengthBounded = Len(tmplCredpks) <= Int(128)
    originLengthBounded = Len(tmplOrigin) <= Int(128)
    boundedCond = And(credpkLengthBounded, originLengthBounded)

    # Usual safety checks
    safetyCond = And(
        Txn.close_remainder_to() == Global.zero_address()
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
    authCredpk = Arg(9)

    # Check that the sender possesses a credential that is bound to this lsig
    size = Int(33)
    senderPossesBoundedCred = listContainsValue(tmplCredpks, authCredpk, size)

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
    attestedCredentialDataAndExts = Substring(
        authenticatorData, Int(37), Len(authenticatorData))

    # Assert user present
    userPresent = GetBit(flags, Int(7))
    userVerified = GetBit(flags, Int(5))
    userLivenessCond = And(userPresent, userVerified)

    # Credential signs (authenticatorData + sha256(clientDataJSON))
    credpk = EcdsaDecompress(
        EcdsaCurve.Secp256r1,
        authCredpk
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
        senderPossesBoundedCred,
        challengeCond,
        unexpiredCond,
        cspkApprovedTxCond
    )

if __name__ == "__main__":
    teal = compileTeal(zkwebauthn(), mode=Mode.Signature, version=9)
    # Get name of this file, without the extension
    filename = os.path.splitext(os.path.basename(__file__))[0]
    with open(os.path.join(sys.path[0], f"{filename}.ts"), "w+") as f:
        sourceCode = f"// version {lsig_version}\nexport default `{teal}`\n"
        f.write(sourceCode)
