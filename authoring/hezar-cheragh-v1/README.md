# Hezar Cheragh Story V1 — Writer Workspace

این پوشه محل نویسندگی انسانی Story V1 است. فایل‌های Markdown اینجا source of truth انسانی‌اند؛ runtime JSON پکیج فقط بعد از تأیید متن و تصمیم‌ها از روی این فایل‌ها تولید یا تکمیل می‌شود.

## قواعد همکاری

- متن داستانی را فقط در بخش‌های مشخص‌شده با `<PASTE APPROVED STORY HERE>` و placeholderهای مرتبط وارد کنید.
- Agent نباید متن داستانی approved را بازنویسی، گسترش، خلاصه یا ادبی‌تر کند.
- Agent فقط مجاز است IDها، escaping و ساختار JSON را normalize کند؛ معنای داستان، سؤال، گزینه، trade-off، effect و تصویر را تغییر نمی‌دهد.
- تصویر فقط از prompt تأییدشده تولید می‌شود.
- مختصات hotspot بعد از تولید تصویر باید به‌صورت بصری بررسی و در `hotspot-coordinates.md` ثبت شوند.
- تا وقتی قراردادهای ناقص مثل Decision Pack چندسؤالی و ending image تصویب نشده‌اند، آن‌ها را با متن یا مسیر hardcoded شبیه‌سازی نکنید.
- این workspace پکیج `1.0.0` نیست و immutable package منتشر نمی‌کند.

## ساختار

```text
authoring/hezar-cheragh-v1/
  README.md
  act-01/
    00-shared-market.md
    01-bakery.md
    02-logistics.md
    03-printing.md
    04-exchange.md
    05-end-of-act.md
    image-prompts.md
    hotspot-coordinates.md
  act-02/ ... act-05/   # همان اسکلت خالی برای نویسندگی بعدی
```

Act 01 template کامل است؛ Actهای 02 تا 05 فقط محل آماده برای همین داده‌ها هستند و هیچ متن داستانی ندارند.

## نگاشت کلی به Story Package

| Markdown | Runtime JSON بعد از approval |
|---|---|
| identity و مسیر checkpoint | `manifest.json`, `storylets.json` |
| opening، story sheet و next hook | `presentation/team-scenes.json`, در صورت نیاز `narrative.json` |
| hotspot و مختصات | `presentation/team-scenes.json` → `hotspots[]` |
| evidence و یافته‌ها | `presentation/team-scenes.json` → `storySheets[].evidence[]` و memory/effectهای لازم |
| Decision Pack و گزینه‌های authoritative | `storylets.json`, `effects.json`؛ خلاصه نمایشی در `presentation/team-scenes.json` |
| Pact و compatibility | `interactions.json`, `effects.json`, `conditions` |
| immediate reaction | `presentation/team-scenes.json` و `presentation/reactions.json` |
| تصویر | `stories/hezar-cheragh/<draft-version>/assets/...` و `backgroundAssetId` |

جزئیات runtime و مسیر Backend در [راهنمای Authoring](../../docs/HEZAR_CHERAGH_V1_AUTHORING_GUIDE.md) ثبت شده است.

## گردش تأیید

1. نویسنده بخش‌های Markdown را پر می‌کند.
2. تیم روایت متن، سؤال‌ها، گزینه‌ها، effects، Pactها و promptهای تصویر را approve می‌کند.
3. مختصات پس از تولید تصویر visually checked می‌شوند.
4. فقط داده approved به Story Package draft منتقل می‌شود.
5. package validation انجام می‌شود؛ سپس درباره version/publish تصمیم‌گیری می‌شود.
