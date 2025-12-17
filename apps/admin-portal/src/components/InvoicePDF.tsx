import React from 'react';
import { Document, Page, Text, View, StyleSheet, PDFDownloadLink, Image } from '@react-pdf/renderer';
import { InvoicePDF_Ledger } from '@/pdf/templates/MinimalLedger';
import { Invoice, InvoiceItem, Customer, Company } from '@/services/api/types';
import { format } from 'date-fns';

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
  },
  companyName: {
    fontSize: 20,
    fontWeight: 'bold',
    marginBottom: 4,
    color: '#111827',
  },
  companyDetails: {
    fontSize: 10,
    color: '#6B7280',
    lineHeight: 1.3,
  },
  invoiceHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 12,
  },
  invoiceTitle: {
    fontSize: 22,
    fontWeight: 'bold',
    color: '#111827',
    marginBottom: 2,
  },
  invoiceNumber: {
    fontSize: 12,
    color: '#6B7280',
    marginBottom: 2,
  },
  statusBadge: {
    paddingHorizontal: 8,
    paddingVertical: 2,
    borderRadius: 12,
    alignSelf: 'flex-start',
  },
  statusText: {
    fontSize: 10,
    textTransform: 'uppercase',
    fontWeight: 'bold',
  },
  billSection: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 12,
  },
  sectionTitle: {
    fontSize: 10,
    fontWeight: 'bold',
    color: '#6B7280',
    marginBottom: 4,
    textTransform: 'uppercase',
    letterSpacing: 0.5,
  },
  sectionContent: {
    fontSize: 10,
    color: '#111827',
    lineHeight: 1.3,
  },
  table: {
    marginTop: 10,
    marginBottom: 10,
  },
  tableHeader: {
    flexDirection: 'row',
    backgroundColor: '#F9FAFB',
    paddingVertical: 6,
    paddingHorizontal: 6,
    borderTopWidth: 1,
    borderTopColor: '#E5E7EB',
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
  },
  tableRow: {
    flexDirection: 'row',
    paddingVertical: 6,
    paddingHorizontal: 6,
    borderBottomWidth: 1,
    borderBottomColor: '#F3F4F6',
  },
  tableHeaderText: {
    fontSize: 10,
    fontWeight: 'bold',
    color: '#6B7280',
    textTransform: 'uppercase',
  },
  tableCellText: {
    fontSize: 10,
    color: '#111827',
  },
  descriptionColumn: {
    flex: 3,
  },
  qtyColumn: {
    flex: 1,
    textAlign: 'center',
  },
  rateColumn: {
    flex: 1.5,
    textAlign: 'right',
  },
  amountColumn: {
    flex: 1.5,
    textAlign: 'right',
  },
  totalsSection: {
    marginTop: 10,
    alignItems: 'flex-end',
  },
  totalRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    width: 230,
    marginBottom: 4,
  },
  totalLabel: {
    fontSize: 10,
    color: '#6B7280',
  },
  totalValue: {
    fontSize: 10,
    color: '#111827',
    textAlign: 'right',
  },
  grandTotalRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    width: 230,
    marginTop: 6,
    paddingTop: 6,
    borderTopWidth: 2,
    borderTopColor: '#E5E7EB',
  },
  grandTotalLabel: {
    fontSize: 12,
    fontWeight: 'bold',
    color: '#111827',
  },
  grandTotalValue: {
    fontSize: 12,
    fontWeight: 'bold',
    color: '#111827',
    textAlign: 'right',
  },
  footer: {
    marginTop: 16,
    paddingTop: 10,
    borderTopWidth: 1,
    borderTopColor: '#E5E7EB',
  },
  footerSection: {
    marginBottom: 10,
  },
  footerTitle: {
    fontSize: 10,
    fontWeight: 'bold',
    color: '#6B7280',
    marginBottom: 4,
    textTransform: 'uppercase',
  },
  footerText: {
    fontSize: 10,
    color: '#6B7280',
    lineHeight: 1.2,
  },
  signatureSection: {
    marginTop: 12,
    flexDirection: 'row',
    justifyContent: 'flex-end',
  },
  signatureBox: {
    width: 160,
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
    width: 130,
    borderTopWidth: 1,
    borderTopColor: '#111827',
    marginTop: 6,
    marginBottom: 2,
  },
  signatureLabel: {
    fontSize: 8,
    color: '#6B7280',
  },
});

