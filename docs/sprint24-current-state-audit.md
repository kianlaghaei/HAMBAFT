# گزارش ممیزی وضعیت فعلی HAMBAFT — Sprint 24

تاریخ ممیزی: ۵ اوت ۲۰۲۶  
شاخه‌ی مبنا: `phase6b/market-experience-pass` در commit `0fbaa68`  
شاخه‌ی Sprint: `sprint24/best-playable-build`

## خلاصه‌ی اجرایی

قوی‌ترین پایه، آخرین commit شاخه‌ی `phase6b` است؛ این شاخه‌ی هم‌اکنون شامل تجربه‌ی بازار، ارائه‌ی معنایی، نقشه‌ی تعاملی، fallback، Public Display، Proposal/Agreement و مسیرهای 2 تا 4 تیمی است. تغییرات محلی اولیه عمدتاً تبدیل line ending بودند و تفاوت معنایی با commit مبنا نداشتند؛ بنابراین مبنای محصول همان `0fbaa68` در نظر گرفته شد.

تصمیم ممیزی: به‌جای گسترش موتور، یک مسیر عمودی و قابل‌نمایش از ورود Admin تا بحران «بار نرسید» تثبیت شود.

## شمارش واقعی

| مورد | تعداد / وضعیت |
|---|---:|
| پروژه‌های solution | ۱۳ خط Project در `Hambaft.sln`؛ ۱۱ پروژه‌ی واقعی و ۲ solution folder |
| endpointهای API، Hub و health | ۳۵ نگاشت |
| command recordها | ۲۹ رکورد قراردادی؛ ۲۷ command/result عملیاتی با احتساب Context و Result |
| event recordها | ۳۹ رکورد شامل metadata و eventهای دامنه |
| projection classها | ۴ projection اصلی؛ ۲ helper projection داخلی |
| نسخه‌های Hezar Cheragh | ۳ نسخه: `0.1.0`، `0.2.0`، `0.3.0` |
| checkpointهای مؤثر Hezar Cheragh | ۵ مورد با احتساب terminal `slice-complete` |
| storyletها | ۲۰ |
| choiceها | ۴۰ |
| interactionها | ۵ |
| behavior profileها / actionها | ۵ / ۵ |
| consequenceها | ۴ |
| Entity Endingها | ۱۲ |
| World Endingها | ۵ |
| routeهای Frontend | ۲۰ تگ Route؛ شامل team، admin، display و dev lab |
| تست‌های Backend | ۲۶ فایل؛ ۱۱۴ Fact/Theory کشف‌شده؛ baseline runner: ۱۴۲ موفق از ۱۴۳ |
| تست‌های Frontend | ۱۴ فایل؛ ۶۳ تست تعریف‌شده |
| تست‌های Playwright | ۲ flow |
| فایل‌های تصویری کاربردی | ۱۰ فایل PNG در `artifacts/visual-world/assets`؛ به‌علاوه ۱ نقشه‌ی نهایی و ۳ concept map |
| شاخه‌های محلی فعال | ۷؛ فقط `sprint24/best-playable-build` شاخه‌ی Sprint است |

## ممیزی شاخه‌ها

- `main` و `origin/main`: پایه‌ی Phase 5، بدون تجربه‌ی کامل بازار.
- `phase4/ink-endings-hezar-cheragh`: vertical slice بک‌اند و endings، بدون shell نهایی Frontend.
- `phase5/react-playable-client`: Client اولیه و routeهای پایه.
- `phase6b/market-experience-pass`: جدیدترین پایه‌ی منسجم؛ شامل نقشه، presentation، scene director و UI shell.
- `phase2/story-runtime` و `phase3/interactions-runtime`: اجداد قابلیت‌های runtime؛ برای مبنای Sprint انتخاب نشدند.
- `sprint24/best-playable-build`: از `0fbaa68` ساخته شد و تغییر Sprint را نگه می‌دارد.
- worktree جداگانه‌ای وجود ندارد و هیچ branch ناقص دیگری با commit جدیدتر از `0fbaa68` پیدا نشد.

## بررسی Backend و داده

- SDK موجود: .NET `10.0.302`؛ PostgreSQL service `postgresql-x64-16` در وضعیت Running.
- اتصال تست resolved شد به `hambaft_test` و اتصال توسعه به `hambaft_dev`.
- fixture تست، نام دیتابیس را قبل از اتصال با `DatabaseSafety.ValidateTestConnection` به `hambaft_test` محدود می‌کند و SharedWorld را رد می‌کند.
- `ConnectionStrings__Hambaft` در host تست با `ConnectionStrings:Hambaft` در factory روی connection تست override نمی‌شود؛ factory هر دو کلید را صریحاً به connection تست تنظیم می‌کند.
- schema Marten، `hambaft`، در integration fixture ساخته و migration آن اعمال می‌شود. `hambaft_dev` هرگز clean/reset نشد.
- شمارش خواندنی PostgreSQL از محیط فعلی به‌دلیل quoting ابزار `psql` موفق به خروجی جدول نشد؛ اتصال و schema از ۱۹ تست integration موفق تأیید شد. هیچ اتصال به SharedWorld انجام نشد.

