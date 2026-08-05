# برنامه‌ی اجرای Sprint 24

## هدف محصول

یک demo قابل‌بازی و قابل‌ارائه از `hezar-cheragh/0.3.0` که از ساخت Session و جفت‌شدن دو Team شروع شود، بحران «بار نرسید / Northern Road Shortage» را به‌صورت spatial و RTL ارائه کند، به یک مذاکره‌ی واقعی Proposal → Counter → Accept برسد و با resolve، reaction، consequence و ending عمومی تمام شود.

## داخل scope

1. تثبیت branch `sprint24/best-playable-build` از آخرین پایه‌ی `phase6b`.
2. اصلاح public projection read path پیش از narrative initialization.
3. حفظ و یکپارچه‌سازی shell فعلی: map، event، active action، readiness و recent pulse در یک صفحه.
4. استفاده از `0.3.0` با دو Team انسانی در Bakery و Logistics و رفتار تألیفی برای Printing و Exchange.
5. حفظ investigation، business activity presentation، spatial proposal target، Counter diff و Agreement map link.
6. حفظ Public Display امن، refresh، reconnect، reduced motion و fallback.
7. مستندسازی audit، final product، completion و screenshot review.

## خارج از scope

- بازنویسی Domain یا Marten.
- افزودن runtime AI، audio، editor عمومی یا deployment.
- تولید دوباره‌ی کل asset library.
- business activity contract اختصاصی جدید در Backend؛ این کار بیش از حد پرریسک و خارج از slice موجود است.
- پشتیبانی نمایشی کامل ۳ و ۴ تیم در Playwright.
- رفع vulnerability dependency بدون release upstream.
- پاک‌سازی `hambaft_dev` یا تغییر SharedWorld.

## ماتریس اولویت

| کار | Impact | Effort | Risk | تصمیم |
|---|---|---|---|---|
| public projection پیش از روایت | critical | زیر ۱ ساعت | low | انجام شد |
| audit و scope docs | high | ۱–۳ ساعت | low | انجام شد |
| build و package `0.3.0` | critical | ۱–۳ ساعت | low | حفظ شد |
| یکپارچه‌سازی shell/map/fallback | critical | ۳–۶ ساعت | medium | حفظ و verify |
| Proposal/Counter/Agreement flow | critical | ۳–۶ ساعت | medium | حفظ و verify |
| activity contract جدید Backend | medium | بیش از ۶ ساعت | high | حذف |
| asset regeneration | low | بیش از ۶ ساعت | medium | حذف |
| Playwright واقعی و screenshots | high | ۳–۴ ساعت | medium | انجام |

## ترتیب اجرا

1. audit branch، config، database safety و baseline.
2. ثبت audit و execution plan.
3. اصلاح public endpoint و اجرای integration targeted.
4. build Frontend و API، اجرای unit/integration/e2e.
5. راه‌اندازی API روی `hambaft_dev` و اجرای local-demo/Playwright واقعی.
6. مرور مسیر Admin → Pair → Story → Proposal → Counter → Accept → Resolve → Reaction → Ending → Public Display.
7. تولید screenshot set بدون secret/log.
8. ثبت final product و completion report، سپس commitهای focused.

## سفر مورد انتظار بازیکن

Admin دو Team می‌سازد، چهار حجره را ایجاد می‌کند، Bakery و Logistics را assign می‌کند و دو کد را یک‌بار تحویل می‌دهد. Teams pair می‌شوند، معرفی کوتاه بازار را می‌بینند، روی نقشه مکان‌ها و evidence را بررسی می‌کنند، choice کسب‌وکار خود را ثبت می‌کنند، برای تأمین اضطراری Proposal می‌فرستند، طرف مقابل Counter می‌دهد، revision پذیرفته می‌شود و Agreement روی نقشه ظاهر می‌شود. Admin checkpoint را resolve می‌کند؛ رفتارهای authored، پیامدهای زمان‌بندی‌شده و pulse بازار دیده می‌شوند و در پایان Entity/World outcome و Public Display ارائه می‌شود.

## duration و acceptance criteria

- مدت انسانی هدف: ۱۲–۱۸ دقیقه، بدون timer مصنوعی.
- ساخت Session تازه و نمایش pairing slips.
- جفت‌شدن هر دو Team با Backend واقعی.
- تفاوت private storylet دو Team و عدم نشت به display.
- حداقل یک investigation/location context و یک business activity.
- Proposal target spatial، Counter revision و Accept.
- Agreement عمومی یا قابل‌مشاهده و reaction نقشه.
- Admin resolve و ending deterministic.
- refresh و reconnect state درست را برگردانند.
- fallback و reduced motion playable بمانند.
- build one-URL در `src/Hambaft.Api/wwwroot` ساخته شود.
- شکست‌های Node 18/Vitest در صورت باقی‌ماندن به‌عنوان محدودیت محیطی، نه feature blocker، ثبت شوند.

## نقاط rollback

- اگر Playwright به دلیل browser/Node محیط شکست خورد، دامنه‌ی جدید اضافه نشود و slice موجود حفظ شود.
- اگر public projection fix به hash lock لطمه بزند، آن را فقط برای view بدون checkpoint/narrative نگه می‌داریم یا revert می‌کنیم.
- اگر activity contract نیازمند تغییر چندلایه شد، همان presentation فعلی حفظ می‌شود.
- در هیچ نقطه‌ای package version منتشرشده حذف یا overwrite نمی‌شود.
