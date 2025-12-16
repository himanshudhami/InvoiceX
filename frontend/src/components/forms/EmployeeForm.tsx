import { useState, useEffect } from 'react'
import { Employee, CreateEmployeeDto, UpdateEmployeeDto } from '@/services/api/types'
import { useCreateEmployee, useUpdateEmployee, useCheckEmployeeIdUnique, useCheckEmailUnique } from '@/hooks/api/useEmployees'
import { useCompanies } from '@/hooks/api/useCompanies'
import { useCreateOrUpdatePayrollInfo, usePayrollInfo } from '@/features/payroll/hooks'
import { cn } from '@/lib/utils'
import { User, Mail, Phone, MapPin, Building, Calendar, Hash } from 'lucide-react'

interface EmployeeFormProps {
  employee?: Employee
  onSuccess: () => void
  onCancel: () => void
}

export const EmployeeForm = ({ employee, onSuccess, onCancel }: EmployeeFormProps) => {
  const { data: companies = [] } = useCompanies()
  const [formData, setFormData] = useState<CreateEmployeeDto>({
    employeeName: '',
    email: '',
    phone: '',
    employeeId: '',
    department: '',
    designation: '',
    hireDate: '',
    status: 'active',
    bankAccountNumber: '',
    bankName: '',
    ifscCode: '',
    panNumber: '',
    addressLine1: '',
    addressLine2: '',
    city: '',
    state: '',
    zipCode: '',
    country: 'India',
    companyId: '',
    contractType: 'Full',
  })

  const [payrollType, setPayrollType] = useState<'employee' | 'contractor'>('employee')
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [debouncedEmployeeId, setDebouncedEmployeeId] = useState('')
  const [debouncedEmail, setDebouncedEmail] = useState('')

  // Statutory identifier fields (stored in payroll info)
  const [uan, setUan] = useState('')
  const [pfAccountNumber, setPfAccountNumber] = useState('')
  const [esiNumber, setEsiNumber] = useState('')

  const createEmployee = useCreateEmployee()
  const updateEmployee = useUpdateEmployee()
  const createOrUpdatePayrollInfo = useCreateOrUpdatePayrollInfo()
  const { data: existingPayrollInfo } = usePayrollInfo(employee?.id || '', !!employee)
  const isEditing = !!employee
  const isLoading = createEmployee.isPending || updateEmployee.isPending || createOrUpdatePayrollInfo.isPending

  // Load payroll info when editing
  useEffect(() => {
    if (existingPayrollInfo) {
      setPayrollType(existingPayrollInfo.payrollType || 'employee')
      if (!formData.companyId && existingPayrollInfo.companyId) {
        setFormData(prev => ({ ...prev, companyId: existingPayrollInfo.companyId }))
      }
      // Load statutory identifiers
      setUan(existingPayrollInfo.uan || '')
      setPfAccountNumber(existingPayrollInfo.pfAccountNumber || '')
      setEsiNumber(existingPayrollInfo.esiNumber || '')
    }
  }, [existingPayrollInfo])

  // Debounced values for uniqueness checks
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedEmployeeId(formData.employeeId || '')
    }, 500)
    return () => clearTimeout(timer)
  }, [formData.employeeId])

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedEmail(formData.email || '')
    }, 500)
    return () => clearTimeout(timer)
  }, [formData.email])

  // Uniqueness checks
  const { data: isEmployeeIdUnique } = useCheckEmployeeIdUnique(
    debouncedEmployeeId, 
    employee?.id
  )
  
  const { data: isEmailUnique } = useCheckEmailUnique(
    debouncedEmail,
    employee?.id
  )

  // Populate form with existing employee data
  useEffect(() => {
    if (employee) {
      setFormData({
        employeeName: employee.employeeName || '',
        email: employee.email || '',
        phone: employee.phone || '',
        employeeId: employee.employeeId || '',
        department: employee.department || '',
        designation: employee.designation || '',
        hireDate: employee.hireDate?.split('T')[0] || '', // Convert to YYYY-MM-DD format
        status: employee.status || 'active',
        bankAccountNumber: employee.bankAccountNumber || '',
        bankName: employee.bankName || '',
        ifscCode: employee.ifscCode || '',
        panNumber: employee.panNumber || '',
        addressLine1: employee.addressLine1 || '',
        addressLine2: employee.addressLine2 || '',
        city: employee.city || '',
        state: employee.state || '',
        zipCode: employee.zipCode || '',
        country: employee.country || 'India',
        companyId: employee.companyId || '',
        contractType: employee.contractType || 'Full',
      })
    }
  }, [employee])

  const handleInputChange = (field: keyof CreateEmployeeDto, value: string | number) => {
    setFormData(prev => ({ ...prev, [field]: value }))
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: '' }))
    }
  }

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!formData.employeeName.trim()) {
      newErrors.employeeName = 'Employee name is required'
    }

    if (formData.email && !formData.email.includes('@')) {
      newErrors.email = 'Please enter a valid email address'
    }

    if (formData.employeeId && !isEmployeeIdUnique) {
      newErrors.employeeId = 'This employee ID is already in use'
    }

    if (formData.email && !isEmailUnique) {
      newErrors.email = 'This email address is already in use'
    }

    if (formData.panNumber && formData.panNumber.length > 0 && formData.panNumber.length !== 10) {
      newErrors.panNumber = 'PAN number should be 10 characters'
    }

    if (formData.ifscCode && formData.ifscCode.length > 0 && formData.ifscCode.length !== 11) {
      newErrors.ifscCode = 'IFSC code should be 11 characters'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    if (!validateForm()) return

    try {
      let savedEmployeeId: string
      if (isEditing && employee) {
        await updateEmployee.mutateAsync({
          id: employee.id,
          data: formData as UpdateEmployeeDto
        })
        savedEmployeeId = employee.id
      } else {
        const newEmployee = await createEmployee.mutateAsync(formData)
        savedEmployeeId = newEmployee.id
      }

      // Create or update payroll info if company is selected
      if (formData.companyId && savedEmployeeId) {
        try {
          await createOrUpdatePayrollInfo.mutateAsync({
            employeeId: savedEmployeeId,
            companyId: formData.companyId,
            payrollType: payrollType,
            taxRegime: 'new',
            isPfApplicable: payrollType === 'employee',
            isEsiApplicable: false,
            isPtApplicable: payrollType === 'employee',
            dateOfJoining: formData.hireDate || undefined,
            // Statutory identifiers
            uan: uan || undefined,
            pfAccountNumber: pfAccountNumber || undefined,
            esiNumber: esiNumber || undefined,
          })
        } catch (payrollError) {
          console.error('Failed to save payroll info:', payrollError)
          // Don't fail the whole operation if payroll info fails
        }
      }

      onSuccess()
    } catch (error) {
      console.error('Failed to save employee:', error)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-8">
      {/* Personal Information */}
      <div>
        <h3 className="text-lg font-medium text-gray-900 mb-4 flex items-center">
          <User className="w-5 h-5 mr-2" />
          Personal Information
        </h3>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Employee Name *
            </label>
            <input
              type="text"
              value={formData.employeeName}
              onChange={(e) => handleInputChange('employeeName', e.target.value)}
              className={cn(
                'w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500',
                errors.employeeName ? 'border-red-300' : 'border-gray-300'
              )}
              placeholder="Enter employee full name"
            />
            {errors.employeeName && <p className="text-red-500 text-sm mt-1">{errors.employeeName}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Employee ID
            </label>
            <input
              type="text"
              value={formData.employeeId || ''}
              onChange={(e) => handleInputChange('employeeId', e.target.value)}
              className={cn(
                'w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500',
                errors.employeeId ? 'border-red-300' : 'border-gray-300'
              )}
              placeholder="Enter company employee ID"
            />
            {errors.employeeId && <p className="text-red-500 text-sm mt-1">{errors.employeeId}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              <Mail className="w-4 h-4 inline mr-1" />
              Email
            </label>
            <input
              type="email"
              value={formData.email || ''}
              onChange={(e) => handleInputChange('email', e.target.value)}
              className={cn(
                'w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500',
                errors.email ? 'border-red-300' : 'border-gray-300'
              )}
              placeholder="Enter email address"
            />
            {errors.email && <p className="text-red-500 text-sm mt-1">{errors.email}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              <Phone className="w-4 h-4 inline mr-1" />
              Phone
            </label>
            <input
              type="tel"
              value={formData.phone || ''}
              onChange={(e) => handleInputChange('phone', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              placeholder="Enter phone number"
            />
          </div>
        </div>
      </div>

      {/* Work Information */}
      <div>
        <h3 className="text-lg font-medium text-gray-900 mb-4 flex items-center">
          <Building className="w-5 h-5 mr-2" />
          Work Information
        </h3>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Company
            </label>
            <select
              value={formData.companyId || ''}
              onChange={(e) => handleInputChange('companyId', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="">Select company</option>
              {companies.map((company) => (
                <option key={company.id} value={company.id}>
                  {company.name}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Payroll Type
            </label>
            <select
              value={payrollType}
              onChange={(e) => setPayrollType(e.target.value as 'employee' | 'contractor')}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="employee">Employee</option>
              <option value="contractor">Contractor</option>
            </select>
            <p className="text-xs text-gray-500 mt-1">
              {payrollType === 'contractor' 
                ? 'Contractors have simplified payroll without statutory deductions'
                : 'Full-time employees with statutory deductions (PF, ESI, PT, TDS)'}
            </p>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Department
            </label>
            <input
              type="text"
              value={formData.department || ''}
              onChange={(e) => handleInputChange('department', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              placeholder="Enter department"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Designation
            </label>
            <input
              type="text"
              value={formData.designation || ''}
              onChange={(e) => handleInputChange('designation', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              placeholder="Enter job title/designation"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              <Calendar className="w-4 h-4 inline mr-1" />
              Hire Date
            </label>
            <input
              type="date"
              value={formData.hireDate || ''}
              onChange={(e) => handleInputChange('hireDate', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Status
            </label>
            <select
              value={formData.status}
              onChange={(e) => handleInputChange('status', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="active">Active</option>
              <option value="inactive">Inactive</option>
              <option value="terminated">Terminated</option>
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Contract Type
            </label>
            <select
              value={formData.contractType || 'Full'}
              onChange={(e) => handleInputChange('contractType', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="Full">Full-time</option>
              <option value="Part">Part-time</option>
              <option value="Contract">Contract</option>
              <option value="Intern">Intern</option>
            </select>
            <p className="text-xs text-gray-500 mt-1">
              Employment contract type for HR records
            </p>
          </div>
        </div>
      </div>

      {/* Banking Information */}
      <div>
        <h3 className="text-lg font-medium text-gray-900 mb-4 flex items-center">
          <Building className="w-5 h-5 mr-2" />
          Banking Information
        </h3>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Bank Name
            </label>
            <input
              type="text"
              value={formData.bankName || ''}
              onChange={(e) => handleInputChange('bankName', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              placeholder="Enter bank name"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Account Number
            </label>
            <input
              type="text"
              value={formData.bankAccountNumber || ''}
              onChange={(e) => handleInputChange('bankAccountNumber', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              placeholder="Enter account number"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              IFSC Code
            </label>
            <input
              type="text"
              value={formData.ifscCode || ''}
              onChange={(e) => handleInputChange('ifscCode', e.target.value.toUpperCase())}
              className={cn(
                'w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500',
                errors.ifscCode ? 'border-red-300' : 'border-gray-300'
              )}
              placeholder="Enter IFSC code"
              maxLength={11}
            />
            {errors.ifscCode && <p className="text-red-500 text-sm mt-1">{errors.ifscCode}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              <Hash className="w-4 h-4 inline mr-1" />
              PAN Number
            </label>
            <input
              type="text"
              value={formData.panNumber || ''}
              onChange={(e) => handleInputChange('panNumber', e.target.value.toUpperCase())}
              className={cn(
                'w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500',
                errors.panNumber ? 'border-red-300' : 'border-gray-300'
              )}
              placeholder="Enter PAN number"
              maxLength={10}
            />
            {errors.panNumber && <p className="text-red-500 text-sm mt-1">{errors.panNumber}</p>}
          </div>
        </div>
      </div>

      {/* Statutory Identifiers - Only show for employees (not contractors) */}
      {payrollType === 'employee' && (
        <div>
          <h3 className="text-lg font-medium text-gray-900 mb-4 flex items-center">
            <Hash className="w-5 h-5 mr-2" />
            Statutory Identifiers
          </h3>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                UAN (Universal Account Number)
              </label>
              <input
                type="text"
                value={uan}
                onChange={(e) => setUan(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                placeholder="Enter 12-digit UAN"
                maxLength={12}
              />
              <p className="text-xs text-gray-500 mt-1">
                Universal Account Number for PF (from EPFO)
              </p>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                PF Account Number
              </label>
              <input
                type="text"
                value={pfAccountNumber}
                onChange={(e) => setPfAccountNumber(e.target.value.toUpperCase())}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                placeholder="e.g., KN/BLR/12345/123456"
              />
              <p className="text-xs text-gray-500 mt-1">
                PF member ID (State/Region/Establishment/Member)
              </p>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                ESI Number (IP Number)
              </label>
              <input
                type="text"
                value={esiNumber}
                onChange={(e) => setEsiNumber(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                placeholder="Enter ESI IP Number"
                maxLength={17}
              />
              <p className="text-xs text-gray-500 mt-1">
                ESI Insurance Person number (17 digits)
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Address Information */}
      <div>
        <h3 className="text-lg font-medium text-gray-900 mb-4 flex items-center">
          <MapPin className="w-5 h-5 mr-2" />
          Address Information
        </h3>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div className="md:col-span-2">
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Address Line 1
            </label>
            <input
              type="text"
              value={formData.addressLine1 || ''}
              onChange={(e) => handleInputChange('addressLine1', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              placeholder="Enter address line 1"
            />
          </div>

          <div className="md:col-span-2">
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Address Line 2
            </label>
            <input
              type="text"
              value={formData.addressLine2 || ''}
              onChange={(e) => handleInputChange('addressLine2', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              placeholder="Enter address line 2"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              City
            </label>
            <input
              type="text"
              value={formData.city || ''}
              onChange={(e) => handleInputChange('city', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              placeholder="Enter city"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              State
            </label>
            <input
              type="text"
              value={formData.state || ''}
              onChange={(e) => handleInputChange('state', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              placeholder="Enter state"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              ZIP Code
            </label>
            <input
              type="text"
              value={formData.zipCode || ''}
              onChange={(e) => handleInputChange('zipCode', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              placeholder="Enter ZIP code"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Country
            </label>
            <input
              type="text"
              value={formData.country}
              onChange={(e) => handleInputChange('country', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              placeholder="Enter country"
            />
          </div>
        </div>
      </div>

      {/* Form Actions */}
      <div className="flex justify-end space-x-3 pt-6 border-t">
        <button
          type="button"
          onClick={onCancel}
          className="px-6 py-2 text-gray-600 hover:text-gray-800 transition-colors"
        >
          Cancel
        </button>
        <button
          type="submit"
          disabled={isLoading}
          className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {isLoading ? (isEditing ? 'Updating...' : 'Creating...') : (isEditing ? 'Update Employee' : 'Create Employee')}
        </button>
      </div>
    </form>
  )
}