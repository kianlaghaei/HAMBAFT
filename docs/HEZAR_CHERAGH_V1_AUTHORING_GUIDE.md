# راهنمای نویسندگی Hezar Cheragh Story V1

این سند قرارداد فعلی را ثبت می‌کند تا Story V1 بعداً در همان pipeline نوشته و تحویل شود. متن فارسی داستانی در این سند یا در نسخه‌های `1.0.0` اضافه نشده است. نسخه‌های `0.4.0`، `0.6.0` و `0.7.0` فقط به‌عنوان نمونه فنی بررسی شده‌اند.

محل واقعی نوشتن داستان، [workspace نویسندگان Hezar Cheragh V1](../authoring/hezar-cheragh-v1/README.md) است. این سند runtime contract را توضیح می‌دهد؛ Markdownهای workspace منبع انسانی approved هستند و قبل از publish به JSON runtime منتقل می‌شوند.

## ۱. ساختار فعلی Story Package

ریشه هر نسخه:

```text
stories/<package-id>/<version>/
  manifest.json
  metrics.json
  entities.json
  storylets.json
  effects.json
  narrative.json
  interactions.json                 # اختیاری؛ Proposal/Agreement/Pact
  behaviors.json                    # اختیاری
  consequences.json                 # اختیاری
  difficulties.json                 # اختیاری
  ink.json                          # اختیاری
  endings/entity-endings.json       # اختیاری؛ پایان هر کسب‌وکار
  endings/world-endings.json        # اختیاری؛ پایان بازار
  presentation/
    locations.json
    scenes.json
    reactions.json
    choices.json
    metric-bands.json
    business-states.json
    relationship-states.json
    characters.json
    ambient-events.json
    team-scenes.json
  assets/
    ...
```

