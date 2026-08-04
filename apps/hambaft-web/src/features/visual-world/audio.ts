export type MarketSound = 'ambient-market' | 'distant-carts' | 'paper-and-seal' | 'soft-crowd-tension' | 'rain-or-water' | 'warehouse-warning' | 'ending-ambience'

class GeneratedMarketAudio {
  private context: AudioContext | null = null
  private nodes: AudioNode[] = []

  async start(sound: MarketSound = 'ambient-market') {
    this.stop()
    const AudioContextType = window.AudioContext ?? (window as typeof window & { webkitAudioContext?: typeof AudioContext }).webkitAudioContext
    if (!AudioContextType) return
    const context = new AudioContextType()
    this.context = context
    await context.resume()
    const master = context.createGain()
    master.gain.value = .035
    master.connect(context.destination)
    const base = context.createOscillator()
    const upper = context.createOscillator()
    const frequencies: Record<MarketSound, [number, number]> = {
      'ambient-market': [82, 123], 'distant-carts': [55, 72], 'paper-and-seal': [180, 240], 'soft-crowd-tension': [67, 101],
      'rain-or-water': [110, 165], 'warehouse-warning': [73, 146], 'ending-ambience': [65, 98],
    }
    ;[base.frequency.value, upper.frequency.value] = frequencies[sound]
    base.type = 'sine'; upper.type = 'triangle'
    const upperGain = context.createGain(); upperGain.gain.value = .18
    base.connect(master); upper.connect(upperGain); upperGain.connect(master)
    base.start(); upper.start()
    this.nodes = [base, upper, upperGain, master]
  }

  stop() {
    this.nodes.forEach((node) => { if ('stop' in node) try { (node as OscillatorNode).stop() } catch { /* already stopped */ } })
    this.nodes = []
    if (this.context) void this.context.close()
    this.context = null
  }
}

export const marketAudio = new GeneratedMarketAudio()
