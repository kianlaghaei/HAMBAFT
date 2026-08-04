import { useState, type FormEvent } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { motion } from 'motion/react'
import { api } from '../../api/client'
import { isCredentialValid, useAuthStore } from '../../auth/authStore'

export function PairingPage() {
  const navigate = useNavigate()
  const team = useAuthStore((state) => state.team)
  const setToken = useAuthStore((state) => state.setToken)
  const [code, setCode] = useState('')
  const [pending, setPending] = useState(false)
  const [error, setError] = useState('')
  if (isCredentialValid(team)) return <Navigate to="/team" replace />

  async function submit(event: FormEvent) {
    event.preventDefault(); setError(''); setPending(true)
    try {
      const token = await api.pair(code.trim().toUpperCase())
      setToken(token)
      await api.teamExperience(token.accessToken)
      navigate('/team', { replace: true })
    } catch { setError('کد جفت‌شدن معتبر نیست یا زمان استفاده از آن گذشته است.') }
    finally { setPending(false) }
  }

  return <main className="pairing-page">
    <motion.section className="pairing-card" initial={{ opacity: 0, y: 16 }} animate={{ opacity: 1, y: 0 }}>
      <span className="lantern" aria-hidden="true">✦</span>
      <p className="eyebrow">HAMBAFT / هزارچراغ</p>
      <h1>درِ حجره را باز کنید</h1>
      <p>کدی را که راهبر بازی به شما داده وارد کنید. هویت کسب‌وکار از همین کد تعیین می‌شود.</p>
      <form onSubmit={submit}>
        <label className="field pairing-field"><span>کد جفت‌شدن</span><input autoFocus autoComplete="one-time-code" inputMode="text" dir="ltr" value={code} onChange={(event) => setCode(event.target.value)} placeholder="ABCD2345" aria-describedby={error ? 'pair-error' : undefined} /></label>
        {error && <p id="pair-error" className="form-error" role="alert">{error}</p>}
        <div className="button-row"><button className="button" disabled={pending || !code.trim()}>{pending ? 'در حال ورود…' : 'ورود به بازار'}</button><button className="button button--ghost" type="button" onClick={() => { setCode(''); setError('') }}>پاک‌کردن</button></div>
      </form>
    </motion.section>
  </main>
}
