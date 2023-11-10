import React, { useState } from 'react'
import styles from './Input.module.css'

interface InputProps {
  type: string
  placeholder: string
  value: string
  onChange?: (value: string) => void
  errorMessage: string
  onBlur?: (e: React.ChangeEvent<HTMLInputElement>) => void
  isTouchedDisabled?: boolean
}

const Input: React.FC<InputProps> = ({ type, placeholder, value, onChange, onBlur, errorMessage = '', isTouchedDisabled = false }) => {
  const [touched, setTouched] = useState(false)
  const [, setFocused] = useState(false)
  const handleFocus = (e: React.ChangeEvent<HTMLInputElement>): void => {
    setFocused(true)
  }

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>): void => {
    if (onChange) {
      onChange(e.target.value)
    }
  }

  const handleBlur = (e: React.ChangeEvent<HTMLInputElement>): void => {
    setTouched(true)
    if (onBlur) {
      onBlur(e)
    }
  }

  const showError = (touched || isTouchedDisabled) && errorMessage.length > 0
  return (
    <div className={styles.inputContainer}>
      <input
        className={showError ? `${styles.input} ${styles.invalid}` : styles.input}
        type={type}
        placeholder={placeholder}
        value={value}
        onChange={handleChange}
        onBlur={handleBlur}
        onFocus={handleFocus}
      />
      {showError && <div className={styles.error}>{errorMessage}</div>}
    </div>
  )
}

export default Input