| نیاز نویسندگی | فایل و فیلد فعلی | مالک | مسیر رسیدن به UI تیم |
|---|---|---|---|
| نام/توضیح پکیج | `manifest.json` → `StoryPackageManifest`: `id`, `version`, `title`, `description`, `minimumTeams`, `maximumTeams`, `entryCheckpointId`, `defaultLocale`, `supportedLocales`, `estimatedDurationMinutes`, `requiredRuntimeVersion` | Package | توسط loader خوانده می‌شود؛ `description` در metadata/catalog و `GET /api/story-packages/{id}/{version}` قابل مشاهده است. |
| پنج Act/Checkpoint | `storylets.json` → `StoryletDefinition.CheckpointId`, `NextCheckpointId`; شروع از `manifest.entryCheckpointId` | Package + Backend authoritative transition | Backend در `AssignCheckpoint` assignmentها را می‌سازد و `currentCheckpointId` را در تجربه تیم می‌فرستد. مدل جداگانه‌ای به نام Act وجود ندارد؛ V1 باید پنج شناسه checkpoint پایدار داشته باشد. |
| روایت مشترک | `storylets.json` با `Scope: WorldPublic` یا `TeamPrivate` و `TargetSelector: World/AllTeams`; برای صحنه تصویری: `presentation/team-scenes.json` با `TargetSelector.Type: AllTeams` | Package | روایت public از world view؛ روایت TeamPrivate و صحنه تیمی از `/api/story/experience`. |
| صحنه اختصاصی تیم | `TeamScenePresentationDefinition`: `checkpointId`, `entityDefinitionId`, `targetSelector`, `priority` | Package؛ انتخاب نهایی Backend | `TeamSceneResolver.Resolve` با checkpoint و entity تحت کنترل تیم، صحنه برنده را انتخاب می‌کند. |
| نریشن آغاز فارسی | `TeamScenePresentationDefinition.OpeningNarrative: string[]`; نریشن Storylet نیز در `narrative.json` با کلید `narrativeRef` است | Package | `ViewProjector.TeamScene` آن را به `TeamScenePresentationView.openingNarrative` تبدیل می‌کند؛ `TeamSceneRenderer` در sheet آغاز نشان می‌دهد. |
| Hotspot و مختصات درصدی | `TeamSceneHotspotDefinition`: `id`, `label`, `x`, `y`, `storySheetId`, `shared`, `targetDescription`, `expectedVisibleObject`, `required`, `displayOrder` | Package؛ اعتبارسنجی مختصات Backend | Backend آن را به `TeamSceneHotspotView` می‌دهد؛ React با `left: x%`, `top: y%` روی تصویر می‌گذارد. |
| Story Sheet | `TeamSceneStorySheetDefinition`: `id`, `title`, `subtitle`, `narrative[]`, `evidence[]`, `actions[]` | Package | با کلیک hotspot، `TeamSceneRenderer` sheet متناظر را باز می‌کند. validator فعلی برای هر sheet سه تا پنج پاراگراف می‌خواهد. |
| Evidence | `InvestigationEvidencePresentationDefinition`: `id`, `title`, `sourceLabel`, `description`, `whyItMatters`, `certainty`, `unlocks`؛ در sheet یا `InvestigationLocations` | Package؛ ثبت investigation در Backend | تیم فقط evidence همان صحنه/Sheet را می‌بیند؛ ثبت با `POST /api/story/investigations` به memory خصوصی تیم تبدیل می‌شود. |
| تصمیم authoritative | `storylets.json` → `StoryletDefinition.Choices[]`: `id`, `labelRef`, `effectIds`, `nextStoryletHint`; effects در `effects.json` | Package تعریف می‌کند، Backend اجرا می‌کند | `SubmitStoryChoice` فقط `choiceId` را می‌پذیرد؛ `ResolveNarrativeCheckpoint` effectها را اجرا می‌کند. |
| Decision Pack نمایشی | `FinalDecisionPresentationDefinition`: `heading`, `summary`, `choices[]` و `DecisionChoicePresentationDefinition`: `choiceId`, `selectedAction`, `acceptedRisk`, `position`, `pactUsed`, `summary` | Package؛ نتیجه واقعی Backend | `ViewProjector.TeamScene` انتخاب ثبت‌شده را به UI می‌دهد و `DecisionSummary` نمایش می‌دهد. این مدل فعلاً سؤال چندمرحله‌ای و پاسخ‌های مستقل ندارد. |
| همکاری/Pact مشروط | `interactions.json` → `InteractionTypeDefinition`; در صحنه `ContextualPacts[]`; شرایط در `ConditionDefinition` و آثار در `EffectDefinition` | Package + Backend rules | Backend فقط Pactهایی را می‌فرستد که target تیم، `availableInteractions` و selectorهای مجاز آن را تأیید کنند؛ UI متن و فرم terms را نشان می‌دهد. |
| سؤال‌های فرم Pact | `InteractionTermSchemaDefinition`: `Type`, `Name`, `Required`, `Minimum`, `Maximum`, `MaximumLength`, `Fields`؛ نوع `Compound` برای چند فیلد | Package + Backend validation | به `termsSchema` در تجربه تیم می‌رسد و `TermsView` فرم را render می‌کند؛ پاسخ با Proposal authoritative است. |
| واکنش فوری/بازار | واکنش جهانی: `presentation/reactions.json` → `WorldReactionPresentationDefinition`; واکنش Team: `TeamScenePresentationDefinition.MarketReactions[]` keyed by `choiceId` | Package؛ state/effects Backend | واکنش Team فقط پس از choice انتخاب‌شده در Team view می‌آید؛ واکنش public از world presentation می‌آید. |
| hook Act بعدی | `StoryletDefinition.NextCheckpointId` و `TeamSceneNextPresentationDefinition`: `title`, `subtitle`, `narrative[]`, `actions[]` | Package + Backend transition | دکمه `OpenNextScene`/`Continue` در renderer فقط hook را باز می‌کند؛ حرکت واقعی checkpoint با Admin resolve انجام می‌شود. |
| تصویر صحنه | `TeamScenePresentationDefinition.BackgroundAssetId` | Package؛ resolution و authorization Backend | `/api/story/scene-asset` فقط تصویر `BackgroundAssetId` صحنه فعلی تیم را برمی‌گرداند؛ React آن را به Blob/object URL تبدیل می‌کند. |
| پایان هر کسب‌وکار | `endings/entity-endings.json` → `EntityEndingDefinition`: `id`, `eligibleEntityDefinitions`, `titleNarrativeRef`, `bodyNarrativeRef`, `conditions`, `priority`, `weight`, `evidenceSelectors`, `publicSummaryNarrativeRef`, `presentationTags` | Package conditions؛ Backend `DeterministicEndingResolver` | پس از Admin `ResolveEndings`، فقط ending همان entity در Team experience می‌آید؛ متن از `narrative.json` و locale انتخاب می‌شود. |
| پایان بازار | `endings/world-endings.json` → `WorldEndingDefinition` با همان الگو، بدون entity eligibility | Package conditions؛ Backend | پس از resolve شدن endingهای entity، world ending به Team و Public Display مناسب projection می‌رسد. |
| localization | `narrative.json` → `{ "fa-IR": { "key": NarrativeDefinition } }`; `defaultLocale` و `supportedLocales` در manifest | Package؛ انتخاب locale در Backend projection | `ViewProjector.Localized` locale درخواستی را فقط اگر موجود باشد انتخاب می‌کند، وگرنه `defaultLocale` را. |
| privacy و targeting | `StoryletScope`, `TargetSelectorDefinition`, `TeamScene.TargetSelector`, `Eligibility`; در Backend: `ViewProjector.Team`, `TeamSceneResolver`, `Phase3` | Backend authoritative | `/api/story/experience` با claimهای `session_id` و `team_id` فقط Team خود را hydrate می‌کند. Team scene در PublicWorldView وجود ندارد و private Team data به Public Display کپی نمی‌شود. |

