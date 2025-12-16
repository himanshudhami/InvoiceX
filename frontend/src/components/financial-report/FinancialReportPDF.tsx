import React from 'react';
import { Document, Page, Text, View, StyleSheet, pdf } from '@react-pdf/renderer';
import { PnLData } from '@/lib/pnlCalculation';
import { Company } from '@/services/api/types';
import { formatINR } from '@/lib/financialUtils';
import { saveAs } from 'file-saver';

const styles = StyleSheet.create({
  page: {
    flexDirection: 'column',
    backgroundColor: '#ffffff',
    paddingTop: 50,
    paddingBottom: 60,
    paddingHorizontal: 40,
    fontFamily: 'Helvetica',
  },
  // Page Header Styles
  pageHeader: {
    position: 'absolute',
    top: 20,
    left: 40,
    right: 40,
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
  // Page Footer Styles
  pageFooter: {
    position: 'absolute',
    bottom: 20,
    left: 40,
    right: 40,
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
  // Header Section
  header: {
    marginBottom: 20,
    textAlign: 'center',
  },
  companyName: {
    fontSize: 20,
    fontWeight: 'bold',
    marginBottom: 8,
    color: '#111827',
  },
  reportTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    marginBottom: 4,
    color: '#1F2937',
  },
  companyDetails: {
    fontSize: 9,
    color: '#6B7280',
    lineHeight: 1.4,
    marginTop: 4,
  },
  periodInfo: {
    fontSize: 10,
    color: '#374151',
    marginTop: 8,
  },
  // Table Styles
  table: {
    width: '100%',
    marginTop: 12,
  },
  tableRow: {
    flexDirection: 'row',
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
    paddingVertical: 6,
  },
  tableRowBold: {
    flexDirection: 'row',
    borderBottomWidth: 2,
    borderBottomColor: '#111827',
    paddingVertical: 8,
    backgroundColor: '#F9FAFB',
  },
  tableRowHighlight: {
    flexDirection: 'row',
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
    paddingVertical: 8,
    backgroundColor: '#EFF6FF',
  },
  tableRowGreen: {
    flexDirection: 'row',
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
    paddingVertical: 8,
    backgroundColor: '#F0FDF4',
  },
  tableCellLabel: {
    flex: 1,
    fontSize: 10,
    color: '#374151',
    paddingLeft: 4,
  },
  tableCellLabelBold: {
    flex: 1,
    fontSize: 10,
    fontWeight: 'bold',
    color: '#111827',
    paddingLeft: 4,
  },
  tableCellLabelIndent: {
    flex: 1,
    fontSize: 10,
    color: '#374151',
    paddingLeft: 20,
  },
  tableCellValue: {
    width: 120,
    fontSize: 10,
    color: '#374151',
    textAlign: 'right',
    paddingRight: 4,
  },
  tableCellValueBold: {
    width: 120,
    fontSize: 10,
    fontWeight: 'bold',
    color: '#111827',
    textAlign: 'right',
    paddingRight: 4,
  },
  tableCellValueLarge: {
    width: 120,
    fontSize: 12,
    fontWeight: 'bold',
    color: '#111827',
    textAlign: 'right',
    paddingRight: 4,
  },
  // Section Headers
  sectionHeader: {
    fontSize: 12,
    fontWeight: 'bold',
    color: '#111827',
    marginTop: 16,
    marginBottom: 8,
    paddingTop: 8,
    borderTopWidth: 1,
    borderTopColor: '#E5E7EB',
  },
  // Notes Section
  notesSection: {
    marginTop: 20,
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: '#E5E7EB',
  },
  notesTitle: {
    fontSize: 11,
    fontWeight: 'bold',
    color: '#111827',
    marginBottom: 8,
  },
  notesList: {
    fontSize: 8,
    color: '#6B7280',
    lineHeight: 1.6,
    marginLeft: 12,
  },
  notesItem: {
    marginBottom: 4,
  },
  // Disclaimer
  disclaimer: {
    marginTop: 16,
    padding: 8,
    backgroundColor: '#FEF3C7',
    borderRadius: 4,
  },
  disclaimerText: {
    fontSize: 8,
    color: '#92400E',
    fontStyle: 'italic',
    lineHeight: 1.4,
  },
});

interface FinancialReportPDFProps {
  data: PnLData;
  company?: Company;
  selectedYear: number;
  selectedMonth?: number;
}

export const FinancialReportPDFDocument: React.FC<FinancialReportPDFProps> = ({
  data,
  company,
  selectedYear,
  selectedMonth,
}) => {
  const periodLabel = selectedMonth
    ? `${new Date(selectedYear, selectedMonth - 1, 1).toLocaleString('default', { month: 'long' })} ${selectedYear}`
    : `Financial Year ${selectedYear}`;

  const estimatedTax = data.netProfit * 0.3;
  const netProfitAfterTax = data.netProfit - estimatedTax;

  return (
    <Document>
      <Page size="A4" style={styles.page}>
        {/* Page Header */}
        <View style={styles.pageHeader} fixed>
          <Text style={styles.headerText}>{company?.name || 'Financial Report'}</Text>
          <Text style={styles.headerText} render={({ pageNumber, totalPages }) => `Page ${pageNumber} of ${totalPages}`} />
        </View>

        {/* Company Header */}
        <View style={styles.header}>
          <Text style={styles.companyName}>{company?.name || 'Your Company'}</Text>
          <Text style={styles.reportTitle}>PROFIT & LOSS STATEMENT</Text>
          <View style={styles.companyDetails}>
            {company?.addressLine1 && <Text>{company.addressLine1}</Text>}
            {company?.addressLine2 && <Text>{company.addressLine2}</Text>}
            {(company?.city || company?.state || company?.zipCode) && (
              <Text>
                {[company?.city, company?.state, company?.zipCode]
                  .filter(Boolean)
                  .join(', ')}
              </Text>
            )}
            {company?.taxNumber && <Text>PAN/GSTIN: {company.taxNumber}</Text>}
          </View>
          <Text style={styles.periodInfo}>Period: {periodLabel}</Text>
        </View>

        {/* REVENUE SECTION */}
        <View style={styles.table}>
          <Text style={styles.sectionHeader}>REVENUE</Text>
          
          <View style={styles.tableRow}>
            <Text style={styles.tableCellLabelIndent}>Sales/Services (Paid Invoices)</Text>
            <Text style={styles.tableCellValue}>{formatINR(data.totalIncome)}</Text>
          </View>
          
          <View style={styles.tableRow}>
            <Text style={styles.tableCellLabelIndent}>Other Income</Text>
            <Text style={styles.tableCellValue}>₹0</Text>
          </View>
          
          <View style={styles.tableRowBold}>
            <Text style={styles.tableCellLabelBold}>TOTAL REVENUE</Text>
            <Text style={styles.tableCellValueBold}>{formatINR(data.totalIncome)}</Text>
          </View>
        </View>

        {/* OPERATING EXPENSES SECTION */}
        <View style={styles.table}>
          <Text style={styles.sectionHeader}>OPERATING EXPENSES</Text>
          
          <View style={styles.tableRow}>
            <Text style={styles.tableCellLabelIndent}>Employee Salaries (Gross)</Text>
            <Text style={styles.tableCellValue}>{formatINR(data.salaryExpense)}</Text>
          </View>
          
          <View style={styles.tableRow}>
            <Text style={styles.tableCellLabelIndent}>Maintenance & Repairs</Text>
            <Text style={styles.tableCellValue}>{formatINR(data.maintenanceExpense)}</Text>
          </View>
          
          {data.opexAssetExpense > 0 && (
            <View style={styles.tableRow}>
              <Text style={styles.tableCellLabelIndent}>OPEX Asset Purchases</Text>
              <Text style={styles.tableCellValue}>{formatINR(data.opexAssetExpense)}</Text>
            </View>
          )}
          
          {data.subscriptionExpense > 0 && (
            <View style={styles.tableRow}>
              <Text style={styles.tableCellLabelIndent}>Software Subscriptions</Text>
              <Text style={styles.tableCellValue}>{formatINR(data.subscriptionExpense)}</Text>
            </View>
          )}
          
          {data.loanInterestExpense > 0 && (
            <View style={styles.tableRow}>
              <Text style={styles.tableCellLabelIndent}>Loan Interest Expense (Section 36(1)(iii))</Text>
              <Text style={styles.tableCellValue}>{formatINR(data.loanInterestExpense)}</Text>
            </View>
          )}
          
          <View style={styles.tableRow}>
            <Text style={styles.tableCellLabelIndent}>Other Operating Expenses</Text>
            <Text style={styles.tableCellValue}>{formatINR(data.otherExpense)}</Text>
          </View>
          
          <View style={styles.tableRowBold}>
            <Text style={styles.tableCellLabelBold}>TOTAL OPERATING EXPENSES</Text>
            <Text style={styles.tableCellValueBold}>{formatINR(data.totalOpex)}</Text>
          </View>
        </View>

        {/* EBITDA */}
        <View style={styles.tableRowHighlight}>
          <Text style={styles.tableCellLabelBold}>EBITDA (Earnings Before Interest, Tax, Depreciation & Amortization)</Text>
          <Text style={styles.tableCellValueLarge}>{formatINR(data.ebitda)}</Text>
        </View>

        {/* DEPRECIATION SECTION */}
        <View style={styles.table}>
          <Text style={styles.sectionHeader}>DEPRECIATION SCHEDULE</Text>
          
          {data.depreciationByCategory.length > 0 ? (
            data.depreciationByCategory.map((dep, index) => (
              <View key={index} style={styles.tableRow}>
                <Text style={styles.tableCellLabelIndent}>
                  {dep.category} (@ {dep.rate}%)
                </Text>
                <Text style={styles.tableCellValue}>{formatINR(dep.amount)}</Text>
              </View>
            ))
          ) : (
            <View style={styles.tableRow}>
              <Text style={styles.tableCellLabelIndent}>Depreciation (General)</Text>
              <Text style={styles.tableCellValue}>{formatINR(data.depreciation)}</Text>
            </View>
          )}
          
          <View style={styles.tableRowBold}>
            <Text style={styles.tableCellLabelBold}>TOTAL DEPRECIATION</Text>
            <Text style={styles.tableCellValueBold}>{formatINR(data.depreciation)}</Text>
          </View>
        </View>

        {/* EBIT */}
        <View style={styles.tableRow}>
          <Text style={styles.tableCellLabelBold}>EBIT (Earnings Before Interest & Tax)</Text>
          <Text style={styles.tableCellValueBold}>{formatINR(data.netProfit)}</Text>
        </View>

        {/* OTHER ITEMS */}
        <View style={styles.table}>
          <Text style={styles.sectionHeader}>OTHER ITEMS</Text>
          
          <View style={styles.tableRow}>
            <Text style={styles.tableCellLabelIndent}>Interest Expense</Text>
            <Text style={styles.tableCellValue}>₹0</Text>
          </View>
          
          <View style={styles.tableRow}>
            <Text style={styles.tableCellLabelIndent}>Provisions</Text>
            <Text style={styles.tableCellValue}>₹0</Text>
          </View>
          
          <View style={styles.tableRowBold}>
            <Text style={styles.tableCellLabelBold}>PROFIT BEFORE TAX</Text>
            <Text style={styles.tableCellValueBold}>{formatINR(data.netProfit)}</Text>
          </View>
        </View>

        {/* INCOME TAX */}
        <View style={styles.table}>
          <Text style={styles.sectionHeader}>INCOME TAX</Text>
          
          <View style={styles.tableRow}>
            <Text style={styles.tableCellLabelIndent}>Tax @ 30% (Estimated)</Text>
            <Text style={styles.tableCellValue}>{formatINR(estimatedTax)}</Text>
          </View>
        </View>

        {/* NET PROFIT */}
        <View style={styles.tableRowGreen}>
          <Text style={styles.tableCellLabelBold}>NET PROFIT / (LOSS)</Text>
          <Text style={styles.tableCellValueLarge}>{formatINR(netProfitAfterTax)}</Text>
        </View>

        {/* TAX RECONCILIATION SECTION */}
        <View style={styles.notesSection}>
          <Text style={styles.notesTitle}>TAX RECONCILIATION</Text>
          <View style={styles.table}>
            <View style={styles.tableRow}>
              <Text style={styles.tableCellLabel}>Book Profit (Above)</Text>
              <Text style={styles.tableCellValue}>{formatINR(data.netProfit)}</Text>
            </View>
            <View style={styles.tableRow}>
              <Text style={styles.tableCellLabel}>Add: Non-deductible items</Text>
              <Text style={styles.tableCellValue}>₹0</Text>
            </View>
            <View style={styles.tableRow}>
              <Text style={styles.tableCellLabel}>Less: Tax-exempt income</Text>
              <Text style={styles.tableCellValue}>₹0</Text>
            </View>
            <View style={styles.tableRowBold}>
              <Text style={styles.tableCellLabelBold}>TAXABLE INCOME (per Income Tax Act)</Text>
              <Text style={styles.tableCellValueBold}>{formatINR(data.netProfit)}</Text>
            </View>
            <View style={styles.tableRow}>
              <Text style={styles.tableCellLabel}>Tax @ 30%</Text>
              <Text style={styles.tableCellValue}>{formatINR(estimatedTax)}</Text>
            </View>
          </View>
        </View>

        {/* NOTES SECTION */}
        <View style={styles.notesSection}>
          <Text style={styles.notesTitle}>NOTES:</Text>
          <View style={styles.notesList}>
            <Text style={styles.notesItem}>1. Depreciation rates per Schedule II, Companies Act 2013</Text>
            <Text style={styles.notesItem}>2. Salary includes gross salary + employer contributions</Text>
            <Text style={styles.notesItem}>3. Subscription expenses include only active subscriptions (paused/cancelled subscriptions do not accrue costs)</Text>
            <Text style={styles.notesItem}>4. All amounts in INR (converted from foreign currencies at fixed rates)</Text>
            <Text style={styles.notesItem}>5. Pro-rata depreciation applied for assets added during the year</Text>
            <Text style={styles.notesItem}>6. Subscription costs are prorated for partial months (start/pause/resume/cancel mid-month)</Text>
            <Text style={styles.notesItem}>7. Tax depreciation as per Income Tax Act rates</Text>
            <Text style={styles.notesItem}>8. This is an estimated statement. Please consult with a tax advisor for final tax calculations.</Text>
          </View>
        </View>

        {/* Disclaimer */}
        <View style={styles.disclaimer}>
          <Text style={styles.disclaimerText}>
            This financial statement is prepared for internal purposes and tax compliance. 
            All calculations are estimates based on available data. For official tax filing, 
            please consult with a certified chartered accountant or tax advisor.
          </Text>
        </View>

        {/* Page Footer */}
        <View style={styles.pageFooter} fixed>
          <Text style={styles.footerText}>
            Generated on {new Date().toLocaleDateString('en-IN', { 
              day: '2-digit', 
              month: 'long', 
              year: 'numeric' 
            })}
          </Text>
          <Text style={styles.footerText}>{company?.name || 'Financial Report'}</Text>
        </View>
      </Page>
    </Document>
  );
};

// Export function to generate and download PDF
export const generateFinancialReportPDF = async (
  data: PnLData,
  company: Company | undefined,
  selectedYear: number,
  selectedMonth: number | undefined,
  filename?: string
) => {
  const blob = await pdf(
    <FinancialReportPDFDocument
      data={data}
      company={company}
      selectedYear={selectedYear}
      selectedMonth={selectedMonth}
    />
  ).toBlob();
  
  const defaultFilename = `Financial_Report_${selectedYear}${selectedMonth ? `_${selectedMonth}` : ''}_${new Date().toISOString().split('T')[0]}.pdf`;
  saveAs(blob, filename || defaultFilename);
};

