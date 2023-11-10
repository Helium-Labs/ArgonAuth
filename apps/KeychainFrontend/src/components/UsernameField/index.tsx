import React, { useState } from 'react'
import styles from './Input.module.css'
import Image from 'next/image'

interface AvailableIconProps {
  isUserNameAvailable?: boolean
  isLoading?: boolean
}
const UserNameAvailability: React.FC<AvailableIconProps> = ({ isUserNameAvailable, isLoading = false }) => {
  if (isLoading) {
    return (
      <div className={styles.iconContainer}>
        <Image src="/images/icons/spinner-solid.svg" alt="AvailableIcon" width={20} height={20} className={styles.spinner} />
      </div>
    )
  }
  if (isUserNameAvailable) {
    return (
      <div className={styles.iconContainer}>
        <Image src="/images/icons/circle-check-regular.svg" alt="AvailableIcon" width={20} height={20} className={styles.checkIcon} />
      </div>
    )
  }
  return (
    <div className={styles.iconContainer}>
      <Image src="/images/icons/circle-xmark-regular.svg" alt="AvailableIcon" width={20} height={20} className={styles.crossIcon} />
    </div>
  )
}

interface InputProps {
  type: string
  placeholder: string
  value: string
  onChange?: (value: string) => void
  errorMessage: string
  onBlur?: (e: React.ChangeEvent<HTMLInputElement>) => void
  isUserNameAvailable?: boolean
  isUserNameAvailableLoading?: boolean
  isTouchedDisabled?: boolean
}
const InputUsername: React.FC<InputProps> = ({ type, placeholder, value, onChange, onBlur, isUserNameAvailable, isUserNameAvailableLoading, errorMessage = '', isTouchedDisabled = false }) => {
  const [touched, setTouched] = useState(false)
  const [focused, setFocused] = useState(false)
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
      {focused && value.length > 3 && (
        <UserNameAvailability isUserNameAvailable={isUserNameAvailable} isLoading={isUserNameAvailableLoading} />
      )}
      {showError && <div className={styles.error}>{errorMessage}</div>}
    </div>
  )
}

export default InputUsername
