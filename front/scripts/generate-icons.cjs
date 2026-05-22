// Gera todos os ícones PWA a partir de public/logo.svg
// Uso: node scripts/generate-icons.cjs
const sharp = require('sharp')
const path = require('path')
const fs = require('fs')

const SRC = path.resolve(__dirname, '../public/logo.svg')
const OUT = path.resolve(__dirname, '../public')

const icons = [
  { name: 'favicon-32x32.png',           size: 32 },
  { name: 'pwa-64x64.png',               size: 64 },
  { name: 'pwa-192x192.png',             size: 192 },
  { name: 'pwa-512x512.png',             size: 512 },
  { name: 'maskable-icon-512x512.png',   size: 512 },
  { name: 'apple-touch-icon-180x180.png',size: 180 },
]

async function generate() {
  const svg = fs.readFileSync(SRC)

  for (const icon of icons) {
    const outPath = path.join(OUT, icon.name)
    await sharp(svg)
      .resize(icon.size, icon.size)
      .png()
      .toFile(outPath)
    console.log(`✓ ${icon.name}`)
  }

  console.log('\nÍcones gerados em public/')
}

generate().catch(console.error)
