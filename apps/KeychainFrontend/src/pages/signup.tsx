import Head from 'next/head'
import styles from '@/styles/Home.module.css'
import { useEffect, useState } from 'react'
import { getUsernameIsAvailable, register } from '@gradian/keychain'
import Button from '@/components/Button'
import Link from 'next/link'
import InputUsername from '@/components/UsernameField'
import { useRouter } from 'next/router'
import SecuredBy from '@/components/SecuredBy'
import { notifyError } from '@/lib'

export default function Home (): JSX.Element {
  const [username, setUsername] = useState('')
  const [usernameIsAvailable, setUsernameIsAvailable] = useState(false)
  const [usernameIsAvailableLoading, setUsernameIsAvailableLoading] = useState(false)
  const [errorMessage, setErrorMessage] = useState('Not available')
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [isLoading, setIsLoading] = useState(false)
  const [signupClicked, setSignupClicked] = useState(false)

  const router = useRouter()

  const handleRegistration = (): void => {
    setSignupClicked(true)
    if (errorMessage.length > 0) {
      return
    }
    setIsLoading(true)
    register(username).then(() => {
      setIsLoading(false)
      void router.push('/')
    }).catch((e) => {
      setIsLoading(false)
      notifyError('Something went wrong, please try again.')
      console.error(e)
    })
  } // Make sure the function is properly closed here

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      // Wrapping the async logic in an IIFE is not needed here
      setUsernameIsAvailableLoading(true)
      getUsernameIsAvailable(username)
        .then((isAvailable) => {
          setUsernameIsAvailable(isAvailable)
          setUsernameIsAvailableLoading(false)
          setErrors((e) => ({
            ...e,
            usernameNotAvailable: isAvailable ? '' : 'Username does not exist'
          }))
        })
        .catch((e) => {
          setUsernameIsAvailableLoading(false)
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

  // update error message based on errors
  useEffect(() => {
    const errorMessages: string[] = Object.values(errors).filter(e => e.length > 0)
    const hasErrors = errorMessages.length > 0
    const errorMessage = hasErrors ? errorMessages.join(', ') : ''
    setErrorMessage(errorMessage)
  }, [errors])

  return (
    <>
      <Head>
        <title>Sign-Up | Gradian Keychain</title>
      </Head>
      <main className={styles.main}>
        <div className={styles.signInForm}>
          <div>
            <h2>Get started</h2>
            <p>Create a new account</p>
          </div>
          <div className={styles.grid}>
            <InputUsername
              type="text"
              placeholder="Enter username"
              value={username}
              onChange={(e) => { setUsername(e) }} // Fixed here
              errorMessage={errorMessage}
              isUserNameAvailable={usernameIsAvailable}
              isUserNameAvailableLoading={usernameIsAvailableLoading}
              isTouchedDisabled={signupClicked}
            />
            <br />
            <br />
            <Button onClick={handleRegistration} loading={isLoading}>Sign-Up</Button>
            <br />
            <p>Have an account? <Link href="/" className='textLink'>Sign-In</Link></p>
          </div>
          <SecuredBy />
        </div>
      </main>
    </>
  )
}