## ۲. مسیر کامل داده تا `/team`

1. `FileSystemStoryPackageLoader` در `src/Hambaft.Infrastructure/StoryPackages/FileSystemStoryPackages.cs` مسیر `stories/<id>/<version>` را پیدا می‌کند، JSONهای required/optional و presentation را می‌خواند و برای package یک `ContentHash` می‌سازد.
2. `StoryPackageValidator` همان فایل، manifest، referenceهای narrative/effect/choice، checkpointها، targetها، story sheetها، evidence، hotspotهای ۰ تا ۱۰۰، image extension و Pact/reaction/ending referenceها را بررسی می‌کند. وجود فیزیکی `BackgroundAssetId` نیز در loader بررسی می‌شود. خطا با `InvalidStoryPackageException` برمی‌گردد.
3. هنگام initialize، `SessionRuntime` package را با `StoryPackageId`, `StoryVersion`, `ContentHash` قفل می‌کند، entry checkpoint را initialize و assignmentهای آن checkpoint را ایجاد می‌کند. در هر resolve، Backend همه responseهای لازم را بررسی می‌کند، `NextCheckpointId` را باید به یک مقصد قطعی برساند، سپس checkpoint بعدی را assign می‌کند.
4. `GET /api/story/experience` در `src/Hambaft.Api/Program.cs` از claimهای Team استفاده می‌کند و `SessionRuntime.GetTeamAsync` را صدا می‌زند.
5. `ViewProjector.Team` در `src/Hambaft.Application/Views.cs` فقط metrics/memories/relationships و storyletهای مجاز Team را نگه می‌دارد. `TeamScene`، `TeamSceneResolver.Resolve` را با checkpoint فعلی، Team، entity تحت کنترل، choiceهای همان Team و memoryهای قابل مشاهده صدا می‌زند.
6. نتیجه به `TeamScenePresentationView` تبدیل می‌شود: `openingNarrative`, `hotspots`, `storySheets`, `evidence`, `contextualPacts`, `finalDecisionPresentation`, `marketReactions`, `authoredActions`, `nextScenePresentation` و `backgroundAssetId`.
7. `apps/hambaft-web/src/features/hezar-cheragh/TeamGameplayScreen.tsx` تجربه را می‌گیرد، تصویر را با token Team از `api.teamSceneAsset` می‌خواند و Blob را به object URL تبدیل می‌کند.
8. `apps/hambaft-web/src/features/team-scene/TeamSceneRenderer.tsx` تصویر را render می‌کند، hotspotها را روی آن می‌گذارد و sheet/next hook را باز می‌کند. commands همچنان به Backend می‌روند؛ React rule بازی را اجرا نمی‌کند.

Admin transition endpoint: `POST /api/sessions/{sessionId}/narrative/resolve` در `Program.cs` فقط با policy `Admin` در دسترس است. این endpoint انتخاب checkpoint دلخواه نمی‌گیرد؛ تنها checkpoint قطعی تعریف‌شده در `NextCheckpointId` را پس از تکمیل responseهای لازم resolve می‌کند.

