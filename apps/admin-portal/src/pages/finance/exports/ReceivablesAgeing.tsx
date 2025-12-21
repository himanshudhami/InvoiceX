import { useState, useMemo } from 'react';
import { useQueryState, parseAsString, parseAsStringEnum } from 'nuqs';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip as RechartsTooltip,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  Legend,
} from 'recharts';
import {
  RefreshCw,
  Download,
  Search,
  AlertTriangle,
  Clock,
  DollarSign,
  IndianRupee,
  Calendar,
  TrendingUp,
  ArrowUpRight,
  ArrowDownRight,
  Filter,
  FileText,
} from 'lucide-react';
import { useExportReceivablesAgeing } from '@/hooks/api';
import { useCompanies } from '@/hooks/api';
import { formatCurrency } from '@/lib/currency';
import { formatDate } from '@/lib/date';
import { cn } from '@/lib/utils';

const formatNumber = (num: number) => num.toLocaleString('en-US', { maximumFractionDigits: 2 });

type AgeingBucket = 'all' | 'current' | '31-60' | '61-90' | '91-180' | '181-270' | 'overdue';

const AGING_COLORS = {
  current: '#22c55e',
  '31-60': '#84cc16',
  '61-90': '#eab308',
  '91-180': '#f97316',
  '181-270': '#ef4444',
  overdue: '#991b1b',
};

