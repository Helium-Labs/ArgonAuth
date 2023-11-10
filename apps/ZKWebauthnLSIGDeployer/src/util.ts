export function base64urlToBase64 (value: string): string {
  const m = value.length % 4
  return value
    .replace(/-/g, '+')
    .replace(/_/g, '/')
    .padEnd(value.length + (m === 0 ? 0 : 4 - m), '=')
}

export function isNullOrUndefined (value: any): boolean {
  const isNull: boolean = value === null
  const isUndefined: boolean = value === undefined
  return isNull || isUndefined
}
