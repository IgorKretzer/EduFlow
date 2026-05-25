export type LoginResponse = {
  accessToken: string;
  expiresAt: string;
  tenantId: string;
  tenantSlug: string;
};

export type DashboardKpi = {
  key: string;
  label: string;
  value: number;
  previousValue?: number | null;
  changePercent?: number | null;
  format: string;
};

export type DelinquencyByUnit = {
  unitCode: string;
  unitName: string;
  delinquencyRate: number;
  debtAmount: number;
  overdueCount: number;
};

export type Insight = {
  code: string;
  title: string;
  message: string;
  severity: string;
  generatedAt: string;
  recommendedAction?: string | null;
  deepLink?: string | null;
  category?: string | null;
};

export type TimeSeriesPoint = {
  date: string;
  value: number;
};

export type DashboardSummary = {
  kpis: DashboardKpi[];
  delinquencyByUnit: DelinquencyByUnit[];
  insights: Insight[];
  revenueTrend: TimeSeriesPoint[];
};

export type DashboardFilter = {
  unitId?: string;
  from?: string;
  to?: string;
};

export type SyncResult = {
  status: string;
  recordsProcessed: number;
  errorMessage?: string | null;
};

export type ErpConfig = {
  providerKey: string;
  endpointUrl: string;
  username: string;
  hasPassword: boolean;
  pageSize: number;
  syncEnabled: boolean;
  syncIntervalMinutes: number;
  lastSyncAtUtc: string | null;
  lastSyncStatus: string | null;
  lastSyncMessage: string | null;
  searchParametersStudents: string | null;
  searchParametersFinancial: string | null;
  searchParametersContracts: string | null;
};

export type SyncSchedulerStatus = {
  schedulerEnabled: boolean;
  pollIntervalSeconds: number;
  defaultIntervalMinutes: number;
};

export type OpsStatus = {
  checkedAtUtc: string;
  components: { id: string; name: string; status: string; detail: string }[];
  queues: { name: string; exists: boolean; messages: number; consumers: number }[];
  pipeline: {
    schedulerEnabled: boolean;
    pollIntervalSeconds: number;
    lastSyncAtUtc: string | null;
    lastSyncStatus: string | null;
    lastSyncMessage: string | null;
  } | null;
  links: { label: string; url: string }[];
};

export type EnrollmentSummary = {
  enrollmentCode: string;
  unitCode: string;
  unitName: string;
  studentStatus: string;
  paymentStatus: string;
  debtAmount: number;
  paidAmount: number;
  churnRisk: string;
  retentionLevel: string;
  trend: string;
  daysSinceLastPayment: number | null;
  operationalScore: number;
};

export type EnrollmentPage = {
  items: EnrollmentSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type EnrollmentFinancialLine = {
  externalId: string;
  debtAmount: number;
  paidAmount: number;
  paymentStatus: string;
  dueDate: string;
  syncedAt: string;
};

export type EnrollmentDetail = {
  summary: EnrollmentSummary;
  financialHistory: EnrollmentFinancialLine[];
};

export type AuthSession = {
  accessToken: string;
  expiresAt: string;
  tenantId: string;
  tenantSlug: string;
  email?: string;
};

export type FinanceAmountSlice = {
  id: string;
  label: string;
  invoiceAmount: number;
  openAmount: number;
};

export type FinanceSituation = {
  id: string;
  label: string;
  amount: number;
};

export type FinanceReceivablesSummary = {
  totalReceivable: number;
  overdue: number;
  toReceive: number;
  totalPaid: number;
  totalInvoice: number;
  peopleCount: number;
  overdueShare: number;
  toReceiveShare: number;
};

export type FinanceReceivables = {
  summary: FinanceReceivablesSummary;
  overdueSlices: FinanceAmountSlice[];
  overdueSituations: FinanceSituation[];
  toMatureSlices: FinanceAmountSlice[];
  toMatureSituations: FinanceSituation[];
};

export type FinanceCashFlowDay = {
  date: string;
  day: number;
  openingBalance: number;
  inflow: number;
  outflow: number;
  operationalBalance: number;
  closingBalance: number;
  scheduledReceivable: number;
};

export type FinanceBreakdown = {
  label: string;
  amount: number;
};

export type FinanceCashFlowSummary = {
  openingBalance: number;
  totalInflow: number;
  totalOutflow: number;
  closingBalance: number;
  delinquencyRatePct: number;
  inflowShare: number;
  outflowShare: number;
  inflowBreakdown: FinanceBreakdown[];
  outflowBreakdown: FinanceBreakdown[];
};

export type FinanceCashFlow = {
  summary: FinanceCashFlowSummary;
  days: FinanceCashFlowDay[];
};
