export type ConvertRequest = {
  amount: number
  fromCurrency: string
  toCurrency: string
}

export type ConvertResponse = {
  auditId: string
  fromCurrency: string
  toCurrency: string
  originalAmount: number
  providerRate: number
  convertedAmount: number
  providerDate?: string | null
  executedAtUtc: string
}

export type AuditResponse = {
  auditId: string
  fromCurrency: string
  toCurrency: string
  originalAmount: number
  providerRate: number
  convertedAmount: number
  providerDate?: string | null
  executedAtUtc: string
  providerBaseUrl: string
}

declare global {
  interface Window {
    __RUNTIME_CONFIG__?: { VITE_API_URL?: string }
  }
}

const runtimeApiUrl = window.__RUNTIME_CONFIG__?.VITE_API_URL

export const apiBaseUrl =
  !runtimeApiUrl || runtimeApiUrl.includes('__VITE_API_URL__') ? '' : runtimeApiUrl

export async function convertCurrency(req: ConvertRequest): Promise<ConvertResponse> {
  const res = await fetch(`${apiBaseUrl}/api/convert`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(req),
  })

  if (!res.ok) {
    let message = `Conversion failed (${res.status})`
    try {
      const body = await res.json()
      message = body?.title || body?.detail || message
    } catch {
      // ignore
    }
    throw new Error(message)
  }

  return (await res.json()) as ConvertResponse
}

export async function fetchAudit(auditId: string): Promise<AuditResponse> {
  const res = await fetch(`${apiBaseUrl}/api/audits/${encodeURIComponent(auditId)}`)
  if (!res.ok) {
    let message = `Audit lookup failed (${res.status})`
    try {
      const body = await res.json()
      message = body?.title || body?.detail || message
    } catch {
      // ignore
    }
    throw new Error(message)
  }
  return (await res.json()) as AuditResponse
}
