export const asBase64Bytes = (str: string): string => `base64(${str})`

export default function getTealInstanceFromTemplateMap (templateMap: any, source: string): string {
  // get copy of source
  let instance = source.slice()

  // Check all template keys are of the form TMPL_*
  for (const key in templateMap) {
    const regex = /TMPL_.*/g
    if (!regex.test(key)) {
      throw new Error('Template key is not of the form TMPL_*')
    }
  }

  // Check all template values are strings
  for (const key in templateMap) {
    if (typeof templateMap[key] !== 'string') {
      throw new Error('Template value is not a string')
    }
  }

  // Check the template provided keys are completely contained in the source
  const regex = /TMPL_.*/g
  const templateKeys = instance.match(regex)
  if (templateKeys === null) {
    throw new Error('No template keys found in source')
  }
  const templateKeysSet = new Set(templateKeys)
  const templateMapKeysSet = new Set(Object.keys(templateMap))
  if (templateKeysSet.size !== templateMapKeysSet.size) {
    throw new Error('Number of template keys in source does not match number of template keys in map')
  }
  for (const key of templateKeysSet) {
    if (!templateMapKeysSet.has(key)) {
      throw new Error('Template key not found in template map')
    }
  }

  // fill out template variables
  for (const key in templateMap) {
    const regex = new RegExp(key, 'g')
    instance = instance.replace(regex, templateMap[key])
  }

  return instance
}
