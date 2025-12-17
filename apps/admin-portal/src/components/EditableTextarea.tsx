import { FC } from 'react'
import TextareaAutosize from 'react-textarea-autosize'
import { Text } from '@react-pdf/renderer'
import compose from '../styles/compose'
import { cn } from '@/lib/utils'

interface Props {
  className?: string
  placeholder?: string
  value?: string
  onChange?: (value: string) => void
  pdfMode?: boolean
  rows?: number
}

// Map old CSS classes to TailwindCSS equivalents
const classTailwindMap: Record<string, string> = {
  'dark': 'text-gray-800',
  'w-100': 'w-full',
  'bold': 'font-bold'
}

const convertToTailwind = (className: string) => {
  return className.split(' ')
    .map(cls => classTailwindMap[cls] || cls)
    .join(' ')
}

const EditableTextarea: FC<Props> = ({
  className,
  placeholder,
  value,
  onChange,
  pdfMode,
  rows,
}) => {
  return (
    <>
      {pdfMode ? (
        <Text style={compose('span ' + (className || ''))}>{value}</Text>
      ) : (
        <TextareaAutosize
          minRows={rows || 1}
          className={cn(
            'border-none bg-transparent outline-none p-1 w-full focus:bg-gray-50 focus:ring-1 focus:ring-primary/20 rounded resize-none',
            convertToTailwind(className || '')
          )}
          placeholder={placeholder || ''}
          value={value || ''}
          onChange={onChange ? (e) => onChange(e.target.value) : undefined}
        />
      )}
    </>
  )
}

export default EditableTextarea
