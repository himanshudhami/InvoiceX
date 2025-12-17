import React, { FC } from 'react'
import { PDFDownloadLink } from '@react-pdf/renderer'
import { Invoice, TInvoice } from '../data/types'
import InvoicePage from './InvoicePage'
import { saveAs } from 'file-saver'

interface Props {
  data: Invoice
  setData(data: Invoice): void
}

const Download: FC<Props> = ({ data, setData }) => {
  function handleInput(e: React.ChangeEvent<HTMLInputElement>) {
    if (!e.target.files?.length) return

    const file = e.target.files[0]
    file
      .text()
      .then((str: string) => {
        try {
          if (!(str.startsWith('{') && str.endsWith('}'))) {
            str = atob(str)
          }
          const d = JSON.parse(str)
          const dParsed = TInvoice.parse(d)
          console.info('parsed correctly')
          setData(dParsed)
        } catch (e) {
          console.error(e)
          return
        }
      })
      .catch((err) => console.error(err))
  }

  function handleSaveTemplate() {
    const blob = new Blob([JSON.stringify(data)], {
      type: 'text/plain;charset=utf-8',
    })
    saveAs(blob, title + '.template')
  }

  const title = data.invoiceTitle ? data.invoiceTitle.toLowerCase() : 'invoice'
  return (
    <div className="fixed top-4 right-4 flex flex-col space-y-4 bg-white p-4 rounded-lg shadow-lg border z-10">
      <div className="flex flex-col items-center">
        <PDFDownloadLink
          key={`pdf-${JSON.stringify(data).substring(0, 50)}`}
          document={<InvoicePage pdfMode={true} data={data} />}
          fileName={`${title}.pdf`}
          aria-label="Save PDF"
          title="Save PDF"
          className="w-12 h-12 bg-blue-500 hover:bg-blue-600 rounded-lg flex items-center justify-center text-white transition-colors"
        >
          📄
        </PDFDownloadLink>
        <p className="text-xs mt-1 text-gray-600">Save PDF</p>
      </div>

      <div className="flex flex-col items-center">
        <button
          onClick={handleSaveTemplate}
          aria-label="Save Template"
          title="Save Template"
          className="w-12 h-12 bg-green-500 hover:bg-green-600 rounded-lg flex items-center justify-center text-white transition-colors"
        >
          💾
        </button>
        <p className="text-xs mt-1 text-gray-600">Save Template</p>
      </div>

      <div className="flex flex-col items-center">
        <label className="w-12 h-12 bg-purple-500 hover:bg-purple-600 rounded-lg flex items-center justify-center text-white transition-colors cursor-pointer">
          📁
          <input type="file" accept=".json,.template" onChange={handleInput} className="hidden" />
        </label>
        <p className="text-xs mt-1 text-gray-600">Upload Template</p>
      </div>
    </div>
  )
}

export default Download
