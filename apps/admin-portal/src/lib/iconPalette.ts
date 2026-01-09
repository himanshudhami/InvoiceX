type IconPalette = {
  text: string
  bg: string
}

const iconPalette: IconPalette[] = [
  { text: 'text-rose-600', bg: 'bg-rose-100' },
  { text: 'text-amber-600', bg: 'bg-amber-100' },
  { text: 'text-emerald-600', bg: 'bg-emerald-100' },
  { text: 'text-sky-600', bg: 'bg-sky-100' },
  { text: 'text-indigo-600', bg: 'bg-indigo-100' },
  { text: 'text-fuchsia-600', bg: 'bg-fuchsia-100' },
  { text: 'text-lime-600', bg: 'bg-lime-100' },
  { text: 'text-orange-600', bg: 'bg-orange-100' },
]

const hashString = (value: string) => {
  let hash = 0
  for (let i = 0; i < value.length; i += 1) {
    hash = (hash * 31 + value.charCodeAt(i)) % 2147483647
  }
  return hash
}

export const getIconStyle = (seed: string): IconPalette => {
  const index = Math.abs(hashString(seed)) % iconPalette.length
  return iconPalette[index]
}
