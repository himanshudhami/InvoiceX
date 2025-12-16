/**
 * Base Excel Export Utility
 * 
 * Single Responsibility: Excel file generation and download
 * Follows DRY principle - reusable across the application
 */

import * as XLSX from 'xlsx'
import type { WorkBook, WorkSheet } from 'xlsx'
import { saveAs } from 'file-saver'

export interface ExcelColumn {
  header: string
  key: string
  width?: number
  format?: (value: any) => string | number
}

export interface ExcelSheet {
  name: string
  columns: ExcelColumn[]
  data: any[]
}

export interface ExcelExportOptions {
  filename: string
  sheets: ExcelSheet[]
  dateFormat?: string
}

/**
 * Formats a value for Excel export
 */
function formatValue(value: any, format?: (value: any) => string | number): any {
  if (value === null || value === undefined) {
    return ''
  }
  
  if (format) {
    return format(value)
  }
  
  // Auto-detect and format dates
  if (value instanceof Date) {
    return value.toISOString().split('T')[0]
  }
  
  if (typeof value === 'string' && /^\d{4}-\d{2}-\d{2}/.test(value)) {
    // ISO date string
    return value.split('T')[0]
  }
  
  return value
}

/**
 * Creates an Excel workbook from the provided sheets
 */
export function createExcelWorkbook(options: ExcelExportOptions): XLSX.WorkBook {
  const workbook = XLSX.utils.book_new()

  options.sheets.forEach((sheet) => {
    // Prepare data with formatted values
    const formattedData = sheet.data.map((row) => {
      const formattedRow: Record<string, any> = {}
      sheet.columns.forEach((column) => {
        const value = row[column.key]
        formattedRow[column.header] = formatValue(value, column.format)
      })
      return formattedRow
    })

    // Create worksheet from formatted data
    const worksheet = XLSX.utils.json_to_sheet(formattedData)

    // Set column widths if specified
    if (sheet.columns.some((col) => col.width)) {
      const colWidths = sheet.columns.map((col) => ({
        wch: col.width || 15,
      }))
      worksheet['!cols'] = colWidths
    }

    // Add worksheet to workbook
    XLSX.utils.book_append_sheet(workbook, worksheet, sheet.name)
  })

  return workbook
}

/**
 * Exports data to Excel file and triggers download
 */
export function exportToExcel(options: ExcelExportOptions): void {
  const workbook = createExcelWorkbook(options)
  
  // Generate Excel file buffer
  const excelBuffer = XLSX.write(workbook, {
    bookType: 'xlsx',
    type: 'array',
  })
  
  // Create blob and download
  const blob = new Blob([excelBuffer], {
    type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  })
  
  const filename = options.filename.endsWith('.xlsx')
    ? options.filename
    : `${options.filename}.xlsx`
  
  saveAs(blob, filename)
}

/**
 * Exports a single sheet to Excel
 */
export function exportSheetToExcel(
  filename: string,
  columns: ExcelColumn[],
  data: any[]
): void {
  exportToExcel({
    filename,
    sheets: [
      {
        name: 'Sheet1',
        columns,
        data,
      },
    ],
  })
}

