import { useMemo, useState } from 'react'
import type { AuditResponse, ConvertResponse } from './api/client'
import { convertCurrency, fetchAudit } from './api/client'

type UiError = { message: string } | null

export default function App() {
  const [amount, setAmount] = useState('')
  const [fromCurrency, setFromCurrency] = useState('USD')
  const [toCurrency, setToCurrency] = useState('EUR')

  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<UiError>(null)

  const [conversion, setConversion] = useState<ConvertResponse | null>(null)

  const [auditId, setAuditId] = useState('')
  const [audit, setAudit] = useState<AuditResponse | null>(null)
  const [auditBusy, setAuditBusy] = useState(false)
  const [auditError, setAuditError] = useState<UiError>(null)

  const parsedAmount = useMemo(() => {
    const n = Number(amount)
    return Number.isFinite(n) ? n : null
  }, [amount])

  async function onConvert(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setConversion(null)
    setAudit(null)

    if (parsedAmount === null) {
      setError({ message: 'Amount must be a number.' })
      return
    }

    setBusy(true)
    try {
      const res = await convertCurrency({
        amount: parsedAmount,
        fromCurrency,
        toCurrency,
      })
      setConversion(res)
      setAuditId(res.auditId)
    } catch (err) {
      setError({ message: err instanceof Error ? err.message : 'Conversion failed.' })
    } finally {
      setBusy(false)
    }
  }

  async function onLookup(e: React.FormEvent) {
    e.preventDefault()
    setAuditError(null)
    setAudit(null)

    if (!auditId.trim()) {
      setAuditError({ message: 'Audit ID is required.' })
      return
    }

    setAuditBusy(true)
    try {
      const res = await fetchAudit(auditId.trim())
      setAudit(res)
    } catch (err) {
      setAuditError({ message: err instanceof Error ? err.message : 'Lookup failed.' })
    } finally {
      setAuditBusy(false)
    }
  }

  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 16, fontFamily: 'system-ui' }}>
      <h1 style={{ margin: 0, marginBottom: 16 }}>Real-Time Currency Conversion</h1>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr', gap: 16 }}>
        <div style={{ border: '1px solid #e5e7eb', borderRadius: 12, padding: 16 }}>
          <h2 style={{ margin: 0, marginBottom: 12, fontSize: 16 }}>Convert</h2>
          <form onSubmit={onConvert}>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 12 }}>
              <label style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
                <span>Amount</span>
                <input
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                  inputMode="decimal"
                  placeholder="100.00"
                  style={{ padding: 8, borderRadius: 8, border: '1px solid #e5e7eb' }}
                />
              </label>
              <label style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
                <span>From</span>
                <input
                  value={fromCurrency}
                  onChange={(e) => setFromCurrency(e.target.value)}
                  placeholder="USD"
                  style={{ padding: 8, borderRadius: 8, border: '1px solid #e5e7eb' }}
                />
              </label>
              <label style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
                <span>To</span>
                <input
                  value={toCurrency}
                  onChange={(e) => setToCurrency(e.target.value)}
                  placeholder="EUR"
                  style={{ padding: 8, borderRadius: 8, border: '1px solid #e5e7eb' }}
                />
              </label>
            </div>
            <div style={{ marginTop: 12, display: 'flex', gap: 12, alignItems: 'center' }}>
              <button
                type="submit"
                disabled={busy}
                style={{ padding: '10px 14px', borderRadius: 10, border: '1px solid #111827', background: '#111827', color: '#fff' }}
              >
                {busy ? 'Converting…' : 'Convert'}
              </button>
              {error ? <div style={{ color: '#b91c1c' }}>{error.message}</div> : null}
            </div>
          </form>
        </div>

        {conversion ? (
          <div style={{ border: '1px solid #e5e7eb', borderRadius: 12, padding: 16 }}>
            <h2 style={{ margin: 0, marginBottom: 12, fontSize: 16 }}>Result</h2>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: 12 }}>
              <div>
                <div style={{ color: '#6b7280', fontSize: 12 }}>Converted Amount</div>
                <div style={{ fontSize: 18, fontWeight: 700 }}>{conversion.convertedAmount}</div>
              </div>
              <div>
                <div style={{ color: '#6b7280', fontSize: 12 }}>Provider Rate</div>
                <div style={{ fontSize: 18, fontWeight: 700 }}>{conversion.providerRate}</div>
              </div>
              <div>
                <div style={{ color: '#6b7280', fontSize: 12 }}>Provider Date</div>
                <div>{conversion.providerDate ?? '—'}</div>
              </div>
              <div>
                <div style={{ color: '#6b7280', fontSize: 12 }}>Executed At (UTC)</div>
                <div>{conversion.executedAtUtc}</div>
              </div>
              <div style={{ gridColumn: '1 / -1' }}>
                <div style={{ color: '#6b7280', fontSize: 12 }}>Audit ID</div>
                <div style={{ fontFamily: 'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace' }}>{conversion.auditId}</div>
              </div>
            </div>
          </div>
        ) : null}

        <div style={{ border: '1px solid #e5e7eb', borderRadius: 12, padding: 16 }}>
          <h2 style={{ margin: 0, marginBottom: 12, fontSize: 16 }}>Audit Lookup</h2>
          <form onSubmit={onLookup}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr auto', gap: 12, alignItems: 'end' }}>
              <label style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
                <span>Audit ID</span>
                <input
                  value={auditId}
                  onChange={(e) => setAuditId(e.target.value)}
                  placeholder="e.g. 3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  style={{ padding: 8, borderRadius: 8, border: '1px solid #e5e7eb' }}
                />
              </label>
              <button
                type="submit"
                disabled={auditBusy}
                style={{ padding: '10px 14px', borderRadius: 10, border: '1px solid #111827', background: '#111827', color: '#fff' }}
              >
                {auditBusy ? 'Looking up…' : 'Lookup'}
              </button>
            </div>
            {auditError ? <div style={{ marginTop: 12, color: '#b91c1c' }}>{auditError.message}</div> : null}
          </form>

          {audit ? (
            <div style={{ marginTop: 16, paddingTop: 16, borderTop: '1px solid #e5e7eb' }}>
              <h3 style={{ margin: 0, marginBottom: 12, fontSize: 14 }}>Stored Conversion</h3>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: 12 }}>
                <div>
                  <div style={{ color: '#6b7280', fontSize: 12 }}>From / To</div>
                  <div>
                    {audit.fromCurrency} → {audit.toCurrency}
                  </div>
                </div>
                <div>
                  <div style={{ color: '#6b7280', fontSize: 12 }}>Original Amount</div>
                  <div>{audit.originalAmount}</div>
                </div>
                <div>
                  <div style={{ color: '#6b7280', fontSize: 12 }}>Provider Rate</div>
                  <div>{audit.providerRate}</div>
                </div>
                <div>
                  <div style={{ color: '#6b7280', fontSize: 12 }}>Converted Amount</div>
                  <div style={{ fontWeight: 700 }}>{audit.convertedAmount}</div>
                </div>
                <div>
                  <div style={{ color: '#6b7280', fontSize: 12 }}>Provider Date</div>
                  <div>{audit.providerDate ?? '—'}</div>
                </div>
                <div>
                  <div style={{ color: '#6b7280', fontSize: 12 }}>Executed At (UTC)</div>
                  <div>{audit.executedAtUtc}</div>
                </div>
                <div style={{ gridColumn: '1 / -1' }}>
                  <div style={{ color: '#6b7280', fontSize: 12 }}>Provider Base URL</div>
                  <div style={{ wordBreak: 'break-all' }}>{audit.providerBaseUrl}</div>
                </div>
              </div>
            </div>
          ) : null}
        </div>
      </div>
    </div>
  )
}