export default function ReceivablesAgeing() {
  const [companyId, setCompanyId] = useQueryState(
    'companyId',
    parseAsString.withDefault('')
  );
  const [bucket, setBucket] = useQueryState(
    'bucket',
    parseAsStringEnum<AgeingBucket>(['all', 'current', '31-60', '61-90', '91-180', '181-270', 'overdue']).withDefault('all')
  );
  const [searchQuery, setSearchQuery] = useState('');
  const [asOfDate] = useState(new Date().toISOString().split('T')[0]);

  const { data: companies } = useCompanies();
  const {
    data: ageingData,
    isLoading,
    refetch,
  } = useExportReceivablesAgeing(companyId || '', asOfDate, {
    enabled: !!companyId,
  });

  // Process data for charts
  const ageingChartData = useMemo(() => {
    if (!ageingData) return [];
    return [
      { name: '0-30 Days', usd: ageingData.current_bucket_usd, inr: ageingData.current_bucket_inr, color: AGING_COLORS.current },
      { name: '31-60 Days', usd: ageingData.bucket_31_60_usd, inr: ageingData.bucket_31_60_inr, color: AGING_COLORS['31-60'] },
      { name: '61-90 Days', usd: ageingData.bucket_61_90_usd, inr: ageingData.bucket_61_90_inr, color: AGING_COLORS['61-90'] },
      { name: '91-180 Days', usd: ageingData.bucket_91_180_usd, inr: ageingData.bucket_91_180_inr, color: AGING_COLORS['91-180'] },
      { name: '181-270 Days', usd: ageingData.bucket_181_270_usd, inr: ageingData.bucket_181_270_inr, color: AGING_COLORS['181-270'] },
      { name: '270+ Days', usd: ageingData.overdue_bucket_usd, inr: ageingData.overdue_bucket_inr, color: AGING_COLORS.overdue },
    ];
  }, [ageingData]);

  const pieChartData = useMemo(() => {
    if (!ageingData) return [];
    return ageingChartData
      .filter(d => d.usd > 0)
      .map(d => ({ name: d.name, value: d.usd, color: d.color }));
  }, [ageingData, ageingChartData]);

  // Filter invoices based on bucket selection
  const filteredInvoices = useMemo(() => {
    if (!ageingData?.invoice_details) return [];
    let invoices = ageingData.invoice_details;

    // Filter by bucket
    if (bucket !== 'all') {
      invoices = invoices.filter(inv => {
        const age = inv.days_outstanding;
        switch (bucket) {
          case 'current': return age <= 30;
          case '31-60': return age > 30 && age <= 60;
          case '61-90': return age > 60 && age <= 90;
          case '91-180': return age > 90 && age <= 180;
          case '181-270': return age > 180 && age <= 270;
          case 'overdue': return age > 270;
          default: return true;
        }
      });
    }

    // Filter by search
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      invoices = invoices.filter(
        inv =>
          inv.invoice_number.toLowerCase().includes(query) ||
          inv.customer_name.toLowerCase().includes(query)
      );
    }

    return invoices;
  }, [ageingData, bucket, searchQuery]);

  // Calculate FEMA deadline stats
  const femaStats = useMemo(() => {
    if (!ageingData?.invoice_details) return { atRisk: 0, critical: 0, violated: 0 };
    const invoices = ageingData.invoice_details;
    return {
      atRisk: invoices.filter(i => i.days_outstanding > 180 && i.days_outstanding <= 240).length,
      critical: invoices.filter(i => i.days_outstanding > 240 && i.days_outstanding <= 270).length,
      violated: invoices.filter(i => i.days_outstanding > 270).length,
    };
  }, [ageingData]);

  const getAgeingBadge = (days: number) => {
    if (days <= 30) return <Badge className="bg-green-500">Current</Badge>;
    if (days <= 60) return <Badge className="bg-lime-500">31-60 Days</Badge>;
    if (days <= 90) return <Badge className="bg-yellow-500">61-90 Days</Badge>;
    if (days <= 180) return <Badge className="bg-orange-500">91-180 Days</Badge>;
    if (days <= 270) return <Badge className="bg-red-500">181-270 Days</Badge>;
    return <Badge variant="destructive">FEMA Overdue</Badge>;
  };

  const getDaysToDeadline = (daysOutstanding: number) => {
    const daysToDeadline = 270 - daysOutstanding;
    if (daysToDeadline < 0) {
      return (
        <span className="text-red-600 font-semibold flex items-center gap-1">
          <AlertTriangle className="h-3 w-3" />
          {Math.abs(daysToDeadline)} days overdue
        </span>
      );
    }
    if (daysToDeadline <= 30) {
      return (
        <span className="text-red-500 font-medium">{daysToDeadline} days left</span>
      );
    }
    if (daysToDeadline <= 60) {
      return (
        <span className="text-orange-500 font-medium">{daysToDeadline} days left</span>
      );
    }
    return <span className="text-gray-600">{daysToDeadline} days left</span>;
  };

  const handleExport = () => {
    // TODO: Implement CSV export
    console.log('Export receivables ageing report');
  };

  if (!companyId) {
    return (
      <div className="container mx-auto py-6 space-y-6">
        <div>
          <h1 className="text-3xl font-bold">Export Receivables Ageing</h1>
          <p className="text-muted-foreground">
            Track outstanding export receivables by age with FEMA deadline monitoring
          </p>
        </div>
        <Card>
          <CardContent className="py-8">
            <div className="text-center space-y-4">
              <FileText className="h-12 w-12 mx-auto text-muted-foreground" />
              <p className="text-muted-foreground">
                Please select a company to view export receivables ageing
              </p>
              <Select value={companyId} onValueChange={setCompanyId}>
                <SelectTrigger className="w-64 mx-auto">
                  <SelectValue placeholder="Select company" />
                </SelectTrigger>
                <SelectContent>
                  {companies?.map(company => (
                    <SelectItem key={company.id} value={company.id}>
                      {company.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="container mx-auto py-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Export Receivables Ageing</h1>
          <p className="text-muted-foreground">
            Outstanding export receivables as of {formatDate(asOfDate)}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Select value={companyId} onValueChange={setCompanyId}>
            <SelectTrigger className="w-48">
              <SelectValue placeholder="Select company" />
            </SelectTrigger>
            <SelectContent>
              {companies?.map(company => (
                <SelectItem key={company.id} value={company.id}>
                  {company.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Button variant="outline" size="icon" onClick={() => refetch()}>
            <RefreshCw className={cn('h-4 w-4', isLoading && 'animate-spin')} />
          </Button>
          <Button variant="outline" onClick={handleExport}>
            <Download className="h-4 w-4 mr-2" />
            Export
          </Button>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium flex items-center gap-2">
              <DollarSign className="h-4 w-4 text-blue-500" />
              Total Outstanding (USD)
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              ${formatNumber(ageingData?.total_outstanding_usd || 0)}
            </div>
            <p className="text-xs text-muted-foreground mt-1">
              {ageingData?.total_invoices || 0} invoices
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium flex items-center gap-2">
              <IndianRupee className="h-4 w-4 text-green-500" />
              Total Outstanding (INR)
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              ₹{formatNumber(ageingData?.total_outstanding_inr || 0)}
            </div>
            <p className="text-xs text-muted-foreground mt-1">
              At current exchange rates
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium flex items-center gap-2">
              <TrendingUp className="h-4 w-4 text-purple-500" />
              Unrealized Forex
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className={cn(
              'text-2xl font-bold flex items-center gap-1',
              (ageingData?.unrealized_forex_gain_loss || 0) >= 0 ? 'text-green-600' : 'text-red-600'
            )}>
              {(ageingData?.unrealized_forex_gain_loss || 0) >= 0 ? (
                <ArrowUpRight className="h-5 w-5" />
              ) : (
                <ArrowDownRight className="h-5 w-5" />
              )}
              ₹{formatNumber(Math.abs(ageingData?.unrealized_forex_gain_loss || 0))}
            </div>
            <p className="text-xs text-muted-foreground mt-1">
              {(ageingData?.unrealized_forex_gain_loss || 0) >= 0 ? 'Potential gain' : 'Potential loss'}
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium flex items-center gap-2">
              <Clock className="h-4 w-4 text-orange-500" />
              Avg Days Outstanding
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {ageingData?.weighted_average_age || 0} days
            </div>
            <p className="text-xs text-muted-foreground mt-1">
              Weighted by amount
            </p>
          </CardContent>
        </Card>
      </div>

      {/* FEMA Alert Cards */}
      {(femaStats.atRisk > 0 || femaStats.critical > 0 || femaStats.violated > 0) && (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {femaStats.atRisk > 0 && (
            <Card className="border-orange-200 bg-orange-50">
              <CardContent className="pt-4">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium text-orange-700">At Risk (180-240 days)</p>
                    <p className="text-2xl font-bold text-orange-900">{femaStats.atRisk} invoices</p>
                  </div>
                  <Clock className="h-8 w-8 text-orange-500" />
                </div>
              </CardContent>
            </Card>
          )}
          {femaStats.critical > 0 && (
            <Card className="border-red-200 bg-red-50">
              <CardContent className="pt-4">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium text-red-700">Critical (240-270 days)</p>
                    <p className="text-2xl font-bold text-red-900">{femaStats.critical} invoices</p>
                  </div>
                  <AlertTriangle className="h-8 w-8 text-red-500" />
                </div>
              </CardContent>
            </Card>
          )}
          {femaStats.violated > 0 && (
            <Card className="border-red-400 bg-red-100">
              <CardContent className="pt-4">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium text-red-800">FEMA Violated (270+ days)</p>
                    <p className="text-2xl font-bold text-red-950">{femaStats.violated} invoices</p>
                  </div>
                  <AlertTriangle className="h-8 w-8 text-red-700 animate-pulse" />
                </div>
              </CardContent>
            </Card>
          )}
        </div>
      )}

      {/* Charts Row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Bar Chart - Ageing by Bucket */}
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Ageing Distribution (USD)</CardTitle>
            <CardDescription>Outstanding amounts by age bucket</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="h-64">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={ageingChartData}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="name" tick={{ fontSize: 11 }} />
                  <YAxis tickFormatter={(v) => `$${(v / 1000).toFixed(0)}K`} />
                  <RechartsTooltip
                    formatter={(value: number) => [`$${formatNumber(value)}`, 'Amount']}
                  />
                  <Bar dataKey="usd" radius={[4, 4, 0, 0]}>
                    {ageingChartData.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={entry.color} />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>

        {/* Pie Chart - Distribution */}
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Portfolio Composition</CardTitle>
            <CardDescription>Receivables distribution by age</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="h-64">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={pieChartData}
                    cx="50%"
                    cy="50%"
                    innerRadius={60}
                    outerRadius={80}
                    paddingAngle={2}
                    dataKey="value"
                    label={({ name, percent }) =>
                      percent > 0.05 ? `${(percent * 100).toFixed(0)}%` : ''
                    }
                    labelLine={false}
                  >
                    {pieChartData.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={entry.color} />
                    ))}
                  </Pie>
                  <Legend />
                  <RechartsTooltip
                    formatter={(value: number) => [`$${formatNumber(value)}`, 'Amount']}
                  />
                </PieChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Detailed Table */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="text-lg">Invoice Details</CardTitle>
              <CardDescription>
                {filteredInvoices.length} of {ageingData?.invoice_details?.length || 0} invoices
              </CardDescription>
            </div>
            <div className="flex items-center gap-2">
              <div className="relative">
                <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Search invoices..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="pl-9 w-64"
                />
              </div>
              <Select value={bucket} onValueChange={(v) => setBucket(v as AgeingBucket)}>
                <SelectTrigger className="w-40">
                  <Filter className="h-4 w-4 mr-2" />
                  <SelectValue placeholder="Filter bucket" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Buckets</SelectItem>
                  <SelectItem value="current">0-30 Days</SelectItem>
                  <SelectItem value="31-60">31-60 Days</SelectItem>
                  <SelectItem value="61-90">61-90 Days</SelectItem>
                  <SelectItem value="91-180">91-180 Days</SelectItem>
                  <SelectItem value="181-270">181-270 Days</SelectItem>
                  <SelectItem value="overdue">270+ Days (FEMA)</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div className="rounded-md border">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Invoice</TableHead>
                  <TableHead>Customer</TableHead>
                  <TableHead>Invoice Date</TableHead>
                  <TableHead>Due Date</TableHead>
                  <TableHead className="text-right">Amount (USD)</TableHead>
                  <TableHead className="text-right">Amount (INR)</TableHead>
                  <TableHead>Age</TableHead>
                  <TableHead>FEMA Deadline</TableHead>
                  <TableHead className="text-right">Forex Impact</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredInvoices.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={9} className="text-center py-8 text-muted-foreground">
                      No invoices found
                    </TableCell>
                  </TableRow>
                ) : (
                  filteredInvoices.map((invoice) => (
                    <TableRow key={invoice.invoice_id}>
                      <TableCell className="font-medium">
                        {invoice.invoice_number}
                      </TableCell>
                      <TableCell>{invoice.customer_name}</TableCell>
                      <TableCell>{formatDate(invoice.invoice_date)}</TableCell>
                      <TableCell>{formatDate(invoice.due_date)}</TableCell>
                      <TableCell className="text-right font-medium">
                        ${formatNumber(invoice.amount_usd)}
                      </TableCell>
                      <TableCell className="text-right">
                        ₹{formatNumber(invoice.amount_inr)}
                      </TableCell>
                      <TableCell>
                        <span title={`${invoice.days_outstanding} days outstanding`}>
                          {getAgeingBadge(invoice.days_outstanding)}
                        </span>
                      </TableCell>
                      <TableCell>
                        <div className="space-y-1">
                          {getDaysToDeadline(invoice.days_outstanding)}
                          <div className="h-1.5 w-full bg-gray-200 rounded-full overflow-hidden">
                            <div
                              className={cn(
                                'h-full rounded-full transition-all',
                                invoice.days_outstanding > 270 ? 'bg-red-600' :
                                invoice.days_outstanding > 180 ? 'bg-orange-500' :
                                'bg-green-500'
                              )}
                              style={{ width: `${Math.min((invoice.days_outstanding / 270) * 100, 100)}%` }}
                            />
                          </div>
                        </div>
                      </TableCell>
                      <TableCell className="text-right">
                        <span className={cn(
                          'font-medium',
                          invoice.unrealized_gain_loss >= 0 ? 'text-green-600' : 'text-red-600'
                        )}>
                          {invoice.unrealized_gain_loss >= 0 ? '+' : ''}
                          ₹{formatNumber(invoice.unrealized_gain_loss)}
                        </span>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </div>
        </CardContent>
      </Card>

      {/* Customer Summary */}
      {ageingData?.by_customer && ageingData.by_customer.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">By Customer</CardTitle>
            <CardDescription>Outstanding receivables grouped by customer</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="rounded-md border">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Customer</TableHead>
                    <TableHead className="text-right">Total USD</TableHead>
                    <TableHead className="text-right">Total INR</TableHead>
                    <TableHead className="text-center">Invoices</TableHead>
                    <TableHead>Avg Age</TableHead>
                    <TableHead className="text-right">Forex Impact</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {ageingData.by_customer.map((customer) => (
                    <TableRow key={customer.customer_id}>
                      <TableCell className="font-medium">
                        {customer.customer_name}
                      </TableCell>
                      <TableCell className="text-right font-medium">
                        ${formatNumber(customer.total_usd)}
                      </TableCell>
                      <TableCell className="text-right">
                        ₹{formatNumber(customer.total_inr)}
                      </TableCell>
                      <TableCell className="text-center">
                        {customer.invoice_count}
                      </TableCell>
                      <TableCell>
                        {getAgeingBadge(customer.average_age)}
                      </TableCell>
                      <TableCell className="text-right">
                        <span className={cn(
                          'font-medium',
                          customer.unrealized_gain_loss >= 0 ? 'text-green-600' : 'text-red-600'
                        )}>
                          {customer.unrealized_gain_loss >= 0 ? '+' : ''}
                          ₹{formatNumber(customer.unrealized_gain_loss)}
                        </span>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
