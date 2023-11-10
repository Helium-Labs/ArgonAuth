// version 1
export default `#pragma version 8
txn CloseRemainderTo
global ZeroAddress
==
txn RekeyTo
global ZeroAddress
==
&&
byte TMPL_CREDPK
len
int 64
<=
&&
arg 0
arg 1
concat
arg 2
concat
arg 3
concat
sha256
arg 4
b==
&&
byte TMPL_CREDPK
ecdsa_pk_decompress Secp256r1
store 1
store 0
arg 4
arg 5
arg 6
load 0
load 1
ecdsa_verify Secp256r1
&&
arg 1
btoi
txn FirstValid
>
arg 1
btoi
txn LastValid
>
&&
&&
txn TxID
arg 7
arg 0
ed25519verify
&&
return`
