import { useState, useRef } from 'react';
import { Upload, FileText, AlertCircle, Check, Download } from 'lucide-react';
import { BulkUploadResult, BulkUploadError, CreateEmployeeDto } from '@/services/api/types';
import { useBulkCreateEmployees } from '@/hooks/api/useEmployees';
import { Modal } from '@/components/ui/Modal';

interface EmployeeBulkUploadModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

type ParsedEmployeesResult = {
  payload: CreateEmployeeDto[];
  errors: BulkUploadError[];
};

const employeeHeaderMap: Record<string, keyof CreateEmployeeDto> = {
  // camelCase / PascalCase
  employeename: 'employeeName',
  email: 'email',
  phone: 'phone',
  employeeid: 'employeeId',
  department: 'department',
  designation: 'designation',
  hiredate: 'hireDate',
  status: 'status',
  bankaccountnumber: 'bankAccountNumber',
  bankname: 'bankName',
  ifsccode: 'ifscCode',
  pannumber: 'panNumber',
  addressline1: 'addressLine1',
  addressline2: 'addressLine2',
  city: 'city',
  state: 'state',
  zipcode: 'zipCode',
  country: 'country',
  contracttype: 'contractType',
  company: 'company',
  companyid: 'companyId',
  // snake_case support (sample CSV)
  employee_name: 'employeeName',
  bank_account_number: 'bankAccountNumber',
  bank_name: 'bankName',
  ifsc_code: 'ifscCode',
  pan_number: 'panNumber',
  address_line1: 'addressLine1',
  address_line2: 'addressLine2',
  zip_code: 'zipCode',
  hire_date: 'hireDate',
  employee_id: 'employeeId',
  contract_type: 'contractType',
  company_id: 'companyId',
  'company id': 'companyId',
  // allow generic id column to flow into employeeId
  id: 'employeeId',
};

const normalizeStatus = (value?: string) => {
  const normalized = (value ?? '').trim().toLowerCase();
  const allowed = ['active', 'inactive', 'terminated', 'permanent'];
  if (allowed.includes(normalized)) return normalized;
  return 'active';
};

