# پوستهٔ رابط بازی هزارچراغ

## هدف

این تغییر یک پوستهٔ بصری و تعاملی برای تجربهٔ تیمی هزارچراغ فراهم می‌کند. پوسته برای اتصال بعدی به نقشهٔ ماژولار بازار و صحنهٔ نخست بازی آماده است و هیچ قانون بازی یا فرمان Backend را پیاده‌سازی نمی‌کند.

## چیدمان دسکتاپ

`HezarCheraghShell` یک چیدمان RTL سه‌قسمتی می‌سازد:

- هدر فشرده برای نام تیم، زمان روایی، وضعیت بازار و اعلان فوری؛
- ناحیهٔ اصلی با نقشه در حدود دو سوم عرض و پنل زمینه‌ای در حدود یک سوم عرض؛
- نوار پایین برای تغییرات اخیر، نبض کسب‌وکار و پیام‌ها/پیمان‌ها.

ارتفاع نقشه به حدود ۶۵٪ ارتفاع پنجره محدود می‌شود تا اقدام فعال و روایت به زیر fold منتقل نشوند.

## چیدمان تبلت

در عرض‌های نزدیک به `1024px`، نقشه در بخش بالایی قرار می‌گیرد و پنل به Bottom Sheet تبدیل می‌شود. سه حالت `collapsed`، `half` و `expanded` با کنترل‌های قابل دسترس در پوستهٔ visual lab قابل بررسی هستند. مکان انتخاب‌شده نیز در همان پنل نمایش داده می‌شود.

## پیکربندی نقشه

`bazaarMapConfig.ts` تنها محل تعریف مختصات و metadata مکان‌هاست. هر مکان یک `assetId` معنایی، مختصات، scale، anchor، z-index و محل label دارد. مختصات فعلی provisional هستند و با تحویل نقشهٔ نهایی بدون تغییر در کامپوننت‌ها قابل جایگزینی‌اند.

`BazaarMapViewport` وضعیت‌های selected، focused، new-information، action-available، changed-since-last-view، pact-target و reaction-focus را روی hotspotها و descriptor اعمال می‌کند.

## Asset registry

`assetRegistry.ts` مسیرها و metadata را از یک manifest resolve می‌کند. manifest توسعه در `devManifest.ts` از placeholderهای خنثی با `src` خالی استفاده می‌کند و برای missing asset fallback و warning توسعه‌ای دارد. APIهای preload و registry برای اتصال PNGهای alpha و overlayهای آینده آماده‌اند.

## حالت‌های پنل

`GameplayPanel` حالت‌های زیر را پوشش می‌دهد:

`SceneIntroduction`، `LocationContext`، `Investigation`، `BusinessActivity`، `PactTargetSelection`، `ProposalLetter`، `Counterproposal`، `CompositeCommitment`، `WaitingForOtherTeams`، `WorldReaction` و `EntityEnding`.

fixtureهای توسعه در `fixtures.ts` قرار دارند و صرفاً برای بررسی بصری هستند. readiness، هزینه، نتیجه و state authoritative از Backend محاسبه نمی‌شوند.

## تعامل مکان و بررسی

انتخاب pointer/keyboard از طریق hotspot و فهرست دسترس‌پذیر انجام می‌شود. انتخاب مکان، context panel را به اطلاعات همان مکان تبدیل می‌کند. حالت Investigation دو مهر تصویری، مکان‌های واجد شرایط، پیام readiness فارسی و نتیجهٔ روایی را نمایش می‌دهد؛ state بررسی در این کار persist نمی‌شود.

## پوستهٔ فعالیت‌ها

چهار renderer placeholder در پنل فعالیت نمایش داده می‌شوند:

- `BakerySupplyAllocation`
- `LogisticsRouteBoard`
- `PrintingEvidenceDesk`
- `ExchangeGuaranteeBoard`

هر renderer عنوان، دستور کوتاه، ناحیهٔ تصویری SVG، implication شناخته‌شده، وضعیت waiting/validation و confirm action دارد. این اجزا قانون یا مقدار جدیدی برای Backend اختراع نمی‌کنند.

## تجربهٔ Pact

هدف پیمان روی نقشه/لیست قابل انتخاب است. Proposal به شکل نامهٔ مؤلفانه نمایش داده می‌شود و termهای typed فعلی را در قالب خوانا ارائه می‌کند. Counterproposal تفاوت‌های unchanged، changed، removed و added را با کلاس و علامت بصری جدا می‌کند. هیچ تغییری در Proposal API انجام نشده است.

## واکنش جهان

World reaction به صورت کارت‌های ترتیبی کنار نقشه ارائه می‌شود؛ مکان واکنش‌داده‌شده focus می‌گیرد و دکمه‌های بعدی/ردکردن وجود دارند. در reduced-motion کارت ثابت باقی می‌ماند و fallback همان معنا را بدون asset نهایی حفظ می‌کند.

## آزمایشگاه توسعه

مسیر Development-only:

`/dev/hezar-cheragh-ui`

این مسیر زمان صبح/غروب/شب، معرفی، بررسی، هر چهار فعالیت، انتخاب Pact، Proposal، Counterproposal، Agreement، واکنش جهان، انتظار، پایان موجودیت، desktop/tablet، fallback و reduced motion را نشان می‌دهد. مسیر در navigation تولیدی ثبت نشده است.

## موارد منتظر

- assetهای نهایی نقشه، ساختمان‌ها، مکان‌ها، sealها و overlayها هنوز باید از worktree تولید بصری وارد manifest شوند؛
- runtime کامل Northern Road Shortage، readiness authoritative، commandهای Backend و پایان‌های واقعی در این تغییر عمداً پیاده نشده‌اند؛
- screenshotهای مرور بصری باید پس از اجرای dev server با Chrome تولید شوند.

## مرزهای حفظ‌شده

قوانین Backend، Proposal/Agreement API، SharedWorld، audio و story runtime تغییر نکرده‌اند.
