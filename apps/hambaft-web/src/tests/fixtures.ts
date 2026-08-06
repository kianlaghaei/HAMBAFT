import type { PublicWorld, TeamExperience } from '../api/schemas'

export const ids = {
  session: '10000000-0000-4000-8000-000000000001',
  team: '20000000-0000-4000-8000-000000000001',
  otherTeam: '20000000-0000-4000-8000-000000000002',
  entity: '30000000-0000-4000-8000-000000000001',
  otherEntity: '30000000-0000-4000-8000-000000000002',
  assignment: '40000000-0000-4000-8000-000000000001',
  proposal: '50000000-0000-4000-8000-000000000001',
  agreement: '60000000-0000-4000-8000-000000000001',
  ending: '70000000-0000-4000-8000-000000000001',
}

const entity = { id: ids.entity, sessionId: ids.session, definitionId: 'bakery-sepideh', displayName: 'نانوایی سپیده', controllerType: 'HumanTeam', controlledByTeamId: ids.team, behaviorProfileId: null, status: 'Active' }
const team = { id: ids.team, sessionId: ids.session, displayName: 'تیم سپیده', controlledEntityId: ids.entity, joinedAtUtc: '2026-08-04T08:00:00Z' }
const ending = { endingResultId: ids.ending, scope: 'Entity', scopeId: ids.entity, endingDefinitionId: 'steady-oven', title: 'چراغ تنور ماند', paragraphs: ['سپیده راهی ساخت که بازار آن را به یاد سپرد.'], presentationTags: { mood: 'warm' }, evidence: [{ kind: 'Choice', referenceId: ids.assignment, stableId: 'stable-price', key: null, value: null, isPublic: false }], contentHash: 'sha256:test', resolvedAtStreamVersion: 22 }

