import React from 'react';
import { Document, Page, Text, View, StyleSheet, PDFDownloadLink, Image } from '@react-pdf/renderer';
import { Quote, QuoteItem, Customer, Company } from '@/services/api/types';
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
  quoteHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 12,
  },
  quoteTitle: {
    fontSize: 22,
    fontWeight: 'bold',
    color: '#111827',
    marginBottom: 2,
  },
  quoteNumber: {
    fontSize: 12,
    color: '#6B7280',
  },
  quoteDetails: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 8,
  },
  detailsSection: {
    flex: 1,
  },
  detailsLabel: {
    fontSize: 10,
    color: '#6B7280',
    marginBottom: 2,
  },
  detailsValue: {
    fontSize: 12,
    color: '#111827',
    marginBottom: 4,
  },
  customerSection: {
    marginBottom: 10,
  },
  sectionTitle: {
    fontSize: 12,
    fontWeight: 'bold',
    color: '#111827',
    marginBottom: 4,
  },
  table: {
    marginTop: 10,
    marginBottom: 10,
  },
  tableHeader: {
    flexDirection: 'row',
    backgroundColor: '#F9FAFB',
    padding: 6,
    borderBottom: '1px solid #E5E7EB',
  },
  tableHeaderText: {
    fontSize: 10,
    fontWeight: 'bold',
    color: '#374151',
  },
  tableRow: {
    flexDirection: 'row',
    padding: 6,
    borderBottom: '1px solid #F3F4F6',
  },
  tableCell: {
    fontSize: 10,
    color: '#374151',
  },
  descriptionCell: {
    flex: 2,
  },
  quantityCell: {
    flex: 1,
    textAlign: 'center',
  },
  priceCell: {
    flex: 1,
    textAlign: 'right',
  },
  totalCell: {
    flex: 1,
    textAlign: 'right',
    fontWeight: 'bold',
  },
  totals: {
    alignSelf: 'flex-end',
    width: '40%',
    marginTop: 10,
  },
  totalRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingVertical: 2,
    paddingHorizontal: 6,
  },
  totalLabel: {
    fontSize: 10,
    color: '#6B7280',
  },
  totalValue: {
    fontSize: 10,
    color: '#111827',
  },
  grandTotalRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingVertical: 6,
    paddingHorizontal: 8,
    backgroundColor: '#F9FAFB',
    borderTop: '1px solid #E5E7EB',
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
  },
  footer: {
    marginTop: 12,
  },
  footerSection: {
    marginBottom: 8,
  },
  footerTitle: {
    fontSize: 12,
    fontWeight: 'bold',
    color: '#111827',
    marginBottom: 4,
  },
  footerText: {
    fontSize: 10,
    color: '#6B7280',
    lineHeight: 1.2,
  },
  signatureSection: {
    marginTop: 12,
    alignItems: 'flex-end',
  },
  signatureBox: {
    width: 150,
    alignItems: 'center',
  },
  signatureText: {
    fontSize: 12,
    marginBottom: 6,
  },
  signatureImage: {
    width: 110,
    height: 44,
    marginBottom: 6,
  },
  signatureLine: {
    width: '100%',
    height: 1,
    backgroundColor: '#000000',
    marginBottom: 2,
  },
  signatureLabel: {
    fontSize: 8,
    color: '#6B7280',
  },
});

interface QuotePDFProps {
  quote: Quote;
  quoteItems: QuoteItem[];
  customer?: Customer;
  company?: Company;
  templateKey?: 'minimal-classic' | 'minimal-ledger';
}

