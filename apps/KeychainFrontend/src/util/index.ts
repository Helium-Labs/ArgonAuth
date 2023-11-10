export function isNullOrUndefined (value: any): boolean {
  const isNull: boolean = value === null
  const isUndefined: boolean = value === undefined
  return isNull || isUndefined
}
