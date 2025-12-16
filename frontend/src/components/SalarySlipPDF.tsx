import React, { useCallback, useState } from 'react';
import { Document, Page, Text, View, StyleSheet, Image, pdf } from '@react-pdf/renderer';
import { EmployeeSalaryTransaction, Employee, Company } from '@/services/api/types';
import { format } from 'date-fns';
import { saveAs } from 'file-saver';

const styles = StyleSheet.create({
  page: {
    flexDirection: 'column',
    backgroundColor: '#ffffff',
    paddingTop: 50,
    paddingBottom: 60,
    paddingHorizontal: 30,
    fontFamily: 'NotoSans',
  },
  // Page Header Styles
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
  // Page Footer Styles
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
  footerCompany: {
    fontSize: 8,
    color: '#6B7280',
    fontWeight: 'bold',
  },
  header: {
    marginBottom: 16,
  },
  companySection: {
    marginBottom: 10,
    textAlign: 'center',
  },
  companyName: {
    fontSize: 24,
    fontWeight: 'bold',
    marginBottom: 6,
    color: '#111827',
  },
  salarySlipTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    marginBottom: 4,
    color: '#1F2937',
  },
  companyDetails: {
    fontSize: 10,
    color: '#6B7280',
    lineHeight: 1.4,
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
    width: 80,
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
  periodTitle: {
    fontSize: 14,
    fontWeight: 'bold',
    color: '#111827',
    marginBottom: 6,
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
    paddingVertical: 10,
    paddingHorizontal: 8,
    marginTop: 8,
    backgroundColor: '#DBEAFE',
    borderWidth: 2,
    borderColor: '#3B82F6',
    borderRadius: 4,
  },
  netSalaryLabel: {
    fontSize: 12,
    fontWeight: 'bold',
    color: '#1E40AF',
  },
  netSalaryValue: {
    fontSize: 14,
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
  signatureSection: {
    marginTop: 24,
    flexDirection: 'row',
    justifyContent: 'space-between',
  },
  signatureBox: {
    width: 200,
    alignItems: 'center',
  },
  signatureImage: {
    width: 120,
    height: 48,
    marginBottom: 6,
  },
  signatureText: {
    fontSize: 11,
    fontFamily: 'Helvetica-Oblique',
    marginBottom: 6,
  },
  signatureLine: {
    width: 160,
    borderTopWidth: 1,
    borderTopColor: '#111827',
    marginTop: 6,
    marginBottom: 4,
  },
  signatureLabel: {
    fontSize: 9,
    color: '#6B7280',
    fontWeight: 'bold',
  },
  disclaimer: {
    marginTop: 16,
    padding: 8,
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
});

interface SalarySlipPDFProps {
  transaction: EmployeeSalaryTransaction;
  employee?: Employee;
  company?: Company;
}

const getMonthName = (month: number): string => {
  const months = [
    'January', 'February', 'March', 'April', 'May', 'June',
    'July', 'August', 'September', 'October', 'November', 'December'
  ];
  return months[month - 1] || '';
};

export const SalarySlipPDFDocument: React.FC<SalarySlipPDFProps> = ({
  transaction,
  employee,
  company,
}) => {
  const formatCurrency = (amount: number) => {
    return `₹${amount.toFixed(2)}`;
  };

  const monthName = getMonthName(transaction.salaryMonth);
  const totalDeductions = transaction.pfEmployee + transaction.pt + transaction.incomeTax + transaction.otherDeductions;

  return (
    <Document>
      <Page size="A4" style={styles.page}>
        {/* Page Header */}
        <View style={styles.pageHeader} fixed>
          <Text style={styles.headerText}>{company?.name || 'Salary Slip'}</Text>
          <Text style={styles.headerText} render={({ pageNumber, totalPages }) => `Page ${pageNumber} of ${totalPages}`} />
        </View>

        {/* Company Header */}
        <View style={styles.header}>
          <View style={styles.companySection}>
            <Text style={styles.companyName}>{company?.name || 'Your Company'}</Text>
            <Text style={styles.salarySlipTitle}>SALARY SLIP</Text>
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
              {company?.phone && <Text>Phone: {company.phone}</Text>}
              {company?.email && <Text>Email: {company.email}</Text>}
              {company?.taxNumber && <Text>PAN/GSTIN: {company.taxNumber}</Text>}
              {company?.tanNumber && <Text>TAN: {company.tanNumber}</Text>}
              {company?.pfRegistrationNumber && <Text>PF Code: {company.pfRegistrationNumber}</Text>}
              {company?.esiRegistrationNumber && <Text>ESI Code: {company.esiRegistrationNumber}</Text>}
            </View>
          </View>
        </View>

        {/* Employee Details and Period */}
        <View style={styles.employeeSection}>
          <View style={styles.employeeDetails}>
            <View style={styles.employeeDetailRow}>
              <Text style={styles.employeeLabel}>Employee Name:</Text>
              <Text style={styles.employeeValue}>{employee?.employeeName || 'N/A'}</Text>
            </View>
            {employee?.employeeId && (
              <View style={styles.employeeDetailRow}>
                <Text style={styles.employeeLabel}>Employee ID:</Text>
                <Text style={styles.employeeValue}>{employee.employeeId}</Text>
              </View>
            )}
            {employee?.designation && (
              <View style={styles.employeeDetailRow}>
                <Text style={styles.employeeLabel}>Designation:</Text>
                <Text style={styles.employeeValue}>{employee.designation}</Text>
              </View>
            )}
            {employee?.department && (
              <View style={styles.employeeDetailRow}>
                <Text style={styles.employeeLabel}>Department:</Text>
                <Text style={styles.employeeValue}>{employee.department}</Text>
              </View>
            )}
            {employee?.panNumber && (
              <View style={styles.employeeDetailRow}>
                <Text style={styles.employeeLabel}>PAN Number:</Text>
                <Text style={styles.employeeValue}>{employee.panNumber}</Text>
              </View>
            )}
            {employee?.bankAccountNumber && (
              <View style={styles.employeeDetailRow}>
                <Text style={styles.employeeLabel}>Bank A/c No:</Text>
                <Text style={styles.employeeValue}>{employee.bankAccountNumber}</Text>
              </View>
            )}
            {employee?.bankName && (
              <View style={styles.employeeDetailRow}>
                <Text style={styles.employeeLabel}>Bank Name:</Text>
                <Text style={styles.employeeValue}>{employee.bankName}</Text>
              </View>
            )}
            {employee?.uan && (
              <View style={styles.employeeDetailRow}>
                <Text style={styles.employeeLabel}>UAN:</Text>
                <Text style={styles.employeeValue}>{employee.uan}</Text>
              </View>
            )}
            {employee?.pfAccountNumber && (
              <View style={styles.employeeDetailRow}>
                <Text style={styles.employeeLabel}>PF A/c No:</Text>
                <Text style={styles.employeeValue}>{employee.pfAccountNumber}</Text>
              </View>
            )}
            {employee?.esiNumber && (
              <View style={styles.employeeDetailRow}>
                <Text style={styles.employeeLabel}>ESI IP No:</Text>
                <Text style={styles.employeeValue}>{employee.esiNumber}</Text>
              </View>
            )}
          </View>

          <View style={styles.periodSection}>
            <Text style={styles.periodTitle}>Salary Period</Text>
            <Text style={styles.periodText}>{monthName} {transaction.salaryYear}</Text>
            <Text style={styles.periodText}>
              {new Date(transaction.salaryYear, transaction.salaryMonth - 1, 1).toLocaleDateString('en-IN', {
                day: '2-digit',
                month: 'short',
              })} - {new Date(transaction.salaryYear, transaction.salaryMonth, 0).toLocaleDateString('en-IN', {
                day: '2-digit',
                month: 'short',
                year: 'numeric',
              })}
            </Text>
            <Text style={styles.periodText}>Pay Date: {transaction.paymentDate ? format(new Date(transaction.paymentDate), 'dd MMM yyyy') : 'Pending'}</Text>
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
            <View style={styles.tableRow}>
              <Text style={[styles.tableCellText, styles.descriptionColumn]}>Basic Salary</Text>
              <Text style={[styles.tableCellText, styles.amountColumn]}>{formatCurrency(transaction.basicSalary)}</Text>
            </View>
            {transaction.hra > 0 && (
              <View style={styles.tableRow}>
                <Text style={[styles.tableCellText, styles.descriptionColumn]}>House Rent Allowance (HRA)</Text>
                <Text style={[styles.tableCellText, styles.amountColumn]}>{formatCurrency(transaction.hra)}</Text>
              </View>
            )}
            {transaction.conveyance > 0 && (
              <View style={styles.tableRow}>
                <Text style={[styles.tableCellText, styles.descriptionColumn]}>Conveyance Allowance</Text>
                <Text style={[styles.tableCellText, styles.amountColumn]}>{formatCurrency(transaction.conveyance)}</Text>
              </View>
            )}
            {transaction.medicalAllowance > 0 && (
              <View style={styles.tableRow}>
                <Text style={[styles.tableCellText, styles.descriptionColumn]}>Medical Allowance</Text>
                <Text style={[styles.tableCellText, styles.amountColumn]}>{formatCurrency(transaction.medicalAllowance)}</Text>
              </View>
            )}
            {transaction.specialAllowance > 0 && (
              <View style={styles.tableRow}>
                <Text style={[styles.tableCellText, styles.descriptionColumn]}>Special Allowance</Text>
                <Text style={[styles.tableCellText, styles.amountColumn]}>{formatCurrency(transaction.specialAllowance)}</Text>
              </View>
            )}
            {transaction.lta > 0 && (
              <View style={styles.tableRow}>
                <Text style={[styles.tableCellText, styles.descriptionColumn]}>Leave Travel Allowance (LTA)</Text>
                <Text style={[styles.tableCellText, styles.amountColumn]}>{formatCurrency(transaction.lta)}</Text>
              </View>
            )}
            {transaction.otherAllowances > 0 && (
              <View style={styles.tableRow}>
                <Text style={[styles.tableCellText, styles.descriptionColumn]}>Other Allowances</Text>
                <Text style={[styles.tableCellText, styles.amountColumn]}>{formatCurrency(transaction.otherAllowances)}</Text>
              </View>
            )}
            <View style={styles.totalsRow}>
              <Text style={[styles.totalsLabel, styles.descriptionColumn]}>GROSS SALARY</Text>
              <Text style={[styles.totalsValue, styles.amountColumn]}>{formatCurrency(transaction.grossSalary)}</Text>
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
            {transaction.pfEmployee > 0 && (
              <View style={styles.tableRow}>
                <Text style={[styles.tableCellText, styles.descriptionColumn]}>Provident Fund (PF) - Employee Contribution</Text>
                <Text style={[styles.tableCellText, styles.amountColumn]}>{formatCurrency(transaction.pfEmployee)}</Text>
              </View>
            )}
            {transaction.pfEmployer > 0 && (
              <View style={styles.tableRow}>
                <Text style={[styles.tableCellText, styles.descriptionColumn]}>Provident Fund (PF) - Employer Contribution</Text>
                <Text style={[styles.tableCellText, styles.amountColumn]}>{formatCurrency(transaction.pfEmployer)}</Text>
              </View>
            )}
            {transaction.pt > 0 && (
              <View style={styles.tableRow}>
                <Text style={[styles.tableCellText, styles.descriptionColumn]}>Professional Tax (PT)</Text>
                <Text style={[styles.tableCellText, styles.amountColumn]}>{formatCurrency(transaction.pt)}</Text>
              </View>
            )}
            {transaction.incomeTax > 0 && (
              <View style={styles.tableRow}>
                <Text style={[styles.tableCellText, styles.descriptionColumn]}>Income Tax (TDS)</Text>
                <Text style={[styles.tableCellText, styles.amountColumn]}>{formatCurrency(transaction.incomeTax)}</Text>
              </View>
            )}
            {transaction.otherDeductions > 0 && (
              <View style={styles.tableRow}>
                <Text style={[styles.tableCellText, styles.descriptionColumn]}>Other Deductions</Text>
                <Text style={[styles.tableCellText, styles.amountColumn]}>{formatCurrency(transaction.otherDeductions)}</Text>
              </View>
            )}
            <View style={styles.totalsRow}>
              <Text style={[styles.totalsLabel, styles.descriptionColumn]}>TOTAL DEDUCTIONS</Text>
              <Text style={[styles.totalsValue, styles.amountColumn]}>{formatCurrency(totalDeductions)}</Text>
            </View>
          </View>
        </View>

        {/* Net Salary */}
        <View style={styles.netSalaryRow}>
          <Text style={styles.netSalaryLabel}>NET SALARY (PAYABLE)</Text>
          <Text style={styles.netSalaryValue}>{formatCurrency(transaction.netSalary)}</Text>
        </View>

        {/* Payment Details */}
        {transaction.paymentDate && (
          <View style={styles.paymentSection}>
            <Text style={styles.paymentTitle}>Payment Details</Text>
            <Text style={styles.paymentText}>
              Payment Date: {format(new Date(transaction.paymentDate), 'dd MMMM yyyy')}
            </Text>
            {transaction.paymentMethod && (
              <Text style={styles.paymentText}>
                Payment Method: {transaction.paymentMethod.replace('_', ' ').toUpperCase()}
              </Text>
            )}
            {transaction.paymentReference && (
              <Text style={styles.paymentText}>
                Transaction Reference: {transaction.paymentReference}
              </Text>
            )}
            {transaction.status && (
              <Text style={styles.paymentText}>
                Status: {transaction.status.toUpperCase()}
              </Text>
            )}
          </View>
        )}

        {/* Signature Section */}
        {company && (
          <View style={styles.signatureSection}>
            <View style={styles.signatureBox}>
              <Text style={styles.signatureText}>This is a system generated salary slip.</Text>
            </View>
            <View style={styles.signatureBox}>
              {company.signatureData && (
                company.signatureType === 'drawn' || company.signatureType === 'uploaded' ? (
                  <Image style={styles.signatureImage} src={company.signatureData} />
                ) : null
              )}
              <View style={styles.signatureLine} />
              <Text style={styles.signatureLabel}>Authorized Signatory</Text>
              {company.name && <Text style={styles.signatureLabel}>{company.name}</Text>}
            </View>
          </View>
        )}

        {/* Disclaimer */}
        <View style={styles.disclaimer}>
          <Text style={styles.disclaimerText}>
            This is a computer generated document and does not require a physical signature. 
            This salary slip is for the month of {monthName} {transaction.salaryYear} and is generated in compliance with Indian tax regulations.
            {employee?.panNumber && ` PAN: ${employee.panNumber}`}
          </Text>
        </View>

        {/* Page Footer */}
        <View style={styles.pageFooter} fixed>
          <View>
            <Text style={styles.footerCompany}>{company?.name || ''}</Text>
            {company?.website && <Text style={styles.footerText}>{company.website}</Text>}
            {company?.email && <Text style={styles.footerText}>{company.email}</Text>}
          </View>
          <View style={{ alignItems: 'flex-end' }}>
            <Text style={styles.footerText}>
              Salary Slip - {monthName} {transaction.salaryYear}
            </Text>
            <Text style={styles.footerText}>
              Generated on {format(new Date(), 'dd MMM yyyy')}
            </Text>
          </View>
        </View>
      </Page>
    </Document>
  );
};

interface SalarySlipPDFDownloadProps extends SalarySlipPDFProps {
  fileName?: string;
  label?: React.ReactNode;
  className?: string;
}

export const SalarySlipPDFDownload: React.FC<SalarySlipPDFDownloadProps> = ({
  transaction,
  employee,
  company,
  fileName,
  label,
  className,
}) => {
  const monthName = getMonthName(transaction.salaryMonth);
  const defaultFileName = `Salary-Slip-${employee?.employeeName || transaction.employeeId}-${monthName}-${transaction.salaryYear}.pdf`;
  const defaultLabel = label ?? 'Download Salary Slip';
  const [isGenerating, setIsGenerating] = useState(false);

  const handleDownload = useCallback(async () => {
    if (isGenerating) return;

    try {
      setIsGenerating(true);

      // Generate PDF on-demand to avoid rendering many PDFs concurrently in tables,
      // which can trigger Yoga Config binding errors in react-pdf.
      const blob = await pdf(
        <SalarySlipPDFDocument transaction={transaction} employee={employee} company={company} />,
      ).toBlob();

      saveAs(blob, fileName || defaultFileName);
    } catch (err) {
      // Keep UI stable; surface error in console for debugging
      // eslint-disable-next-line no-console
      console.error('Failed to generate Salary Slip PDF', err);
    } finally {
      setIsGenerating(false);
    }
  }, [company, defaultFileName, employee, fileName, isGenerating, transaction]);

  return (
    <button
      type="button"
      className={className}
      disabled={isGenerating}
      title="Download Salary Slip"
      onClick={handleDownload}
    >
      {isGenerating ? 'Preparing…' : defaultLabel}
    </button>
  );
};

