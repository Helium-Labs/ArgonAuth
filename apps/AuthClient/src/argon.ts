import Argon from 'argon-client'
import { displayEmailCodeModal, teardownEmailCodeModal } from './completeRegistrationModal'
import type algosdk from 'algosdk'

export default class ArgonInterface {
  private readonly Argon: Argon
  jwt: string
  constructor () {
    this.Argon = new Argon()
    this.jwt = ''
  }

  public async signIn (username: string): Promise<string> {
    this.jwt = await this.Argon.signIn(username)
    return this.jwt
  }

  public async signUp (username: string): Promise<void> {
    // await this.Argon.initRegister(username)
    const onEmailCodeSubmit = async (emailCode: string): Promise<void> => {
      const isValid = await this.Argon.getEmailCodeIsValid(username, emailCode)
      if (!isValid) {
        throw new Error('Invalid email code.')
      }
      await this.Argon.register(username, emailCode)
      teardownEmailCodeModal()
    }
    const sendEmailCode = async (): Promise<void> => {
      await this.Argon.initRegister(username)
    }
    displayEmailCodeModal({
      onEmailCodeSubmit,
      sendEmailCode
    })
  }

  public async tokenIsValid (jwt: string): Promise<boolean> {
    const isValid = await this.Argon.jwtIsValid(jwt)
    return isValid
  }

  public async signTx (tx: algosdk.Transaction): Promise<string> {
    return await this.Argon.signTransaction(tx)
  }

  public async getAddress (): Promise<string> {
    return await this.Argon.getAddress()
  }
}
