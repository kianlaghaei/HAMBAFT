import { useEffect, useRef } from 'react'
import type { SceneDescriptor, SceneLocation } from './types'

const colors = { ink: 0x26271f, wall: 0x5b4a36, roof: 0x8f653d, paper: 0xf4e3b8, gold: 0xd7b968, night: 0x18201f, sky: 0xbda56e, red: 0x8c3f32, green: 0x53664a }

export function PixiMarketCanvas({ descriptor, onFailure }: { descriptor: SceneDescriptor; onFailure: () => void }) {
  const host = useRef<HTMLDivElement>(null)
  useEffect(() => {
    let disposed = false
    let cleanup = () => undefined
    void import('pixi.js').then(async ({ Application, Container, Graphics, Text }) => {
      if (!host.current || disposed) return
      const app = new Application()
      await app.init({ resizeTo: host.current, antialias: true, autoDensity: true, resolution: Math.min(window.devicePixelRatio || 1, 2), backgroundAlpha: 0 })
      if (!host.current || disposed) { app.destroy(true, { children: true }); return }
      app.canvas.setAttribute('aria-hidden', 'true')
      host.current.appendChild(app.canvas)
      const layer = (label: string) => { const value = new Container({ label }); app.stage.addChild(value); return value }
      const background = layer('background architecture')
      const lighting = layer('lighting and time')
      const businesses = layer('business locations')
      const population = layer('ambient population')
      const movement = layer('messengers and movement')
      const relationships = layer('relationships and agreements')
      const effects = layer('event effects')
      const atmosphere = layer('foreground atmosphere')
      const camera = layer('camera and transitions')
      const architecture = new Graphics().rect(20, 30, 960, 640).fill({ color: colors.wall }).roundRect(300, 185, 400, 390, 150).cut().fill()
      background.addChild(architecture)
      const courtyard = new Graphics().ellipse(500, 405, 132, 92).fill({ color: colors.paper, alpha: .48 }).stroke({ color: colors.gold, width: 3, alpha: .6 })
      background.addChild(courtyard)
      const office = new Graphics().poly([430, 180, 500, 100, 570, 180, 570, 235, 430, 235]).fill(colors.roof).rect(484, 184, 32, 51).fill(colors.ink)
      background.addChild(office)
      const entrance = new Graphics().moveTo(900, 650).lineTo(900, 560).quadraticCurveTo(940, 500, 980, 560).lineTo(980, 650).stroke({ color: colors.gold, width: 9 })
      background.addChild(entrance)
      const route = new Graphics().moveTo(1000, 625).bezierCurveTo(900, 620, 870, 565, 800, 525).stroke({ color: colors.paper, width: 8, alpha: .35 })
      background.addChild(route)
      const timeColor = { morning: 0xf2c878, noon: 0xffe3a0, dusk: 0xb66c53, night: colors.night }[descriptor.time]
      lighting.addChild(new Graphics().rect(0, 0, 1000, 700).fill({ color: timeColor, alpha: descriptor.time === 'night' ? .45 : .16 }))
      const scaleX = () => app.screen.width / 1000
      const scaleY = () => app.screen.height / 700
      const resize = () => { app.stage.scale.set(scaleX(), scaleY()) }
      resize(); app.renderer.on('resize', resize)
      const drawBusiness = (location: SceneLocation) => {
        const shop = new Container({ x: location.x, y: location.y })
        const shape = new Graphics().poly([-75, -18, 0, -52, 75, -18, 75, 47, -75, 47]).fill(location.controlled ? colors.green : colors.roof).stroke({ color: location.controlled ? colors.gold : colors.paper, width: location.controlled ? 4 : 2 }).rect(-17, 15, 34, 32).fill(colors.ink)
        const label = new Text({ text: location.name, style: { fill: colors.paper, fontFamily: 'Tahoma, sans-serif', fontSize: 17, fontWeight: '600', align: 'center' } }); label.anchor.set(.5, 0); label.y = 57
        shop.addChild(shape, label); businesses.addChild(shop)
      }
      descriptor.locations.forEach(drawBusiness)
      descriptor.connections.forEach((connection) => {
        const from = descriptor.locations.find((value) => value.entityId === connection.fromEntityId); const to = descriptor.locations.find((value) => value.entityId === connection.toEntityId)
        if (!from || !to) return
        const connectionColor = connection.kind === 'trust' ? 0x88a976 : connection.kind === 'obligation' ? colors.gold : connection.kind === 'debt' ? colors.red : colors.paper
        const line = new Graphics().moveTo(from.x, from.y).lineTo(to.x, to.y).stroke({ color: connectionColor, width: connection.kind === 'agreement' ? 5 : 2, alpha: connection.damaged ? .3 : .72 })
        relationships.addChild(line)
        if (connection.status === 'executed') app.ticker.add(() => { line.alpha = .62 + Math.sin(performance.now() / 180) * .28 })
      })
      descriptor.uncontrolledEntityIds.forEach((entityId, index) => {
        const at = descriptor.locations.find((value) => value.entityId === entityId); if (!at) return
        const person = new Graphics().circle(0, -10, 5).fill(colors.paper).moveTo(0, -4).lineTo(0, 17).moveTo(-8, 3).lineTo(8, 3).moveTo(0, 17).lineTo(-7, 28).moveTo(0, 17).lineTo(7, 28).stroke({ color: colors.paper, width: 2 }); person.position.set(at.x + 55 + index * 4, at.y + 40); population.addChild(person)
      })
      descriptor.messengers.forEach((messenger, index) => {
        const from = descriptor.locations.find((value) => value.entityId === messenger.fromEntityId); const to = descriptor.locations.find((value) => value.entityId === messenger.toEntityId); if (!from || !to) return
        const letter = new Graphics().rect(-13, -9, 26, 18).fill(colors.paper).stroke({ color: messenger.state === 'rejected' || messenger.state === 'failed' ? colors.red : colors.gold, width: 2 }).circle(0, 0, 4).fill(colors.red)
        if (messenger.state === 'countered') letter.circle(8, 0, 3).fill(colors.gold)
        letter.position.set(messenger.state === 'countered' ? to.x : from.x, messenger.state === 'countered' ? to.y : from.y); movement.addChild(letter)
        const started = performance.now() + index * 180
        app.ticker.add(() => {
          const progress = Math.max(0, Math.min(1, (performance.now() - started) / 1250))
          const reverse = messenger.state === 'countered'
          const a = reverse ? to : from; const b = reverse ? from : to
          letter.position.set(a.x + (b.x - a.x) * progress, a.y + (b.y - a.y) * progress - Math.sin(progress * Math.PI) * 35)
          letter.alpha = messenger.state === 'expired' ? 1 - progress * .8 : 1
          if (messenger.state === 'rejected' || messenger.state === 'failed') letter.rotation = progress * .7
        })
      })
      if (descriptor.avanVisible) {
        const avan = new Graphics().poly([840, 470, 870, 375, 900, 470]).fill({ color: colors.ink, alpha: .92 }).circle(870, 383, 11).fill(colors.gold)
        effects.addChild(avan)
      }
      if (descriptor.events.includes('missing-bell')) effects.addChild(new Graphics().poly([480, 237, 490, 205, 510, 205, 520, 237]).stroke({ color: colors.paper, width: 4 }).moveTo(468, 194).lineTo(532, 255).stroke({ color: colors.red, width: 5 }))
      if (descriptor.events.includes('cargo-shortage')) effects.addChild(new Graphics().rect(810, 560, 55, 35).stroke({ color: colors.paper, width: 3 }).moveTo(810, 560).lineTo(865, 595).moveTo(865, 560).lineTo(810, 595).stroke({ color: colors.paper, width: 2 }))
      if (descriptor.pressure) atmosphere.addChild(new Graphics().rect(0, 0, 1000, 700).stroke({ color: colors.red, width: 35, alpha: Math.min(.4, descriptor.pressure / 250) }))
      if (descriptor.ending !== 'none') {
        const veil = new Graphics().rect(0, 0, 1000, 700).fill({ color: descriptor.ending === 'world' ? colors.ink : colors.gold, alpha: .34 }); camera.addChild(veil)
      }
      cleanup = () => { app.renderer.off('resize', resize); app.destroy(true, { children: true }); if (host.current) host.current.replaceChildren() }
    }).catch(() => { if (!disposed) onFailure() })
    return () => { disposed = true; cleanup() }
  }, [descriptor, onFailure])
  return <div className="pixi-market" ref={host} data-testid="pixi-market" />
}
