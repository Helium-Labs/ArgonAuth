import React from 'react'
import styles from './SecuredBy.module.css'
import Image from 'next/image'

const SecuredBy: React.FC = () => {
  return (
    <span className={`${styles.securedByContainer}`}>
      Secured by
      <Image src="/images/logo.png" alt="SecuredBy" height={50} width={50} className={styles.securedByContainerImage} />
      <span className={`${styles.logoText}`}>
        Gradian
      </span>
    </span>
  )
}

export default SecuredBy
