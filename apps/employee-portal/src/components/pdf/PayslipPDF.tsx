import { Document, Page, Text, View, StyleSheet } from '@react-pdf/renderer'
import { format } from 'date-fns'
import type { PayslipDetail } from '@/types'

const styles = StyleSheet.create({
  page: {
    flexDirection: 'column',
    backgroundColor: '#ffffff',
    paddingTop: 50,
    paddingBottom: 60,
    paddingHorizontal: 30,
    fontFamily: 'Helvetica',
  },
  pageHeader: {
    position: 'absolute',
    top: 20,
    left: 30,
    right: 30,
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingBottom: 8,
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
  },
  headerText: {
    fontSize: 9,
    color: '#6B7280',
  },
  pageFooter: {
    position: 'absolute',
    bottom: 20,
    left: 30,
    right: 30,
    paddingTop: 8,
    borderTopWidth: 1,
    borderTopColor: '#E5E7EB',
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  footerText: {
    fontSize: 8,
    color: '#9CA3AF',
  },
  header: {
    marginBottom: 16,
  },
  companySection: {
    marginBottom: 10,
    textAlign: 'center',
  },
  salarySlipTitle: {
    fontSize: 22,
    fontWeight: 'bold',
    marginBottom: 4,
    color: '#1F2937',
  },
  periodTitle: {
    fontSize: 14,
    color: '#6B7280',
    marginBottom: 4,
  },
  employeeSection: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginTop: 16,
    marginBottom: 12,
    padding: 12,
    backgroundColor: '#F9FAFB',
    borderRadius: 4,
  },
  employeeDetails: {
    flex: 1,
  },
  employeeDetailRow: {
    flexDirection: 'row',
    marginBottom: 4,
  },
  employeeLabel: {
    fontSize: 9,
    color: '#6B7280',
    width: 90,
    fontWeight: 'bold',
  },
  employeeValue: {
    fontSize: 9,
    color: '#111827',
    flex: 1,
  },
  periodSection: {
    flex: 1,
    alignItems: 'flex-end',
  },
  periodText: {
    fontSize: 10,
    color: '#6B7280',
    marginBottom: 2,
  },
  earningsSection: {
    marginTop: 16,
    marginBottom: 12,
  },
  deductionsSection: {
    marginTop: 12,
    marginBottom: 12,
  },
  sectionTitle: {
    fontSize: 12,
    fontWeight: 'bold',
    color: '#111827',
    marginBottom: 8,
    paddingBottom: 4,
    borderBottomWidth: 2,
    borderBottomColor: '#3B82F6',
  },
  table: {
    marginTop: 6,
  },
  tableHeader: {
    flexDirection: 'row',
    backgroundColor: '#F3F4F6',
    paddingVertical: 8,
    paddingHorizontal: 8,
    borderTopWidth: 1,
    borderTopColor: '#E5E7EB',
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
  },
  tableRow: {
    flexDirection: 'row',
    paddingVertical: 6,
    paddingHorizontal: 8,
    borderBottomWidth: 1,
    borderBottomColor: '#F3F4F6',
  },
  tableHeaderText: {
    fontSize: 10,
    fontWeight: 'bold',
    color: '#111827',
  },
  tableCellText: {
    fontSize: 10,
    color: '#111827',
  },
  descriptionColumn: {
    flex: 3,
  },
  amountColumn: {
    flex: 1.5,
    textAlign: 'right',
  },
  totalsRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingVertical: 8,
    paddingHorizontal: 8,
    marginTop: 4,
    backgroundColor: '#F9FAFB',
    borderTopWidth: 2,
    borderTopColor: '#E5E7EB',
    borderBottomWidth: 2,
    borderBottomColor: '#E5E7EB',
  },
  totalsLabel: {
    fontSize: 11,
    fontWeight: 'bold',
    color: '#111827',
  },
  totalsValue: {
    fontSize: 11,
    fontWeight: 'bold',
    color: '#111827',
  },
  netSalaryRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingVertical: 12,
    paddingHorizontal: 10,
    marginTop: 12,
    backgroundColor: '#DBEAFE',
    borderWidth: 2,
    borderColor: '#3B82F6',
    borderRadius: 4,
  },
  netSalaryLabel: {
    fontSize: 14,
    fontWeight: 'bold',
    color: '#1E40AF',
  },
  netSalaryValue: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#1E40AF',
  },
  paymentSection: {
    marginTop: 16,
    padding: 12,
    backgroundColor: '#F9FAFB',
    borderRadius: 4,
    borderLeftWidth: 3,
    borderLeftColor: '#10B981',
  },
  paymentTitle: {
    fontSize: 10,
    fontWeight: 'bold',
    color: '#111827',
    marginBottom: 6,
  },
  paymentText: {
    fontSize: 9,
    color: '#6B7280',
    marginBottom: 3,
  },
  disclaimer: {
    marginTop: 20,
    padding: 10,
    backgroundColor: '#FEF3C7',
    borderRadius: 4,
    borderLeftWidth: 3,
    borderLeftColor: '#F59E0B',
  },
  disclaimerText: {
    fontSize: 8,
    color: '#92400E',
    lineHeight: 1.3,
    fontStyle: 'italic',
  },
})

