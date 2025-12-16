import { FC, PropsWithChildren } from 'react'
import { View as PdfView } from '@react-pdf/renderer'
import compose from '../styles/compose'
import { cn } from '@/lib/utils'

interface Props {
  className?: string
  pdfMode?: boolean
}

// Map old CSS classes to TailwindCSS equivalents
const classTailwindMap: Record<string, string> = {
  'flex': 'flex',
  'w-100': 'w-full',
  'w-50': 'w-1/2',
  'w-55': 'w-[55%]',
  'w-45': 'w-[45%]',
  'w-60': 'w-[60%]',
  'w-40': 'w-[40%]',
  'w-48': 'w-[48%]',
  'w-17': 'w-[17%]',
  'w-18': 'w-[18%] text-right',
  'w-auto': 'w-auto',
  'mt-40': 'mt-10',
  'mt-30': 'mt-8', 
  'mt-20': 'mt-5',
  'mt-10': 'mt-2',
  'mb-5': 'mb-1',
  'mb-20': 'mb-5',
  'p-4-8': 'px-2 py-1',
  'p-5': 'p-1',
  'pb-10': 'pb-2',
  'right': 'text-right flex flex-col items-end',
  'center': 'text-center flex justify-center',
  'bold': 'font-bold',
  'bg-dark': 'bg-gray-600 text-white',
  'bg-gray': 'bg-gray-100',
  'row': 'border-b border-gray-200 relative group',
  'dark': 'text-gray-800',
  'border-b-2': 'border-b-2',
  'border-gray-300': 'border-gray-300',
  'h-16': 'h-16',
  'text-sm': 'text-sm'
}

const convertToTailwind = (className: string) => {
  return className.split(' ')
    .map(cls => classTailwindMap[cls] || cls)
    .join(' ')
}

const View: FC<PropsWithChildren<Props>> = ({ className, pdfMode, children }) => {
  return (
    <>
      {pdfMode ? (
        <PdfView style={compose(className || '')}>{children}</PdfView>
      ) : (
        <div className={cn(convertToTailwind(className || ''))}>{children}</div>
      )}
    </>
  )
}

export default View
