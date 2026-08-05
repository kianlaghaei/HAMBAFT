# محصول نهایی Sprint 24

## تجربه‌ی قابل‌بازی

HAMBAFT اکنون یک demo فارسی و RTL-first از بازار هزارچراغ دارد: راهبر یک Session دو تیمی می‌سازد، Teams با کد pair می‌شوند، نقشه‌ی بازار و event عمومی را می‌بینند، private storylet خود را پاسخ می‌دهند، مکان‌ها را بررسی می‌کنند و درباره‌ی بحران «بار نرسید» مذاکره می‌کنند.

مسیر اصلی از `morning-without-bell` به `cargo-did-not-arrive` و سپس `avan-offer` و `market-gathering` می‌رود. انتخاب‌ها، Agreement، authored behavior، consequence و dual ending در Backend authoritative و event-sourced باقی می‌مانند.

## اجرای محلی

```bash
dotnet restore
dotnet build
npm ci --prefix apps/hambaft-web
npm run build --prefix apps/hambaft-web
```

برای one-URL demo، connection توسعه باید به `hambaft_dev` باشد و API روی `http://localhost:5297` اجرا شود. برای تست‌ها فقط `hambaft_test` استفاده می‌شود.

## setup راهبر

1. در `/admin`، بسته‌ی `بازار هزارچراغ — 0.3.0` و ۲ تیم را انتخاب کنید.
2. برای demo، `نانوایی سپیده` و `باربری راه‌نو` را به دو Team بدهید.
3. pairing slips را یک‌بار چاپ/کپی کنید.
4. `شروع جلسه` و سپس `مقداردهی روایت` را بزنید.
5. لینک `/display/{sessionId}` را روی نمایشگر عمومی باز کنید.
6. پس از پاسخ Teamها، از `اجرای زنده` پیشنهادها، Agreementها، consequenceها و resolve را کنترل کنید.

## مدت و محدودیت

مدت انسانی مورد انتظار ۱۲–۱۸ دقیقه است و discussion تیمی بخشی از زمان است. activityهای business فعلاً authored presentation روی choiceهای Backend هستند؛ contract اختصاصی allocation/route هنوز در scope این Sprint نیست. Pixi و fallback هر دو موجودند، اما assetهای PNG فعلی هنوز کاملاً به‌عنوان لایه‌های modular runtime متصل نشده‌اند. audio عمداً حذف شده است.
