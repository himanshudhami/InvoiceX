import { useCompanies, useCustomers, useProducts } from '@/hooks/api'

/**
 * Test page to verify API integration with the backend
 * This demonstrates React Query hooks fetching data from the .NET API
 */
const ApiTest = () => {
  const { data: companies, isLoading: companiesLoading, error: companiesError } = useCompanies()
  const { data: customers, isLoading: customersLoading, error: customersError } = useCustomers()
  const { data: products, isLoading: productsLoading, error: productsError } = useProducts()

  return (
    <div className="container mx-auto p-6 space-y-8">
      <h1 className="text-3xl font-bold text-gray-900">API Integration Test</h1>
      <p className="text-gray-600">Testing connection to .NET backend API at http://localhost:5000/api</p>

      {/* Companies Section */}
      <div className="bg-white p-6 rounded-lg shadow">
        <h2 className="text-xl font-semibold mb-4">Companies</h2>
        {companiesLoading && (
          <div className="flex items-center space-x-2">
            <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-600"></div>
            <span>Loading companies...</span>
          </div>
        )}
        {companiesError && (
          <div className="text-red-600 p-3 bg-red-50 rounded">
            Error loading companies: {companiesError.message}
          </div>
        )}
        {companies && (
          <div>
            <p className="text-gray-600 mb-2">Found {companies.length} companies</p>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {companies.slice(0, 4).map((company) => (
                <div key={company.id} className="border p-3 rounded">
                  <h3 className="font-medium">{company.name}</h3>
                  <p className="text-sm text-gray-600">
                    {company.city}, {company.state} {company.zipCode}
                  </p>
                  <p className="text-xs text-gray-500">{company.email}</p>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>

      {/* Customers Section */}
      <div className="bg-white p-6 rounded-lg shadow">
        <h2 className="text-xl font-semibold mb-4">Customers</h2>
        {customersLoading && (
          <div className="flex items-center space-x-2">
            <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-green-600"></div>
            <span>Loading customers...</span>
          </div>
        )}
        {customersError && (
          <div className="text-red-600 p-3 bg-red-50 rounded">
            Error loading customers: {customersError.message}
          </div>
        )}
        {customers && (
          <div>
            <p className="text-gray-600 mb-2">Found {customers.length} customers</p>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {customers.slice(0, 4).map((customer) => (
                <div key={customer.id} className="border p-3 rounded">
                  <h3 className="font-medium">{customer.name}</h3>
                  <p className="text-sm text-gray-600">{customer.companyName}</p>
                  <p className="text-xs text-gray-500">{customer.email}</p>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>

      {/* Products Section */}
      <div className="bg-white p-6 rounded-lg shadow">
        <h2 className="text-xl font-semibold mb-4">Products</h2>
        {productsLoading && (
          <div className="flex items-center space-x-2">
            <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-purple-600"></div>
            <span>Loading products...</span>
          </div>
        )}
        {productsError && (
          <div className="text-red-600 p-3 bg-red-50 rounded">
            Error loading products: {productsError.message}
          </div>
        )}
        {products && (
          <div>
            <p className="text-gray-600 mb-2">Found {products.length} products</p>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {products.slice(0, 4).map((product) => (
                <div key={product.id} className="border p-3 rounded">
                  <h3 className="font-medium">{product.name}</h3>
                  <p className="text-sm text-gray-600">{product.description}</p>
                  <p className="text-xs text-gray-500">
                    ${product.unitPrice} - {product.category}
                  </p>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>

      {/* API Status Summary */}
      <div className="bg-gray-50 p-6 rounded-lg">
        <h2 className="text-xl font-semibold mb-4">API Status Summary</h2>
        <div className="grid grid-cols-3 gap-4 text-center">
          <div className="p-4 bg-white rounded">
            <h3 className="font-medium">Companies API</h3>
            <div className={`mt-2 px-2 py-1 rounded text-sm ${
              companiesError ? 'bg-red-100 text-red-800' : 
              companiesLoading ? 'bg-yellow-100 text-yellow-800' : 
              'bg-green-100 text-green-800'
            }`}>
              {companiesError ? 'Error' : companiesLoading ? 'Loading' : 'Connected'}
            </div>
          </div>
          <div className="p-4 bg-white rounded">
            <h3 className="font-medium">Customers API</h3>
            <div className={`mt-2 px-2 py-1 rounded text-sm ${
              customersError ? 'bg-red-100 text-red-800' : 
              customersLoading ? 'bg-yellow-100 text-yellow-800' : 
              'bg-green-100 text-green-800'
            }`}>
              {customersError ? 'Error' : customersLoading ? 'Loading' : 'Connected'}
            </div>
          </div>
          <div className="p-4 bg-white rounded">
            <h3 className="font-medium">Products API</h3>
            <div className={`mt-2 px-2 py-1 rounded text-sm ${
              productsError ? 'bg-red-100 text-red-800' : 
              productsLoading ? 'bg-yellow-100 text-yellow-800' : 
              'bg-green-100 text-green-800'
            }`}>
              {productsError ? 'Error' : productsLoading ? 'Loading' : 'Connected'}
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

export default ApiTest