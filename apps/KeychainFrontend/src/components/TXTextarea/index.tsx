import React from 'react'
import styles from './Input.module.css'

function formatTXString (tx: string): string {
  try {
    const parsedJSON = JSON.parse(tx)
    const formattedJSONString = JSON.stringify(parsedJSON, null, 2).replaceAll('"', '')
    // remove first and last curly braces, one indent level
    return formattedJSONString
  } catch (error) {
    // If the string is not valid JSON, return the original string
    return tx
  }
}

interface TextAreaProps {
  value: string
}

const TextArea: React.FC<TextAreaProps> = ({ value }) => {
  return (
    <textarea
      className={styles.textarea}
      placeholder="Transaction"
      value={formatTXString(value)}
      readOnly
    />
  )
}

export default TextArea
