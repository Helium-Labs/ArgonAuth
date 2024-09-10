import Argon from 'argon-client';
import { displayEmailCodeModal, teardownEmailCodeModal } from './completeRegistrationModal';
export default class ArgonInterface {
    Argon;
    jwt;
    constructor() {
        this.Argon = new Argon();
        this.jwt = '';
    }
    async signIn(username) {
        this.jwt = await this.Argon.signIn(username);
        return this.jwt;
    }
    async signUp(username) {
        // await this.Argon.initRegister(username)
        const onEmailCodeSubmit = async (emailCode) => {
            const isValid = await this.Argon.getEmailCodeIsValid(username, emailCode);
            if (!isValid) {
                throw new Error('Invalid email code.');
            }
            await this.Argon.register(username, emailCode);
            teardownEmailCodeModal();
        };
        const sendEmailCode = async () => {
            await this.Argon.initRegister(username);
        };
        displayEmailCodeModal({
            onEmailCodeSubmit,
            sendEmailCode
        });
    }
    async tokenIsValid(jwt) {
        const isValid = await this.Argon.jwtIsValid(jwt);
        return isValid;
    }
    async signTx(tx) {
        return await this.Argon.signTransaction(tx);
    }
    async getAddress() {
        return await this.Argon.getAddress();
    }
}
