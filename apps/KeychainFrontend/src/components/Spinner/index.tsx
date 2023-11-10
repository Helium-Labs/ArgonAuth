import React from 'react'
import styles from './Spinner.module.css'

const Spinner: React.FC = () => {
  return (
    <span className={styles.spinner}>
      <div className={styles.bounce1}></div>
      <div className={styles.bounce2}></div>
      <div className={styles.bounce3}></div>
    </span>
  )
}

export default Spinner
