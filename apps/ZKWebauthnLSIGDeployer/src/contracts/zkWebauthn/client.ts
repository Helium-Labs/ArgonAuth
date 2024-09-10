import getTealInstanceFromTemplateMap, { asBase64Bytes } from '../util'
import source from './tealV2'

export default async function getZKWebauthnLSIGInstance (credPKXYB64: string, originB64: string): Promise<string> {
  // define map of template variables
  const templateMap: any = {
    TMPL_C: asBase64Bytes(credPKXYB64),
    TMPL_O: asBase64Bytes(originB64)
  }

  const teal = getTealInstanceFromTemplateMap(templateMap, source)
  return btoa(teal)
}
