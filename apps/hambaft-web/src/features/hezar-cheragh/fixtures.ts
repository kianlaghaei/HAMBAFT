/** Development fixtures for visual verification of all gameplay panel modes.
 *  No Backend rules are implemented here - purely presentational. */

export type PanelMode =
  | 'SceneIntroduction'
  | 'LocationContext'
  | 'Investigation'
  | 'BusinessActivity'
  | 'PactTargetSelection'
  | 'ProposalLetter'
  | 'Counterproposal'
  | 'CompositeCommitment'
  | 'WaitingForOtherTeams'
  | 'WorldReaction'
  | 'EntityEnding'

export type BusinessActivityKind = 'BakerySupplyAllocation' | 'LogisticsRouteBoard' | 'PrintingEvidenceDesk' | 'ExchangeGuaranteeBoard'

export type InvestigationFixture = {
  mode: 'Investigation'
  remainingSeals: number
  totalSeals: number
  eligibleLocations: Array<{ id: string; name: string; condition: string }>
}

export type BusinessActivityFixture = {
  mode: 'BusinessActivity'
  kind: BusinessActivityKind
  title: string
  instructions: string
  implication: string
  canConfirm: boolean
}

export type PactTargetFixture = {
  mode: 'PactTargetSelection'
  availableTargets: Array<{ id: string; name: string; businessKind: string; condition: string }>
  selectedTargetId?: string
}

export type ProposalLetterFixture = {
  mode: 'ProposalLetter'
  title: string
  from: string
  to: string
  body: string
  terms: Array<{ name: string; value: string }>
}

export type CounterproposalFixture = {
  mode: 'Counterproposal'
  title: string
  from: string
  to: string
  originalTerms: Array<{ name: string; value: string; status: 'unchanged' | 'changed' | 'removed' }>
  addedTerms: Array<{ name: string; value: string }>
}

export type WorldReactionFixture = {
  mode: 'WorldReaction'
  reactions: Array<{ outcomeLine: string; locationName: string; locationId: string }>
}

export type PanelFixture =
  | { mode: 'SceneIntroduction'; title: string; narrative: string; eventTitle: string }
  | { mode: 'LocationContext'; location: { name: string; whyItMatters: string; whoIsHere: string; currentCondition: string; recentChange: string; availableActions: string[] } }
  | InvestigationFixture
  | BusinessActivityFixture
  | PactTargetFixture
  | ProposalLetterFixture
  | CounterproposalFixture
  | { mode: 'CompositeCommitment'; agreements: Array<{ title: string; parties: string[]; description: string }> }
  | { mode: 'WaitingForOtherTeams'; message: string }
  | WorldReactionFixture
  | { mode: 'EntityEnding'; title: string; evidence: string[]; narrative: string }

