import React from 'react'
import { Document, Page, Text, View, StyleSheet, pdf } from '@react-pdf/renderer'
import { PayrollTransaction } from '@/features/payroll/types/payroll'
import { Employee } from '@/services/api/types'
import { format } from 'date-fns'
import { saveAs } from 'file-saver'

const styles = StyleSheet.create({
  page: {
    flexDirection: 'column',
    backgroundColor: '#ffffff',
    padding: 40,
    fontFamily: 'Helvetica',
  },
  header: {
    marginBottom: 20,
    textAlign: 'center',
  },
  companyName: {
    fontSize: 20,
    fontWeight: 'bold',
    marginBottom: 8,
  },
  title: {
    fontSize: 16,
    fontWeight: 'bold',
    marginBottom: 4,
  },
  period: {
    fontSize: 10,
    color: '#666',
  },
  section: {
    marginBottom: 15,
  },
  sectionTitle: {
    fontSize: 12,
    fontWeight: 'bold',
    marginBottom: 8,
    borderBottomWidth: 1,
    borderBottomColor: '#ccc',
    paddingBottom: 4,
  },
  row: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 4,
    fontSize: 10,
  },
  label: {
    color: '#666',
  },
  value: {
    fontWeight: 'bold',
  },
  totalRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginTop: 8,
    paddingTop: 8,
    borderTopWidth: 1,
    borderTopColor: '#ccc',
    fontSize: 12,
    fontWeight: 'bold',
  },
  netPayBox: {
    marginTop: 15,
    padding: 10,
    backgroundColor: '#E3F2FD',
    borderRadius: 4,
    textAlign: 'center',
  },
  netPayLabel: {
    fontSize: 10,
    color: '#666',
    marginBottom: 4,
  },
  netPayValue: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#1976D2',
  },
})

interface PayslipPDFProps {
  transaction: PayrollTransaction
  employee?: Employee
  company?: { name: string }
}