## baseline واقعی

### Backend

- `dotnet restore`: موفق.
- `dotnet build`: موفق، ۰ warning و ۰ error.
- `dotnet test`: معماری ۷/۷، end-to-end برابر ۹/۹، unit برابر ۱۰۷/۱۰۷، integration برابر ۱۹/۲۰؛ تنها شکست، 404 در public endpoint پس از `Start` با package ساختگی `demo.story`.
- علت شکست: public read path پیش از مقداردهی روایت، package را load/hash می‌کرد؛ اصلاح Sprint باعث شد projection عمومی امن در وضعیت بدون روایت مستقیماً برگردد.

### Frontend

- `npm ci`: موفق؛ Node 18 هشدار engine برای وابستگی‌های نیازمند Node 20+ داد.
- `npm run typecheck`: موفق.
- `npm run lint`: موفق.
- `npm run test`: شکست baseline محیطی؛ Vitest/JSDOM در Node 18 با `@exodus/bytes` خطای `ERR_REQUIRE_ESM` داد و ۱۴ worker بالا نیامد.
- `npm run build`: موفق؛ هشدار engine نسخه‌ی Node، annotation در SignalR و chunk بزرگ‌تر از ۵۰۰KB.
- `npm audit`: دو high severity در dependencyهای React Router، مستندشده و خارج از scope Sprint.

### Playwright

در baseline هنوز علیه backend واقعی اجرا نشده بود. پیکربندی موجود یک flow کامل 2-Team و یک local-demo flow دارد؛ اجرای نهایی پس از build و راه‌اندازی host انجام می‌شود.

## طبقه‌بندی قابلیت‌ها

| قابلیت | وضعیت |
|---|---|
| event sourcing، replay، hash lock | Complete and verified |
| Proposal revision immutability و Team privacy | Complete and verified |
| Agreement، behavior، consequence و endings | Complete and verified در تست‌های Backend |
| Ink presentation | Complete and verified؛ presentation-only |
| Hezar Cheragh `0.3.0` | Complete but visually weak در بعضی business activityها |
| semantic presentation و Scene Director | Complete and verified |
| نقشه‌ی fallback و Pixi | Complete but visually weak؛ Pixi فعلاً vector market است و assetهای PNG بیشتر در manifest/dev lab هستند |
| Team shell و routeهای بازی | Complete but visually weak |
| business-specific activity | Implemented but not integrated به contract اختصاصی Backend؛ فعلاً choice/presentation محور |
| Proposal target map و Counter diff | Complete and verified در UI/unit |
| Admin setup و runtime | Complete but visually weak |
| Public Display privacy | Complete and verified با تست‌های semantic |
| 2-Team readiness | Ready؛ مسیر اصلی |
| 3-Team readiness | Backend و package compatible؛ در Sprint هدف اصلی نیست |
| 4-Team readiness | Backend و package compatible؛ در Sprint هدف اصلی نیست |
| refresh و SignalR refetch hint | Implemented but not fully Playwright-verified |
| reduced-motion و fallback | Complete but not yet real-browser verified |
| local one-URL demo | Build-ready؛ Playwright نهایی باید تأیید کند |

## ارزیابی محصول

- **Backend maturity:** زیاد؛ مرز authority و replay روشن است.
- **Story maturity:** متوسط رو به زیاد؛ چهار checkpoint روایی، choiceهای توزیع‌شده و endings دارد، اما بحران کوتاه و بعضی متن‌ها هنوز به flow انتخابی نزدیک‌اند.
- **UI maturity:** متوسط؛ shell و RTL خوب است، اما بعضی activityها placeholder بصری‌اند.
- **Map maturity:** متوسط؛ fallback خوانا و semantic است، نقشه‌ی PNG نهایی کیفیت خوبی دارد، اما اتصال کامل assetهای modular به runtime هنوز انجام نشده.
- **Gameplay maturity:** متوسط؛ investigation/pact/activity در presentation حاضرند، authoritative activity contract عمومی نشده.
- **Admin usability:** خوب برای demo؛ setup، کدها، runtime، resolve و display link موجود است.
- **Public Display:** خوب و privacy-safe؛ negotiation خصوصی وارد آن نمی‌شود.
- **Accessibility:** keyboard map target، accessible location list، reduced motion و fallback موجود است.
- **مدت:** duration package برابر ۳۵ دقیقه است؛ slice انتخابی با discussion انسانی حدود ۱۲–۱۸ دقیقه هدف‌گذاری شد.
- **local-demo readiness:** build واقعی موفق؛ اجرای end-to-end به نسخه‌ی Node و Chrome محیط وابسته است.

## نتیجه‌ی scope

محصول پیشنهادی و انتخاب‌شده: «دموی polished هزارچراغ از ورود به بازار تا یک بحران کامل اقتصادی، با ۲ تیم انسانی، دو کسب‌وکار مؤثر، دو کسب‌وکار رفتار تألیفی، یک Proposal/Counter/Agreement، واکنش بازار و پایان عمومی.»
