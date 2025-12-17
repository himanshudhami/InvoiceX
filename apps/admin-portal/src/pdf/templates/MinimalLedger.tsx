import { Page, Text, View, StyleSheet, Image } from '@react-pdf/renderer'
import { Invoice, InvoiceItem, Customer, Company } from '@/services/api/types'

const styles = StyleSheet.create({
  page: {
    paddingTop: 50,
    paddingBottom: 60,
    paddingHorizontal: 30,
    fontFamily: 'NotoSans'
  },
  // Header Styles
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
  // Footer Styles
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
  // Content Styles
  title: { fontSize: 22, marginBottom: 6, color: '#111827', fontWeight: 'bold' },
  infoRow: { flexDirection: 'row', gap: 20, marginBottom: 12 },
  label: { color: '#6B7280', fontSize: 10, marginBottom: 4, fontWeight: 'bold', textTransform: 'uppercase' },
  value: { color: '#111827', fontSize: 10 },
  topRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 16 },
  logoBox: { width: 60, height: 60, borderWidth: 1, borderColor: '#E5E7EB', borderRadius: 4, alignItems: 'center', justifyContent: 'center' },
  columns: { flexDirection: 'row', justifyContent: 'space-between', marginTop: 8, marginBottom: 16 },
  col: { width: '48%' },
  addressLine: { fontSize: 10, color: '#111827', marginBottom: 2, lineHeight: 1.4 },
  table: { marginTop: 8 },
  thead: { flexDirection: 'row', paddingVertical: 8, backgroundColor: '#F9FAFB', borderTopWidth: 1, borderTopColor: '#E5E7EB', borderBottomWidth: 1, borderBottomColor: '#E5E7EB' },
  th: { fontSize: 10, color: '#6B7280', textTransform: 'uppercase', fontWeight: 'bold', letterSpacing: 0.5 },
  tr: { flexDirection: 'row', paddingVertical: 8, borderBottomWidth: 1, borderBottomColor: '#F3F4F6' },
  cellItem: { flex: 3 },
  cellQty: { flex: 1, textAlign: 'center' },
  cellPrice: { flex: 1.2, textAlign: 'right' },
  cellTotal: { flex: 1.2, textAlign: 'right' },
  totals: { marginTop: 16, alignItems: 'flex-end' },
  totalRow: { flexDirection: 'row', width: 240, justifyContent: 'space-between', marginBottom: 6 },
  hr: { height: 1, backgroundColor: '#E5E7EB', width: 240, marginVertical: 6 },
  totalLabel: { fontSize: 10, color: '#6B7280' },
  totalValue: { fontSize: 10, color: '#111827' },
  grandTotal: { fontSize: 16, color: '#111827', fontWeight: 'bold' },
  notesSection: { marginTop: 20, flexDirection: 'row', gap: 20 },
  notesBox: { flex: 1 },
  notesTitle: { fontSize: 10, color: '#6B7280', fontWeight: 'bold', marginBottom: 6, textTransform: 'uppercase' },
  notesText: { fontSize: 10, color: '#111827', lineHeight: 1.4 },
  signatureSection: { marginTop: 20, flexDirection: 'row', justifyContent: 'flex-end' },
  signatureBox: { width: 160, alignItems: 'center' },
  signatureImage: { width: 120, height: 48, marginBottom: 6 },
  signatureText: { fontSize: 11, fontFamily: 'Helvetica-Oblique', marginBottom: 6 },
  signatureLine: { width: 130, borderTopWidth: 1, borderTopColor: '#111827', marginTop: 6, marginBottom: 2 },
  signatureLabel: { fontSize: 8, color: '#6B7280' },
})

