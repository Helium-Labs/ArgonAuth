import algo from 'algosdk'
const mainnetBaseServer = 'https://mainnet-api.algonode.cloud'
const testnetBaseServer = 'https://testnet-api.algonode.cloud'

const token = ''
const port = 443
const mainnetClient = new algo.Algodv2(token, mainnetBaseServer, port)
const testnetClient = new algo.Algodv2(token, testnetBaseServer, port)

function algoClient (isMainNet: boolean = true): algo.Algodv2 {
  return isMainNet ? mainnetClient : testnetClient
}

export { mainnetClient, testnetClient, algoClient }
const isMainNet = true
export const algod = isMainNet ? mainnetClient : testnetClient
