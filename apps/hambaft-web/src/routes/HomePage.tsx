import { Link } from 'react-router-dom'
import { motion } from 'motion/react'

export function HomePage() {
  return <main className="home-page"><motion.div className="home-mark" initial={{ scale: .9, opacity: 0 }} animate={{ scale: 1, opacity: 1 }}>✦</motion.div><p className="eyebrow">روایت جمعی در بازار هزارچراغ</p><h1>HAMBAFT</h1><p className="home-subtitle">هر حجره داستان خودش را می‌بیند؛ بازار نتیجه تصمیم‌های همه را به یاد می‌سپارد.</p><div className="home-actions"><Link className="button" to="/pair">ورود تیم</Link><Link className="button button--ghost" to="/admin">میز راهبر</Link></div></main>
}
