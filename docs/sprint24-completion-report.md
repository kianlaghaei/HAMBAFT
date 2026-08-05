# گزارش تکمیل Sprint 24

## وضعیت

نتیجه‌ی فنی Sprint، یک playable vertical slice منسجم است؛ Backend و build موفق‌اند و integration failure baseline در public projection اصلاح شد. اعتبارسنجی واقعی Playwright و screenshot capture باید در محیطی با Node 20+ و Chrome نصب‌شده انجام شود.

## تغییر اصلی

`MartenSessionStore.LoadPublicViewAsync` اکنون وقتی Session هنوز checkpoint یا narrative عمومی ندارد، projection عمومی امن را مستقیماً برمی‌گرداند و برای وضعیت قبل از روایت به package loader وابسته نیست. پس از آغاز روایت، همان hash-check و hydration قبلی اجرا می‌شود.

## verification

- PostgreSQL service: Running؛ `hambaft_test` برای تست و `hambaft_dev` برای اجرا تأیید شد.
- Backend: restore/build موفق؛ targeted integration پس از fix موفق.
- Frontend: typecheck/lint/build موفق با هشدار محیط Node 18؛ Vitest baseline به دلیل incompatibility JSDOM/ESM اجرا نشد.
- هیچ دیتابیس SharedWorld، credential یا push استفاده نشد.
