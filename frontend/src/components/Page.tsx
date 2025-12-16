import { FC, PropsWithChildren } from 'react'
import { Page as PdfPage } from '@react-pdf/renderer'
import compose from '../styles/compose'
import { cn } from '@/lib/utils'

interface Props {
  className?: string
  pdfMode?: boolean
}

const Page: FC<PropsWithChildren<Props>> = ({ className, pdfMode, children }) => {
  return (
    <>
      {pdfMode ? (
        <PdfPage size="A4" style={compose('page ' + (className || ''))}>
          {children}
        </PdfPage>
      ) : (
        <div className={cn('font-sans text-sm text-gray-600 p-8 bg-white max-w-4xl mx-auto', className)}>
          {children}
        </div>
      )}
    </>
  )
}

export default Page
