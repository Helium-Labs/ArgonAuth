import { OpenAPIRoute, Path, Str } from '@cloudflare/itty-router-openapi'
import getZKWebauthnLSIGInstance from './contracts/zkWebauthn/client'
import { base64urlToBase64 } from './util'

export class GetWebauthnLSIG extends OpenAPIRoute {
  static schema = {
    tags: ['WebauthnLSIG'],
    summary: 'Get the LSIG for a given Webauthn credential',
    parameters: {
      credpk: Path(Str, {
        description: 'Authenticator Credential Public Key (ES256), in compressed form.'
      })
    },
    responses: {
      200: {
        description: 'Success response', // Add a description for the response
        schema: {
          metaData: {},
          compiledTeal: new Str({ example: 'lorem', required: true, description: 'Compiled teal code for the given Webauthn credential.' })
        }
      }
    }
  }

  async handle (request: Request, env: any, context: any, data: any): Promise<any> {
    // Retrieve the validated slug
    const { credpk } = data.params

    const credPKXYB64 = base64urlToBase64(credpk)
    const lsig: string = await getZKWebauthnLSIGInstance(credPKXYB64)

    return {
      metaData: {},
      compiledTeal: lsig
    }
  }
}
