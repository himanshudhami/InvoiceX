import { useState, useCallback, useMemo } from 'react'
import { useSearchParams, Link } from 'react-router-dom'
import { useBankAccounts } from '@/hooks/api/useBankAccounts'
import { useImportBankTransactions } from '@/hooks/api/useBankTransactions'
import {
  ImportBankTransactionDto,
  ImportBankTransactionsResult
} from '@/services/api/types'
import {
  BANK_CSV_MAPPINGS,
  BankType,
  parseCSVRow
} from '@/services/api/finance/banking/bankTransactionService'
import {
  Upload,
  FileSpreadsheet,
  AlertCircle,
  CheckCircle,
  ArrowLeft,
  Trash2,
  RefreshCw
} from 'lucide-react'
import Papa from 'papaparse'

interface ParsedRow {
  transactionDate: string
  valueDate?: string
  description?: string
  referenceNumber?: string
  chequeNumber?: string
  transactionType: 'credit' | 'debit'
  amount: number
  balanceAfter?: number
  rawData: string
  isValid: boolean
  error?: string
}

const BankStatementImport = () => {
  const [searchParams] = useSearchParams()
  const preselectedAccountId = searchParams.get('accountId')

  const [selectedAccountId, setSelectedAccountId] = useState(preselectedAccountId || '')
  const [selectedBankType, setSelectedBankType] = useState<BankType | 'custom'>('HDFC')
  const [parsedRows, setParsedRows] = useState<ParsedRow[]>([])
  const [parseError, setParseError] = useState<string | null>(null)
  const [importResult, setImportResult] = useState<ImportBankTransactionsResult | null>(null)
  const [skipDuplicates, setSkipDuplicates] = useState(true)

  // Custom column mapping
  const [customMapping, setCustomMapping] = useState({
    dateColumn: '',
    descriptionColumn: '',
    chequeColumn: '',
    withdrawalColumn: '',
    depositColumn: '',
    balanceColumn: '',
    dateFormat: 'DD/MM/YYYY'
  })
  const [availableColumns, setAvailableColumns] = useState<string[]>([])

  const { data: bankAccounts = [], isLoading: accountsLoading } = useBankAccounts()
  const importTransactions = useImportBankTransactions()

  const selectedAccount = bankAccounts.find(a => a.id === selectedAccountId)

  const handleFileUpload = useCallback((event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    if (!file) return

    setParseError(null)
    setImportResult(null)
    setParsedRows([])

    // First parse without headers to handle CSVs with info rows before column headers
    Papa.parse(file, {
      header: false,
      skipEmptyLines: true,
      complete: (results) => {
        if (results.errors.length > 0) {
          setParseError(`CSV parsing error: ${results.errors[0].message}`)
          return
        }

        const rawData = results.data as string[][]
        if (rawData.length === 0) {
          setParseError('No data found in CSV file')
          return
        }

        // Get the expected date column name for the selected bank
        const mapping = BANK_CSV_MAPPINGS[selectedBankType]
        const expectedDateColumn = mapping?.dateColumn || 'Date'

        // Find the header row by looking for the date column name
        let headerRowIndex = 0
        for (let i = 0; i < Math.min(rawData.length, 20); i++) {
          const row = rawData[i]
          if (row.some(cell => cell && cell.trim() === expectedDateColumn)) {
            headerRowIndex = i
            break
          }
        }

        // Extract headers and data - handle duplicate column names by adding suffix
        const rawHeaders = rawData[headerRowIndex].map(h => h?.trim() || '')
        const headerCounts: Record<string, number> = {}
        const headers = rawHeaders.map(header => {
          if (!header) return ''
          // Track occurrences of each header
          headerCounts[header] = (headerCounts[header] || 0) + 1
          // If this is a duplicate, add suffix
          if (headerCounts[header] > 1) {
            return `${header}_${headerCounts[header]}`
          }
          return header
        })
        const dataRows = rawData.slice(headerRowIndex + 1)

        // Convert to objects with headers as keys
        const data: Record<string, string>[] = dataRows
          .filter(row => row.length > 0 && row.some(cell => cell && cell.trim()))
          .map(row => {
            const obj: Record<string, string> = {}
            headers.forEach((header, index) => {
              if (header) {
                obj[header] = row[index] || ''
              }
            })
            return obj
          })

        if (data.length === 0) {
          setParseError('No data found in CSV file')
          return
        }

        // Set available columns for custom mapping
        setAvailableColumns(headers.filter(h => h))

        // If custom mapping, wait for user to configure columns
        if (selectedBankType === 'custom') {
          setParseError('Please configure column mappings below, then click "Apply Mapping"')
          return
        }

        const parsed: ParsedRow[] = []

        for (let i = 0; i < data.length; i++) {
          const row = data[i]
          try {
            const result = parseCSVRow(row, mapping)
            if (result) {
              parsed.push({
                ...result,
                isValid: true
              })
            }
          } catch (err) {
            parsed.push({
              transactionDate: '',
              transactionType: 'debit',
              amount: 0,
              rawData: JSON.stringify(row),
              isValid: false,
              error: `Row ${i + headerRowIndex + 2}: ${err instanceof Error ? err.message : 'Parse error'}`
            })
          }
        }

        if (parsed.length === 0) {
          setParseError('No valid transactions found. Please check the bank type selection matches your CSV format.')
          return
        }

        setParsedRows(parsed)
      },
      error: (error) => {
        setParseError(`Failed to read file: ${error.message}`)
      }
    })

    // Reset file input
    event.target.value = ''
  }, [selectedBankType])

  const applyCustomMapping = useCallback(() => {
    if (!customMapping.dateColumn || !customMapping.withdrawalColumn || !customMapping.depositColumn) {
      setParseError('Please select at least Date, Withdrawal, and Deposit columns')
      return
    }

    setParseError(null)

    // Re-read the file - but we don't have it anymore.
    // Instead, we'll need the user to upload again
    setParseError('Please upload the CSV file again after configuring the mapping')
  }, [customMapping])

  const removeRow = useCallback((index: number) => {
    setParsedRows(prev => prev.filter((_, i) => i !== index))
  }, [])

  const validRows = useMemo(() => parsedRows.filter(r => r.isValid), [parsedRows])
  const invalidRows = useMemo(() => parsedRows.filter(r => !r.isValid), [parsedRows])

  const handleImport = async () => {
    if (!selectedAccountId) {
      setParseError('Please select a bank account')
      return
    }

    if (validRows.length === 0) {
      setParseError('No valid transactions to import')
      return
    }

    const transactions: ImportBankTransactionDto[] = validRows.map(row => ({
      transactionDate: row.transactionDate,
      valueDate: row.valueDate,
      description: row.description,
      referenceNumber: row.referenceNumber,
      chequeNumber: row.chequeNumber,
      transactionType: row.transactionType,
      amount: row.amount,
      balanceAfter: row.balanceAfter,
      rawData: row.rawData
    }))

    try {
      const result = await importTransactions.mutateAsync({
        bankAccountId: selectedAccountId,
        transactions,
        skipDuplicates
      })
      setImportResult(result)
      setParsedRows([])
    } catch (error) {
      setParseError(`Import failed: ${error instanceof Error ? error.message : 'Unknown error'}`)
    }
  }

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: selectedAccount?.currency || 'INR',
      maximumFractionDigits: 2,
    }).format(amount)
  }

  const totalCredits = validRows.filter(r => r.transactionType === 'credit').reduce((sum, r) => sum + r.amount, 0)
  const totalDebits = validRows.filter(r => r.transactionType === 'debit').reduce((sum, r) => sum + r.amount, 0)

  if (accountsLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Link
          to="/bank/accounts"
          className="p-2 text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded-lg"
        >
          <ArrowLeft className="h-5 w-5" />
        </Link>
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Import Bank Statement</h1>
          <p className="text-gray-600 mt-1">Upload CSV file to import bank transactions</p>
        </div>
      </div>

      {/* Import Result */}
      {importResult && (
        <div className="bg-green-50 border border-green-200 rounded-lg p-6">
          <div className="flex items-start gap-3">
            <CheckCircle className="h-6 w-6 text-green-600 flex-shrink-0" />
            <div>
              <h3 className="font-medium text-green-800">Import Completed</h3>
              <div className="mt-2 text-sm text-green-700 space-y-1">
                <p><strong>{importResult.importedCount}</strong> transactions imported</p>
                {importResult.skippedCount > 0 && (
                  <p><strong>{importResult.skippedCount}</strong> duplicates skipped</p>
                )}
                {importResult.failedCount > 0 && (
                  <p className="text-red-600"><strong>{importResult.failedCount}</strong> failed</p>
                )}
                <p className="text-xs text-green-600">Batch ID: {importResult.batchId}</p>
              </div>
              {importResult.errors.length > 0 && (
                <div className="mt-3 text-sm text-red-600">
                  <p className="font-medium">Errors:</p>
                  <ul className="list-disc list-inside">
                    {importResult.errors.map((err, i) => (
                      <li key={i}>{err}</li>
                    ))}
                  </ul>
                </div>
              )}
              <button
                onClick={() => setImportResult(null)}
                className="mt-3 text-sm text-green-600 hover:text-green-800"
              >
                Import another file
              </button>
            </div>
          </div>
        </div>
      )}

      {!importResult && (
        <>
          {/* Configuration */}
          <div className="bg-white rounded-lg shadow p-6 space-y-4">
            <h2 className="text-lg font-medium text-gray-900">Import Settings</h2>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {/* Bank Account Selection */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Bank Account *
                </label>
                <select
                  value={selectedAccountId}
                  onChange={(e) => setSelectedAccountId(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                >
                  <option value="">Select a bank account</option>
                  {bankAccounts.filter(a => a.isActive).map((account) => (
                    <option key={account.id} value={account.id}>
                      {account.accountName} - {account.bankName} ({account.accountNumber.slice(-4)})
                    </option>
                  ))}
                </select>
              </div>

              {/* Bank Type Selection */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Bank CSV Format *
                </label>
                <select
                  value={selectedBankType}
                  onChange={(e) => setSelectedBankType(e.target.value as BankType | 'custom')}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                >
                  {Object.keys(BANK_CSV_MAPPINGS).map((bank) => (
                    <option key={bank} value={bank}>{bank}</option>
                  ))}
                  <option value="custom">Custom Mapping</option>
                </select>
              </div>
            </div>

            {/* Custom Mapping */}
            {selectedBankType === 'custom' && availableColumns.length > 0 && (
              <div className="border-t pt-4 mt-4">
                <h3 className="text-sm font-medium text-gray-700 mb-3">Custom Column Mapping</h3>
                <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
                  <div>
                    <label className="block text-xs text-gray-500 mb-1">Date Column *</label>
                    <select
                      value={customMapping.dateColumn}
                      onChange={(e) => setCustomMapping(prev => ({ ...prev, dateColumn: e.target.value }))}
                      className="w-full px-2 py-1 text-sm border rounded"
                    >
                      <option value="">Select column</option>
                      {availableColumns.map(col => (
                        <option key={col} value={col}>{col}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="block text-xs text-gray-500 mb-1">Description</label>
                    <select
                      value={customMapping.descriptionColumn}
                      onChange={(e) => setCustomMapping(prev => ({ ...prev, descriptionColumn: e.target.value }))}
                      className="w-full px-2 py-1 text-sm border rounded"
                    >
                      <option value="">Select column</option>
                      {availableColumns.map(col => (
                        <option key={col} value={col}>{col}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="block text-xs text-gray-500 mb-1">Withdrawal Column *</label>
                    <select
                      value={customMapping.withdrawalColumn}
                      onChange={(e) => setCustomMapping(prev => ({ ...prev, withdrawalColumn: e.target.value }))}
                      className="w-full px-2 py-1 text-sm border rounded"
                    >
                      <option value="">Select column</option>
                      {availableColumns.map(col => (
                        <option key={col} value={col}>{col}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="block text-xs text-gray-500 mb-1">Deposit Column *</label>
                    <select
                      value={customMapping.depositColumn}
                      onChange={(e) => setCustomMapping(prev => ({ ...prev, depositColumn: e.target.value }))}
                      className="w-full px-2 py-1 text-sm border rounded"
                    >
                      <option value="">Select column</option>
                      {availableColumns.map(col => (
                        <option key={col} value={col}>{col}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="block text-xs text-gray-500 mb-1">Balance Column</label>
                    <select
                      value={customMapping.balanceColumn}
                      onChange={(e) => setCustomMapping(prev => ({ ...prev, balanceColumn: e.target.value }))}
                      className="w-full px-2 py-1 text-sm border rounded"
                    >
                      <option value="">Select column</option>
                      {availableColumns.map(col => (
                        <option key={col} value={col}>{col}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="block text-xs text-gray-500 mb-1">Date Format</label>
                    <select
                      value={customMapping.dateFormat}
                      onChange={(e) => setCustomMapping(prev => ({ ...prev, dateFormat: e.target.value }))}
                      className="w-full px-2 py-1 text-sm border rounded"
                    >
                      <option value="DD/MM/YYYY">DD/MM/YYYY</option>
                      <option value="DD-MM-YYYY">DD-MM-YYYY</option>
                      <option value="DD/MM/YY">DD/MM/YY</option>
                      <option value="DD MMM YYYY">DD MMM YYYY</option>
                      <option value="YYYY-MM-DD">YYYY-MM-DD</option>
                    </select>
                  </div>
                </div>
                <button
                  onClick={applyCustomMapping}
                  className="mt-3 px-4 py-2 text-sm bg-gray-100 hover:bg-gray-200 rounded-md"
                >
                  Apply Mapping
                </button>
              </div>
            )}

            {/* Options */}
            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                id="skipDuplicates"
                checked={skipDuplicates}
                onChange={(e) => setSkipDuplicates(e.target.checked)}
                className="rounded border-gray-300"
              />
              <label htmlFor="skipDuplicates" className="text-sm text-gray-700">
                Skip duplicate transactions (based on date, amount, and description)
              </label>
            </div>
          </div>

          {/* File Upload */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-medium text-gray-900 mb-4">Upload CSV File</h2>
            <div className="border-2 border-dashed border-gray-300 rounded-lg p-8 text-center hover:border-blue-400 transition-colors">
              <input
                type="file"
                accept=".csv"
                onChange={handleFileUpload}
                className="hidden"
                id="csvUpload"
                disabled={!selectedAccountId}
              />
              <label
                htmlFor="csvUpload"
                className={`cursor-pointer ${!selectedAccountId ? 'opacity-50 cursor-not-allowed' : ''}`}
              >
                <FileSpreadsheet className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                <p className="text-gray-600 mb-2">
                  {selectedAccountId
                    ? 'Click to upload or drag and drop'
                    : 'Please select a bank account first'
                  }
                </p>
                <p className="text-sm text-gray-500">CSV files only</p>
              </label>
            </div>
          </div>

          {/* Parse Error */}
          {parseError && (
            <div className="bg-red-50 border border-red-200 rounded-lg p-4">
              <div className="flex items-center gap-2">
                <AlertCircle className="h-5 w-5 text-red-600" />
                <p className="text-red-700">{parseError}</p>
              </div>
            </div>
          )}

          {/* Preview */}
          {parsedRows.length > 0 && (
            <div className="bg-white rounded-lg shadow">
              <div className="p-6 border-b">
                <div className="flex justify-between items-center">
                  <div>
                    <h2 className="text-lg font-medium text-gray-900">Preview</h2>
                    <p className="text-sm text-gray-500 mt-1">
                      {validRows.length} valid transactions, {invalidRows.length} errors
                    </p>
                  </div>
                  <div className="flex items-center gap-4">
                    <div className="text-right">
                      <p className="text-sm text-gray-500">Total Credits</p>
                      <p className="text-lg font-semibold text-green-600">{formatCurrency(totalCredits)}</p>
                    </div>
                    <div className="text-right">
                      <p className="text-sm text-gray-500">Total Debits</p>
                      <p className="text-lg font-semibold text-red-600">{formatCurrency(totalDebits)}</p>
                    </div>
                  </div>
                </div>
              </div>

              {/* Transaction Table */}
              <div className="overflow-x-auto max-h-96 overflow-y-auto">
                <table className="w-full text-sm">
                  <thead className="bg-gray-50 sticky top-0">
                    <tr>
                      <th className="px-4 py-3 text-left font-medium text-gray-500">Date</th>
                      <th className="px-4 py-3 text-left font-medium text-gray-500">Description</th>
                      <th className="px-4 py-3 text-left font-medium text-gray-500">Type</th>
                      <th className="px-4 py-3 text-right font-medium text-gray-500">Amount</th>
                      <th className="px-4 py-3 text-right font-medium text-gray-500">Balance</th>
                      <th className="px-4 py-3 text-center font-medium text-gray-500">Status</th>
                      <th className="px-4 py-3"></th>
                    </tr>
                  </thead>
                  <tbody className="divide-y">
                    {parsedRows.map((row, index) => (
                      <tr key={index} className={row.isValid ? '' : 'bg-red-50'}>
                        <td className="px-4 py-3 whitespace-nowrap">
                          {row.transactionDate ? new Date(row.transactionDate).toLocaleDateString('en-IN') : '-'}
                        </td>
                        <td className="px-4 py-3 max-w-xs truncate" title={row.description}>
                          {row.description || '-'}
                        </td>
                        <td className="px-4 py-3">
                          <span className={`inline-flex px-2 py-0.5 text-xs font-medium rounded ${
                            row.transactionType === 'credit'
                              ? 'bg-green-100 text-green-700'
                              : 'bg-red-100 text-red-700'
                          }`}>
                            {row.transactionType}
                          </span>
                        </td>
                        <td className={`px-4 py-3 text-right font-medium ${
                          row.transactionType === 'credit' ? 'text-green-600' : 'text-red-600'
                        }`}>
                          {formatCurrency(row.amount)}
                        </td>
                        <td className="px-4 py-3 text-right text-gray-500">
                          {row.balanceAfter ? formatCurrency(row.balanceAfter) : '-'}
                        </td>
                        <td className="px-4 py-3 text-center">
                          {row.isValid ? (
                            <CheckCircle className="h-4 w-4 text-green-500 mx-auto" />
                          ) : (
                            <span title={row.error}><AlertCircle className="h-4 w-4 text-red-500 mx-auto" /></span>
                          )}
                        </td>
                        <td className="px-4 py-3">
                          <button
                            onClick={() => removeRow(index)}
                            className="text-gray-400 hover:text-red-600"
                          >
                            <Trash2 className="h-4 w-4" />
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {/* Import Button */}
              <div className="p-6 border-t bg-gray-50">
                <div className="flex justify-between items-center">
                  <button
                    onClick={() => setParsedRows([])}
                    className="px-4 py-2 text-gray-600 hover:text-gray-800"
                  >
                    Clear
                  </button>
                  <button
                    onClick={handleImport}
                    disabled={validRows.length === 0 || importTransactions.isPending}
                    className="inline-flex items-center px-6 py-2 bg-blue-600 text-white font-medium rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    {importTransactions.isPending ? (
                      <>
                        <RefreshCw className="h-4 w-4 mr-2 animate-spin" />
                        Importing...
                      </>
                    ) : (
                      <>
                        <Upload className="h-4 w-4 mr-2" />
                        Import {validRows.length} Transactions
                      </>
                    )}
                  </button>
                </div>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  )
}

export default BankStatementImport
