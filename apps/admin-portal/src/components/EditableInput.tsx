import { FC } from 'react'
import { Text } from '@react-pdf/renderer'
import compose from '../styles/compose'
import { cn } from '@/lib/utils'

interface Props {
  className?: string
  placeholder?: string
  value?: string
  onChange?: (value: string) => void
  pdfMode?: boolean
}

// Map old CSS classes to TailwindCSS equivalents
const classTailwindMap: Record<string, string> = {
  'right': 'text-right',
  'center': 'text-center',
  'bold': 'font-bold',
  'dark': 'text-gray-800',
  'white': 'text-white',
  'fs-20': 'text-xl',
  'fs-45': 'text-5xl',
  'mb-5': 'mb-1',
  'w-100': 'w-full',
  'blue': 'text-blue-500',
  'ml-30': 'ml-8'
}

const convertToTailwind = (className: string) => {
  return className.split(' ')
    .map(cls => classTailwindMap[cls] || cls)
    .join(' ')
}

const EditableInput: FC<Props> = ({ className, placeholder, value, onChange, pdfMode }) => {
  return (
    <>
      {pdfMode ? (
        <Text style={compose('span ' + (className || ''))}>{value}</Text>
      ) : (
        <input
          type="text"
          className={cn(
            'border-none bg-transparent outline-none p-1 w-full focus:bg-gray-50 focus:ring-1 focus:ring-primary/20 rounded',
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

export default EditableInput
