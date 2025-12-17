import * as React from 'react'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from './select'

interface PageSizeSelectProps {
  value: number
  onChange: (value: number) => void
  options?: number[]
  size?: 'sm' | 'default'
  className?: string
}

const DEFAULT_OPTIONS = [10, 20, 50, 100]

export function PageSizeSelect({
  value,
  onChange,
  options = DEFAULT_OPTIONS,
  size = 'sm',
  className,
}: PageSizeSelectProps) {
  return (
    <Select value={String(value)} onValueChange={(v) => onChange(Number(v))}>
      <SelectTrigger size={size} className={className}>
        <SelectValue placeholder={`${value} / page`} />
      </SelectTrigger>
      <SelectContent>
        {options.map((opt) => (
          <SelectItem key={opt} value={String(opt)}>
            {opt} / page
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  )
}
