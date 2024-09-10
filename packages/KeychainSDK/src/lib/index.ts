import { algod, webauthnLSIGClient } from '../api'

export async function getWebauthnCompiledLSIG (credpkB64Url: string, originB64Url: string): Promise<string> {
  const getGetWebauthnLSIGResponse = await webauthnLSIGClient.getGetWebauthnLSIG(credpkB64Url, originB64Url)
  const compiledPyTeal: string = Buffer.from(getGetWebauthnLSIGResponse.data.compiledTeal, 'base64').toString('utf-8')
  const compiledTeal = await algod.compile(compiledPyTeal).do()
  return compiledTeal.result
}