export const devFixtures: Record<string, PanelFixture> = {
  introduction: {
    mode: 'SceneIntroduction',
    title: 'بازار هزارچراغ',
    eventTitle: 'صبحِ بی‌زنگ',
    narrative: 'صبح است و زنگ بازار به صدا درنیامده. بار promised دیروز هنوز نرسیده. حاج‌صادق از دفتر خارج نشده. بازار آرام است، اما این آرامش بوی تردید می‌دهد.',
  },
  investigation: {
    mode: 'Investigation',
    remainingSeals: 1,
    totalSeals: 2,
    eligibleLocations: [
      { id: 'bakery-sepideh', name: 'نانوایی سپیده', condition: 'فعالیت کمتر از حد معمول' },
      { id: 'logistics-rah-no', name: 'باربری راه نو', condition: 'دفتر نیمه‌باز' },
      { id: 'market-entrance', name: 'دروازه بازار', condition: 'مسیر شمال بسته' },
    ],
  } as InvestigationFixture,
  bakery: {
    mode: 'BusinessActivity',
    kind: 'BakerySupplyAllocation',
    title: 'تخصیص سهمیه آرد',
    instructions: 'مقدار آردی که امروز می‌خواهید تخصیص دهید را مشخص کنید. سهمیه محدود است.',
    implication: 'تخصیص امروز روی موجودی سه روز آینده اثر می‌گذارد.',
    canConfirm: true,
  } as BusinessActivityFixture,
  logistics: {
    mode: 'BusinessActivity',
    kind: 'LogisticsRouteBoard',
    title: 'تابلوی مسیر باربری',
    instructions: 'مسیرهای فعال و بسته را بررسی کنید. مسیر شمال همچنان مسدود است.',
    implication: 'بسته بودن مسیر شمال به معنی تأخیر در دریافت بار است.',
    canConfirm: false,
  } as BusinessActivityFixture,
  printing: {
    mode: 'BusinessActivity',
    kind: 'PrintingEvidenceDesk',
    title: 'میز مستندات چاپ',
    instructions: 'مدارک ثبت‌شده را مرور کنید و صحت آنها را تأیید کنید.',
    implication: 'تأیید مدارک روی اعتبار عمومی اثرگذار است.',
    canConfirm: true,
  } as BusinessActivityFixture,
  exchange: {
    mode: 'BusinessActivity',
    kind: 'ExchangeGuaranteeBoard',
    title: 'تابلوی ضمانت صرافی',
    instructions: 'نرخ‌های امروز را ببینید و ضمانت‌های فعال را مدیریت کنید.',
    implication: 'تغییرات ضمانت روی نقدینگی بازار اثر می‌گذارد.',
    canConfirm: true,
  } as BusinessActivityFixture,
  pactTarget: {
    mode: 'PactTargetSelection',
    availableTargets: [
      { id: 'bakery-sepideh', name: 'نانوایی سپیده', businessKind: 'bakery', condition: 'نیازمند آرد' },
      { id: 'logistics-rah-no', name: 'باربری راه نو', businessKind: 'logistics', condition: 'منتظر بار' },
      { id: 'exchange-mizan', name: 'صرافی میزان', businessKind: 'exchange', condition: 'فعال' },
    ],
  } as PactTargetFixture,
  proposal: {
    mode: 'ProposalLetter',
    title: 'پیشنهاد همکاری تأمین آرد',
    from: 'چاپ روشن',
    to: 'نانوایی سپیده',
    body: 'با سلام و احترام. با توجه به شرایط فعلی بازار و نیاز شما به آرد مرغوب، پیشنهاد می‌کنیم قراردادی برای تأمین پایدار منعقد کنیم. شرایط پیشنهادی به پیوست تقدیم می‌شود.',
    terms: [
      { name: 'مقدار', value: '۲۰۰ کیسه در هفته' },
      { name: 'مدت', value: '۴ هفته' },
      { name: 'نمایش عمومی', value: 'بله' },
    ],
  } as ProposalLetterFixture,
  counterproposal: {
    mode: 'Counterproposal',
    title: 'پیشنهاد متقابل تأمین آرد',
    from: 'نانوایی سپیده',
    to: 'چاپ روشن',
    originalTerms: [
      { name: 'مقدار', value: '۲۰۰ کیسه در هفته', status: 'changed' },
      { name: 'مدت', value: '۴ هفته', status: 'unchanged' },
      { name: 'نمایش عمومی', value: 'بله', status: 'removed' },
    ],
    addedTerms: [
      { name: 'تضمین کیفیت', value: 'آرد درجه یک' },
      { name: 'یادداشت', value: 'شرط فسخ یک‌طرفه حذف شود' },
    ],
  } as CounterproposalFixture,
  waiting: {
    mode: 'WaitingForOtherTeams',
    message: 'منتظر تصمیم تیم‌های دیگر... بازار در حال بررسی پیشنهاد شماست.',
  },
  worldReaction: {
    mode: 'WorldReaction',
    reactions: [
      { outcomeLine: 'نانوایی سپیده: سهمیه آرد کاهش یافت و موجودی به نصف رسید.', locationName: 'نانوایی سپیده', locationId: 'bakery-sepideh' },
      { outcomeLine: 'باربری راه نو: مسیر شمال همچنان بسته است و بار معطل مانده.', locationName: 'باربری راه نو', locationId: 'logistics-rah-no' },
      { outcomeLine: 'چاپ روشن: سفارش جدیدی ثبت شد و اعتبار عمومی افزایش یافت.', locationName: 'چاپ روشن', locationId: 'printing-roshan' },
    ],
  } as WorldReactionFixture,
  entityEnding: {
    mode: 'EntityEnding',
    title: 'پایان مسیر نانوایی سپیده',
    narrative: 'نانوایی سپیده به دلیل کمبود مداوم آرد و عدم توانایی در تأمین تعهدات، چراغش خاموش شد. رد پای تصمیم‌های ثبت‌شده در این سرانجام باقی خواهد ماند.',
    evidence: ['قول قیمت ثابت نقض شد', 'موجودی به صفر رسید', 'پیمان با چاپ روشن اجرا نشد'],
  },
  locationContext: {
    mode: 'LocationContext',
    location: {
      name: 'نانوایی سپیده',
      whyItMatters: 'نانوایی سپیده اصلی‌ترین تأمین‌کننده نان بازار است و وضعیت آن روی تمام حجره‌ها اثر می‌گذارد.',
      whoIsHere: 'سپیده (مدیر)، دو کارگر',
      currentCondition: 'فعالیت کمتر از حد معمول - آرد رو به اتمام',
      recentChange: 'سهمیه آرد دیروز نصف شد',
      availableActions: ['بازرسی حجره', 'گفتگو با سپیده', 'بررسی موجودی'],
    },
  },
  compositeCommitment: {
    mode: 'CompositeCommitment',
    agreements: [
      { title: 'تأمین آرد', parties: ['نانوایی سپیده', 'چاپ روشن'], description: 'تعهد تأمین ۲۰۰ کیسه آرد در هفته به مدت ۴ هفته' },
      { title: 'حمل بار', parties: ['باربری راه نو', 'صرافی میزان'], description: 'تعهد حمل ماهانه محموله از مسیر جنوب' },
    ],
  },
}
