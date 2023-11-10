import styles from '@/styles/Home.module.css'
import Input from '@/components/Input'
import { useEffect, useState } from 'react'
import { getUsernameIsAvailable, signIn } from '@gradian/keychain'
import Button from '@/components/Button'
import Link from 'next/link'
import SecuredBy from '@/components/SecuredBy'
import Head from 'next/head'
import { notifyError } from '@/lib'

export default function Home (): JSX.Element {
  const [username, setUsername] = useState('')
  const [errorMessage, setErrorMessage] = useState('Not available')
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [isLoading, setIsLoading] = useState(false)
  const [signupClicked, setSignupClicked] = useState(false)

  const handleRegistration = (): void => {
    setSignupClicked(true)
    if (errorMessage.length > 0) {
      return
    }
    setIsLoading(true)
    signIn(username).then(() => {
      setIsLoading(false)
    }).catch(e => {
      setIsLoading(false)
      notifyError('Something went wrong, please try again.')
      console.error(e)
    })
  }

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      // Wrapping the async logic in an IIFE
      getUsernameIsAvailable(username).then((isAvailable) => {
        setErrors((e) => ({
          ...e,
          usernameNotAvailable: isAvailable ? 'Username does not exist' : ''
        }))
      }).catch((e) => {
        setErrors((e) => ({
          ...e,
          usernameNotAvailable: 'Could not check username availability'
        }))
      })
    }, 500)
    return () => { clearTimeout(timeoutId) }
  }, [username])

  useEffect(() => {
    setErrors((e) => ({
      ...e,
      usernameLength: username.length >= 3 ? '' : 'Username must be at least 3 characters long'
    }))
  }, [username])

  useEffect(() => {
    const errorMessages = Object.values(errors).filter((e) => e.length > 0)
    const hasErrors = errorMessages.length > 0
    const errorMessage = hasErrors ? errorMessages.join(', ') : ''
    setErrorMessage(errorMessage)
  }, [errors])

  return (
    <>
      <Head>
        <title>Sign-In | Gradian Keychain</title>
      </Head>
      <main className={`${styles.main}`}>
        <div className={styles.signInForm}>
          <div>
            <h2>Welcome back</h2>
            <p>Sign in to your account</p>
          </div>
          <div className={styles.grid}>
            <Input
              type="text"
              placeholder="Enter username"
              value={username}
              onChange={(e) => { setUsername(e) }} // Fixed here
              errorMessage={errorMessage}
              isTouchedDisabled={signupClicked}
            />
            <br /><br />
            <Button onClick={handleRegistration} loading={isLoading}>Sign-In</Button>
            <br />
            <p>Don&apos;t have an account? <Link href="/signup" className='textLink'>Sign-Up</Link></p>
          </div>
          <SecuredBy />
        </div>
      </main>
    </>
  )
}
