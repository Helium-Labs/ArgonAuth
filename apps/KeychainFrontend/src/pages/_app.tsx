import '@/styles/globals.css'
import { isNullOrUndefined } from '@/util'
import type { AppProps } from 'next/app'
import { Inter, Montserrat } from 'next/font/google'
import { useEffect, useState } from 'react'

const inter = Inter({
  subsets: ['latin'],
  variable: '--font-inter',
  preload: true
})
const montserrat = Montserrat({
  subsets: ['latin'],
  variable: '--font-montserrat',
  preload: true
})

export default function App ({ Component, pageProps }: AppProps): JSX.Element {
  const [notifications, setNotifications] = useState<any[]>([])

  useEffect(() => {
    // listen for window custom events that contain a json payload with fields: title, body, type.
    const onMessage = (event: any): void => {
      try {
        const { title, body, type } = JSON.parse(event.detail)
        const titleNullOrUndefined: boolean = isNullOrUndefined(title)
        const bodyNullOrUndefined: boolean = isNullOrUndefined(body)
        const typeNullOrUndefined: boolean = isNullOrUndefined(type)
        if (!titleNullOrUndefined && !bodyNullOrUndefined && !typeNullOrUndefined) {
          setNotifications([...notifications, { title, body, type }])
        }
      } catch (e) {
        console.error(e)
      }
    }
    window.addEventListener('message', onMessage)
    return () => {
      window.removeEventListener('message', onMessage)
    }
  }, [notifications])

  // dequeue the notification after 5 seconds
  useEffect(() => {
    if (notifications.length > 0) {
      const timer = setTimeout(() => {
        console.log('dequeueing notification')
        setNotifications(e => e.slice(1))
      }, 5000)
      return () => { clearTimeout(timer) }
    }
  }, [notifications.length])

  return (
    <>
      <div className={`notificationContainer ${inter.className}`}>
        {notifications.map(({ title, body, type }, i) => (
          <div key={i} className={'notification'}>
            <h4>{title}</h4>
            <p>{body}</p>
          </div>
        ))}
      </div>
      <div className={`globalContainer ${montserrat.variable} ${inter.variable}`}>
        <Component {...pageProps} />
      </div>
    </>
  )
}