interface InvoicePDFProps {
  invoice: Invoice;
  invoiceItems: InvoiceItem[];
  customer?: Customer;
  company?: Company;
  templateKey?: 'minimal-classic' | 'minimal-ledger';
}

const getStatusColor = (status?: string) => {
  switch (status?.toLowerCase()) {
    case 'paid':
      return '#10B981';
    case 'sent':
      return '#3B82F6';
    case 'overdue':
      return '#EF4444';
    case 'draft':
      return '#6B7280';
    default:
      return '#6B7280';
  }
};

export const InvoicePDFDocument: React.FC<InvoicePDFProps> = ({
  invoice,
  invoiceItems,
  customer,
  company,
  templateKey
}) => {
  const formatCurrency = (amount: number) => {
    const symbol = invoice.currency === 'EUR' ? '€' : invoice.currency === 'GBP' ? '£' : invoice.currency === 'INR' ? '₹' : '$';
    return `${symbol}${amount.toFixed(2)}`;
  };

  if (templateKey === 'minimal-ledger') {
    return (
      <Document>
        <InvoicePDF_Ledger invoice={invoice} invoiceItems={invoiceItems} customer={customer} company={company} />
      </Document>
    )
  }

  return (
    <Document>
      <Page size="A4" style={styles.page}>
        {/* Page Header */}
        <View style={styles.pageHeader} fixed>
          <Text style={styles.headerText}>{company?.name || 'Invoice'}</Text>
          <Text style={styles.headerText} render={({ pageNumber, totalPages }) => `Page ${pageNumber} of ${totalPages}`} />
        </View>

        {/* Company Header */}
        <View style={styles.header}>
          <View style={styles.companySection}>
            <Text style={styles.companyName}>{company?.name || 'Your Company'}</Text>
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
              {company?.email && <Text>{company.email}</Text>}
              {company?.phone && <Text>{company.phone}</Text>}
              {company?.taxNumber && <Text>Tax ID: {company.taxNumber}</Text>}
            </View>
          </View>
        </View>

        {/* Invoice Title and Status */}
        <View style={styles.invoiceHeader}>
          <View>
            <Text style={styles.invoiceTitle}>INVOICE</Text>
            <Text style={styles.invoiceNumber}>#{invoice.invoiceNumber}</Text>
            <View style={[styles.statusBadge, { backgroundColor: getStatusColor(invoice.status) + '20' }]}>
              <Text style={[styles.statusText, { color: getStatusColor(invoice.status) }]}>
                {invoice.status?.toUpperCase() || 'DRAFT'}
              </Text>
            </View>
          </View>
          <View style={{ alignItems: 'flex-end' }}>
            <Text style={{ fontSize: 20, fontWeight: 'bold', color: '#111827', marginBottom: 4 }}>
              {formatCurrency(invoice.totalAmount)}
            </Text>
            <Text style={{ fontSize: 10, color: '#6B7280' }}>
              Invoice Date: {format(new Date(invoice.invoiceDate), 'MMM dd, yyyy')}
            </Text>
            <Text style={{ fontSize: 10, color: '#6B7280' }}>
              Due Date: {format(new Date(invoice.dueDate), 'MMM dd, yyyy')}
            </Text>
          </View>
        </View>

        {/* Bill To and Invoice Details */}
        <View style={styles.billSection}>
          <View style={{ flex: 1 }}>
            <Text style={styles.sectionTitle}>Bill To</Text>
            <View style={styles.sectionContent}>
              <Text style={{ fontWeight: 'bold', marginBottom: 4 }}>
                {customer?.name || 'Unknown Customer'}
              </Text>
              {customer?.companyName && <Text>{customer.companyName}</Text>}
              {customer?.addressLine1 && <Text>{customer.addressLine1}</Text>}
              {customer?.addressLine2 && <Text>{customer.addressLine2}</Text>}
              {(customer?.city || customer?.state || customer?.zipCode) && (
                <Text>
                  {[customer?.city, customer?.state, customer?.zipCode]
                    .filter(Boolean)
                    .join(', ')}
                </Text>
              )}
              {customer?.email && <Text>{customer.email}</Text>}
              {customer?.taxNumber && <Text>Tax ID: {customer.taxNumber}</Text>}
            </View>
          </View>

          {(invoice.poNumber || invoice.projectName) && (
            <View style={{ flex: 1, alignItems: 'flex-end' }}>
              <Text style={styles.sectionTitle}>Reference</Text>
              <View style={styles.sectionContent}>
                {invoice.poNumber && (
                  <Text>PO Number: {invoice.poNumber}</Text>
                )}
                {invoice.projectName && (
                  <Text>Project: {invoice.projectName}</Text>
                )}
              </View>
            </View>
          )}
        </View>

        {/* Line Items Table */}
        <View style={styles.table}>
          {/* Table Header */}
          <View style={styles.tableHeader}>
            <Text style={[styles.tableHeaderText, styles.descriptionColumn]}>Description</Text>
            <Text style={[styles.tableHeaderText, styles.qtyColumn]}>Qty</Text>
            <Text style={[styles.tableHeaderText, styles.rateColumn]}>Rate</Text>
            <Text style={[styles.tableHeaderText, styles.amountColumn]}>Amount</Text>
          </View>

          {/* Table Rows */}
          {invoiceItems.map((item, index) => (
            <View key={item.id || index} style={styles.tableRow}>
              <Text style={[styles.tableCellText, styles.descriptionColumn]}>
                {item.description}
              </Text>
              <Text style={[styles.tableCellText, styles.qtyColumn]}>
                {item.quantity}
              </Text>
              <Text style={[styles.tableCellText, styles.rateColumn]}>
                {formatCurrency(item.unitPrice)}
              </Text>
              <Text style={[styles.tableCellText, styles.amountColumn]}>
                {formatCurrency(item.lineTotal)}
              </Text>
            </View>
          ))}
        </View>

        {/* Totals */}
        <View style={styles.totalsSection}>
          <View style={styles.totalRow}>
            <Text style={styles.totalLabel}>Subtotal</Text>
            <Text style={styles.totalValue}>{formatCurrency(invoice.subtotal)}</Text>
          </View>

          {invoice.taxAmount && invoice.taxAmount > 0 && (
            <View style={styles.totalRow}>
              <Text style={styles.totalLabel}>Tax</Text>
              <Text style={styles.totalValue}>{formatCurrency(invoice.taxAmount)}</Text>
            </View>
          )}

          {invoice.discountAmount && invoice.discountAmount > 0 && (
            <View style={styles.totalRow}>
              <Text style={styles.totalLabel}>Discount</Text>
              <Text style={styles.totalValue}>-{formatCurrency(invoice.discountAmount)}</Text>
            </View>
          )}

          <View style={styles.grandTotalRow}>
            <Text style={styles.grandTotalLabel}>Total</Text>
            <Text style={styles.grandTotalValue}>{formatCurrency(invoice.totalAmount)}</Text>
          </View>

          {invoice.paidAmount && invoice.paidAmount > 0 && (
            <View style={[styles.totalRow, { marginTop: 8 }]}>
              <Text style={[styles.totalLabel, { color: '#10B981' }]}>Paid</Text>
              <Text style={[styles.totalValue, { color: '#10B981' }]}>
                {formatCurrency(invoice.paidAmount)}
              </Text>
            </View>
          )}

          {invoice.paidAmount && invoice.paidAmount < invoice.totalAmount && (
            <View style={styles.totalRow}>
              <Text style={[styles.totalLabel, { fontWeight: 'bold' }]}>Balance Due</Text>
              <Text style={[styles.totalValue, { fontWeight: 'bold', color: '#EF4444' }]}>
                {formatCurrency(invoice.totalAmount - invoice.paidAmount)}
              </Text>
            </View>
          )}
        </View>

        {/* Signature Section */}
        {company?.signatureData && (
          <View style={styles.signatureSection}>
            <View style={styles.signatureBox}>
              {company.signatureType === 'typed' ? (
                <Text style={[
                  styles.signatureText,
                  {
                    fontFamily: company.signatureFont === '"Dancing Script", cursive' ? 'Helvetica-Oblique' :
                               company.signatureFont === '"Great Vibes", cursive' ? 'Helvetica-Oblique' :
                               company.signatureFont === '"Pacifico", cursive' ? 'Helvetica-Oblique' :
                               company.signatureFont === '"Satisfy", cursive' ? 'Helvetica-Oblique' :
                               company.signatureFont === '"Caveat", cursive' ? 'Helvetica-Oblique' :
                               'Helvetica-Oblique',
                    color: company.signatureColor || '#000000'
                  }
                ]}>
                  {company.signatureName || company.signatureData}
                </Text>
              ) : (
                company.signatureType === 'drawn' || company.signatureType === 'uploaded' ? (
                  <Image style={styles.signatureImage} src={company.signatureData} />
                ) : null
              )}
              <View style={styles.signatureLine} />
              <Text style={styles.signatureLabel}>Authorized Signature</Text>
            </View>
          </View>
        )}

        {/* Content Footer - Notes, Terms, Payment Instructions */}
        <View style={styles.footer}>
          {invoice.notes && (
            <View style={styles.footerSection}>
              <Text style={styles.footerTitle}>Notes</Text>
              <Text style={styles.footerText}>{invoice.notes}</Text>
            </View>
          )}

          {invoice.terms && (
            <View style={styles.footerSection}>
              <Text style={styles.footerTitle}>Terms & Conditions</Text>
              <Text style={styles.footerText}>{invoice.terms}</Text>
            </View>
          )}

          {invoice.paymentInstructions && (
            <View style={styles.footerSection}>
              <Text style={styles.footerTitle}>Payment Instructions</Text>
              <Text style={styles.footerText}>{invoice.paymentInstructions}</Text>
            </View>
          )}

          {!invoice.paymentInstructions && company?.paymentInstructions && (
            <View style={styles.footerSection}>
              <Text style={styles.footerTitle}>Payment Instructions</Text>
              <Text style={styles.footerText}>{company.paymentInstructions}</Text>
            </View>
          )}
        </View>

        {/* Page Footer */}
        <View style={styles.pageFooter} fixed>
          <View>
            <Text style={styles.footerCompany}>{company?.name || ''}</Text>
            {company?.website && <Text style={styles.footerText}>{company.website}</Text>}
            {company?.email && <Text style={styles.footerText}>{company.email}</Text>}
          </View>
          <View style={{ alignItems: 'flex-end' }}>
            <Text style={styles.footerText}>Invoice #{invoice.invoiceNumber}</Text>
            <Text style={styles.footerText}>Generated on {format(new Date(), 'MMM dd, yyyy')}</Text>
          </View>
        </View>
      </Page>
    </Document>
  );
};

interface InvoicePDFDownloadProps extends InvoicePDFProps {
  fileName?: string;
  label?: string;
  className?: string;
}

export const InvoicePDFDownload: React.FC<InvoicePDFDownloadProps> = ({
  invoice,
  invoiceItems,
  customer,
  company,
  templateKey,
  fileName,
  label = 'Download',
  className,
}) => {
  const defaultFileName = `Invoice-${invoice.invoiceNumber}-${format(new Date(), 'yyyy-MM-dd')}.pdf`;

  return (
    <PDFDownloadLink
      document={
        <InvoicePDFDocument
          invoice={invoice}
          invoiceItems={invoiceItems}
          customer={customer}
          company={company}
          templateKey={templateKey}
        />
      }
      fileName={fileName || defaultFileName}
    >
      {({ loading }) => (
        <button
          type="button"
          className={className}
          disabled={loading}
        >
          {loading ? 'Preparing…' : label}
        </button>
      )}
    </PDFDownloadLink>
  );
};
