import { useState, useRef } from 'react';
import { Upload, FileText, AlertCircle, Check, Download } from 'lucide-react';
import { BulkUploadResult, BulkUploadError, CreateAssetDto } from '@/services/api/types';
import { useBulkCreateAssets } from '@/hooks/api/useAssets';
import { Modal } from '@/components/ui/Modal';

interface AssetBulkUploadModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

type ParsedAssetsResult = {
  payload: CreateAssetDto[];
  errors: BulkUploadError[];
};

const assetHeaderMap: Record<string, keyof CreateAssetDto> = {
  // camelCase / PascalCase
  companyid: 'companyId',
  categoryid: 'categoryId',
  modelid: 'modelId',
  assettype: 'assetType',
  status: 'status',
  assettag: 'assetTag',
  serialnumber: 'serialNumber',
  name: 'name',
  description: 'description',
  location: 'location',
  vendor: 'vendor',
  purchasetype: 'purchaseType',
  invoicereference: 'invoiceReference',
  purchasedate: 'purchaseDate',
  inservicedate: 'inServiceDate',
  depreciationstartdate: 'depreciationStartDate',
  warrantyexpiration: 'warrantyExpiration',
  purchasecost: 'purchaseCost',
  currency: 'currency',
  depreciationmethod: 'depreciationMethod',
  usefullifemonths: 'usefulLifeMonths',
  salvagevalue: 'salvageValue',
  residualbookvalue: 'residualBookValue',
  notes: 'notes',
  // snake_case support
  company_id: 'companyId',
  category_id: 'categoryId',
  model_id: 'modelId',
  asset_type: 'assetType',
  asset_tag: 'assetTag',
  serial_number: 'serialNumber',
  purchase_type: 'purchaseType',
  invoice_reference: 'invoiceReference',
  purchase_date: 'purchaseDate',
  in_service_date: 'inServiceDate',
  depreciation_start_date: 'depreciationStartDate',
  warranty_expiration: 'warrantyExpiration',
  purchase_cost: 'purchaseCost',
  depreciation_method: 'depreciationMethod',
  useful_life_months: 'usefulLifeMonths',
  salvage_value: 'salvageValue',
  residual_book_value: 'residualBookValue',
};

const normalizeStatus = (value?: string) => {
  const normalized = (value ?? '').trim().toLowerCase();
  const allowed = ['available', 'assigned', 'maintenance', 'retired', 'reserved', 'lost'];
  if (allowed.includes(normalized)) return normalized;
  return 'available';
};

const normalizeAssetType = (value?: string) => {
  const normalized = (value ?? '').trim();
  const allowed = ['IT_Asset', 'Fixed_Asset', 'Intangible_Asset'];
  if (allowed.includes(normalized)) return normalized;
  return 'IT_Asset';
};

const normalizePurchaseType = (value?: string) => {
  const normalized = (value ?? '').trim().toLowerCase();
  const allowed = ['capex', 'opex'];
  if (allowed.includes(normalized)) return normalized;
  return 'capex';
};

const normalizeDepreciationMethod = (value?: string) => {
  const normalized = (value ?? '').trim().toLowerCase();
  const allowed = ['none', 'straight_line', 'double_declining', 'sum_of_years_digits'];
  if (allowed.includes(normalized)) return normalized;
  return 'none';
};

