import React, { useState, useRef, useEffect } from 'react'
import { Spinner } from '../components'
import toast from 'react-hot-toast'

export interface EmailCodeProps {
  onEmailCodeSubmit: (emailCode: string) => Promise<void>
  sendEmailCode: () => Promise<void>
}
const EmailCode = ({ onEmailCodeSubmit, sendEmailCode }: EmailCodeProps): JSX.Element => {
  const [code, setCode] = useState<string[]>(new Array(6).fill(''))
  const inputsRef = useRef<Array<HTMLInputElement | null>>(new Array(6).fill(null))
  const [isLoading, setIsLoading] = useState<boolean>(false)
  const emailSentRef = useRef(false)

  useEffect(() => {
    if (sendEmailCode !== undefined && !emailSentRef.current) {
      emailSentRef.current = true // Mark that an email is being sent.
      sendEmailCode()
        .then(() => {
          toast.success('Email Code sent to your email.')
        })
        .catch((e: any) => {
          toast.error('Email Code failed to send. Try Again.')
          console.error(e)
        })
    }
  }, [])

  const handleInputChange = (event: React.ChangeEvent<HTMLInputElement>, index: number): void => {
    const value = event.target.value
    setCode(prevCode => {
      const newCode = [...prevCode]
      newCode[index] = value
      return newCode
    })
    if (value !== undefined && index < 5) {
      inputsRef.current[index + 1]?.focus()
    }
  }

  const handleOnPaste = (event: React.ClipboardEvent<HTMLInputElement>): void => {
    event.preventDefault()
    const pasteData = event.clipboardData.getData('text').slice(0, 6)
    setCode(pasteData.split(''))

    const nextInputIndex = inputsRef.current.findIndex((input, idx) => input?.value === '' && idx >= pasteData.length)
    if (nextInputIndex !== -1) {
      inputsRef.current[nextInputIndex]?.focus()
    }
  }

  const handleVerifyWork = async (): Promise<void> => {
    setIsLoading(true)
    const emailCode = code.join('')

    try {
      await onEmailCodeSubmit(emailCode)
      toast.success('Sign in successful')
    } catch (e) {
      toast.error('Either the email code is invalid or registration failed. Please try again.')
      console.error(e)
    } finally {
      setIsLoading(false)
    }
  }
  const handleVerify = (): void => {
    handleVerifyWork().then(() => {
      console.log('Verify successful')
    }).catch(e => {
      console.error(e)
    })
  }

  return (
    <main style={{
      display: 'flex',
      flexDirection: 'column',
      justifyContent: 'center',
      alignItems: 'center',
      background: 'white',
      padding: '1rem' // Tailwind p-4 (1rem assuming 1rem=16px)
    }}>
      <div style={{
        margin: 'auto',
        maxWidth: '32rem',
        textAlign: 'center',
        display: 'flex',
        alignItems: 'center',
        columnGap: '1rem',
        marginBottom: '1rem'
      }}>
        <h1 style={{
          fontSize: '1.875rem', // text-3xl: 1.875rem, text-2xl: 1.5rem for smaller screens
          fontWeight: 'bold',
          color: '#000' // Tailwind text-gray-900, assuming not in dark mode
        }}>Email Code</h1>
        {/* ... SVG Icon ... */}
      </div>
      <p style={{
        color: '#718096', // Tailwind text-gray-500
        textAlign: 'center',
        marginBottom: '48px' // Tailwind mb-12 (3rem assuming 1rem=16px)
      }}>
        Enter the code we sent to your email address.
      </p>
      <div style={{
        display: 'flex',
        columnGap: '16px' // Tailwind space-x-4 (1rem assuming 1rem=16px)
      }}>
        {code.map((num, index) => (
          <input
            key={index}
            ref={(el) => { inputsRef.current[index] = el }}
            onPaste={handleOnPaste}
            type="tel"
            maxLength={1}
            value={num}
            onChange={event => { handleInputChange(event, index) }}
            style={{
              width: '48px', // Tailwind w-12 (3rem assuming 1rem=16px)
              height: '48px', // Tailwind h-12 (3rem assuming 1rem=16px)
              borderRadius: '12px', // Tailwind rounded-lg (.75rem assuming 1rem=16px)
              border: '2px solid #d2d6dc', // Tailwind border-gray-300
              textAlign: 'center',
              fontSize: '1.25rem', // Tailwind text-xl
              fontWeight: 'medium' // Tailwind font-medium
              // Tailwind focus:border-blue-500 & focus:outline-none styles cannot be implemented inline, would need JS
            }}
            pattern="\d*"
            inputMode="numeric"
            autoFocus={index === 0}
          />
        ))}
      </div>
      <button
        onClick={handleVerify}
        style={{
          display: 'inline-flex',
          alignItems: 'center',
          justifyContent: 'center',
          borderRadius: '0.375rem', // Tailwind rounded-lg
          backgroundColor: '#6366f1', // Tailwind bg-blue-500
          padding: '12px 20px', // Tailwind px-5 py-3 (.75rem and 1.25rem)
          fontSize: '0.875rem', // Tailwind text-sm
          fontWeight: 'medium', // Tailwind font-medium
          color: 'white',
          marginTop: '20px' // Tailwind my-5 (1.25rem assuming 1rem=16px)
          // Tailwind hover:bg-blue-600, focus:outline-none, focus:ring-2, focus:ring-offset-2, focus:ring-blue-500 styles cannot be implemented
        }}
      >
        {isLoading && <Spinner />} {/* Assume Spinner has its own style */}
        Verify
      </button>
      <p style={{
        color: '#718096', // Tailwind text-gray-500
        textAlign: 'center',
        marginTop: '40px' // Tailwind mt-10 (2.5rem assuming 1rem=16px)
      }}>
        Didn't receive the code?{' '}
        {/* <Link
           to={routes.signup or redirectUri}
           style={{ Tailwind styles for text-blue-500, focus:outline-none, focus:underline }}
         >Try Again</Link>
        */}
      </p>
    </main>
  )
}

export default EmailCode
