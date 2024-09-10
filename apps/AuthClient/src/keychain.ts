import Keychain from '@gradian/keychain'
import { displayEmailCodeModal, teardownEmailCodeModal } from './completeRegistrationModal'
import type algosdk from 'algosdk'

export default class KeychainInterface {
  private readonly Keychain: Keychain
  jwt: string
  constructor () {
    this.Keychain = new Keychain()
    this.jwt = ''
  }

  public async signIn (username: string): Promise<string> {
    this.jwt = await this.Keychain.signIn(username)
    return this.jwt
  }

  public async signUp (username: string): Promise<void> {
    // await this.Keychain.initRegister(username)
    const onEmailCodeSubmit = async (emailCode: string): Promise<void> => {
      const isValid = await this.Keychain.getEmailCodeIsValid(username, emailCode)
      if (!isValid) {
        throw new Error('Invalid email code.')
      }
      await this.Keychain.register(username, emailCode)
      teardownEmailCodeModal()
    }
    const sendEmailCode = async (): Promise<void> => {
      await this.Keychain.initRegister(username)
    }
    displayEmailCodeModal({
      onEmailCodeSubmit,
      sendEmailCode
    })
  }

  public async tokenIsValid (jwt: string): Promise<boolean> {
    const isValid = await this.Keychain.jwtIsValid(jwt)
    return isValid
  }

  public async signTx (tx: algosdk.Transaction): Promise<string> {
    return await this.Keychain.signTransaction(tx)
  }

  public async getAddress (): Promise<string> {
    return await this.Keychain.getAddress()
  }
}
