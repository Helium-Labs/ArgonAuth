// version 1
export default `#pragma version 9
txn CloseRemainderTo
global ZeroAddress
==
byte TMPL_C
len
int 128
<=
byte TMPL_O
len
int 128
<=
&&
&&
byte TMPL_C
arg 9
int 33
callsub listContainsValue_0
&&
arg 5
byte "challenge"
json_ref JSONString
base64_decode URLEncoding
arg 0
arg 1
concat
arg 2
concat
arg 3
concat
sha256
b==
arg 9
ecdsa_pk_decompress Secp256r1
store 1
store 0
arg 4
arg 5
sha256
concat
sha256
arg 6
arg 7
load 0
load 1
ecdsa_verify Secp256r1
&&
arg 5
byte "origin"
json_ref JSONString
byte TMPL_O
b==
&&
arg 4
extract 32 1
int 7
getbit
arg 4
extract 32 1
int 5
getbit
&&
&&
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
arg 8
arg 0
ed25519verify_bare
&&
return

// listContainsValue
listContainsValue_0:
proto 3 1
int 0
store 2
listContainsValue_0_l1:
load 2
frame_dig -1
<
bz listContainsValue_0_l5
frame_dig -3
load 2
frame_dig -1
*
frame_dig -1
extract3
frame_dig -2
b==
bnz listContainsValue_0_l4
load 2
int 1
+
store 2
b listContainsValue_0_l1
listContainsValue_0_l4:
int 1
retsub
listContainsValue_0_l5:
int 0
retsub`
