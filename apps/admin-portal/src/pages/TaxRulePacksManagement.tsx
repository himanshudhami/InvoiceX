import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { taxRulePackService } from '@/services/api/taxRulePackService';
import {
  TaxRulePack,
  TdsCalculationRequest,
  TdsCalculationResult,
  IncomeTaxCalculationRequest,
  IncomeTaxCalculationResult,
  PfEsiRates,
} from '@/services/api/types';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Badge } from '@/components/ui/badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import {
  Loader2,
  CheckCircle,
  Calculator,
  FileText,
  TrendingUp,
  IndianRupee,
  RefreshCw,
  ChevronRight,
  PlayCircle,
} from 'lucide-react';
import toast from 'react-hot-toast';

// Format currency in Indian style (lakhs/crores)
const formatIndianCurrency = (amount: number): string => {
  return new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR',
    maximumFractionDigits: 0,
  }).format(amount);
};

const formatPercentage = (rate: number): string => {
  return `${rate}%`;
};

const formatDate = (dateStr?: string): string => {
  if (!dateStr) return '-';
  return new Date(dateStr).toLocaleDateString('en-IN', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  });
};

export default function TaxRulePacksManagement() {
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState('packs');
  const [selectedPack, setSelectedPack] = useState<TaxRulePack | null>(null);

  // TDS Calculator state
  const [tdsRequest, setTdsRequest] = useState<TdsCalculationRequest>({
    sectionCode: '194J',
    payeeType: 'individual',
    amount: 100000,
    hasPan: true,
    transactionDate: new Date().toISOString().split('T')[0],
  });
  const [tdsResult, setTdsResult] = useState<TdsCalculationResult | null>(null);

  // Income Tax Calculator state
  const [itRequest, setItRequest] = useState<IncomeTaxCalculationRequest>({
    taxableIncome: 1500000,
    regime: 'new',
    ageCategory: 'general',
    financialYear: '2025-26',
  });
  const [itResult, setItResult] = useState<IncomeTaxCalculationResult | null>(null);

  // Fetch all rule packs
  const { data: rulePacks, isLoading } = useQuery({
    queryKey: ['tax-rule-packs'],
    queryFn: () => taxRulePackService.getAll(),
  });

  // Fetch current FY
  const { data: currentFyData } = useQuery({
    queryKey: ['current-fy'],
    queryFn: () => taxRulePackService.getCurrentFy(),
  });

  // Fetch PF/ESI rates for current FY
  const { data: pfEsiRates } = useQuery({
    queryKey: ['pf-esi-rates', currentFyData?.financialYear],
    queryFn: () => taxRulePackService.getPfEsiRates(currentFyData?.financialYear || '2025-26'),
    enabled: !!currentFyData?.financialYear,
  });

  // Activate mutation
  const activateMutation = useMutation({
    mutationFn: (id: string) => taxRulePackService.activate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tax-rule-packs'] });
      toast.success('Rule pack activated successfully');
    },
    onError: (error: Error) => {
      toast.error(`Failed to activate: ${error.message}`);
    },
  });

  // TDS calculation mutation
  const tdsMutation = useMutation({
    mutationFn: (request: TdsCalculationRequest) => taxRulePackService.calculateTds(request),
    onSuccess: (result) => {
      setTdsResult(result);
    },
    onError: (error: Error) => {
      toast.error(`TDS calculation failed: ${error.message}`);
    },
  });

  // Income tax calculation mutation
  const itMutation = useMutation({
    mutationFn: (request: IncomeTaxCalculationRequest) => taxRulePackService.calculateIncomeTax(request),
    onSuccess: (result) => {
      setItResult(result);
    },
    onError: (error: Error) => {
      toast.error(`Income tax calculation failed: ${error.message}`);
    },
  });

  const handleCalculateTds = () => {
    tdsMutation.mutate(tdsRequest);
  };

  const handleCalculateIncomeTax = () => {
    itMutation.mutate(itRequest);
  };

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'active':
        return <Badge className="bg-green-100 text-green-800">Active</Badge>;
      case 'draft':
        return <Badge className="bg-yellow-100 text-yellow-800">Draft</Badge>;
      case 'superseded':
        return <Badge className="bg-gray-100 text-gray-800">Superseded</Badge>;
      default:
        return <Badge variant="outline">{status}</Badge>;
    }
  };

  // Parse JSON safely
  const parseJsonSafely = (data: unknown): Record<string, unknown> | null => {
    if (!data) return null;
    if (typeof data === 'object') return data as Record<string, unknown>;
    return null;
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex justify-between items-start">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Tax Rule Packs</h1>
          <p className="text-gray-500 dark:text-gray-400">
            Manage versioned tax configurations for Indian compliance (TDS, Income Tax, PF/ESI)
          </p>
        </div>
        <div className="text-right">
          <Badge variant="outline" className="text-lg px-4 py-2">
            Current FY: {currentFyData?.financialYear || '2025-26'}
          </Badge>
        </div>
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="packs">
            <FileText className="h-4 w-4 mr-2" />
            Rule Packs
          </TabsTrigger>
          <TabsTrigger value="tds-calculator">
            <Calculator className="h-4 w-4 mr-2" />
            TDS Calculator
          </TabsTrigger>
          <TabsTrigger value="it-calculator">
            <TrendingUp className="h-4 w-4 mr-2" />
            Income Tax Calculator
          </TabsTrigger>
          <TabsTrigger value="pf-esi">
            <IndianRupee className="h-4 w-4 mr-2" />
            PF/ESI Rates
          </TabsTrigger>
        </TabsList>

        {/* Rule Packs Tab */}
        <TabsContent value="packs" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Tax Rule Packs</CardTitle>
              <CardDescription>
                View and manage financial year-wise tax configurations. Only one pack can be active per FY.
              </CardDescription>
            </CardHeader>
            <CardContent>
              {isLoading ? (
                <div className="flex justify-center py-8">
                  <Loader2 className="h-8 w-8 animate-spin text-gray-400" />
                </div>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Pack Code</TableHead>
                      <TableHead>Name</TableHead>
                      <TableHead>Financial Year</TableHead>
                      <TableHead>Version</TableHead>
                      <TableHead>Status</TableHead>
                      <TableHead>Activated</TableHead>
                      <TableHead>Actions</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {rulePacks?.map((pack) => (
                      <TableRow key={pack.id}>
                        <TableCell className="font-mono font-medium">{pack.packCode}</TableCell>
                        <TableCell>{pack.packName}</TableCell>
                        <TableCell>
                          <Badge variant="outline">FY {pack.financialYear}</Badge>
                        </TableCell>
                        <TableCell>v{pack.version}</TableCell>
                        <TableCell>{getStatusBadge(pack.status)}</TableCell>
                        <TableCell className="text-sm">
                          {pack.activatedAt ? (
                            <div>
                              <div>{formatDate(pack.activatedAt)}</div>
                              <div className="text-gray-500 text-xs">by {pack.activatedBy}</div>
                            </div>
                          ) : (
                            '-'
                          )}
                        </TableCell>
                        <TableCell>
                          <div className="flex gap-2">
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => setSelectedPack(pack)}
                            >
                              <ChevronRight className="h-4 w-4" />
                              View
                            </Button>
                            {pack.status === 'draft' && (
                              <Button
                                variant="default"
                                size="sm"
                                onClick={() => activateMutation.mutate(pack.id)}
                                disabled={activateMutation.isPending}
                              >
                                {activateMutation.isPending ? (
                                  <Loader2 className="h-4 w-4 animate-spin mr-1" />
                                ) : (
                                  <PlayCircle className="h-4 w-4 mr-1" />
                                )}
                                Activate
                              </Button>
                            )}
                          </div>
                        </TableCell>
                      </TableRow>
                    ))}
                    {(!rulePacks || rulePacks.length === 0) && (
                      <TableRow>
                        <TableCell colSpan={7} className="text-center text-gray-500 py-8">
                          No rule packs found. Run database migrations to seed initial packs.
                        </TableCell>
                      </TableRow>
                    )}
                  </TableBody>
                </Table>
              )}
            </CardContent>
          </Card>

          {/* Selected Pack Details */}
          {selectedPack && (
            <Card>
              <CardHeader className="flex flex-row items-center justify-between">
                <div>
                  <CardTitle>{selectedPack.packName}</CardTitle>
                  <CardDescription>
                    {selectedPack.sourceNotification || 'Budget / Finance Act details'}
                  </CardDescription>
                </div>
                <Button variant="ghost" size="sm" onClick={() => setSelectedPack(null)}>
                  Close
                </Button>
              </CardHeader>
              <CardContent className="space-y-6">
                {/* Income Tax Slabs */}
                {selectedPack.incomeTaxSlabs && (
                  <div>
                    <h4 className="font-semibold mb-3">Income Tax Slabs (New Regime)</h4>
                    <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-4">
                      <pre className="text-sm overflow-auto">
                        {JSON.stringify(selectedPack.incomeTaxSlabs, null, 2)}
                      </pre>
                    </div>
                  </div>
                )}

                {/* TDS Rates */}
                {selectedPack.tdsRates && (
                  <div>
                    <h4 className="font-semibold mb-3">TDS Rates</h4>
                    <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-4">
                      <pre className="text-sm overflow-auto">
                        {JSON.stringify(selectedPack.tdsRates, null, 2)}
                      </pre>
                    </div>
                  </div>
                )}

                {/* PF/ESI Rates */}
                {selectedPack.pfEsiRates && (
                  <div>
                    <h4 className="font-semibold mb-3">PF/ESI Rates</h4>
                    <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-4">
                      <pre className="text-sm overflow-auto">
                        {JSON.stringify(selectedPack.pfEsiRates, null, 2)}
                      </pre>
                    </div>
                  </div>
                )}
              </CardContent>
            </Card>
          )}
        </TabsContent>

        {/* TDS Calculator Tab */}
        <TabsContent value="tds-calculator" className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Calculator className="h-5 w-5" />
                  TDS Calculator
                </CardTitle>
                <CardDescription>
                  Calculate TDS deduction based on current FY rules
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label>TDS Section</Label>
                    <Select
                      value={tdsRequest.sectionCode}
                      onValueChange={(v) => setTdsRequest({ ...tdsRequest, sectionCode: v })}
                    >
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="194J">194J - Professional/Technical Fees</SelectItem>
                        <SelectItem value="194C">194C - Contractor Payments</SelectItem>
                        <SelectItem value="194H">194H - Commission/Brokerage</SelectItem>
                        <SelectItem value="194A">194A - Interest Other Than Securities</SelectItem>
                        <SelectItem value="194I">194I - Rent</SelectItem>
                        <SelectItem value="194T">194T - Partner Payments (New)</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>

                  <div className="space-y-2">
                    <Label>Payee Type</Label>
                    <Select
                      value={tdsRequest.payeeType}
                      onValueChange={(v) => setTdsRequest({ ...tdsRequest, payeeType: v })}
                    >
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="individual">Individual / HUF</SelectItem>
                        <SelectItem value="company">Company</SelectItem>
                        <SelectItem value="partnership">Partnership Firm</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                </div>

                <div className="space-y-2">
                  <Label>Gross Amount (INR)</Label>
                  <Input
                    type="number"
                    value={tdsRequest.amount}
                    onChange={(e) =>
                      setTdsRequest({ ...tdsRequest, amount: Number(e.target.value) })
                    }
                  />
                </div>

                <div className="flex items-center gap-4">
                  <label className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      checked={tdsRequest.hasPan}
                      onChange={(e) =>
                        setTdsRequest({ ...tdsRequest, hasPan: e.target.checked })
                      }
                      className="rounded"
                    />
                    <span>Payee has PAN</span>
                  </label>
                </div>

                <Button
                  onClick={handleCalculateTds}
                  disabled={tdsMutation.isPending}
                  className="w-full"
                >
                  {tdsMutation.isPending ? (
                    <Loader2 className="h-4 w-4 animate-spin mr-2" />
                  ) : (
                    <Calculator className="h-4 w-4 mr-2" />
                  )}
                  Calculate TDS
                </Button>
              </CardContent>
            </Card>

            {/* TDS Result */}
            <Card>
              <CardHeader>
                <CardTitle>Calculation Result</CardTitle>
              </CardHeader>
              <CardContent>
                {tdsResult ? (
                  <div className="space-y-4">
                    <div className="grid grid-cols-2 gap-4">
                      <div className="p-4 bg-gray-50 dark:bg-gray-800 rounded-lg">
                        <div className="text-sm text-gray-500">Gross Amount</div>
                        <div className="text-xl font-bold">
                          {formatIndianCurrency(tdsResult.grossAmount)}
                        </div>
                      </div>
                      <div className="p-4 bg-red-50 dark:bg-red-900/20 rounded-lg">
                        <div className="text-sm text-red-600">TDS Amount</div>
                        <div className="text-xl font-bold text-red-600">
                          {formatIndianCurrency(tdsResult.tdsAmount)}
                        </div>
                      </div>
                      <div className="p-4 bg-green-50 dark:bg-green-900/20 rounded-lg">
                        <div className="text-sm text-green-600">Net Amount</div>
                        <div className="text-xl font-bold text-green-600">
                          {formatIndianCurrency(tdsResult.netAmount)}
                        </div>
                      </div>
                      <div className="p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
                        <div className="text-sm text-blue-600">Applicable Rate</div>
                        <div className="text-xl font-bold text-blue-600">
                          {formatPercentage(tdsResult.applicableRate)}
                        </div>
                      </div>
                    </div>

                    {tdsResult.thresholdApplied && (
                      <div className="p-3 bg-yellow-50 dark:bg-yellow-900/20 rounded-lg text-sm text-yellow-700">
                        Amount is below threshold - No TDS applicable
                      </div>
                    )}

                    <div className="text-xs text-gray-500">
                      FY: {tdsResult.financialYear} | Rule Pack v{tdsResult.rulePackVersion}
                    </div>
                  </div>
                ) : (
                  <div className="text-center text-gray-500 py-8">
                    Enter values and click Calculate to see results
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        {/* Income Tax Calculator Tab */}
        <TabsContent value="it-calculator" className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <TrendingUp className="h-5 w-5" />
                  Income Tax Calculator
                </CardTitle>
                <CardDescription>
                  Calculate income tax under New/Old regime for FY {currentFyData?.financialYear}
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="space-y-2">
                  <Label>Taxable Income (INR)</Label>
                  <Input
                    type="number"
                    value={itRequest.taxableIncome}
                    onChange={(e) =>
                      setItRequest({ ...itRequest, taxableIncome: Number(e.target.value) })
                    }
                    placeholder="Enter annual taxable income"
                  />
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label>Tax Regime</Label>
                    <Select
                      value={itRequest.regime}
                      onValueChange={(v) =>
                        setItRequest({ ...itRequest, regime: v as 'new' | 'old' })
                      }
                    >
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="new">New Regime (Default)</SelectItem>
                        <SelectItem value="old">Old Regime</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>

                  <div className="space-y-2">
                    <Label>Age Category</Label>
                    <Select
                      value={itRequest.ageCategory}
                      onValueChange={(v) =>
                        setItRequest({
                          ...itRequest,
                          ageCategory: v as 'general' | 'senior' | 'super_senior',
                        })
                      }
                    >
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="general">Below 60 years</SelectItem>
                        <SelectItem value="senior">60-80 years (Senior)</SelectItem>
                        <SelectItem value="super_senior">Above 80 years (Super Senior)</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                </div>

                <div className="space-y-2">
                  <Label>Financial Year</Label>
                  <Select
                    value={itRequest.financialYear}
                    onValueChange={(v) => setItRequest({ ...itRequest, financialYear: v })}
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="2025-26">FY 2025-26 (AY 2026-27)</SelectItem>
                      <SelectItem value="2024-25">FY 2024-25 (AY 2025-26)</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <Button
                  onClick={handleCalculateIncomeTax}
                  disabled={itMutation.isPending}
                  className="w-full"
                >
                  {itMutation.isPending ? (
                    <Loader2 className="h-4 w-4 animate-spin mr-2" />
                  ) : (
                    <Calculator className="h-4 w-4 mr-2" />
                  )}
                  Calculate Tax
                </Button>
              </CardContent>
            </Card>

            {/* Income Tax Result */}
            <Card>
              <CardHeader>
                <CardTitle>Tax Computation</CardTitle>
              </CardHeader>
              <CardContent>
                {itResult ? (
                  <div className="space-y-4">
                    <div className="grid grid-cols-2 gap-4">
                      <div className="p-4 bg-gray-50 dark:bg-gray-800 rounded-lg">
                        <div className="text-sm text-gray-500">Taxable Income</div>
                        <div className="text-xl font-bold">
                          {formatIndianCurrency(itResult.taxableIncome)}
                        </div>
                      </div>
                      <div className="p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
                        <div className="text-sm text-blue-600">Total Tax</div>
                        <div className="text-xl font-bold text-blue-600">
                          {formatIndianCurrency(itResult.totalTax)}
                        </div>
                      </div>
                    </div>

                    {/* Tax Breakdown */}
                    <div className="space-y-2">
                      <h4 className="font-medium">Breakdown</h4>
                      <Table>
                        <TableBody>
                          <TableRow>
                            <TableCell>Base Tax</TableCell>
                            <TableCell className="text-right">
                              {formatIndianCurrency(itResult.baseTax)}
                            </TableCell>
                          </TableRow>
                          {itResult.rebate > 0 && (
                            <TableRow>
                              <TableCell>Less: Rebate u/s 87A</TableCell>
                              <TableCell className="text-right text-green-600">
                                -{formatIndianCurrency(itResult.rebate)}
                              </TableCell>
                            </TableRow>
                          )}
                          {itResult.surcharge > 0 && (
                            <TableRow>
                              <TableCell>Add: Surcharge</TableCell>
                              <TableCell className="text-right">
                                {formatIndianCurrency(itResult.surcharge)}
                              </TableCell>
                            </TableRow>
                          )}
                          <TableRow>
                            <TableCell>Add: Health & Education Cess (4%)</TableCell>
                            <TableCell className="text-right">
                              {formatIndianCurrency(itResult.cess)}
                            </TableCell>
                          </TableRow>
                          <TableRow className="font-bold">
                            <TableCell>Total Tax Liability</TableCell>
                            <TableCell className="text-right">
                              {formatIndianCurrency(itResult.totalTax)}
                            </TableCell>
                          </TableRow>
                        </TableBody>
                      </Table>
                    </div>

                    {/* Slab Breakdown */}
                    {itResult.slabBreakdown && itResult.slabBreakdown.length > 0 && (
                      <div className="space-y-2">
                        <h4 className="font-medium">Slab-wise Computation</h4>
                        <Table>
                          <TableHeader>
                            <TableRow>
                              <TableHead>Slab</TableHead>
                              <TableHead>Rate</TableHead>
                              <TableHead className="text-right">Amount</TableHead>
                              <TableHead className="text-right">Tax</TableHead>
                            </TableRow>
                          </TableHeader>
                          <TableBody>
                            {itResult.slabBreakdown.map((slab, idx) => (
                              <TableRow key={idx}>
                                <TableCell className="text-sm">
                                  {formatIndianCurrency(slab.slabMin ?? 0)} -{' '}
                                  {slab.slabMax
                                    ? formatIndianCurrency(slab.slabMax)
                                    : 'Above'}
                                </TableCell>
                                <TableCell>{formatPercentage(slab.rate ?? 0)}</TableCell>
                                <TableCell className="text-right">
                                  {formatIndianCurrency(slab.taxableAmount ?? 0)}
                                </TableCell>
                                <TableCell className="text-right">
                                  {formatIndianCurrency(slab.taxAmount ?? 0)}
                                </TableCell>
                              </TableRow>
                            ))}
                          </TableBody>
                        </Table>
                      </div>
                    )}

                    <div className="flex justify-between items-center p-3 bg-green-50 dark:bg-green-900/20 rounded-lg">
                      <span>Effective Tax Rate</span>
                      <span className="font-bold text-green-600">
                        {(itResult.effectiveRate ?? 0).toFixed(2)}%
                      </span>
                    </div>

                    <div className="text-xs text-gray-500">
                      Regime: {itResult.regime.toUpperCase()} | FY: {itResult.financialYear}
                    </div>
                  </div>
                ) : (
                  <div className="text-center text-gray-500 py-8">
                    Enter income and click Calculate to see tax computation
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        {/* PF/ESI Rates Tab */}
        <TabsContent value="pf-esi" className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <IndianRupee className="h-5 w-5" />
                  Provident Fund (PF) Rates
                </CardTitle>
                <CardDescription>
                  EPF contribution rates for FY {currentFyData?.financialYear}
                </CardDescription>
              </CardHeader>
              <CardContent>
                {pfEsiRates ? (
                  <div className="space-y-4">
                    <div className="grid grid-cols-2 gap-4">
                      <div className="p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
                        <div className="text-sm text-blue-600">Employee Contribution</div>
                        <div className="text-2xl font-bold text-blue-600">
                          {formatPercentage(pfEsiRates.employeePfRate)}
                        </div>
                      </div>
                      <div className="p-4 bg-green-50 dark:bg-green-900/20 rounded-lg">
                        <div className="text-sm text-green-600">Employer Contribution</div>
                        <div className="text-2xl font-bold text-green-600">
                          {formatPercentage(pfEsiRates.employerPfRate)}
                        </div>
                      </div>
                    </div>
                    <div className="p-4 bg-gray-50 dark:bg-gray-800 rounded-lg">
                      <div className="text-sm text-gray-500">PF Wage Ceiling</div>
                      <div className="text-xl font-bold">
                        {formatIndianCurrency(pfEsiRates.pfWageCeiling)}
                        <span className="text-sm font-normal text-gray-500"> / month</span>
                      </div>
                    </div>
                  </div>
                ) : (
                  <div className="flex justify-center py-8">
                    <Loader2 className="h-8 w-8 animate-spin text-gray-400" />
                  </div>
                )}
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <IndianRupee className="h-5 w-5" />
                  ESI (Employees' State Insurance) Rates
                </CardTitle>
                <CardDescription>
                  ESIC contribution rates for FY {currentFyData?.financialYear}
                </CardDescription>
              </CardHeader>
              <CardContent>
                {pfEsiRates ? (
                  <div className="space-y-4">
                    <div className="grid grid-cols-2 gap-4">
                      <div className="p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
                        <div className="text-sm text-blue-600">Employee Contribution</div>
                        <div className="text-2xl font-bold text-blue-600">
                          {formatPercentage(pfEsiRates.employeeEsiRate)}
                        </div>
                      </div>
                      <div className="p-4 bg-green-50 dark:bg-green-900/20 rounded-lg">
                        <div className="text-sm text-green-600">Employer Contribution</div>
                        <div className="text-2xl font-bold text-green-600">
                          {formatPercentage(pfEsiRates.employerEsiRate)}
                        </div>
                      </div>
                    </div>
                    <div className="p-4 bg-gray-50 dark:bg-gray-800 rounded-lg">
                      <div className="text-sm text-gray-500">ESI Wage Ceiling</div>
                      <div className="text-xl font-bold">
                        {formatIndianCurrency(pfEsiRates.esiWageCeiling)}
                        <span className="text-sm font-normal text-gray-500"> / month</span>
                      </div>
                    </div>
                    <div className="p-3 bg-yellow-50 dark:bg-yellow-900/20 rounded-lg text-sm text-yellow-700">
                      ESI is applicable only if gross wages are below the wage ceiling
                    </div>
                  </div>
                ) : (
                  <div className="flex justify-center py-8">
                    <Loader2 className="h-8 w-8 animate-spin text-gray-400" />
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
        </TabsContent>
      </Tabs>
    </div>
  );
}
