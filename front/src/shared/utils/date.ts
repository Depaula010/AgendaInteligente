const LOCALE = 'pt-BR'

let _tz = 'America/Sao_Paulo'

export function setAppTimezone(tz: string): void {
  _tz = tz
}

export function getAppTimezone(): string {
  return _tz
}

export function fmtDateTime(iso: string): string {
  return new Date(iso).toLocaleString(LOCALE, {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    timeZone: _tz,
  })
}

export function fmtTime(iso: string): string {
  return new Date(iso).toLocaleTimeString(LOCALE, { hour: '2-digit', minute: '2-digit', timeZone: _tz })
}

export function fmtDateShort(iso: string): string {
  return new Date(iso).toLocaleDateString(LOCALE, {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    timeZone: _tz,
  })
}