const MONTHS = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December'
]

const getMonthName = (month: number): string => MONTHS[month - 1] || ''

const formatCurrency = (amount: number | null | undefined): string => {
  if (amount === null || amount === undefined || Number.isNaN(amount)) {
    return '-'
  }
  return `₹${amount.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
}

interface PayslipPDFProps {
  payslip: PayslipDetail
  companyName?: string
}

export function PayslipPDFDocument({ payslip, companyName = 'Your Company' }: PayslipPDFProps) {
  const monthName = getMonthName(payslip.month)
  const periodStart = new Date(payslip.year, payslip.month - 1, 1)
  const periodEnd = new Date(payslip.year, payslip.month, 0)

  return (
    <Document>
      <Page size="A4" style={styles.page}>
        {/* Page Header */}
        <View style={styles.pageHeader} fixed>
          <Text style={styles.headerText}>{companyName}</Text>
          <Text style={styles.headerText} render={({ pageNumber, totalPages }) => `Page ${pageNumber} of ${totalPages}`} />
        </View>

        {/* Title */}
        <View style={styles.header}>
          <View style={styles.companySection}>
            <Text style={styles.salarySlipTitle}>SALARY SLIP</Text>
            <Text style={styles.periodTitle}>
              {monthName} {payslip.year}
            </Text>
          </View>
        </View>

        {/* Employee Details */}
        <View style={styles.employeeSection}>
          <View style={styles.employeeDetails}>
            <View style={styles.employeeDetailRow}>
              <Text style={styles.employeeLabel}>Employee Name:</Text>
              <Text style={styles.employeeValue}>{payslip.employeeName}</Text>
            </View>
            <View style={styles.employeeDetailRow}>
              <Text style={styles.employeeLabel}>Employee Code:</Text>
              <Text style={styles.employeeValue}>{payslip.employeeCode}</Text>
            </View>
            {payslip.designation && (
              <View style={styles.employeeDetailRow}>
                <Text style={styles.employeeLabel}>Designation:</Text>
                <Text style={styles.employeeValue}>{payslip.designation}</Text>
              </View>
            )}
            {payslip.department && (
              <View style={styles.employeeDetailRow}>
                <Text style={styles.employeeLabel}>Department:</Text>
                <Text style={styles.employeeValue}>{payslip.department}</Text>
              </View>
            )}
            {payslip.panNumber && (
              <View style={styles.employeeDetailRow}>
                <Text style={styles.employeeLabel}>PAN Number:</Text>
                <Text style={styles.employeeValue}>{payslip.panNumber}</Text>
              </View>
            )}
            {payslip.bankAccount && (
              <View style={styles.employeeDetailRow}>
                <Text style={styles.employeeLabel}>Bank Account:</Text>
                <Text style={styles.employeeValue}>{payslip.bankAccount}</Text>
              </View>
            )}
          </View>

          <View style={styles.periodSection}>
            <Text style={[styles.periodText, { fontWeight: 'bold', fontSize: 12, marginBottom: 4 }]}>
              Pay Period
            </Text>
            <Text style={styles.periodText}>
              {format(periodStart, 'dd MMM')} - {format(periodEnd, 'dd MMM yyyy')}
            </Text>
            {payslip.paidOn && (
              <Text style={styles.periodText}>
                Paid on: {format(new Date(payslip.paidOn), 'dd MMM yyyy')}
              </Text>
            )}
          </View>
        </View>

        {/* Earnings Section */}
        <View style={styles.earningsSection}>
          <Text style={styles.sectionTitle}>EARNINGS</Text>
          <View style={styles.table}>
            <View style={styles.tableHeader}>
              <Text style={[styles.tableHeaderText, styles.descriptionColumn]}>Description</Text>
              <Text style={[styles.tableHeaderText, styles.amountColumn]}>Amount (₹)</Text>
            </View>
            {payslip.earnings.map((earning, index) => (
              <View key={index} style={styles.tableRow}>
                <Text style={[styles.tableCellText, styles.descriptionColumn]}>{earning.name}</Text>
                <Text style={[styles.tableCellText, styles.amountColumn]}>{formatCurrency(earning.amount)}</Text>
              </View>
            ))}
            <View style={styles.totalsRow}>
              <Text style={[styles.totalsLabel, styles.descriptionColumn]}>GROSS EARNINGS</Text>
              <Text style={[styles.totalsValue, styles.amountColumn]}>{formatCurrency(payslip.grossEarnings)}</Text>
            </View>
          </View>
        </View>

        {/* Deductions Section */}
        <View style={styles.deductionsSection}>
          <Text style={styles.sectionTitle}>DEDUCTIONS</Text>
          <View style={styles.table}>
            <View style={styles.tableHeader}>
              <Text style={[styles.tableHeaderText, styles.descriptionColumn]}>Description</Text>
              <Text style={[styles.tableHeaderText, styles.amountColumn]}>Amount (₹)</Text>
            </View>
            {payslip.deductions.map((deduction, index) => (
              <View key={index} style={styles.tableRow}>
                <Text style={[styles.tableCellText, styles.descriptionColumn]}>{deduction.name}</Text>
                <Text style={[styles.tableCellText, styles.amountColumn]}>{formatCurrency(deduction.amount)}</Text>
              </View>
            ))}
            <View style={styles.totalsRow}>
              <Text style={[styles.totalsLabel, styles.descriptionColumn]}>TOTAL DEDUCTIONS</Text>
              <Text style={[styles.totalsValue, styles.amountColumn]}>{formatCurrency(payslip.totalDeductions)}</Text>
            </View>
          </View>
        </View>

        {/* Net Salary */}
        <View style={styles.netSalaryRow}>
          <Text style={styles.netSalaryLabel}>NET SALARY (PAYABLE)</Text>
          <Text style={styles.netSalaryValue}>{formatCurrency(payslip.netPay)}</Text>
        </View>

        {/* Payment Status */}
        {payslip.paidOn && (
          <View style={styles.paymentSection}>
            <Text style={styles.paymentTitle}>Payment Details</Text>
            <Text style={styles.paymentText}>
              Payment Date: {format(new Date(payslip.paidOn), 'dd MMMM yyyy')}
            </Text>
            <Text style={styles.paymentText}>
              Status: {payslip.status.toUpperCase()}
            </Text>
          </View>
        )}

        {/* Disclaimer */}
        <View style={styles.disclaimer}>
          <Text style={styles.disclaimerText}>
            This is a computer generated document and does not require a physical signature.
            This salary slip is for the month of {monthName} {payslip.year} and is generated in compliance with Indian tax regulations.
            {payslip.panNumber && ` PAN: ${payslip.panNumber}`}
          </Text>
        </View>

        {/* Page Footer */}
        <View style={styles.pageFooter} fixed>
          <Text style={styles.footerText}>{companyName}</Text>
          <Text style={styles.footerText}>
            Salary Slip - {monthName} {payslip.year} | Generated on {format(new Date(), 'dd MMM yyyy')}
          </Text>
        </View>
      </Page>
    </Document>
  )
}
