import { algod } from '../../api/algoClient'
import getTealInstanceFromTemplateMap, { asBase64Bytes } from '../util'
import source from './teal'

export default async function getZKWebauthnLSIGInstance (credPKXYB64: string): Promise<string> {
  // define map of template variables
  const templateMap: any = {
    TMPL_CREDPK: asBase64Bytes(credPKXYB64)
  }

  const teal = getTealInstanceFromTemplateMap(templateMap, source)
  const compiledTeal = await algod.compile(teal).do()

  return compiledTeal.result
}
