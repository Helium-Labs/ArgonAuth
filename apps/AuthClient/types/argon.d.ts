import type algosdk from 'algosdk';
export default class ArgonInterface {
    private readonly Argon;
    jwt: string;
    constructor();
    signIn(username: string): Promise<string>;
    signUp(username: string): Promise<void>;
    tokenIsValid(jwt: string): Promise<boolean>;
    signTx(tx: algosdk.Transaction): Promise<string>;
    getAddress(): Promise<string>;
}