export const teamExperienceFixture: TeamExperience = {
  id: ids.team, sessionId: ids.session, team, controlledEntity: entity,
  visibleMetrics: [{ scope: 'Entity', scopeId: ids.entity, metricKey: 'Liquidity', numericValue: 42 }],
  visibleMemories: [], visibleRelationships: [], stateVersion: 12, currentCheckpointId: 'morning-without-bell',
  privateStorylets: [{ assignmentId: ids.assignment, storyletId: 'bakery-opening', checkpointId: 'morning-without-bell', title: 'دفتر نیمه‌باز', paragraphs: ['این روایت خصوصی سپیده است.'], presentationTags: { speaker: 'شاگرد نانوا' }, choices: [{ id: 'keep-price', label: 'قیمت را نگه دار', shortOutcome: null }], requiredResponse: true, submitted: false, submittedChoiceId: null }],
  inbox: [], outbox: [], agreements: [], availableInteractionTypes: [{ interactionTypeId: 'emergency-supply', allowedTargetTeamIds: [ids.otherTeam] }], pendingConsequences: [], entityEnding: null, worldEnding: null,
  teamScenePresentation: {
    sceneId: 'test-hidden-stall', title: 'خبر پنهانِ حجره', openingNarrative: ['روایت افتتاحیه‌ی آزمون یک.', 'روایت افتتاحیه‌ی آزمون دو.', 'روایت افتتاحیه‌ی آزمون سه.'], objective: 'دو مکان را بررسی کنید و بر اساس شواهد تصمیم بگیرید.', requiredInvestigationCount: 2,
    investigationLocations: [
      { id: 'haj-sadegh-office', title: 'دفتر حاج صادق', identity: 'دفتر بسته‌ی بازار', narrative: 'روایت authored دفتر.', evidence: [
        { id: 'office-erased-loads', title: 'سه ردیف پاک‌شده', sourceLabel: 'تکه دفتر آورده‌شده توسط رحیم', description: 'شماره سه محموله از دفتر خط خورده است.', whyItMatters: 'خروج بارها ثبت شده است.', certainty: 'confirmed', unlocks: ['مقایسه شماره بارها'] },
        { id: 'office-wrong-seal', title: 'مهر با تاریخ ناخوانا', sourceLabel: 'پاکت روی میز', description: 'تاریخ مهر پاک شده است.', whyItMatters: 'اعتبار مجوز روشن نیست.', certainty: 'uncertain', unlocks: [] },
        { id: 'office-noon-note', title: 'یادداشت تا ظهر', sourceLabel: 'حاشیه دفتر', description: 'تصمیم باید تا ظهر گرفته شود.', whyItMatters: 'تعویق هزینه دارد.', certainty: 'probable', unlocks: [] },
      ] },
      { id: 'market-entrance', title: 'دروازه بازار', identity: 'راه ورود بار و خبر', narrative: 'روایت authored دروازه.', evidence: [
        { id: 'gate-route-permit', title: 'اجازه خروج از راه فرعی', sourceLabel: 'دفتر ورود دروازه', description: 'سه گاری با اجازه موقت منحرف شدند.', whyItMatters: 'بارها پیش از تحویل تغییر مسیر داده‌اند.', certainty: 'confirmed', unlocks: [] },
        { id: 'gate-wheel-tracks', title: 'حرف نگهبان و رد چرخ‌ها', sourceLabel: 'گفته نگهبان', description: 'رد چرخ تازه به راه فرعی می‌رود.', whyItMatters: 'بخشی از واقعیت پنهان است.', certainty: 'uncertain', unlocks: [] },
        { id: 'gate-open-window', title: 'مسیر فرعی هنوز باز است', sourceLabel: 'برنامه دروازه', description: 'راه فرعی تا پیش از ظهر باز می‌ماند.', whyItMatters: 'فرصت پیگیری محدود است.', certainty: 'probable', unlocks: [] },
      ] },
    ],
    businessActivity: { entityDefinitionId: 'bakery-sepideh', title: 'تخصیص سهم نانوایی', summary: 'فعالیت authored نانوایی.', implication: 'اثر authored روی حجره.', unlockedActions: ['اقدام authored'] }, contextualPacts: [],
    finalDecisionPresentation: { heading: 'آنچه حالا می‌دانید', summary: 'خلاصه‌ی authored تصمیم.', choices: [{ choiceId: 'keep-price', selectedAction: 'قیمت را نگه دار', acceptedRisk: 'ریسک authored', position: 'خصوصی', pactUsed: 'بدون پیمان', summary: 'تصمیم authored' }], selectedChoiceId: null, selectedChoice: null }, marketReactions: [], investigatedLocationIds: [], revealedEvidenceIds: [],
    subtitle: 'روایت آزمون', backgroundAssetId: 'assets/test.svg', backgroundAssetUrl: '/api/story/scene-asset',
    hotspots: [{ id: 'office-marker', label: 'دفتر', x: 50, y: 30, storySheetId: 'office-sheet', shared: false, investigated: false }],
    storySheets: [{ id: 'office-sheet', title: 'دفتر', subtitle: 'روایت مکان', narrative: ['بند یک', 'بند دو', 'بند سه'], evidence: [{ id: 'office-proof', title: 'مدرک', sourceLabel: 'دفتر', description: 'شرح مدرک', whyItMatters: 'اهمیت مدرک', certainty: 'confirmed', unlocks: [] }], actions: [{ id: 'record-office', label: 'ثبت بررسی', kind: 'RecordInvestigation', targetId: 'office-sheet', available: true }] }],
    authoredActions: [{ id: 'choose-location', label: 'انتخاب مکان', kind: 'ShowMap', targetId: null, available: true }], nextScenePresentation: null,
  },
  sessionMetadata: { sessionId: ids.session, status: 'Running', storyPackageId: 'hezar-cheragh', storyVersion: '0.1.0', contentHash: 'sha256:test', difficultyId: 'standard', currentCheckpointId: 'morning-without-bell' },
}

export const completedExperience: TeamExperience = { ...teamExperienceFixture, privateStorylets: [], entityEnding: ending, worldEnding: { ...ending, endingResultId: '70000000-0000-4000-8000-000000000002', scope: 'World', scopeId: ids.session, title: 'بازار چراغش را نگه داشت', evidence: [] }, sessionMetadata: { ...teamExperienceFixture.sessionMetadata!, status: 'Completed' } }

