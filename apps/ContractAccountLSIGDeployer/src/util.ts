export function isNullOrUndefined (value: any): boolean {
  const isNull: boolean = value === null
  const isUndefined: boolean = value === undefined
  return isNull || isUndefined
}

export function base64urlToBase64 (base64url: string): string {
  let base64 = base64url
    .replace(/-/g, '+')
    .replace(/_/g, '/')

  while (base64.length % 4 !== 0) {
    base64 += '='
  }

  return base64
}

export function base64ToBase64url (base64: string): string {
  return base64
    .replace(/\+/g, '-') // Replace '+' with '-'
    .replace(/\//g, '_') // Replace '/' with '_'
    .replace(/=+$/, '') // Remove any trailing '=' padding characters
}
