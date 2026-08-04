import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { ApiError, api } from '../../api/client'
import { queryKeys } from '../../api/queryKeys'
import { useAuthStore } from '../../auth/authStore'
import { useUiStore } from '../../state/uiStore'
import { ErrorState, LoadingState } from '../../components/States'

export function AdminHomePage() {
  const navigate = useNavigate()
  const setToken = useAuthStore((state) => state.setToken)
  const saveCodes = useUiStore((state) => state.saveSetupCodes)
  const packages = useQuery({ queryKey: queryKeys.packages, queryFn: api.packages })
  const [selectedKey, setSelectedKey] = useState('hezar-cheragh@0.2.0')
  const [difficulty, setDifficulty] = useState('standard')
  const [teamCount, setTeamCount] = useState(2)
  const [names, setNames] = useState(['تیم سپیده', 'تیم راه‌نو', 'تیم روشن', 'تیم میزان'])
  const [businesses, setBusinesses] = useState(['bakery-sepideh', 'logistics-rah-no', 'printing-roshan', 'exchange-mizan'])
  const [existingId, setExistingId] = useState('')
  const [pending, setPending] = useState(false)
  const [error, setError] = useState<unknown>(null)

  if (packages.isLoading) return <LoadingState />
  if (packages.isError || !packages.data) return <ErrorState error={packages.error} retry={() => void packages.refetch()} />
  const metadata = packages.data.find((item) => `${item.id}@${item.version}` === selectedKey) ?? packages.data[0]

  async function create(event: FormEvent) {
    event.preventDefault(); if (!metadata) return; setPending(true); setError(null)
    try {
      const selectedBusinesses = businesses.slice(0, teamCount)
      if (new Set(selectedBusinesses).size !== teamCount) {
        throw new ApiError(400, 'Invalid setup', 'برای هر تیم یک حجرهٔ متفاوت انتخاب کنید.')
      }
      const packageData = await api.package(metadata.id, metadata.version)
      const created = await api.createSession({ storyPackageId: metadata.id, storyVersion: metadata.version, contentHash: metadata.contentHash, seed: Math.floor(Math.random() * 2_000_000_000), difficultyId: difficulty, expectedVersion: 0 })
      const sessionId = created.sessionId
      setToken(await api.devToken('admin', sessionId))
      let version = created.stateVersion
      const teams: Array<{ id: string; code: string; name: string }> = []
      for (let index = 0; index < teamCount; index += 1) {
        const team = await api.addTeam(sessionId, { displayName: names[index] || `تیم ${index + 1}`, expectedVersion: version })
        version = team.stateVersion
        if (!team.resourceId || !team.pairingCode) throw new Error('Pairing code was not returned once')
        teams.push({ id: team.resourceId, code: team.pairingCode, name: names[index] || `تیم ${index + 1}` })
      }
      const entityIds = new Map<string, string>()
      for (let index = 0; index < packageData.entities.length; index += 1) {
        const definition = packageData.entities[index]
        if (!definition) continue
        const teamIndex = businesses.slice(0, teamCount).indexOf(definition.id)
        const human = teamIndex >= 0
        const eligibleProfiles = packageData.behaviorProfiles.filter((profile) => profile.eligibleEntityDefinitionIds.includes(definition.id))
        const profile = eligibleProfiles[index % Math.max(1, eligibleProfiles.length)]
        const entity = await api.createEntity(sessionId, { definitionId: definition.id, displayName: definition.displayName, controllerType: human ? 'HumanTeam' : 'AuthoredBehavior', behaviorProfileId: human ? null : profile?.id, expectedVersion: version })
        version = entity.stateVersion
        if (entity.resourceId) entityIds.set(definition.id, entity.resourceId)
      }
      for (let index = 0; index < teams.length; index += 1) {
        const team = teams[index]; const entityId = entityIds.get(businesses[index] ?? '')
        if (!team || !entityId) throw new Error('Team assignment is incomplete')
        const assigned = await api.assignEntity(sessionId, { entityId, teamId: team.id, expectedVersion: version })
        version = assigned.stateVersion
      }
      saveCodes(sessionId, teams.map((team, index) => ({ teamId: team.id, teamName: team.name, businessName: packageData.entities.find((entity) => entity.id === businesses[index])?.displayName ?? '', code: team.code })))
      navigate(`/admin/session/${sessionId}/setup`)
    } catch (caught) { setError(caught) } finally { setPending(false) }
  }

  async function openExisting(event: FormEvent) {
    event.preventDefault(); setPending(true); setError(null)
    try { setToken(await api.devToken('admin', existingId.trim())); navigate(`/admin/session/${existingId.trim()}/runtime`) }
    catch (caught) { setError(caught) } finally { setPending(false) }
  }

  return <main className="admin-page"><header className="admin-hero"><p className="eyebrow">میز راهبر</p><h1>راه‌اندازی جلسه هزارچراغ</h1><p>ساخت جلسه، تخصیص حجره‌ها و ورود به کنترل اجرای زنده.</p></header>
    <div className="admin-columns"><section className="panel"><h2>جلسه تازه</h2><form onSubmit={create}>
      <label className="field"><span>بسته داستان</span><select value={selectedKey} onChange={(event) => setSelectedKey(event.target.value)}>{packages.data.filter((item) => item.isValid).map((item) => <option key={`${item.id}@${item.version}`} value={`${item.id}@${item.version}`}>{item.title} — {item.version}</option>)}</select></label>
      <div className="form-grid"><label className="field"><span>سختی</span><select value={difficulty} onChange={(event) => setDifficulty(event.target.value)}><option value="standard">استاندارد</option><option value="hard">دشوار</option></select></label><label className="field"><span>تعداد تیم</span><select value={teamCount} onChange={(event) => setTeamCount(Number(event.target.value))}>{[2, 3, 4].map((count) => <option key={count}>{count}</option>)}</select></label></div>
      <div className="team-setup-list">{Array.from({ length: teamCount }, (_, index) => <fieldset key={index}><legend>تیم {index + 1}</legend><label className="field"><span>نام نمایشی</span><input value={names[index]} onChange={(event) => setNames((current) => current.map((name, item) => item === index ? event.target.value : name))} required /></label><label className="field"><span>حجره</span><select value={businesses[index]} onChange={(event) => setBusinesses((current) => current.map((business, item) => item === index ? event.target.value : business))}>{['bakery-sepideh', 'logistics-rah-no', 'printing-roshan', 'exchange-mizan'].map((id) => <option key={id} value={id}>{({ 'bakery-sepideh': 'نانوایی سپیده', 'logistics-rah-no': 'باربری راه‌نو', 'printing-roshan': 'چاپخانه روشن', 'exchange-mizan': 'صرافی میزان' } as Record<string, string>)[id]}</option>)}</select></label></fieldset>)}</div>
      <button className="button" disabled={pending}>{pending ? 'در حال ساخت…' : 'ساخت و پیکربندی جلسه'}</button>
    </form></section>
    <section className="panel"><h2>ادامه جلسه موجود</h2><p className="form-help">در محیط Development، Backend برای همین Session یک توکن Admin کوتاه‌عمر صادر می‌کند.</p><form onSubmit={openExisting}><label className="field"><span>شناسه جلسه</span><input dir="ltr" value={existingId} onChange={(event) => setExistingId(event.target.value)} required /></label><button className="button button--ghost" disabled={pending}>بازکردن کنترل اجرا</button></form></section></div>
    {error !== null && <ErrorState error={error} />}
  </main>
}
