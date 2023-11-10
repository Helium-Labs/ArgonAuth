import React, { useState, useEffect, type MouseEventHandler } from 'react'
import styles from './Button.module.css'
import Spinner from '../Spinner'

interface ButtonProps {
  loading?: boolean
  disabled?: boolean
  onClick: MouseEventHandler<HTMLButtonElement>
  children: React.ReactNode
}

let lastEventTime = Date.now()
let suspiciousClicks = 0

const Button: React.FC<ButtonProps> = ({ loading = false, disabled = false, onClick = () => {}, children }) => {
  const [mouseHasMoved, setMouseHasMoved] = useState(false)

  useEffect(() => {
    const mouseMoveHandler = (): void => {
      setMouseHasMoved(true)
    }
    window.addEventListener('mousemove', mouseMoveHandler)

    return () => {
      window.removeEventListener('mousemove', mouseMoveHandler)
    }
  }, [])

  const validateHumanClick = (e: React.MouseEvent<HTMLButtonElement>): boolean => {
    if (!e.isTrusted || Date.now() - lastEventTime < 1000 || !mouseHasMoved) {
      suspiciousClicks++
      console.log('Possible bot detected. Number of suspicious clicks: ' + suspiciousClicks)
      return false
    }

    lastEventTime = Date.now()
    setMouseHasMoved(false)
    return true
  }

  const handleClick = (e: React.MouseEvent<HTMLButtonElement>): void => {
    if (validateHumanClick(e)) {
      onClick(e)
    }
  }

  return (
    <button className={styles.button} onClick={handleClick} disabled={loading || disabled}>
      {loading ? <Spinner /> : children}
    </button>
  )
}

export default Button
