# X25519 Elliptic Curve Diffie-Hellman (ECDH) Messaging API

Provides a simple API for encrypting and decrypting messages between two parties with X25519 ECDH, making use of best practices and highly trusted crypto implementations.

⭐ Stars ⭐ and contributions are highly appreciated.

![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)

## Features

- Encrypt messages with X25519 ECDH derived shared secret
- Decrypt messages with X25519 ECDH derived shared secret

## Installation

Install via npm:

```bash
npm install @gradian/x25519ecdh
```

## Usage

```javascript
import { generateX25519KeyPair, X25519ECDHMessageChannel } from "@gradian/x25519ecdh"

// Step 0: Exchange public keys (simulate a network transfer or other method of exchange)
const aliceKeys = generateX25519KeyPair();
const bobKeys = generateX25519KeyPair();

// Step 1: Each party creates their own message channel instance
const aliceChannel = new X25519ECDHMessageChannel(bobKeys.pub, aliceKeys); // Alice's message channel with her key pair
const bobChannel = new X25519ECDHMessageChannel(aliceKeys.pub, bobKeys); // Bob's message channel with his key pair

// Step 2: Alice sends an encrypted message to Bob
const encryptedPayloadFromAlice = await aliceChannel.encrypt("Hello, Bob!");

// Bob receives the encrypted payload from Alice and decrypts it
const decryptedPayloadByBob = await bobChannel.decrypt(encryptedPayloadFromAlice);
console.log(`Bob decrypted: ${decryptedPayloadByBob.unencryptedData}`);

// Step 3: Bob sends a reply back to Alice
const encryptedPayloadFromBob = await bobChannel.encrypt("Hi, Alice!");

// Alice receives the encrypted payload from Bob and decrypts it
const decryptedPayloadByAlice = await aliceChannel.decrypt(encryptedPayloadFromBob);
console.log(`Alice decrypted: ${decryptedPayloadByAlice.unencryptedData}`);
```

## Todo

Currently there's no way to verify who sent a given message, so a good todo is:

- Add message sender authentication with DSA

## Building

To build the project, run:

```bash
npm run build
```

## License

This project is licensed under the MIT License - see the [LICENSE](./LICENSE) file for details.

## Disclaimer

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE, TITLE, AND NON-INFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES, OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT, OR OTHERWISE, ARISING FROM, OUT OF, OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

By using this software, you acknowledge and agree that the authors and contributors of this software are not responsible or liable, directly or indirectly, for any damage or loss caused, or alleged to be caused, by or in connection with the use of or reliance on this software. This includes, but is not limited to, any bugs, errors, defects, failures, or omissions in the software or its documentation. Additionally, the authors are not responsible for any security vulnerabilities or potential breaches that may arise from the use of this software.

You are solely responsible for the risks associated with using this software and should take any necessary precautions before utilizing it in any production or critical systems. It's strongly recommended to review the software thoroughly and test its functionalities in a controlled environment before any broader application.