export const publicWorldFixture: PublicWorld = {
  id: ids.session, status: 'Running', entities: [entity, { ...entity, id: ids.otherEntity, definitionId: 'logistics-rah-no', displayName: 'باربری راه‌نو', controlledByTeamId: ids.otherTeam }], worldMetrics: [], publicMemories: [], publicRelationships: [], stateVersion: 12,
  storyPackageId: 'hezar-cheragh', storyVersion: '0.1.0', contentHash: 'sha256:test', currentCheckpointId: 'morning-without-bell', narrative: { storyletId: 'world-opening', narrativeRef: 'world-opening', title: 'بازار بی‌زنگ', paragraphs: ['امروز زنگ بازار به صدا درنیامد.'], presentationTags: {} }, publicAgreements: [], publicConsequences: [], worldEnding: null, publicEntityEndingSummaries: [], difficultyId: 'standard',
  worldPresentation: {
    sceneId: 'test-market', timeOfDay: 'morning', timeLabel: 'صبح', atmosphereLabel: 'بازار', publicEvent: '', courtyardActivity: '', soundscape: 'market', avanVisible: false, visualTags: [], pulse: [], semanticMetrics: [],
    locations: [
      { id: 'haj-sadegh-office', displayName: 'دفتر حاج صادق', shortIdentity: 'دفتر بازار', whyItMatters: '', currentCondition: 'بسته', whoIsHere: '', recentChange: '', availableActions: [], presentationTags: ['team-map'], x: 50, y: 15, entityId: null, entityDefinitionId: null, businessIdentity: null },
      { id: 'bakery-sepideh', displayName: 'نانوایی سپیده', shortIdentity: 'نانوایی', whyItMatters: '', currentCondition: 'فعال', whoIsHere: '', recentChange: '', availableActions: [], presentationTags: ['team-map', 'bakery'], x: 27, y: 28, entityId: ids.entity, entityDefinitionId: 'bakery-sepideh', businessIdentity: 'نان' },
      { id: 'logistics-rah-no', displayName: 'باربری راه‌نو', shortIdentity: 'باربری', whyItMatters: '', currentCondition: 'فعال', whoIsHere: '', recentChange: '', availableActions: [], presentationTags: ['team-map', 'logistics'], x: 25, y: 57, entityId: ids.otherEntity, entityDefinitionId: 'logistics-rah-no', businessIdentity: 'بار' },
      { id: 'printing-roshan', displayName: 'چاپخانه روشن', shortIdentity: 'چاپخانه', whyItMatters: '', currentCondition: 'فعال', whoIsHere: '', recentChange: '', availableActions: [], presentationTags: ['team-map', 'printing'], x: 74, y: 29, entityId: null, entityDefinitionId: 'printing-roshan', businessIdentity: 'چاپ' },
      { id: 'exchange-mizan', displayName: 'صرافی میزان', shortIdentity: 'صرافی', whyItMatters: '', currentCondition: 'فعال', whoIsHere: '', recentChange: '', availableActions: [], presentationTags: ['team-map', 'exchange'], x: 75, y: 57, entityId: null, entityDefinitionId: 'exchange-mizan', businessIdentity: 'مهر' },
      { id: 'market-entrance', displayName: 'دروازه بازار', shortIdentity: 'ورودی', whyItMatters: '', currentCondition: 'فعال', whoIsHere: '', recentChange: '', availableActions: [], presentationTags: ['team-map'], x: 50, y: 78, entityId: null, entityDefinitionId: null, businessIdentity: null },
    ],
    businesses: [], characters: [], ambientEvents: [], reactions: [],
  },
}

export function jwt(role: 'Team' | 'Admin' | 'PublicDisplay', teamId?: string) {
  const encode = (value: object) => btoa(JSON.stringify(value)).replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_')
  return `${encode({ alg: 'none' })}.${encode({ client_role: role, session_id: ids.session, team_id: teamId, exp: 4102444800 })}.signature`
}
