import Keychain from '@gradian/keychain';
import { displayEmailCodeModal, teardownEmailCodeModal } from './completeRegistrationModal';
export default class KeychainInterface {
    Keychain;
    jwt;
    constructor() {
        this.Keychain = new Keychain();
        this.jwt = '';
    }
    async signIn(username) {
        this.jwt = await this.Keychain.signIn(username);
        return this.jwt;
    }
    async signUp(username) {
        // await this.Keychain.initRegister(username)
        const onEmailCodeSubmit = async (emailCode) => {
            const isValid = await this.Keychain.getEmailCodeIsValid(username, emailCode);
            if (!isValid) {
                throw new Error('Invalid email code.');
            }
            await this.Keychain.register(username, emailCode);
            teardownEmailCodeModal();
        };
        const sendEmailCode = async () => {
            await this.Keychain.initRegister(username);
        };
        displayEmailCodeModal({
            onEmailCodeSubmit,
            sendEmailCode
        });
    }
    async tokenIsValid(jwt) {
        const isValid = await this.Keychain.jwtIsValid(jwt);
        return isValid;
    }
    async signTx(tx) {
        return await this.Keychain.signTransaction(tx);
    }
    async getAddress() {
        return await this.Keychain.getAddress();
    }
}