export function InvoicePDF_Ledger({ invoice, invoiceItems, customer, company }:{ invoice:Invoice; invoiceItems:InvoiceItem[]; customer?:Customer; company?:Company }){
  const currency = invoice.currency === 'EUR' ? '€' : invoice.currency === 'GBP' ? '£' : invoice.currency === 'INR' ? '₹' : '$'
  const formatMoney = (n:number) => `${currency}${n.toFixed(2)}`

  return (
    <Page size="A4" style={styles.page}>
      {/* Page Header */}
      <View style={styles.pageHeader} fixed>
        <Text style={styles.headerText}>{company?.name || 'Invoice'}</Text>
        <Text style={styles.headerText} render={({ pageNumber, totalPages }) => `Page ${pageNumber} of ${totalPages}`} />
      </View>

      {/* Document Header */}
      <View style={styles.topRow}>
        <View>
          <Text style={styles.title}>Invoice</Text>
          <View style={styles.infoRow}>
            <View>
              <Text style={styles.label}>Invoice NO:</Text>
              <Text style={styles.value}>{invoice.invoiceNumber}</Text>
            </View>
            <View>
              <Text style={styles.label}>Issue date:</Text>
              <Text style={styles.value}>{new Date(invoice.invoiceDate).toLocaleDateString()}</Text>
            </View>
            <View>
              <Text style={styles.label}>Due date:</Text>
              <Text style={styles.value}>{new Date(invoice.dueDate).toLocaleDateString()}</Text>
            </View>
          </View>
        </View>
        {company?.logoUrl ? (
          <Image src={company.logoUrl} style={{ width: 60, height: 60, objectFit: 'contain' }} />
        ) : (
          <View style={styles.logoBox}><Text>L</Text></View>
        )}
      </View>

      {/* From / To */}
      <View style={styles.columns}>
        <View style={styles.col}>
          <Text style={styles.label}>From</Text>
          <Text style={styles.addressLine}>{company?.name}</Text>
          {company?.email && <Text style={styles.addressLine}>{company.email}</Text>}
          {company?.phone && <Text style={styles.addressLine}>{company.phone}</Text>}
          {company?.addressLine1 && <Text style={styles.addressLine}>{company.addressLine1}</Text>}
          {company?.addressLine2 && <Text style={styles.addressLine}>{company.addressLine2}</Text>}
          <Text style={styles.addressLine}>{[company?.city, company?.state, company?.country].filter(Boolean).join(', ')}</Text>
          {company?.taxNumber && <Text style={styles.addressLine}>VAT ID: {company.taxNumber}</Text>}
        </View>
        <View style={styles.col}>
          <Text style={styles.label}>To</Text>
          <Text style={styles.addressLine}>{customer?.companyName || customer?.name}</Text>
          {customer?.email && <Text style={styles.addressLine}>{customer.email}</Text>}
          {customer?.phone && <Text style={styles.addressLine}>{customer.phone}</Text>}
          {customer?.addressLine1 && <Text style={styles.addressLine}>{customer.addressLine1}</Text>}
          {customer?.addressLine2 && <Text style={styles.addressLine}>{customer.addressLine2}</Text>}
          <Text style={styles.addressLine}>{[customer?.city, customer?.state, customer?.country].filter(Boolean).join(', ')}</Text>
          {customer?.taxNumber && <Text style={styles.addressLine}>VAT ID: {customer.taxNumber}</Text>}
        </View>
      </View>

      {/* Items */}
      <View style={styles.table}>
        <View style={styles.thead}>
          <Text style={[styles.th, styles.cellItem]}>Item</Text>
          <Text style={[styles.th, styles.cellQty]}>Quantity</Text>
          <Text style={[styles.th, styles.cellPrice]}>Price</Text>
          <Text style={[styles.th, styles.cellTotal]}>Total</Text>
        </View>
        {invoiceItems.map((it, idx) => (
          <View key={it.id || idx} style={styles.tr}>
            <Text style={[styles.value, styles.cellItem]}>{it.description}</Text>
            <Text style={[styles.value, styles.cellQty]}>{it.quantity}</Text>
            <Text style={[styles.value, styles.cellPrice]}>{formatMoney(it.unitPrice)}</Text>
            <Text style={[styles.value, styles.cellTotal]}>{formatMoney(it.lineTotal)}</Text>
          </View>
        ))}
      </View>

      {/* Totals */}
      <View style={styles.totals}>
        <View style={styles.totalRow}>
          <Text style={styles.totalLabel}>Subtotal</Text>
          <Text style={styles.totalValue}>{formatMoney(invoice.subtotal)}</Text>
        </View>
        <View style={styles.hr} />
        {invoice.taxAmount ? (
          <View style={styles.totalRow}>
            <Text style={styles.totalLabel}>VAT</Text>
            <Text style={styles.totalValue}>{formatMoney(invoice.taxAmount)}</Text>
          </View>
        ) : null}
        <View style={styles.hr} />
        <View style={[styles.totalRow, { marginTop: 6 }]}>
          <Text style={styles.totalLabel}>Total</Text>
          <Text style={styles.grandTotal}>{formatMoney(invoice.totalAmount)}</Text>
        </View>
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

      {/* Notes and Payment Details */}
      {(invoice.notes || invoice.paymentInstructions || company?.paymentInstructions) && (
        <View style={styles.notesSection}>
          {invoice.notes && (
            <View style={styles.notesBox}>
              <Text style={styles.notesTitle}>Notes</Text>
              <Text style={styles.notesText}>{invoice.notes}</Text>
            </View>
          )}
          {(invoice.paymentInstructions || company?.paymentInstructions) && (
            <View style={styles.notesBox}>
              <Text style={styles.notesTitle}>Payment Instructions</Text>
              <Text style={styles.notesText}>{invoice.paymentInstructions || company?.paymentInstructions}</Text>
            </View>
          )}
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
          <Text style={styles.footerText}>Invoice #{invoice.invoiceNumber}</Text>
          <Text style={styles.footerText}>Generated on {new Date().toLocaleDateString()}</Text>
        </View>
      </View>
    </Page>
  )
}

