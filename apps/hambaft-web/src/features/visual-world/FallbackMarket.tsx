import type { SceneConnection, SceneDescriptor, SceneLocation } from './types'

function point(descriptor: SceneDescriptor, entityId: string) {
  return descriptor.locations.find((location) => location.entityId === entityId)
}

function Connection({ descriptor, connection }: { descriptor: SceneDescriptor; connection: SceneConnection }) {
  const from = point(descriptor, connection.fromEntityId)
  const to = point(descriptor, connection.toEntityId)
  if (!from || !to) return null
  return <line className={`market-connection market-connection--${connection.kind}${connection.damaged ? ' is-damaged' : ''}${connection.status ? ` is-${connection.status}` : ''}`} x1={from.x} y1={from.y} x2={to.x} y2={to.y} />
}

function Business({ location, autonomous }: { location: SceneLocation; autonomous: boolean }) {
  return <g className={`market-business market-business--${location.businessKind}${location.controlled ? ' is-controlled' : ''}${!location.active ? ' is-closed' : ''}`} transform={`translate(${location.x - 76} ${location.y - 46})`}>
    <path d="M5 28 L76 2 L147 28 V88 H5 Z" />
    <path className="market-awning" d="M0 29 H152 L140 50 H12 Z" />
    <rect x="59" y="54" width="34" height="34" />
    <text x="76" y="109" textAnchor="middle">{location.name}</text>
    {autonomous && <circle className="activity-pulse" cx="132" cy="18" r="8" />}
  </g>
}

export function FallbackMarket({ descriptor, reduced = false }: { descriptor: SceneDescriptor; reduced?: boolean }) {
  return <svg className={`fallback-market time-${descriptor.time}${reduced ? ' is-reduced' : ''}`} viewBox="0 0 1000 700" role="img" aria-label={`نقشه زنده بازار؛ ${descriptor.title}`}>
    <defs>
      <radialGradient id="courtyardGlow"><stop offset="0" stopColor="#efd486" stopOpacity=".45" /><stop offset="1" stopColor="#a67b2d" stopOpacity="0" /></radialGradient>
      <filter id="softGlow"><feGaussianBlur stdDeviation="7" /></filter>
    </defs>
    <rect className="market-sky" width="1000" height="700" />
    <path className="market-architecture" d="M35 80 H965 V650 H35 Z M370 240 Q500 125 630 240 V560 Q500 635 370 560 Z" fillRule="evenodd" />
    <path className="cargo-route" d="M1000 625 C890 620 880 555 805 532" />
    <g className="market-entrance"><path d="M900 650 V555 Q940 500 980 555 V650" /><text x="940" y="680" textAnchor="middle">دروازه و مسیر بار</text></g>
    <ellipse className="courtyard-glow" cx="500" cy="405" rx="190" ry="145" />
    <ellipse className="courtyard" cx="500" cy="405" rx="128" ry="90" />
    <g className="haj-office" transform="translate(430 95)"><path d="M0 40 L70 0 L140 40 V105 H0 Z" /><rect x="54" y="55" width="32" height="50" /><path d="M49 80 H91" /><text x="70" y="127" textAnchor="middle">دفتر بسته حاج صادق</text></g>
    {descriptor.connections.map((connection) => <Connection descriptor={descriptor} connection={connection} key={connection.id} />)}
    {descriptor.locations.map((location) => <Business location={location} autonomous={descriptor.uncontrolledEntityIds.includes(location.entityId)} key={location.entityId} />)}
    {!reduced && descriptor.messengers.map((messenger, index) => {
      const from = point(descriptor, messenger.fromEntityId); const to = point(descriptor, messenger.toEntityId)
      if (!from || !to) return null
      return <g className={`market-letter letter-${messenger.state}`} style={{ offsetPath: `path('M ${from.x} ${from.y} L ${to.x} ${to.y}')`, animationDelay: `${index * .2}s` }} key={messenger.id}><rect x="-13" y="-9" width="26" height="18" /><circle cx="0" cy="0" r="4" />{messenger.state === 'countered' && <circle cx="8" cy="0" r="3" />}</g>
    })}
    {descriptor.uncontrolledEntityIds.map((entityId, index) => { const at = point(descriptor, entityId); return at ? <g className="ambient-person" transform={`translate(${at.x + 55 + index * 4} ${at.y + 42})`} key={entityId}><circle cy="-12" r="5" /><path d="M0 -7 V10 M-8 0 H8 M0 10 L-7 22 M0 10 L7 22" /></g> : null })}
    {descriptor.avanVisible && <g className="avan-presence" transform="translate(882 380)"><path d="M0 80 Q15 10 42 0 Q69 10 84 80 Z" /><circle cx="42" cy="12" r="10" /><text x="42" y="102" textAnchor="middle">آوان</text></g>}
    {descriptor.events.includes('missing-bell') && <g className="silent-bell" transform="translate(500 208)"><path d="M-24 30 Q-20 -10 0 -20 Q20 -10 24 30 Z" /><path d="M-30 31 H30" /><circle cy="39" r="5" /><path className="bell-slash" d="M-38 -25 L38 50" /></g>}
    {descriptor.events.includes('cargo-shortage') && <g className="empty-crates" transform="translate(820 570)"><rect width="50" height="30" /><path d="M0 0 L50 30 M50 0 L0 30" /></g>}
    {descriptor.pressure > 0 && <rect className="pressure-vignette" width="1000" height="700" />}
    {descriptor.ending !== 'none' && <g className={`ending-veil ending-veil--${descriptor.ending}`}><rect width="1000" height="700" /><text x="500" y="360" textAnchor="middle">{descriptor.ending === 'world' ? 'پایان مشترک' : 'پایان این چراغ'}</text></g>}
  </svg>
}