## ۳. pipeline تصویر

- محل ذخیره: داخل همان package و version؛ پیشنهاد نویسندگی برای V1: `stories/hezar-cheragh/<version>/assets/team-scenes/act-<nn>/<scene-id>.<ext>`. این convention جدید اجباری نیست؛ `BackgroundAssetId` باید دقیقاً همین path نسبی package را حمل کند.
- فرمت‌های فعلی: `.svg`, `.webp`, `.png`, `.jpg`, `.jpeg`. هم validator و هم `FileSystemStoryPackageAssetReader` همین مجموعه را قبول می‌کنند.
- resolution: `BackgroundAssetId` از `presentation/team-scenes.json` به package-relative path تبدیل می‌شود. loader وجود فایل را چک می‌کند؛ asset reader path traversal، path مطلق و خروج از directory package را رد می‌کند.
- authorization: route `GET /api/story/scene-asset` policy `Team` دارد، `session_id` و `team_id` token را با تجربه تیم تطبیق می‌دهد و asset را از package/version قفل‌شده و صحنه فعلی همان Team می‌خواند. کلاینت assetId دلخواه ارسال نمی‌کند.
- نام‌گذاری: validator نام‌گذاری را enforce نمی‌کند. نام پایدار، لاتین، lowercase و بدون فاصله استفاده شود؛ نام باید scene/act را توصیف کند. برای replacement در draft، فایل را در همان draft version جایگزین و hash جدید package را بگیرید؛ نسخه منتشرشده را overwrite نکنید و برای آن version جدید بسازید. مسیر React تغییر نمی‌کند.
- مختصات: `x` و `y` درصد از ۰ تا ۱۰۰ هستند، نسبت به box نهایی `TeamSceneRenderer`. CSS تصویر `object-fit: cover` دارد و hotspot با `translate(-50%, -50%)` anchor می‌شود؛ مختصات را روی همان crop/نسبت تصویر نهایی QA کنید.
- پایان‌ها: قالب فعلی `EntityEndingDefinition` و `WorldEndingDefinition` هیچ `imageAssetId` ندارد؛ `EndingPresentationView`، endpoint asset و `EndingView` نیز تصویر پایان را پشتیبانی نمی‌کنند. بنابراین فعلاً فقط تصویر صحنه قابل تحویل است؛ برای ending image باید بعداً یک contract/route/renderer کوچک و صریح اضافه شود، نه hardcode در React.

## ۴. قالب خالی authoring برای Story V1

این قالب جای متن داستان نیست؛ فقط محل الصاق داده‌های بعدی را مشخص می‌کند.

### Manifest و پنج Act

```text
stories/hezar-cheragh/<draft-version>/manifest.json
  id: "hezar-cheragh"
  version: "<draft-version>"
  title: "<PASTE PACKAGE TITLE>"
  description: "<PASTE PACKAGE DESCRIPTION>"
  entryCheckpointId: "<ACT-01-CHECKPOINT-ID>"
  defaultLocale: "fa-IR"
  supportedLocales: ["fa-IR"]

storylets.json
  Act 1: <ACT-01-CHECKPOINT-ID> -> <ACT-02-CHECKPOINT-ID>
  Act 2: <ACT-02-CHECKPOINT-ID> -> <ACT-03-CHECKPOINT-ID>
  Act 3: <ACT-03-CHECKPOINT-ID> -> <ACT-04-CHECKPOINT-ID>
  Act 4: <ACT-04-CHECKPOINT-ID> -> <ACT-05-CHECKPOINT-ID>
  Act 5: <ACT-05-CHECKPOINT-ID> -> <FINAL-CHECKPOINT-ID or ending transition>
```

### Act 1 صحنه‌ها و محتوای مورد نیاز

الگوی پایه را مثل `stories/hezar-cheragh/0.4.0/presentation/team-scenes.json` نگه دارید: چهار رکورد Team-specific، هر رکورد با `entityDefinitionId` و محتوای مستقل همان کسب‌وکار. برای نیاز V1، یک رکورد `AllTeams` برای context مشترک پیش از آن‌ها اضافه می‌شود و asset/hotspot/sheet/next-hook از قرارداد فعلی اضافه می‌گردد. متن `0.4.0` کپی نمی‌شود.

