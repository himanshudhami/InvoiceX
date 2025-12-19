import { FC, useState, useEffect } from 'react'
import { Invoice, ProductLine } from '../data/types'
import { initialInvoice, initialProductLine } from '../data/initialData'
import EditableInput from './EditableInput'
import EditableSelect from './EditableSelect'
import EditableTextarea from './EditableTextarea'
import EditableCalendarInput from './EditableCalendarInput'
import EditableFileImage from './EditableFileImage'
import CustomerSelector from './CustomerSelector'
import ProjectSelector from './ProjectSelector'
import CompanySelector from './CompanySelector'
import countryList from '../data/countryList'
import Document from './Document'
import Page from './Page'
import View from './View'
import Text from './Text'
import Download from './DownloadPDF'
import { format } from 'date-fns/format'
import { Customer, Company } from '@/services/api/types'

// Font registration is now handled centrally in utils/pdfFonts.ts

interface Props {
  data?: Invoice
  pdfMode?: boolean
  onChange?: (invoice: Invoice) => void
}

const InvoicePage: FC<Props> = ({ data, pdfMode, onChange }) => {
  const [invoice, setInvoice] = useState<Invoice>(data ? { ...data } : { ...initialInvoice })
  const [subTotal, setSubTotal] = useState<number>()
  const [saleTax, setSaleTax] = useState<number>()
  const [selectedCustomer, setSelectedCustomer] = useState<Customer | undefined>()
  const [projectName, setProjectName] = useState<string>('')
  const [selectedCompany, setSelectedCompany] = useState<Company | undefined>()

  const dateFormat = 'MMM dd, yyyy'
  const invoiceDate = invoice.invoiceDate !== '' ? new Date(invoice.invoiceDate) : new Date()
  const invoiceDueDate =
    invoice.invoiceDueDate !== ''
      ? new Date(invoice.invoiceDueDate)
      : new Date(invoiceDate.valueOf())

  if (invoice.invoiceDueDate === '') {
    invoiceDueDate.setDate(invoiceDueDate.getDate() + 30)
  }

  const handleChange = (name: keyof Invoice, value: string | number) => {
    if (name !== 'productLines') {
      const newInvoice = { ...invoice }

      if (name === 'logoWidth' && typeof value === 'number') {
        newInvoice[name] = value
      } else if (name !== 'logoWidth' && typeof value === 'string') {
        newInvoice[name] = value
      }

      setInvoice(newInvoice)
    }
  }

  const handleProductLineChange = (index: number, name: keyof ProductLine, value: string) => {
    const productLines = invoice.productLines.map((productLine, i) => {
      if (i === index) {
        const newProductLine = { ...productLine }

        if (name === 'description') {
          newProductLine[name] = value
        } else {
          if (
            value[value.length - 1] === '.' ||
            (value[value.length - 1] === '0' && value.includes('.'))
          ) {
            newProductLine[name] = value
          } else {
            const n = parseFloat(value)

            newProductLine[name] = (n ? n : 0).toString()
          }
        }

        return newProductLine
      }

      return { ...productLine }
    })

    setInvoice({ ...invoice, productLines })
  }

  const handleRemove = (i: number) => {
    const productLines = invoice.productLines.filter((_, index) => index !== i)

    setInvoice({ ...invoice, productLines })
  }

  const handleAdd = () => {
    const productLines = [...invoice.productLines, { ...initialProductLine }]

    setInvoice({ ...invoice, productLines })
  }

  const handleCustomerSelect = (customer: Customer) => {
    setSelectedCustomer(customer)
    setInvoice({
      ...invoice,
      clientName: customer.name,
      clientAddress: customer.address_line1,
      clientAddress2: customer.address_line2 ? `${customer.city}, ${customer.state} ${customer.zip_code}` : `${customer.city}, ${customer.state} ${customer.zip_code}`,
      clientCountry: customer.country
    })
  }

  const handleProjectNameChange = (value: string) => {
    setProjectName(value)
    setInvoice({
      ...invoice,
      clientAddress: value
    })
  }

  const handleCompanySelect = (company: Company) => {
    setSelectedCompany(company)
    setInvoice({
      ...invoice,
      companyName: company.name,
      companyAddress: company.address_line1,
      companyAddress2: company.address_line2 ? `${company.city}, ${company.state} ${company.zip_code}` : `${company.city}, ${company.state} ${company.zip_code}`,
      companyCountry: company.country
    })
  }

  const calculateAmount = (quantity: string, rate: string) => {
    const quantityNumber = parseFloat(quantity)
    const rateNumber = parseFloat(rate)
    const amount = quantityNumber && rateNumber ? quantityNumber * rateNumber : 0

    return amount.toFixed(2)
  }

  useEffect(() => {
    let subTotal = 0

    invoice.productLines.forEach((productLine) => {
      const quantityNumber = parseFloat(productLine.quantity)
      const rateNumber = parseFloat(productLine.rate)
      const amount = quantityNumber && rateNumber ? quantityNumber * rateNumber : 0

      subTotal += amount
    })

    setSubTotal(subTotal)
  }, [invoice.productLines])

  useEffect(() => {
    const match = invoice.taxLabel.match(/(\d+)%/)
    const taxRate = match ? parseFloat(match[1]) : 0
    const saleTax = subTotal ? (subTotal * taxRate) / 100 : 0

    setSaleTax(saleTax)
  }, [subTotal, invoice.taxLabel])

  useEffect(() => {
    if (onChange) {
      onChange(invoice)
    }
  }, [onChange, invoice])

  // Initialize selected entities from invoice data on mount
  useEffect(() => {
    if (data && !selectedCustomer && !selectedCompany) {
      // This will help maintain selected state when PDF is generated
      // The actual values are already in the invoice object
    }
  }, [data, selectedCustomer, selectedCompany])

  return (
    <Document pdfMode={pdfMode}>
      <Page className="invoice-wrapper" pdfMode={pdfMode}>
        {!pdfMode && <Download data={invoice} setData={(d) => setInvoice(d)} />}

        <View className="center mb-20" pdfMode={pdfMode}>
          <EditableFileImage
            className="logo center"
            placeholder="Your Logo"
            value={invoice.logo}
            width={invoice.logoWidth}
            pdfMode={pdfMode}
            onChangeImage={(value) => handleChange('logo', value)}
            onChangeWidth={(value) => handleChange('logoWidth', value)}
          />
          {!pdfMode ? (
            <CompanySelector
              selectedCompany={selectedCompany}
              onCompanySelect={handleCompanySelect}
              pdfMode={pdfMode}
            />
          ) : (
            <Text className="fs-20 bold center" pdfMode={pdfMode}>{invoice.companyName}</Text>
          )}
        </View>

        <View className="flex mt-30" pdfMode={pdfMode}>
          <View className="w-50" pdfMode={pdfMode}>
            <EditableInput
              className="fs-45 bold blue"
              placeholder="INVOICE"
              value={invoice.title}
              onChange={(value) => handleChange('title', value)}
              pdfMode={pdfMode}
            />
          </View>
          <View className="w-50" pdfMode={pdfMode}>
            <View className="flex mb-5" pdfMode={pdfMode}>
              <View className="w-40" pdfMode={pdfMode}>
                <EditableInput
                  className="bold"
                  value={invoice.billTo}
                  onChange={(value) => handleChange('billTo', value)}
                  pdfMode={pdfMode}
                />
              </View>
              <View className="w-60" pdfMode={pdfMode}>
                {!pdfMode ? (
                  <CustomerSelector
                    selectedCustomer={selectedCustomer}
                    onCustomerSelect={handleCustomerSelect}
                    pdfMode={pdfMode}
                    companyId={invoice.companyId}
                  />
                ) : (
                  <Text pdfMode={pdfMode}>{invoice.clientName}</Text>
                )}
              </View>
            </View>
            <View className="flex mb-5" pdfMode={pdfMode}>
              <View className="w-40" pdfMode={pdfMode}>
                <EditableInput
                  className="bold"
                  value={invoice.invoiceDateLabel}
                  onChange={(value) => handleChange('invoiceDateLabel', value)}
                  pdfMode={pdfMode}
                />
              </View>
              <View className="w-60" pdfMode={pdfMode}>
                <EditableCalendarInput
                  value={format(invoiceDate, dateFormat)}
                  selected={invoiceDate}
                  onChange={(date) =>
                    handleChange(
                      'invoiceDate',
                      date && !Array.isArray(date) ? format(date, dateFormat) : '',
                    )
                  }
                  pdfMode={pdfMode}
                />
              </View>
            </View>
            <View className="flex mb-5" pdfMode={pdfMode}>
              <View className="w-40" pdfMode={pdfMode}>
                <EditableInput
                  className="bold"
                  value={invoice.invoiceDueDateLabel}
                  onChange={(value) => handleChange('invoiceDueDateLabel', value)}
                  pdfMode={pdfMode}
                />
              </View>
              <View className="w-60" pdfMode={pdfMode}>
                <EditableCalendarInput
                  value={format(invoiceDueDate, dateFormat)}
                  selected={invoiceDueDate}
                  onChange={(date) =>
                    handleChange(
                      'invoiceDueDate',
                      date ? (!Array.isArray(date) ? format(date, dateFormat) : '') : '',
                    )
                  }
                  pdfMode={pdfMode}
                />
              </View>
            </View>
            <View className="flex mb-5" pdfMode={pdfMode}>
              <View className="w-40" pdfMode={pdfMode}>
                <Text className="bold" pdfMode={pdfMode}>Project Name</Text>
              </View>
              <View className="w-60" pdfMode={pdfMode}>
                <ProjectSelector
                  value={projectName}
                  onChange={handleProjectNameChange}
                  pdfMode={pdfMode}
                />
              </View>
            </View>
          </View>
        </View>

        <View className="flex mt-10" pdfMode={pdfMode}>
          <View className="w-50" pdfMode={pdfMode}>
            <View className="flex mb-5" pdfMode={pdfMode}>
              <View className="w-40" pdfMode={pdfMode}>
                <EditableInput
                  className="bold"
                  value={invoice.invoiceTitleLabel}
                  onChange={(value) => handleChange('invoiceTitleLabel', value)}
                  pdfMode={pdfMode}
                />
              </View>
              <View className="w-60" pdfMode={pdfMode}>
                <EditableInput
                  placeholder="INV-12"
                  value={invoice.invoiceTitle}
                  onChange={(value) => handleChange('invoiceTitle', value)}
                  pdfMode={pdfMode}
                />
              </View>
            </View>
          </View>
          <View className="w-50" pdfMode={pdfMode}>
          </View>
        </View>

        <View className="mt-30 bg-dark flex" pdfMode={pdfMode}>
          <View className="w-48 p-4-8" pdfMode={pdfMode}>
            <EditableInput
              className="white bold"
              value={invoice.productLineDescription}
              onChange={(value) => handleChange('productLineDescription', value)}
              pdfMode={pdfMode}
            />
          </View>
          <View className="w-17 p-4-8" pdfMode={pdfMode}>
            <EditableInput
              className="white bold right"
              value={invoice.productLineQuantity}
              onChange={(value) => handleChange('productLineQuantity', value)}
              pdfMode={pdfMode}
            />
          </View>
          <View className="w-17 p-4-8" pdfMode={pdfMode}>
            <EditableInput
              className="white bold right"
              value={invoice.productLineQuantityRate}
              onChange={(value) => handleChange('productLineQuantityRate', value)}
              pdfMode={pdfMode}
            />
          </View>
          <View className="w-18 p-4-8" pdfMode={pdfMode}>
            <EditableInput
              className="white bold right"
              value={invoice.productLineQuantityAmount}
              onChange={(value) => handleChange('productLineQuantityAmount', value)}
              pdfMode={pdfMode}
            />
          </View>
        </View>

        {invoice.productLines.map((productLine, i) => {
          return pdfMode && productLine.description === '' ? (
            <Text key={i}></Text>
          ) : (
            <View key={i} className="row flex" pdfMode={pdfMode}>
              <View className="w-48 p-4-8" pdfMode={pdfMode}>
                <EditableTextarea
                  className="dark"
                  rows={2}
                  placeholder="Enter item name/description"
                  value={productLine.description}
                  onChange={(value) => handleProductLineChange(i, 'description', value)}
                  pdfMode={pdfMode}
                />
              </View>
              <View className="w-17 p-4-8" pdfMode={pdfMode}>
                <EditableInput
                  className="dark right"
                  value={productLine.quantity}
                  onChange={(value) => handleProductLineChange(i, 'quantity', value)}
                  pdfMode={pdfMode}
                />
              </View>
              <View className="w-17 p-4-8" pdfMode={pdfMode}>
                <EditableInput
                  className="dark right"
                  value={productLine.rate}
                  onChange={(value) => handleProductLineChange(i, 'rate', value)}
                  pdfMode={pdfMode}
                />
              </View>
              <View className="w-18 p-4-8" pdfMode={pdfMode}>
                <Text className="dark right" pdfMode={pdfMode}>
                  {calculateAmount(productLine.quantity, productLine.rate)}
                </Text>
              </View>
              {!pdfMode && (
                <button
                  className="absolute -right-8 top-2 w-6 h-6 bg-red-500 hover:bg-red-600 text-white rounded-full flex items-center justify-center text-xs transition-colors opacity-0 group-hover:opacity-100"
                  aria-label="Remove Row"
                  title="Remove Row"
                  onClick={() => handleRemove(i)}
                >
                  ×
                </button>
              )}
            </View>
          )
        })}

        <View className="flex" pdfMode={pdfMode}>
          <View className="w-50 mt-10" pdfMode={pdfMode}>
            {!pdfMode && (
              <button 
                className="inline-flex items-center px-3 py-2 text-sm bg-green-500 text-white rounded hover:bg-green-600 transition-colors"
                onClick={handleAdd}
              >
                <span className="mr-2">+</span>
                Add Line Item
              </button>
            )}
          </View>
          <View className="w-50 mt-10" pdfMode={pdfMode}>
            <View className="flex" pdfMode={pdfMode}>
              <View className="w-50 p-5" pdfMode={pdfMode}>
                <EditableInput
                  value={invoice.subTotalLabel}
                  onChange={(value) => handleChange('subTotalLabel', value)}
                  pdfMode={pdfMode}
                />
              </View>
              <View className="w-50 p-5" pdfMode={pdfMode}>
                <Text className="right bold dark" pdfMode={pdfMode}>
                  {subTotal?.toFixed(2)}
                </Text>
              </View>
            </View>
            <View className="flex" pdfMode={pdfMode}>
              <View className="w-50 p-5" pdfMode={pdfMode}>
                <EditableInput
                  value={invoice.taxLabel}
                  onChange={(value) => handleChange('taxLabel', value)}
                  pdfMode={pdfMode}
                />
              </View>
              <View className="w-50 p-5" pdfMode={pdfMode}>
                <Text className="right bold dark" pdfMode={pdfMode}>
                  {saleTax?.toFixed(2)}
                </Text>
              </View>
            </View>
            <View className="flex bg-gray p-5" pdfMode={pdfMode}>
              <View className="w-50 p-5" pdfMode={pdfMode}>
                <EditableInput
                  className="bold"
                  value={invoice.totalLabel}
                  onChange={(value) => handleChange('totalLabel', value)}
                  pdfMode={pdfMode}
                />
              </View>
              <View className="w-50 p-5 flex" pdfMode={pdfMode}>
                <EditableInput
                  className="dark bold right ml-30"
                  value={invoice.currency}
                  onChange={(value) => handleChange('currency', value)}
                  pdfMode={pdfMode}
                />
                <Text className="right bold dark w-auto" pdfMode={pdfMode}>
                  {(typeof subTotal !== 'undefined' && typeof saleTax !== 'undefined'
                    ? subTotal + saleTax
                    : 0
                  ).toFixed(2)}
                </Text>
              </View>
            </View>
          </View>
        </View>

        <View className="flex mt-30" pdfMode={pdfMode}>
          <View className="w-50" pdfMode={pdfMode}>
            <EditableInput
              className="bold"
              value={invoice.termLabel}
              onChange={(value) => handleChange('termLabel', value)}
              pdfMode={pdfMode}
            />
            <EditableTextarea
              className="w-100"
              rows={3}
              value={invoice.term}
              onChange={(value) => handleChange('term', value)}
              pdfMode={pdfMode}
            />
          </View>
          <View className="w-50" pdfMode={pdfMode}>
            <View className="right" pdfMode={pdfMode}>
              <Text className="bold mb-10" pdfMode={pdfMode}>
                AMOUNT DUE
              </Text>
              <Text className="fs-45 bold blue" pdfMode={pdfMode}>
                {`${invoice.currency} ${(typeof subTotal !== 'undefined' && typeof saleTax !== 'undefined'
                  ? subTotal + saleTax
                  : 0
                ).toFixed(2)}`}
              </Text>
            </View>
          </View>
        </View>
        
        <View className="center mt-40" pdfMode={pdfMode}>
          <Text className="fs-20 bold blue" pdfMode={pdfMode}>THANK YOU!</Text>
        </View>
        
        <View className="mt-40" pdfMode={pdfMode}>
          <View className="flex" pdfMode={pdfMode}>
            <View className="w-50" pdfMode={pdfMode}>
              <View className="mt-20" pdfMode={pdfMode}>
                <Text className="bold mb-5" pdfMode={pdfMode}>Authorized Signature:</Text>
                <View className="border-b-2 border-gray-300 h-16 mb-5" pdfMode={pdfMode}></View>
                <Text className="text-sm" pdfMode={pdfMode}>Name & Title</Text>
              </View>
            </View>
            <View className="w-50" pdfMode={pdfMode}>
              <View className="mt-20" pdfMode={pdfMode}>
                <Text className="bold mb-5" pdfMode={pdfMode}>Date:</Text>
                <View className="border-b-2 border-gray-300 h-16" pdfMode={pdfMode}></View>
              </View>
            </View>
          </View>
        </View>
      </Page>
    </Document>
  )
}

export default InvoicePage
