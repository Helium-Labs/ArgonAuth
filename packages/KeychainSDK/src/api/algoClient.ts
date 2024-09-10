import algo from 'algosdk'
import { isTestEnvirnoment } from '../constants'
import { getAlgokitClients } from 'algokit-testkit'

const mainnetBaseServer = 'https://mainnet-api.algonode.cloud'

const token = ''
const port = 443
const mainnetClient = new algo.Algodv2(token, mainnetBaseServer, port)
const { algod: algokitAlgod } = getAlgokitClients()

export const algod = isTestEnvirnoment ? algokitAlgod : mainnetClient