فایل اصلی پیشنهادی: `presentation/team-scenes.json`.

```text
Act 1 shared context
  sceneId: <ACT-01-SHARED-SCENE-ID>
  checkpointId: <ACT-01-CHECKPOINT-ID>
  targetSelector: { type: AllTeams }
  backgroundAssetId: <PASTE SHARED IMAGE REFERENCE>
  OpeningNarrative: [<PASTE BLOCK 1>, <PASTE BLOCK 2>, ...]
  Hotspots: [<PASTE 2-4 AUTHORED HOTSPOTS>]
  StorySheets: [<PASTE SHEETS, 3-5 NARRATIVE PARAGRAPHS EACH>]
  Evidence: <PASTE EVIDENCE INSIDE EACH SHEET>
  Decision Pack: <PASTE DISPLAY SUMMARY; authoritative choices live in storylets.json>
  Reactions: <PASTE SHARED/WORLD REACTION REFERENCES>
  NextScenePresentation: <PASTE NEXT-ACT HOOK>

Bakery scene
  entityDefinitionId: <BAKERY ENTITY ID>
  BackgroundAssetId: <PASTE BAKERY IMAGE REFERENCE>
  Hotspots / StorySheets / Evidence / Decision Pack / Reactions / NextScenePresentation: <PASTE>

Logistics scene
  entityDefinitionId: <LOGISTICS ENTITY ID>
  BackgroundAssetId: <PASTE LOGISTICS IMAGE REFERENCE>
  Hotspots / StorySheets / Evidence / Decision Pack / Reactions / NextScenePresentation: <PASTE>

Printing scene
  entityDefinitionId: <PRINTING ENTITY ID>
  BackgroundAssetId: <PASTE PRINTING IMAGE REFERENCE>
  Hotspots / StorySheets / Evidence / Decision Pack / Reactions / NextScenePresentation: <PASTE>

Exchange scene
  entityDefinitionId: <EXCHANGE ENTITY ID>
  backgroundAssetId: <PASTE EXCHANGE IMAGE REFERENCE>
  Hotspots / StorySheets / Evidence / Decision Pack / Reactions / NextScenePresentation: <PASTE>
```

### بخش asset در هر نسخه

این بخش باید همراه `team-scenes.json` تحویل شود؛ مسیر React نوشته نمی‌شود:

```text
stories/hezar-cheragh/<draft-version>/
  assets/
    team-scenes/
      act-01/
        shared-context.webp       # backgroundAssetId: assets/team-scenes/act-01/shared-context.webp
        bakery.webp               # backgroundAssetId: assets/team-scenes/act-01/bakery.webp
        logistics.webp            # backgroundAssetId: assets/team-scenes/act-01/logistics.webp
        printing.webp             # backgroundAssetId: assets/team-scenes/act-01/printing.webp
        exchange.webp             # backgroundAssetId: assets/team-scenes/act-01/exchange.webp
      act-02/
      act-03/
      act-04/
      act-05/
```

برای هر تصویر، این سه مورد باید با هم بررسی شوند:

1. فایل واقعاً داخل همان draft package/version وجود داشته باشد.
2. `backgroundAssetId` دقیقاً path نسبی بالا باشد و extension مجاز داشته باشد.
3. مختصات `hotspots[].x/y` روی نسخه نهایی همان تصویر QA شود؛ جایگزینی تصویر بدون تغییر path، React را تغییر نمی‌دهد اما ممکن است مختصات را نیازمند بازبینی کند.

تصویر ثابت Act همان `backgroundAssetId` صحنه resolved است. اگر در یک Act برای shared context و چهار business scene پنج تصویر لازم باشد، پنج scene record با پنج asset reference نوشته می‌شود؛ در هر لحظه فقط asset صحنه مجاز همان Team تحویل می‌شود.

برای هر hotspot این فیلدها اجباری‌اند: `id`, `label`, `x`, `y`, `storySheetId`, `targetDescription`, `expectedVisibleObject`, `required`, `displayOrder`. برای هر sheet فعلاً `narrative` را در بازه ۳ تا ۵ پاراگراف نگه دارید و حداقل یک evidence و action قابل ثبت داشته باشید.

### تصمیم، Pact و پایان‌ها

