import { FC } from 'react'
import { Text as PdfText } from '@react-pdf/renderer'
import compose from '../styles/compose'
import { cn } from '@/lib/utils'

interface Props {
  className?: string
  pdfMode?: boolean
  children?: string
}

// Map old CSS classes to TailwindCSS equivalents
const classTailwindMap: Record<string, string> = {
  'right': 'text-right block w-full',
  'center': 'text-center block w-full',
  'bold': 'font-bold',
  'dark': 'text-gray-800',
  'white': 'text-white',
  'fs-20': 'text-xl',
  'fs-45': 'text-5xl',
  'mb-5': 'mb-1',
  'mb-10': 'mb-2',
  'blue': 'text-blue-500',
  'w-auto': 'w-auto',
  'text-sm': 'text-sm'
}

const convertToTailwind = (className: string) => {
  return className.split(' ')
    .map(cls => classTailwindMap[cls] || cls)
    .join(' ')
}

const Text: FC<Props> = ({ className, pdfMode, children }) => {
  return (
    <>
      {pdfMode ? (
        <PdfText style={compose('span ' + (className || ''))}>{children}</PdfText>
      ) : (
        <span className={cn('inline-block pr-3 py-1', convertToTailwind(className || ''))}>{children}</span>
      )}
    </>
  )
}

export default Text
