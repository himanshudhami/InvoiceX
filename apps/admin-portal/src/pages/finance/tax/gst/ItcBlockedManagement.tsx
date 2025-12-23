import { useState } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import {
  useItcBlockedCategories,
  useItcBlockedSummary,
  useItcAvailabilityReport,
} from '@/features/gst-compliance/hooks';
import { useCompanies } from '@/hooks/api/useCompanies';
import type { ItcBlockedCategory, ItcBlockedSummary } from '@/services/api/types';
import { DataTable } from '@/components/ui/DataTable';
import {
  FileX,
  Building2,
  AlertTriangle,
  Info,
  Receipt,
  Ban,
  Car,
  Utensils,
  Gift,
  Building,
  Wrench,
  FileText,
} from 'lucide-react';
import { cn } from '@/lib/utils';

// Generate return period options (last 12 months)
const generateReturnPeriods = () => {
  const periods = [];
  const now = new Date();
  for (let i = 0; i < 12; i++) {
    const date = new Date(now.getFullYear(), now.getMonth() - i, 1);
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const year = date.getFullYear();
    periods.push({
      value: `${month}${year}`,
      label: `${date.toLocaleString('default', { month: 'short' })} ${year}`,
    });
  }
  return periods;
};

// Category icons mapping
const getCategoryIcon = (categoryCode: string) => {
  const iconMap: Record<string, typeof Car> = {
    'MOTOR_VEHICLE': Car,
    'FOOD_BEVERAGE': Utensils,
    'CLUB_MEMBERSHIP': Gift,
    'RENT_A_CAB': Car,
    'TRAVEL': Car,
    'CONSTRUCTION': Building,
    'PERSONAL_CONSUMPTION': Gift,
    'GOODS_LOST': Ban,
    'GOODS_DESTROYED': Ban,
    'FREE_SAMPLES': Gift,
    'WORKS_CONTRACT': Wrench,
  };
  return iconMap[categoryCode] || FileX;
};

