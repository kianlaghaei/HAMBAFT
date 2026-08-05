# ممیزی مسیرهای تجربه Team — Phase 1

تاریخ ممیزی: ۱۴۰۵/۰۵/۰۵

## مسیرهای ثبت‌شده

| مسیر | طبقه‌بندی | وضعیت Phase 1 |
| --- | --- | --- |
| `/team` | مسیر canonical تجربه Team | تنها مقصد Team؛ gameplay فعلی را render می‌کند |
| `/team/story` | زیرصفحه قدیمی Team | redirect داخلی به `/team` |
| `/team/market` | زیرصفحه redundant Team | redirect داخلی به `/team` |
| `/team/messages` | زیرصفحه redundant Team | redirect داخلی به `/team` |
| `/team/agreements` | زیرصفحه redundant Team | redirect داخلی به `/team` |
| `/team/status` | زیرصفحه redundant Team | redirect داخلی به `/team` |
| `/team/ending` | زیرصفحه قدیمی Team | redirect داخلی به `/team` |
| `/pair` | authentication/pairing | بدون تغییر؛ pairing موفق به `/team` می‌رود |
| `/admin` | Admin | بدون تغییر functional |
| `/admin/session/:sessionId/setup` | Admin | بدون تغییر |
| `/admin/session/:sessionId/runtime` | Admin | بدون تغییر |
| `/display/:sessionId` | Public Display | بدون تغییر |
| `/dev/hezar-cheragh-ui` | internal/dev | بدون تغییر و فقط در development |
| `/` و `*` | عمومی / fallback | ریشه برای Team paired به `/team` هدایت می‌شود |

## یافته‌های navigation

- `TeamShell` یک نوار route-based برای Story، Market، Messages، Agreements و Status داشت.
- خود gameplay screen به Status، Messages و Agreements لینک route-based داشت.
- pairing و refresh بر اساس credential ذخیره‌شده در `sessionStorage` انجام می‌شد؛ این سازوکار حفظ می‌شود.

## privacy و guard

- `TeamGuard` فقط credential معتبر با نقش Team را می‌پذیرد و در غیر این صورت به `/pair` می‌فرستد.
- `AdminGuard` برای مسیرهای private Admin حفظ می‌شود.
- مسیرهای Public Display و Admin از Team route tree جدا هستند و در این فاز تغییر نمی‌کنند.
