import { FC } from 'react'
import EditableInput from './EditableInput'

interface Props {
  value: string
  onChange: (value: string) => void
  pdfMode?: boolean
}

const ProjectSelector: FC<Props> = ({ value, onChange, pdfMode }) => {
  return (
    <EditableInput
      placeholder="Enter project name"
      value={value}
      onChange={onChange}
      pdfMode={pdfMode}
    />
  )
}

export default ProjectSelector