const ItcBlockedManagement = () => {
  const returnPeriods = generateReturnPeriods();
  const [selectedCompanyId, setSelectedCompanyId] = useState<string>('');
  const [selectedReturnPeriod, setSelectedReturnPeriod] = useState(returnPeriods[0].value);

  const { data: companies = [] } = useCompanies();

  // Fetch blocked categories (static reference data)
  const { data: blockedCategories = [], isLoading: loadingCategories } = useItcBlockedCategories();

  // Fetch summary for selected company and period
  const { data: summary, isLoading: loadingSummary } = useItcBlockedSummary(
    selectedCompanyId,
    selectedReturnPeriod,
    !!selectedCompanyId
  );

  // Fetch availability report
  const { data: availabilityReport } = useItcAvailabilityReport(
    selectedCompanyId,
    selectedReturnPeriod,
    !!selectedCompanyId
  );

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 0,
    }).format(amount);
  };

  // Category columns
  const categoryColumns: ColumnDef<ItcBlockedCategory>[] = [
    {
      accessorKey: 'categoryCode',
      header: 'Category',
      cell: ({ row }) => {
        const category = row.original;
        const Icon = getCategoryIcon(category.categoryCode);
        return (
          <div className="flex items-start gap-3">
            <div className="p-2 bg-red-100 rounded-lg">
              <Icon className="h-5 w-5 text-red-600" />
            </div>
            <div>
              <div className="font-medium text-gray-900">{category.categoryName}</div>
              <div className="text-xs text-gray-500 font-mono">{category.categoryCode}</div>
            </div>
          </div>
        );
      },
    },
    {
      accessorKey: 'sectionReference',
      header: 'Section Reference',
      cell: ({ row }) => (
        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-purple-100 text-purple-800">
          {row.original.sectionReference}
        </span>
      ),
    },
    {
      accessorKey: 'description',
      header: 'Description',
      cell: ({ row }) => (
        <p className="text-sm text-gray-700 max-w-md">
          {row.original.description}
        </p>
      ),
    },
    {
      accessorKey: 'examples',
      header: 'Examples',
      cell: ({ row }) => {
        const examples = row.original.examples || [];
        return (
          <div className="text-sm text-gray-600">
            {examples.slice(0, 2).map((ex, idx) => (
              <div key={idx}>• {ex}</div>
            ))}
            {examples.length > 2 && (
              <div className="text-gray-400">+{examples.length - 2} more</div>
            )}
          </div>
        );
      },
    },
  ];

  const isLoading = loadingCategories || loadingSummary;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-start">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">ITC Blocked - Section 17(5)</h1>
          <p className="text-gray-600 mt-2">
            Track Input Tax Credit that cannot be claimed under Section 17(5) of CGST Act
          </p>
        </div>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg shadow p-4">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <label htmlFor="companyFilter" className="block text-sm font-medium text-gray-700 mb-1">
              Company
            </label>
            <select
              id="companyFilter"
              value={selectedCompanyId}
              onChange={(e) => setSelectedCompanyId(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="">Select a company</option>
              {companies.map((company) => (
                <option key={company.id} value={company.id}>
                  {company.name}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label htmlFor="periodFilter" className="block text-sm font-medium text-gray-700 mb-1">
              Return Period
            </label>
            <select
              id="periodFilter"
              value={selectedReturnPeriod}
              onChange={(e) => setSelectedReturnPeriod(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              {returnPeriods.map((period) => (
                <option key={period.value} value={period.value}>
                  {period.label}
                </option>
              ))}
            </select>
          </div>
        </div>
      </div>

      {/* Info Box */}
      <div className="bg-red-50 border border-red-200 rounded-lg p-4">
        <div className="flex items-start gap-3">
          <AlertTriangle className="h-5 w-5 text-red-600 mt-0.5" />
          <div>
            <h3 className="font-medium text-red-800">Important: Blocked ITC under Section 17(5)</h3>
            <p className="text-sm text-red-700 mt-1">
              ITC is blocked for certain categories of goods and services as per Section 17(5) of CGST Act.
              These credits cannot be claimed even if GST has been paid on purchase. Blocked credits must be
              expensed and cannot be used for set-off against output tax liability.
            </p>
          </div>
        </div>
      </div>

      {/* Summary Section */}
      {selectedCompanyId && summary && (
        <>
          {/* Summary Cards */}
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div className="bg-white rounded-lg shadow p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-500">Total Blocked ITC</p>
                  <p className="text-2xl font-bold text-red-600">
                    {formatCurrency(summary.totalBlockedItc || 0)}
                  </p>
                </div>
                <div className="p-3 bg-red-100 rounded-full">
                  <FileX className="h-6 w-6 text-red-600" />
                </div>
              </div>
              <p className="text-sm text-gray-500 mt-2">
                {summary.transactionCount || 0} transactions
              </p>
            </div>
            <div className="bg-white rounded-lg shadow p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-500">CGST Blocked</p>
                  <p className="text-2xl font-bold text-orange-600">
                    {formatCurrency(summary.cgstBlocked || 0)}
                  </p>
                </div>
                <div className="p-3 bg-orange-100 rounded-full">
                  <Ban className="h-6 w-6 text-orange-600" />
                </div>
              </div>
            </div>
            <div className="bg-white rounded-lg shadow p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-500">SGST Blocked</p>
                  <p className="text-2xl font-bold text-orange-600">
                    {formatCurrency(summary.sgstBlocked || 0)}
                  </p>
                </div>
                <div className="p-3 bg-orange-100 rounded-full">
                  <Ban className="h-6 w-6 text-orange-600" />
                </div>
              </div>
            </div>
            <div className="bg-white rounded-lg shadow p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-500">IGST Blocked</p>
                  <p className="text-2xl font-bold text-orange-600">
                    {formatCurrency(summary.igstBlocked || 0)}
                  </p>
                </div>
                <div className="p-3 bg-orange-100 rounded-full">
                  <Ban className="h-6 w-6 text-orange-600" />
                </div>
              </div>
            </div>
          </div>

          {/* Blocked by Category */}
          {summary.byCategory && Object.keys(summary.byCategory).length > 0 && (
            <div className="bg-white rounded-lg shadow p-6">
              <h3 className="font-medium text-gray-900 mb-4">Blocked ITC by Category</h3>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                {Object.entries(summary.byCategory).map(([category, amount]) => {
                  const categoryInfo = blockedCategories.find(c => c.categoryCode === category);
                  const Icon = getCategoryIcon(category);
                  return (
                    <div key={category} className="p-4 bg-gray-50 rounded-lg">
                      <div className="flex items-center gap-2 mb-2">
                        <Icon className="h-4 w-4 text-red-600" />
                        <p className="text-sm text-gray-600 truncate">
                          {categoryInfo?.categoryName || category}
                        </p>
                      </div>
                      <p className="text-lg font-bold text-red-600">{formatCurrency(amount as number)}</p>
                    </div>
                  );
                })}
              </div>
            </div>
          )}
        </>
      )}

      {/* ITC Availability Report */}
      {selectedCompanyId && availabilityReport && (
        <div className="bg-white rounded-lg shadow p-6">
          <h3 className="font-medium text-gray-900 mb-4">ITC Availability Report</h3>
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Category</th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Total GST</th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Available ITC</th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Blocked ITC</th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">% Blocked</th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {availabilityReport.categories?.map((cat: any, idx: number) => (
                  <tr key={idx}>
                    <td className="px-4 py-3 text-sm text-gray-900">{cat.categoryName}</td>
                    <td className="px-4 py-3 text-sm text-gray-900 text-right">{formatCurrency(cat.totalGst)}</td>
                    <td className="px-4 py-3 text-sm text-green-600 text-right font-medium">{formatCurrency(cat.availableItc)}</td>
                    <td className="px-4 py-3 text-sm text-red-600 text-right font-medium">{formatCurrency(cat.blockedItc)}</td>
                    <td className="px-4 py-3 text-sm text-right">
                      <span className={cn(
                        'px-2 py-0.5 rounded-full text-xs font-medium',
                        cat.blockedPercentage > 50 ? 'bg-red-100 text-red-800' :
                          cat.blockedPercentage > 0 ? 'bg-yellow-100 text-yellow-800' :
                            'bg-green-100 text-green-800'
                      )}>
                        {cat.blockedPercentage?.toFixed(1)}%
                      </span>
                    </td>
                  </tr>
                ))}
                {availabilityReport.totals && (
                  <tr className="bg-gray-50 font-medium">
                    <td className="px-4 py-3 text-sm text-gray-900">Total</td>
                    <td className="px-4 py-3 text-sm text-gray-900 text-right">{formatCurrency(availabilityReport.totals.totalGst)}</td>
                    <td className="px-4 py-3 text-sm text-green-600 text-right">{formatCurrency(availabilityReport.totals.availableItc)}</td>
                    <td className="px-4 py-3 text-sm text-red-600 text-right">{formatCurrency(availabilityReport.totals.blockedItc)}</td>
                    <td className="px-4 py-3 text-sm text-right">
                      <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-gray-200 text-gray-800">
                        {availabilityReport.totals.blockedPercentage?.toFixed(1)}%
                      </span>
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Blocked Categories Reference */}
      <div className="bg-white rounded-lg shadow">
        <div className="p-4 border-b border-gray-200">
          <h2 className="text-lg font-semibold text-gray-900 flex items-center gap-2">
            <Info className="h-5 w-5 text-gray-500" />
            Blocked ITC Categories Reference
          </h2>
          <p className="text-sm text-gray-500 mt-1">
            Categories of goods and services where ITC is blocked under Section 17(5) of CGST Act
          </p>
        </div>
        <div className="p-6">
          {isLoading ? (
            <div className="flex items-center justify-center h-32">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
            </div>
          ) : (
            <DataTable
              columns={categoryColumns}
              data={blockedCategories}
              searchPlaceholder="Search blocked categories..."
            />
          )}
        </div>
      </div>

      {/* Key Points */}
      <div className="bg-white rounded-lg shadow p-6">
        <h3 className="font-medium text-gray-900 mb-4">Key Points to Remember</h3>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div className="flex items-start gap-3">
            <div className="p-2 bg-blue-100 rounded-lg">
              <FileText className="h-5 w-5 text-blue-600" />
            </div>
            <div>
              <p className="font-medium text-gray-900">Motor Vehicles Exception</p>
              <p className="text-sm text-gray-600">
                ITC is available for motor vehicles used for transportation of goods,
                passenger transportation, or training purposes.
              </p>
            </div>
          </div>
          <div className="flex items-start gap-3">
            <div className="p-2 bg-blue-100 rounded-lg">
              <Building className="h-5 w-5 text-blue-600" />
            </div>
            <div>
              <p className="font-medium text-gray-900">Works Contract Exception</p>
              <p className="text-sm text-gray-600">
                ITC is allowed for works contract services used for further supply
                of works contract service.
              </p>
            </div>
          </div>
          <div className="flex items-start gap-3">
            <div className="p-2 bg-blue-100 rounded-lg">
              <Receipt className="h-5 w-5 text-blue-600" />
            </div>
            <div>
              <p className="font-medium text-gray-900">Composition Scheme</p>
              <p className="text-sm text-gray-600">
                Taxpayers under composition scheme cannot claim any ITC on their purchases.
              </p>
            </div>
          </div>
          <div className="flex items-start gap-3">
            <div className="p-2 bg-blue-100 rounded-lg">
              <Ban className="h-5 w-5 text-blue-600" />
            </div>
            <div>
              <p className="font-medium text-gray-900">Personal Consumption</p>
              <p className="text-sm text-gray-600">
                ITC is blocked for goods/services used for personal consumption
                of employees or directors.
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ItcBlockedManagement;
