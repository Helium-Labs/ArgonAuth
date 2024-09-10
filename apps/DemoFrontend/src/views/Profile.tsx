import { algod, fundAccount, getAlgokitTestkit } from 'algokit-testkit'
import React, { useEffect, useRef } from 'react'
import { keychain } from '../lib'
import toast from 'react-hot-toast'
import algosdk from 'algosdk'
interface TX {
  id: string
}

const Table = ({ items }: { items: TX[] }): JSX.Element => {
  return (
    <>
  {/*
  Heads up! 👋

  This component comes with some `rtl` classes. Please remove them if they are not needed in your project.
*/}
  <div className="overflow-x-auto">
    <table className="min-w-full divide-y-2 divide-gray-200 bg-white text-sm">
      <thead className="ltr:text-left rtl:text-right">
        <tr>
          <th className="whitespace-nowrap px-4 py-2 font-medium text-gray-900 text-left">
            TxId
          </th>
          <th className="px-4 py-2" />
        </tr>
      </thead>
      <tbody className="divide-y divide-gray-200">
        {items.map((tx) => (
        <tr key={tx.id}>
          <td className="whitespace-nowrap px-4 py-2 font-medium text-gray-900">
            {tx.id}
          </td>
          <td className="whitespace-nowrap px-4 py-2">
            <a
              href="#"
              className="inline-block rounded bg-indigo-600 px-4 py-2 text-xs font-medium text-white hover:bg-indigo-700"
            >
              View
            </a>
          </td>
        </tr>))}
      </tbody>
    </table>
  </div>
</>
  )
}
const testTx = [
  {
    id: '4NCAQIR7HTS2NLR6ZMSNMGFZD4LIT6EOQESYZKIQPGLU7VNVNOXVBOY4JE'
  },
  {
    id: '4NCAQIR7HTS2NLR6ZMSNMGFZD4LIT6EOQESYZKIQPGLU7VNVNOXVBOY4JE'
  },
  {
    id: '4NCAQIR7HTS2NLR6ZMSNMGFZD4LIT6EOQESYZKIQPGLU7VNVNOXVBOY4JE'
  }
]
const Profile = (): JSX.Element => {
  const [items, setItems] = React.useState<TX[]>([])
  const [balance, setBalance] = React.useState<number>(0)
  const [addr, setAddr] = React.useState<string>()
  const getBalance = async (addr: string): Promise<number> => {
    const { algod } = await getAlgokitTestkit()
    const balance = await algod.accountInformation(addr).do()
    return balance.amount
  }

  useEffect(() => {
    keychain.getAddress().then((addr) => {
      setAddr(addr)
      getBalance(addr).then((balance) => {
        setBalance(balance)
      }).catch((err) => {
        toast.error('Error getting balance')
        console.log(err)
      })
    }).catch((err) => {
      toast.error('Error getting address')
      console.log(err)
    })
  }, [items.length])

  const sendTx = async (): Promise<void> => {
    if (addr === undefined) {
      toast.error('Error getting address')
      return
    }
    // create self send tx
    const suggestedParams = await algod.getTransactionParams().do()
    const tx = algosdk.makePaymentTxnWithSuggestedParamsFromObject({
      from: addr,
      to: addr,
      amount: 0,
      suggestedParams
    })
    keychain.signTx(tx).then((txId) => {
      toast.success('Transaction sent')
      setItems([...items, { id: txId }])
    }).catch((err) => {
      toast.error('Error sending transaction')
      console.log(err)
    })
  }

  const fund = async (): Promise<void> => {
    if (addr === undefined) {
      toast.error('Error getting address')
      return
    }
    await fundAccount(addr, 100_000)
    getBalance(addr).then((balance) => {
      setBalance(balance)
    }).catch((err) => {
      toast.error('Error getting balance')
      console.log(err)
    })
    toast.success('Account funded with 10k microAlgo')
  }

  return (
    <>
      <nav className="bg-white flex w-full justify-between items-center shadow-lg mb-10">
        {/* A left justified title saying "Profile" */}
        <div className="flex items-center justify-between h-16 w-full px-8">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <p className="text-2xl font-bold text-gray-800">
                Profile
              </p>
            </div>
          </div>
          <div className="ml-4 flex items-center md:ml-6">
            <button
              type="button"
              className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none"
              onClick={() => {
                // just refresh the page
                window.location.reload()
              }}
            >
              Logout
            </button>
          </div>
        </div>
        {/* A right justified logout button */}
      </nav>
      <main className="w-full flex flex-col items-center justify-center px-4">
        <div className="items-center w-full bg-white my-5 space-y-5 max-w-2xl">
          <h2>Details</h2>
          <article className="rounded-lg border border-gray-300 bg-white p-6">
            <div>
              <p className="text-sm text-gray-500">Address</p>
              <p className="text-md font-medium text-gray-900">{addr}</p>
            </div>
          </article>
          <article className="rounded-lg border border-gray-300 bg-white p-6">
            <div>
              <p className="text-sm text-gray-500">Balance (microALGO)</p>
              <p className="text-lg font-medium text-gray-900">{balance}</p>
            </div>
          </article>
          <h2>Fund Account</h2>
          <a
            className="inline-flex items-center between gap-2 rounded border border-indigo-600 bg-indigo-600 px-8 py-3 text-white hover:bg-transparent hover:text-indigo-600 focus:outline-none focus:ring active:text-indigo-500"
            onClick={() => {
              fund()
                .then(() => {})
                .catch(() => {})
            }}
          >
            <span className="text-sm font-medium"> Fund Account </span>
          </a>
          <h2>Test Transaction</h2>
          <article className="rounded-lg border border-gray-300 bg-white p-6">
            <div>
              <p className="text-sm text-gray-500 ">Type of Transaction</p>
              <div className="text-lg font-medium text-gray-900 flex justify-between items-center">
                <p>Self-Send</p>
                <a
                  className="inline-flex items-center between gap-2 rounded border border-indigo-600 bg-indigo-600 px-8 py-3 text-white hover:bg-transparent hover:text-indigo-600 focus:outline-none focus:ring active:text-indigo-500"
                  onClick={() => {
                    sendTx()
                      .then(() => {})
                      .catch(() => {})
                  }}
                >
                  <span className="text-sm font-medium"> Sign </span>
                  <svg
                    className="h-5 w-5 rtl:rotate-180"
                    xmlns="http://www.w3.org/2000/svg"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M17 8l4 4m0 0l-4 4m4-4H3"
                    />
                  </svg>
                </a>
              </div>
            </div>
          </article>
          <h2>Transactions</h2>
          <Table items={items} />
        </div>
      </main>
    </>
  )
}

export default Profile