const PayslipPDFDocument: React.FC<PayslipPDFProps> = ({
  transaction,
  employee,
  company,
}) => {
  const getMonthYear = (month: number, year: number) => {
    const date = new Date(year, month - 1, 1)
    return format(date, 'MMMM yyyy')
  }

  const formatCurrency = (amount: number) => {
    return `₹${amount.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
  }

  return (
    <Document>
      <Page size="A4" style={styles.page}>
        <View style={styles.header}>
          <Text style={styles.companyName}>{company?.name || 'Company Name'}</Text>
          <Text style={styles.title}>Salary Slip</Text>
          <Text style={styles.period}>{getMonthYear(transaction.payrollMonth, transaction.payrollYear)}</Text>
        </View>

        {/* Employee Details */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Employee Details</Text>
          <View style={styles.row}>
            <Text style={styles.label}>Name:</Text>
            <Text style={styles.value}>{transaction.employeeName || employee?.employeeName || '—'}</Text>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>Employee ID:</Text>
            <Text style={styles.value}>{employee?.employeeId || '—'}</Text>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>PAN:</Text>
            <Text style={styles.value}>—</Text>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>UAN:</Text>
            <Text style={styles.value}>—</Text>
          </View>
        </View>

        {/* Attendance */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Attendance</Text>
          <View style={styles.row}>
            <Text style={styles.label}>Working Days:</Text>
            <Text style={styles.value}>{transaction.workingDays}</Text>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>Present Days:</Text>
            <Text style={styles.value}>{transaction.presentDays}</Text>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>LOP Days:</Text>
            <Text style={styles.value}>{transaction.lopDays}</Text>
          </View>
        </View>

        {/* Earnings */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Earnings</Text>
          <View style={styles.row}>
            <Text style={styles.label}>Basic Salary</Text>
            <Text style={styles.value}>{formatCurrency(transaction.basicEarned)}</Text>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>HRA</Text>
            <Text style={styles.value}>{formatCurrency(transaction.hraEarned)}</Text>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>Dearness Allowance</Text>
            <Text style={styles.value}>{formatCurrency(transaction.daEarned)}</Text>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>Conveyance Allowance</Text>
            <Text style={styles.value}>{formatCurrency(transaction.conveyanceEarned)}</Text>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>Medical Allowance</Text>
            <Text style={styles.value}>{formatCurrency(transaction.medicalEarned)}</Text>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>Special Allowance</Text>
            <Text style={styles.value}>{formatCurrency(transaction.specialAllowanceEarned)}</Text>
          </View>
          {transaction.bonusPaid > 0 && (
            <View style={styles.row}>
              <Text style={styles.label}>Bonus</Text>
              <Text style={styles.value}>{formatCurrency(transaction.bonusPaid)}</Text>
            </View>
          )}
          {transaction.arrears > 0 && (
            <View style={styles.row}>
              <Text style={styles.label}>Arrears</Text>
              <Text style={styles.value}>{formatCurrency(transaction.arrears)}</Text>
            </View>
          )}
          <View style={styles.totalRow}>
            <Text>Gross Earnings</Text>
            <Text>{formatCurrency(transaction.grossEarnings)}</Text>
          </View>
        </View>

        {/* Deductions */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Deductions</Text>
          <View style={styles.row}>
            <Text style={styles.label}>PF (Employee)</Text>
            <Text style={styles.value}>{formatCurrency(transaction.pfEmployee)}</Text>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>ESI (Employee)</Text>
            <Text style={styles.value}>{formatCurrency(transaction.esiEmployee)}</Text>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>Professional Tax</Text>
            <Text style={styles.value}>{formatCurrency(transaction.professionalTax)}</Text>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>TDS</Text>
            <Text style={styles.value}>{formatCurrency(transaction.tdsDeducted)}</Text>
          </View>
          {transaction.loanRecovery > 0 && (
            <View style={styles.row}>
              <Text style={styles.label}>Loan Recovery</Text>
              <Text style={styles.value}>{formatCurrency(transaction.loanRecovery)}</Text>
            </View>
          )}
          {transaction.otherDeductions > 0 && (
            <View style={styles.row}>
              <Text style={styles.label}>Other Deductions</Text>
              <Text style={styles.value}>{formatCurrency(transaction.otherDeductions)}</Text>
            </View>
          )}
          <View style={styles.totalRow}>
            <Text>Total Deductions</Text>
            <Text>{formatCurrency(transaction.totalDeductions)}</Text>
          </View>
        </View>

        {/* Net Pay */}
        <View style={styles.netPayBox}>
          <Text style={styles.netPayLabel}>Net Payable</Text>
          <Text style={styles.netPayValue}>{formatCurrency(transaction.netPayable)}</Text>
        </View>

        {/* Employer Contributions */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Employer Contributions (Informational)</Text>
          <View style={styles.row}>
            <Text style={styles.label}>PF (Employer)</Text>
            <Text style={styles.value}>{formatCurrency(transaction.pfEmployer)}</Text>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>ESI (Employer)</Text>
            <Text style={styles.value}>{formatCurrency(transaction.esiEmployer)}</Text>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>Gratuity Provision</Text>
            <Text style={styles.value}>{formatCurrency(transaction.gratuityProvision)}</Text>
          </View>
          <View style={styles.totalRow}>
            <Text>Total Employer Cost</Text>
            <Text>{formatCurrency(transaction.totalEmployerCost)}</Text>
          </View>
        </View>
      </Page>
    </Document>
  )
}

export const PayslipPDFDownload: React.FC<PayslipPDFProps> = (props) => {
  const handleDownload = async () => {
    try {
      const doc = <PayslipPDFDocument {...props} />
      const asPdf = pdf(doc)
      const blob = await asPdf.toBlob()
      const fileName = `Payslip_${props.transaction.employeeName || 'Employee'}_${props.transaction.payrollMonth}_${props.transaction.payrollYear}.pdf`
      saveAs(blob, fileName)
    } catch (error) {
      console.error('Failed to generate PDF:', error)
    }
  }

  return (
    <button
      onClick={handleDownload}
      className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700"
    >
      Download PDF
    </button>
  )
}



