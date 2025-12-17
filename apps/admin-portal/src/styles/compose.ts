import styles from './styles'

// IMPORTANT:
// Do not import '@react-pdf/renderer' here. This file is used by the web UI styling pipeline
// and importing react-pdf at runtime can lead to duplicate module instances under Vite,
// causing Yoga Config "instanceof" BindingError during PDF rendering.
type PdfStyles = Record<string, unknown>

const compose = (classes: string): PdfStyles => {
  const css: PdfStyles = {
    //@ts-ignore
    '@import': 'url(https://fonts.bunny.net/css?family=nunito:400,600)',
  }

  const classesArray: string[] = classes.replace(/\s+/g, ' ').split(' ')

  classesArray.forEach((className) => {
    if (styles[className] !== undefined) {
      Object.assign(css, styles[className])
    }
  })

  return css
}

export default compose
