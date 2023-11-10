import Head from 'next/head'
import styles from '@/styles/Home.module.css'
import { type MouseEvent, useEffect, useState } from 'react'
import Button from '@/components/Button'
import { notifyError } from '@/lib'
import TextArea from '@/components/TXTextarea'
import { isNullOrUndefined } from '@/util'

function signTransaction (): void {
// @TODO: Implement this function
}

function getIsSandboxed (): boolean {
  try {
    // Attempt to create and execute a script
    const script = document.createElement('script')
    script.innerHTML = 'var test = 1;'
    document.body.appendChild(script)
    document.body.removeChild(script)
  } catch (e) {
    // If an error is thrown, then the iframe might be sandboxed
    return true
  }
  try {
    // Attempt to access a property of the parent window
    const test = window?.top?.location?.href
    const testIsNullOrUndefined: boolean = isNullOrUndefined(test)
    if (testIsNullOrUndefined) {
      return true
    }
  } catch (e) {
    // If an error is thrown, then the iframe might be sandboxed
    return true
  }

  // If none of the above checks failed, the page is likely not sandboxed
  return true
}

export default function Home (): JSX.Element {
  const [username] = useState(`{
    "txn": {
      "amt": 5000000,
      "fee": 1000,
      "fv": 6000000,
      "gen": "mainnet-v1.0",
      "gh": "wGHE2Pwdvd7S12BL5FaOP20EGYesN73ktiC1qzkkit8=",
      "lv": 6001000,
      "note": "SGVsbG8gV29ybGQ=",
      "rcv": "GD64YIY3TWGDMCNPP553DZPPR6LDUSFQOIJVFDPPXWEG3FVOJCCDBBHU5A",
      "snd": "EW64GC6F24M7NDSC5R3ES4YUVE3ZXXNMARJHDCCCLIHZU6TBEOC7XRSBG4",
      "type": "pay"
    }
  }`)

  const [isLoading, setIsLoading] = useState<boolean>(false)
  const [, setApproveClicked] = useState(false)
  const [isSandboxed, setIsSandboxed] = useState<boolean>(false)

  useEffect(() => {
    setIsSandboxed(getIsSandboxed())
  }, [])

  const handleApprove = (event: MouseEvent<HTMLButtonElement>): void => {
    setApproveClicked(true)
    try {
      setIsLoading(true)
      signTransaction()
      setIsLoading(false)
      console.log('success')
    } catch (e: any) {
      setIsLoading(false)
      notifyError('Something went wrong, please try again.')
      console.error(e)
    }
  }

  // eslint-disable-next-line no-constant-condition
  if (true || isSandboxed) {
    return (
      <>
        <Head>
          <title>Approve Transaction | Gradian Keychain</title>
        </Head>
        <main className={`${styles.mainWallet}`}>
          <div className={styles.walletContainer}>
            <div>
              <h2>
                Transaction
              </h2>
              <p>
                Signing Request
              </p>
            </div>
            <div className={styles.walletGrid}>
              <TextArea
                value={username}
              />
              <br></br>
              <Button onClick={handleApprove} loading={isLoading}>Approve</Button>
            </div>
            {/* <SecuredBy /> */}
          </div>
        </main>
      </>
    )
  }

  return (
    <>
      <Head>
        <title>Approve Transaction | Gradian Keychain</title>
      </Head>
      <main className={`${styles.mainWallet}`}>
        <div className={styles.walletContainer}>
          <div>
            <h2>
              You are not in sandbox mode
            </h2>
            <p>
              Signing Request
            </p>
          </div>
        </div>
      </main>
    </>
  )
}
