import { type ClassValue, clsx } from 'clsx'
import { twMerge } from 'tailwind-merge'

/**
 * cn — utilitário para combinar classes Tailwind de forma segura.
 * Usa clsx para condicionais e tailwind-merge para resolver conflitos.
 */
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}