const parseAssetCsv = async (file: File): Promise<ParsedAssetsResult> => {
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

    // Support YYYY-MM-DD
    const isoMatch = cleaned.match(/^(\d{4})-(\d{1,2})-(\d{1,2})$/);
    if (isoMatch) {
      const [, y, m, d] = isoMatch;
      const year = Number(y);
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
  const records: CreateAssetDto[] = [];
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

    const record: Partial<CreateAssetDto> = {
      assetType: 'IT_Asset',
      status: 'available',
      purchaseType: 'capex',
      currency: 'USD',
      depreciationMethod: 'none',
    };

    // Map fields only when non-empty to avoid model binding issues
    Object.entries(raw).forEach(([header, value]) => {
      const key = assetHeaderMap[header];
      if (!key) return;
      if (!value) return;

      if (key === 'status') {
        record.status = normalizeStatus(value);
        return;
      }
      if (key === 'assetType') {
        record.assetType = normalizeAssetType(value);
        return;
      }
      if (key === 'purchaseType') {
        record.purchaseType = normalizePurchaseType(value);
        return;
      }
      if (key === 'depreciationMethod') {
        record.depreciationMethod = normalizeDepreciationMethod(value);
        return;
      }
      if (key === 'companyId' || key === 'categoryId' || key === 'modelId') {
        // Try to parse as GUID
        try {
          (record as any)[key] = value;
        } catch {
          errors.push({
            rowNumber: i + 1,
            errorMessage: `Invalid ${key} format (must be a valid UUID)`,
            fieldName: key,
          });
        }
        return;
      }
      if (key === 'purchaseDate' || key === 'inServiceDate' || key === 'depreciationStartDate' || key === 'warrantyExpiration') {
        const dateOnly = tryParseDate(value);
        if (dateOnly) {
          (record as any)[key] = dateOnly;
        } else {
          errors.push({
            rowNumber: i + 1,
            errorMessage: `Invalid ${key} format (use YYYY-MM-DD, DD/MM/YYYY, or DD MMM YYYY)`,
            fieldName: key,
          });
        }
        return;
      }
      if (key === 'purchaseCost' || key === 'salvageValue' || key === 'residualBookValue') {
        const num = parseFloat(value);
        if (!Number.isNaN(num)) {
          (record as any)[key] = num;
        }
        return;
      }
      if (key === 'usefulLifeMonths') {
        const num = parseInt(value, 10);
        if (!Number.isNaN(num)) {
          (record as any)[key] = num;
        }
        return;
      }
      // default string fields
      (record as any)[key] = value;
    });

    if (!record.assetTag) {
      errors.push({ rowNumber: i + 1, errorMessage: 'AssetTag is required', fieldName: 'AssetTag' });
      continue;
    }

    if (!record.name) {
      errors.push({ rowNumber: i + 1, errorMessage: 'Name is required', fieldName: 'Name' });
      continue;
    }

    if (!record.companyId) {
      errors.push({ rowNumber: i + 1, errorMessage: 'CompanyId is required', fieldName: 'CompanyId' });
      continue;
    }

    records.push(record as CreateAssetDto);
  }

  return { payload: records, errors };
};

export const AssetBulkUploadModal = ({ isOpen, onClose, onSuccess }: AssetBulkUploadModalProps) => {
  const [file, setFile] = useState<File | null>(null);
  const [uploadResult, setUploadResult] = useState<BulkUploadResult | null>(null);
  const [isDragOver, setIsDragOver] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const bulkCreate = useBulkCreateAssets();

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
      const parsed = await parseAssetCsv(file);

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
        assets: parsed.payload,
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
      console.error('Asset bulk upload failed:', error);
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
      'CompanyId',
      'AssetTag',
      'Name',
      'AssetType',
      'Status',
      'SerialNumber',
      'Description',
      'Location',
      'Vendor',
      'PurchaseType',
      'InvoiceReference',
      'PurchaseDate',
      'InServiceDate',
      'DepreciationStartDate',
      'WarrantyExpiration',
      'PurchaseCost',
      'Currency',
      'DepreciationMethod',
      'UsefulLifeMonths',
      'SalvageValue',
      'ResidualBookValue',
      'Notes',
    ];

    const sampleRow = [
      '00000000-0000-0000-0000-000000000001', // CompanyId (UUID)
      'ASSET-001',
      'Laptop Dell XPS 15',
      'IT_Asset',
      'available',
      'SN123456789',
      'High-performance laptop for development',
      'Office Floor 2',
      'Dell Inc',
      'capex',
      'INV-2024-001',
      '2024-01-15', // YYYY-MM-DD
      '2024-01-20',
      '2024-01-20',
      '2027-01-15',
      '1500.00',
      'USD',
      'straight_line',
      '36',
      '300.00',
      '',
      'Initial purchase for development team',
    ];

    const csvContent = [headers.join(','), sampleRow.join(',')].join('\n');
    const blob = new Blob([csvContent], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'assets_template.csv';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
  };

  return (
    <Modal isOpen={isOpen} onClose={handleClose} title="Bulk Upload Assets" size="lg">
      <div className="space-y-6">
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
          <div className="flex items-start space-x-3">
            <FileText className="w-5 h-5 text-blue-600 mt-0.5" />
            <div className="flex-1">
              <h4 className="font-medium text-blue-900">Download Template</h4>
              <p className="text-sm text-blue-700 mt-1">Download the CSV template to format your asset data.</p>
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
            <li>• Required columns: CompanyId (UUID), AssetTag, Name</li>
            <li>• Optional columns: CategoryId, ModelId, AssetType (IT_Asset/Fixed_Asset/Intangible_Asset), Status, SerialNumber, Description, Location, Vendor</li>
            <li>• PurchaseType: capex or opex (defaults to capex)</li>
            <li>• Currency: USD, EUR, GBP, INR, CAD, AUD (defaults to USD)</li>
            <li>• DepreciationMethod: none, straight_line, double_declining, sum_of_years_digits (defaults to none)</li>
            <li>• Date formats: YYYY-MM-DD, DD/MM/YYYY, or DD MMM YYYY</li>
            <li>• snake_case headers from template are accepted (e.g. asset_tag, purchase_date)</li>
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




