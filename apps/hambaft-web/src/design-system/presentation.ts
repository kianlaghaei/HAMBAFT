export const faDigits = (value: number | string) => String(value).replace(/\d/g, (digit) => '۰۱۲۳۴۵۶۷۸۹'[Number(digit)] ?? digit)

const checkpoints: Record<string, string> = {
  'morning-without-bell': 'صبحِ بی‌زنگ',
  'shipment-missing': 'بار نرسید',
  'avan-offer': 'پیشنهاد آوان',
  'market-gathering': 'جمع‌شدن بازار',
  outcome: 'پس از تصمیم بازار',
}

const metrics: Record<string, string> = {
  Liquidity: 'نقدینگی', Capacity: 'ظرفیت', Inventory: 'موجودی', Reliability: 'اعتبار عملی',
  LocalTrust: 'اعتبار محلی', DebtExposure: 'تعهد بدهی', Pressure: 'فشار بازار',
  PublicTrust: 'اعتماد عمومی', Autonomy: 'استقلال', Transparency: 'شفافیت', Resilience: 'تاب‌آوری',
  Trust: 'اعتماد', Obligation: 'تعهد', Influence: 'نفوذ', DebtObligation: 'دین',
}

const memories: Record<string, string> = {
  'stable-price-promised': 'قول قیمت ثابت ثبت شد.',
  'hidden-route-kept': 'بخشی از دفتر پنهان ماند.',
  'hidden-route-revealed': 'مسیر فرعی افشا شد.',
}

const consequences: Record<string, string> = {
  'stable-price-review': 'بازبینی قول قیمت ثابت',
  'hidden-route-review': 'پیامد مسیر پنهان',
  'unattributed-notice-review': 'بازبینی اطلاعیه بی‌نام',
  'emergency-credit-review': 'سررسید اعتبار اضطراری',
}

export const checkpointLabel = (key?: string | null) => key ? (checkpoints[key] ?? 'فصل تازه‌ای در بازار') : 'پیش از آغاز روایت'
export const metricLabel = (key: string) => metrics[key] ?? key.replace(/[._-]/g, ' ')
export const memoryLabel = (key: string) => memories[key] ?? 'یک نشانه در دفتر حجره ثبت شد.'
export const consequenceLabel = (key: string) => consequences[key] ?? 'پیامدی در ادامه روایت در انتظار است.'

export const statusLabel = (status: string | number) => ({
  Created: 'ایجادشده', Lobby: 'آماده‌سازی', Running: 'در حال اجرا', Paused: 'متوقف موقت', Completed: 'پایان‌یافته',
  Pending: 'در انتظار پاسخ', Countered: 'پیشنهاد متقابل', Accepted: 'پذیرفته‌شده', Rejected: 'ردشده', Expired: 'منقضی',
  Cancelled: 'لغوشده', Active: 'فعال', Executed: 'اجراشده', Failed: 'ناموفق',
  0: 'فعال', 1: 'اجراشده', 2: 'ناموفق', 3: 'لغوشده',
}[String(status)] ?? String(status))

export const sessionStatusName = (status: string | number) => ({ 0: 'Created', 1: 'Lobby', 2: 'Running', 3: 'Paused', 4: 'Completed', 5: 'Cancelled', 6: 'Faulted' }[String(status)] ?? String(status))
export const sessionStatusLabel = (status: string | number) => statusLabel(sessionStatusName(status))
export const proposalStatusLabel = (status: string | number) => statusLabel(({ 0: 'Pending', 1: 'Countered', 2: 'Accepted', 3: 'Rejected', 4: 'Expired', 5: 'Cancelled', 6: 'Executed', 7: 'Failed' }[String(status)] ?? status))
export const agreementStatusLabel = (status: string | number) => statusLabel(({ 0: 'Active', 1: 'Executed', 2: 'Failed', 3: 'Cancelled' }[String(status)] ?? status))

export const controllerLabel = (value: string | number) => ({ HumanTeam: 'تیم انسانی', AuthoredBehavior: 'رفتار تألیفی', System: 'سامانه', 0: 'تیم انسانی', 1: 'رفتار تألیفی', 2: 'سامانه' }[String(value)] ?? String(value))

export const termLabel = (name: string) => ({ units: 'مقدار', public: 'نمایش عمومی', note: 'یادداشت', capacityUnits: 'واحد ظرفیت', supportDurationCheckpoints: 'مدت پشتیبانی', publicDisclosure: 'اعلام عمومی' }[name] ?? name)

export function endingEvidenceText(evidence: { kind: string | number; stableId?: string | null; key?: string | null; value?: string | null }) {
  if (evidence.key) {
    const subject = metrics[evidence.key] ?? memories[evidence.key]
    if (subject) return `${subject}${evidence.value ? `: ${evidence.value}` : ''}`
  }
  const kind = ({ 0: 'DomainEvent', 1: 'Choice', 2: 'Proposal', 3: 'Agreement', 4: 'Memory', 5: 'MetricSnapshot', 6: 'Relationship', 7: 'BehaviorAction', 8: 'Consequence' } as Record<string, string>)[String(evidence.kind)] ?? String(evidence.kind)
  return ({ Choice: 'یکی از تصمیم‌های ثبت‌شده شما در این سرانجام مؤثر بود.', Proposal: 'یک پیشنهاد ثبت‌شده مسیر این سرانجام را تغییر داد.', Agreement: 'یک پیمان پذیرفته‌شده در این سرانجام اثر گذاشت.', Memory: 'یادی که بازار نگه داشت در این نتیجه مؤثر بود.', MetricSnapshot: 'وضعیت نهایی یکی از شاخص‌های آشکار در این نتیجه مؤثر بود.', Relationship: 'رابطه ثبت‌شده میان حجره‌ها در این نتیجه اثر داشت.', BehaviorAction: 'رفتار یک کسب‌وکار بدون تیم در این نتیجه مؤثر بود.', Consequence: 'یک پیامد فعال‌شده به این سرانجام راه برد.', DomainEvent: 'یک رویداد ثبت‌شده در مسیر این پایان مؤثر بود.' } as Record<string, string>)[kind] ?? 'یک رویداد ثبت‌شده در مسیر این پایان مؤثر بود.'
}