const parseEmployeeCsv = async (file: File): Promise<ParsedEmployeesResult> => {
  const toDateOnlyString = (date: Date) => {
    const year = date.getUTCFullYear();
    const month = String(date.getUTCMonth() + 1).padStart(2, '0');
    const day = String(date.getUTCDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  };

  const tryParseDate = (value: string) => {
    const cleaned = value.trim().replace(/,+/g, ' ');

    // Support DD/MM/YYYY or DD-MM-YYYY
    const match = cleaned.match(/^(\d{1,2})[\/-](\d{1,2})[\/-](\d{2,4})$/);
    if (match) {
      const [, d, m, y] = match;
      const year = Number(y.length === 2 ? `20${y}` : y);
      const monthIndex = Number(m) - 1;
      const day = Number(d);
      const date = new Date(Date.UTC(year, monthIndex, day));
      if (!Number.isNaN(date.getTime())) return toDateOnlyString(date);
    }

    const parsed = Date.parse(`${cleaned} UTC`);
    if (!Number.isNaN(parsed)) {
      return toDateOnlyString(new Date(parsed));
    }

    return undefined;
  };

  // CSV helpers to handle quoted commas
  const splitCsvLine = (line: string): string[] => {
    const result: string[] = [];
    let current = '';
    let inQuotes = false;

    for (let i = 0; i < line.length; i++) {
      const char = line[i];
      if (char === '"') {
        if (inQuotes && line[i + 1] === '"') {
          current += '"';
          i++;
        } else {
          inQuotes = !inQuotes;
        }
      } else if (char === ',' && !inQuotes) {
        result.push(current);
        current = '';
      } else {
        current += char;
      }
    }
    result.push(current);
    return result;
  };

  const cleanField = (value: string) => value.trim().replace(/^"(.*)"$/, '$1').replace(/""/g, '"');

  const text = await file.text();
  const lines = text.split(/\r?\n/).filter((line) => line.trim().length > 0);
  if (lines.length < 2) {
    return {
      payload: [],
      errors: [{ rowNumber: 0, errorMessage: 'File is empty', fieldName: undefined }],
    };
  }

    const headers = splitCsvLine(lines[0]).map((h) => cleanField(h).toLowerCase());
  const records: CreateEmployeeDto[] = [];
  const errors: BulkUploadError[] = [];

  for (let i = 1; i < lines.length; i++) {
    const row = lines[i];
    if (!row.trim()) continue;
    const cols = splitCsvLine(row).map(cleanField);
    const raw: Record<string, string> = {};
    headers.forEach((header, idx) => {
      raw[header] = cols[idx]?.trim() || '';
    });

    // Skip rows that are entirely empty (commas only)
    const allEmpty = Object.values(raw).every((v) => v === '');
    if (allEmpty) continue;

    const record: Partial<CreateEmployeeDto> = {
      status: 'active',
      country: 'India',
    };

    // Map fields only when non-empty to avoid model binding issues
    Object.entries(raw).forEach(([header, value]) => {
      const key = employeeHeaderMap[header];
      if (!key) return;
      if (!value) return;
      if (key === 'status') {
        record.status = normalizeStatus(value);
        return;
      }
      if (key === 'hireDate') {
        const dateOnly = tryParseDate(value);
        if (dateOnly) {
          record.hireDate = dateOnly;
        } else {
          errors.push({
            rowNumber: i + 1,
            errorMessage: 'Invalid HireDate format (use YYYY-MM-DD, DD/MM/YYYY, or DD MMM YYYY)',
            fieldName: 'HireDate'
          });
        }
        return;
      }
      // default string fields
      (record as any)[key] = value;
    });

    if (!record.employeeName) {
      errors.push({ rowNumber: i + 1, errorMessage: 'EmployeeName is required', fieldName: 'EmployeeName' });
      continue;
    }

    records.push(record as CreateEmployeeDto);
  }

  return { payload: records, errors };
};

export const EmployeeBulkUploadModal = ({ isOpen, onClose, onSuccess }: EmployeeBulkUploadModalProps) => {
  const [file, setFile] = useState<File | null>(null);
  const [uploadResult, setUploadResult] = useState<BulkUploadResult | null>(null);
  const [isDragOver, setIsDragOver] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const bulkCreate = useBulkCreateEmployees();

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(true);
  };

  const handleDragLeave = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(false);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(false);

    const droppedFiles = Array.from(e.dataTransfer.files);
    const validFile = droppedFiles.find(
      (f) =>
        f.type === 'text/csv' ||
        f.type === 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' ||
        f.type === 'application/vnd.ms-excel',
    );

    if (validFile) {
      setFile(validFile);
      setUploadResult(null);
    }
  };

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0];
    if (selectedFile) {
      setFile(selectedFile);
      setUploadResult(null);
    }
  };

  const handleUpload = async () => {
    if (!file) return;

    try {
      const parsed = await parseEmployeeCsv(file);

      if (parsed.errors.length > 0) {
        setUploadResult({
          successCount: 0,
          failureCount: parsed.errors.length,
          totalCount: parsed.errors.length,
          errors: parsed.errors,
          createdIds: [],
        });
        return;
      }

      const result = await bulkCreate.mutateAsync({
        employees: parsed.payload,
        skipValidationErrors: true,
      });

      setUploadResult(result);

      if (result.failureCount === 0) {
        setTimeout(() => {
          onSuccess();
          handleClose();
        }, 1500);
      }
    } catch (error) {
      console.error('Employee bulk upload failed:', error);
    }
  };

  const handleClose = () => {
    setFile(null);
    setUploadResult(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
    onClose();
  };

  const downloadTemplate = () => {
    const headers = [
      'EmployeeName',
      'Email',
      'Phone',
      'EmployeeId',
      'Department',
      'Designation',
      'HireDate',
      'Status',
      'BankAccountNumber',
      'BankName',
      'IfscCode',
      'PanNumber',
      'AddressLine1',
      'AddressLine2',
      'City',
      'State',
      'ZipCode',
      'Country',
      'ContractType',
      'Company',
      'Company id',
    ];

    const sampleRow = [
      'John Doe',
      'john.doe@example.com',
      '9999999999',
      'EMP-001',
      'Development',
      'Software Engineer',
      '2024-01-15', // YYYY-MM-DD
      'active',
      '1234567890',
      'Kotak Bank',
      'KKBK0000001',
      'ABCDE1234F',
      '123 Main Street',
      'Suite 101',
      'Bangalore',
      'Karnataka',
      '560001',
      'India',
      'Full',
      'Xcdify',
      'ee76a307-15ff-4886-8c6d-dcc4669074e7', // Company id (UUID)
    ];

    const csvContent = [headers.join(','), sampleRow.join(',')].join('\n');
    const blob = new Blob([csvContent], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'employees_template.csv';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
  };

  return (
    <Modal isOpen={isOpen} onClose={handleClose} title="Bulk Upload Employees" size="lg">
      <div className="space-y-6">
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
          <div className="flex items-start space-x-3">
            <FileText className="w-5 h-5 text-blue-600 mt-0.5" />
            <div className="flex-1">
              <h4 className="font-medium text-blue-900">Download Template</h4>
              <p className="text-sm text-blue-700 mt-1">Download the CSV template to format your employee data.</p>
              <button
                onClick={downloadTemplate}
                className="mt-2 inline-flex items-center space-x-2 text-sm text-blue-600 hover:text-blue-800"
              >
                <Download className="w-4 h-4" />
                <span>Download Template</span>
              </button>
            </div>
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">Upload File</label>

          <div
            className={`border-2 border-dashed rounded-lg p-8 text-center transition-colors ${
              isDragOver
                ? 'border-blue-400 bg-blue-50'
                : file
                ? 'border-green-400 bg-green-50'
                : 'border-gray-300 hover:border-gray-400'
            }`}
            onDragOver={handleDragOver}
            onDragLeave={handleDragLeave}
            onDrop={handleDrop}
          >
            {file ? (
              <div className="space-y-2">
                <Check className="w-8 h-8 text-green-600 mx-auto" />
                <p className="text-sm font-medium text-green-700">{file.name}</p>
                <p className="text-xs text-green-600">{(file.size / 1024).toFixed(2)} KB</p>
                <button
                  onClick={() => {
                    setFile(null);
                    if (fileInputRef.current) fileInputRef.current.value = '';
                  }}
                  className="text-sm text-red-600 hover:text-red-800 underline"
                >
                  Remove file
                </button>
              </div>
            ) : (
              <div className="space-y-2">
                <Upload className={`w-8 h-8 mx-auto ${isDragOver ? 'text-blue-600' : 'text-gray-400'}`} />
                <p className="text-sm font-medium text-gray-600">
                  Drop your CSV file here, or{' '}
                  <button onClick={() => fileInputRef.current?.click()} className="text-blue-600 hover:text-blue-800 underline">
                    browse
                  </button>
                </p>
                <p className="text-xs text-gray-500">Supports .csv up to 10MB</p>
              </div>
            )}
          </div>

          <input ref={fileInputRef} type="file" accept=".csv" onChange={handleFileSelect} className="hidden" />
        </div>

        {uploadResult && (
          <div className="space-y-4">
            <div
              className={`border rounded-lg p-4 ${
                uploadResult.failureCount === 0 ? 'border-green-200 bg-green-50' : 'border-yellow-200 bg-yellow-50'
              }`}
            >
              <div className="flex items-start space-x-3">
                {uploadResult.failureCount === 0 ? (
                  <Check className="w-5 h-5 text-green-600 mt-0.5" />
                ) : (
                  <AlertCircle className="w-5 h-5 text-yellow-600 mt-0.5" />
                )}
                <div className="flex-1">
                  <h4 className={`font-medium ${uploadResult.failureCount === 0 ? 'text-green-900' : 'text-yellow-900'}`}>
                    Upload {uploadResult.failureCount === 0 ? 'Completed' : 'Completed with Errors'}
                  </h4>
                  <div className="text-sm mt-1">
                    <p className="text-green-700">✓ {uploadResult.successCount} records processed successfully</p>
                    {uploadResult.failureCount > 0 && <p className="text-red-700">✗ {uploadResult.failureCount} records failed</p>}
                  </div>
                </div>
              </div>
            </div>

            {uploadResult.errors.length > 0 && (
              <div className="border border-red-200 rounded-lg">
                <div className="border-b border-red-200 bg-red-50 px-4 py-2">
                  <h4 className="font-medium text-red-900">Error Details</h4>
                </div>
                <div className="max-h-40 overflow-y-auto">
                  {uploadResult.errors.map((error, index) => (
                    <div key={index} className="px-4 py-3 border-b border-red-100 last:border-b-0">
                      <p className="text-sm font-medium text-red-800">Row {error.rowNumber}</p>
                      <ul className="text-sm text-red-700 mt-1 list-disc list-inside">
                        <li>{error.errorMessage}</li>
                        {error.fieldName && <li>Field: {error.fieldName}</li>}
                      </ul>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        )}

        <div className="bg-gray-50 border border-gray-200 rounded-lg p-4">
          <h4 className="font-medium text-gray-900 mb-2">File Format Requirements:</h4>
          <ul className="text-sm text-gray-700 space-y-1">
            <li>• Required column: EmployeeName (Employee_Name also accepted)</li>
            <li>• Optional columns: EmployeeId/Id, Email, Phone, Department, Designation, HireDate (YYYY-MM-DD or DD MMM YYYY)</li>
            <li>• snake_case headers from template are accepted (e.g. bank_account_number)</li>
            <li>• Status defaults to active if omitted (allows active/inactive/terminated/permanent)</li>
            <li>• ContractType and Company are supported columns</li>
          </ul>
        </div>

        <div className="flex justify-end space-x-3">
          <button
            type="button"
            onClick={handleClose}
            disabled={bulkCreate.isPending}
            className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:opacity-50"
          >
            {uploadResult?.failureCount === 0 ? 'Done' : 'Cancel'}
          </button>
          {!uploadResult && (
            <button
              type="button"
              onClick={handleUpload}
              disabled={!file || bulkCreate.isPending}
              className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:opacity-50"
            >
              {bulkCreate.isPending ? 'Uploading...' : 'Upload'}
            </button>
          )}
        </div>
      </div>
    </Modal>
  );
};