export const QuotePDFDocument: React.FC<QuotePDFProps> = ({
  quote,
  quoteItems,
  customer,
  company
}) => {
  const formatCurrency = (amount: number) => {
    const currencySymbol = quote.currency === 'EUR' ? '€' : quote.currency === 'GBP' ? '£' : quote.currency === 'INR' ? '₹' : '$';
    return `${currencySymbol}${amount.toFixed(2)}`;
  };

  return (
    <Document>
      <Page size="A4" style={styles.page}>
        {/* Page Header */}
        <View style={styles.pageHeader} fixed>
          <Text style={styles.headerText}>{company?.name || 'Quote'}</Text>
          <Text style={styles.headerText} render={({ pageNumber, totalPages }) => `Page ${pageNumber} of ${totalPages}`} />
        </View>

        {/* Content Header */}
        <View style={styles.header}>
          {company?.logoUrl && (
            <Image src={company.logoUrl} style={{ width: 120, height: 60, marginBottom: 10 }} />
          )}

          <View style={styles.companySection}>
            <Text style={styles.companyName}>{company?.name || 'Company Name'}</Text>
            <Text style={styles.companyDetails}>
              {company?.addressLine1 && `${company.addressLine1}\n`}
              {company?.addressLine2 && `${company.addressLine2}\n`}
              {company?.city && `${company.city}, `}
              {company?.state && `${company.state} `}
              {company?.zipCode && `${company.zipCode}\n`}
              {company?.country && `${company.country}\n`}
              {company?.email && `Email: ${company.email}\n`}
              {company?.phone && `Phone: ${company.phone}\n`}
              {company?.website && `Website: ${company.website}`}
            </Text>
          </View>
        </View>

        {/* Quote Header */}
        <View style={styles.quoteHeader}>
          <View>
            <Text style={styles.quoteTitle}>QUOTE</Text>
            <Text style={styles.quoteNumber}>{quote.quoteNumber}</Text>
          </View>
        </View>

        {/* Quote & Customer Details */}
        <View style={styles.quoteDetails}>
          <View style={styles.detailsSection}>
            <Text style={styles.sectionTitle}>Quote Details</Text>
            <Text style={styles.detailsLabel}>Quote Date</Text>
            <Text style={styles.detailsValue}>{format(new Date(quote.quoteDate), 'MMMM dd, yyyy')}</Text>

            <Text style={styles.detailsLabel}>Valid Until</Text>
            <Text style={styles.detailsValue}>
              {quote.validUntil ? format(new Date(quote.validUntil), 'MMMM dd, yyyy') : '—'}
            </Text>
          </View>

          <View style={styles.detailsSection}>
            <Text style={styles.sectionTitle}>Bill To</Text>
            <Text style={styles.detailsValue}>{customer?.name || 'Customer Name'}</Text>
            {customer?.companyName && (
              <Text style={styles.detailsValue}>{customer.companyName}</Text>
            )}
            {customer?.addressLine1 && (
              <Text style={styles.detailsValue}>
                {customer.addressLine1}
                {customer.addressLine2 && `, ${customer.addressLine2}`}
              </Text>
            )}
            {(customer?.city || customer?.state || customer?.zipCode) && (
              <Text style={styles.detailsValue}>
                {customer.city && `${customer.city}, `}
                {customer.state && `${customer.state} `}
                {customer.zipCode && customer.zipCode}
              </Text>
            )}
            {customer?.country && (
              <Text style={styles.detailsValue}>{customer.country}</Text>
            )}
            {customer?.email && (
              <Text style={styles.detailsValue}>{customer.email}</Text>
            )}
            {customer?.phone && (
              <Text style={styles.detailsValue}>{customer.phone}</Text>
            )}
          </View>
        </View>

        {/* Line Items Table */}
        <View style={styles.table}>
          <View style={styles.tableHeader}>
            <Text style={[styles.tableHeaderText, styles.descriptionCell]}>Description</Text>
            <Text style={[styles.tableHeaderText, styles.quantityCell]}>Qty</Text>
            <Text style={[styles.tableHeaderText, styles.priceCell]}>Unit Price</Text>
            <Text style={[styles.tableHeaderText, styles.totalCell]}>Total</Text>
          </View>

          {quoteItems.map((item) => (
            <View key={item.id} style={styles.tableRow}>
              <Text style={[styles.tableCell, styles.descriptionCell]}>{item.description}</Text>
              <Text style={[styles.tableCell, styles.quantityCell]}>{item.quantity}</Text>
              <Text style={[styles.tableCell, styles.priceCell]}>{formatCurrency(item.unitPrice)}</Text>
              <Text style={[styles.tableCell, styles.totalCell]}>{formatCurrency(item.lineTotal)}</Text>
            </View>
          ))}
        </View>

        {/* Totals */}
        <View style={styles.totals}>
          <View style={styles.totalRow}>
            <Text style={styles.totalLabel}>Subtotal</Text>
            <Text style={styles.totalValue}>{formatCurrency(quote.subtotal)}</Text>
          </View>

          {quote.discountAmount && quote.discountAmount > 0 && (
            <View style={styles.totalRow}>
              <Text style={styles.totalLabel}>Discount</Text>
              <Text style={styles.totalValue}>-{formatCurrency(quote.discountAmount)}</Text>
            </View>
          )}

          {quote.taxAmount && quote.taxAmount > 0 && (
            <View style={styles.totalRow}>
              <Text style={styles.totalLabel}>Tax</Text>
              <Text style={styles.totalValue}>{formatCurrency(quote.taxAmount)}</Text>
            </View>
          )}

          <View style={styles.grandTotalRow}>
            <Text style={styles.grandTotalLabel}>Total</Text>
            <Text style={styles.grandTotalValue}>{formatCurrency(quote.totalAmount)}</Text>
          </View>
        </View>

        {/* Footer - Notes, Terms, Payment Instructions */}
        <View style={styles.footer}>
          {quote.notes && (
            <View style={styles.footerSection}>
              <Text style={styles.footerTitle}>Notes</Text>
              <Text style={styles.footerText}>{quote.notes}</Text>
            </View>
          )}

          {quote.terms && (
            <View style={styles.footerSection}>
              <Text style={styles.footerTitle}>Terms & Conditions</Text>
              <Text style={styles.footerText}>{quote.terms}</Text>
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

        {/* Page Footer */}
        <View style={styles.pageFooter} fixed>
          <View>
            <Text style={styles.footerCompany}>{company?.name || ''}</Text>
            {company?.website && <Text style={styles.footerText}>{company.website}</Text>}
            {company?.email && <Text style={styles.footerText}>{company.email}</Text>}
          </View>
          <View style={{ alignItems: 'flex-end' }}>
            <Text style={styles.footerText}>Quote #{quote.quoteNumber}</Text>
            <Text style={styles.footerText}>Generated on {format(new Date(), 'MMM dd, yyyy')}</Text>
          </View>
        </View>
      </Page>
    </Document>
  );
};

interface QuotePDFDownloadProps extends QuotePDFProps {
  fileName?: string;
  children: React.ReactNode;
}

export const QuotePDFDownload: React.FC<QuotePDFDownloadProps> = ({
  quote,
  quoteItems,
  customer,
  company,
  templateKey,
  fileName,
  children
}) => {
  const defaultFileName = `Quote-${quote.quoteNumber}-${format(new Date(), 'yyyy-MM-dd')}.pdf`;

  return (
    <PDFDownloadLink
      document={
        <QuotePDFDocument
          quote={quote}
          quoteItems={quoteItems}
          customer={customer}
          company={company}
          templateKey={templateKey}
        />
      }
      fileName={fileName || defaultFileName}
    >
      {children}
    </PDFDownloadLink>
  );
};