```text
storylets.json
  <ACT-01-TEAM-STORYLET>
    choices: [<PASTE AUTHORITATIVE CHOICES>]
    effects: [<PASTE EFFECT IDS>]
    nextCheckpointId: <ACT-02-CHECKPOINT-ID>

interactions.json
  <PASTE CONDITIONAL COOPERATION / PACT DEFINITIONS>
  <PASTE COMPOUND TERMS / QUESTIONS FOR THE PACT FORM>

endings/entity-endings.json
  <BUSINESS-A>:
    ending-1: <PASTE DEFINITION AND NARRATIVE REFS>
    ending-2: <PASTE DEFINITION AND NARRATIVE REFS>
  <BUSINESS-B>: <TWO ENDINGS>
  <BUSINESS-C>: <TWO ENDINGS>
  <BUSINESS-D>: <TWO ENDINGS>

endings/world-endings.json
  market-ending-1: <PASTE DEFINITION AND NARRATIVE REFS>
  market-ending-2: <PASTE DEFINITION AND NARRATIVE REFS>
```

## ۵. بررسی سازگاری V1

| نیاز | وضعیت فعلی |
|---|---|
| پنج Act | پشتیبانی می‌شود با پنج زنجیره `CheckpointId`/`NextCheckpointId`; شمارنده یا مدل Act جدا وجود ندارد. |
| تا چهار تصویر Team-specific در هر Act | پشتیبانی می‌شود با sceneهای targetشده برای entityها؛ هر scene یک `BackgroundAssetId` دارد. |
| یک تصویر ثابت در هر Act | پشتیبانی می‌شود برای هر scene resolved؛ تصویر در طول scene عوض نمی‌شود. |
| ۲ تا ۴ hotspot | پشتیبانی می‌شود؛ validator بازه مختصات و reference را می‌سنجد، اما count دقیق V1 را enforce نمی‌کند. |
| نریشن چندبخشی خوانا | پشتیبانی می‌شود؛ sheetها ۳ تا ۵ paragraph و opening/next آرایه‌ای هستند. |
| evidence | پشتیبانی می‌شود. |
| Decision Pack چندسؤالی | **پشتیبانی کامل نمی‌شود**؛ `FinalDecisionPresentation` فقط summary/choice metadata است و Backend یک `choiceId` واحد ثبت می‌کند. چند سؤال Pact از `Compound` terms پشتیبانی می‌شود، اما این معادل Decision Pack چندسؤالی نیست. |
| انتقال Act تحت کنترل Admin | پشتیبانی می‌شود با `/narrative/resolve` و next checkpoint قطعی؛ انتخاب مقصد دلخواه Admin وجود ندارد. |
| دو پایان بازار | پشتیبانی می‌شود با حداقل دو `WorldEndingDefinition` و یک fallback بدون شرط. |
| دو پایان برای هر کسب‌وکار | پشتیبانی می‌شود؛ برای هر entity دو `EntityEndingDefinition` با condition/priority تعریف می‌شود و در صورت وجود endings، validator fallback هر entity قابل بازی را می‌خواهد. |

نتیجه: pipeline برای authoring ساختاری Story V1 آماده است، به‌جز قرارداد Decision Pack چندسؤالی و ending image. این دو را نباید با متن یا مسیر hardcoded در React شبیه‌سازی کرد. قبل از نهایی‌کردن V1 باید semantics پاسخ‌های چندسؤالی (و در صورت نیاز تصویر پایان) به‌صورت صریح تصویب و سپس کوچک‌ترین contract لازم اضافه شود.

## ۶. محدودیت‌ها و تغییرات این آماده‌سازی

- نسخه immutable `1.0.0` ساخته یا publish نشده است؛ این repository draft/publish workflow صریحی برای آن نشان نمی‌دهد.
- هیچ متن جدید داستانی اضافه نشده و محتوای `0.6.0`/`0.7.0` منبع V1 نیست.
- فقط `DeterministicStoryPackageHasher` اصلاح شد تا فایل‌های غیر JSON، به‌خصوص تصویر، با `File.ReadAllBytesAsync` hash شوند. این تغییر مسیر asset یا contract UI را عوض نمی‌کند.
- هیچ تغییر frontend، Backend gameplay rule، Story Package موجود یا database انجام نشده است.
