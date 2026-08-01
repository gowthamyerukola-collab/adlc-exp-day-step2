import { FormEvent, useEffect, useMemo, useState } from 'react'

type ConversionAudit = {
  id: string
  requestedAmount: number
  sourceCurrency: string
  targetCurrency: string
  appliedRate: number
  convertedAmount: number
  providerMarker: string
  executionTimestampUtc: string
}

type ProblemDetails = {
  title?: string
  detail?: string
}

type AppProps = {
  apiBaseUrl: string
}

const initialForm = {
  amount: '100.00',
  fromCurrency: 'USD',
  toCurrency: 'EUR',
}

export default function App({ apiBaseUrl }: AppProps) {
  const [form, setForm] = useState(initialForm)
  const [selectedRecord, setSelectedRecord] = useState<ConversionAudit | null>(null)
  const [recentRecords, setRecentRecords] = useState<ConversionAudit[]>([])
  const [lookupId, setLookupId] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isLoadingRecent, setIsLoadingRecent] = useState(true)
  const [message, setMessage] = useState<string | null>(null)

  const conversionsUrl = useMemo(() => `${normalizeBaseUrl(apiBaseUrl)}/api/conversions`, [apiBaseUrl])

  useEffect(() => {
    void loadRecent()
  }, [conversionsUrl])

  async function loadRecent() {
    setIsLoadingRecent(true)
    try {
      const response = await fetch(`${conversionsUrl}?limit=10`)
      if (!response.ok) {
        throw new Error('Unable to load recent audit history.')
      }

      const records = (await response.json()) as ConversionAudit[]
      setRecentRecords(records)
      if (!selectedRecord && records.length > 0) {
        setSelectedRecord(records[0])
      }
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Unable to load recent audit history.')
    } finally {
      setIsLoadingRecent(false)
    }
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSubmitting(true)
    setMessage(null)

    try {
      const payload = {
        amount: Number(form.amount),
        fromCurrency: form.fromCurrency.trim().toUpperCase(),
        toCurrency: form.toCurrency.trim().toUpperCase(),
      }

      const response = await fetch(conversionsUrl, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(payload),
      })

      if (!response.ok) {
        const problem = (await response.json().catch(() => null)) as ProblemDetails | null
        throw new Error(problem?.title ?? problem?.detail ?? 'Unable to submit conversion.')
      }

      const created = (await response.json()) as ConversionAudit
      setSelectedRecord(created)
      setLookupId(created.id)
      setRecentRecords((current) => [created, ...current.filter((record) => record.id !== created.id)].slice(0, 10))
      setMessage(`Stored audit record ${created.id}.`)
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Unable to submit conversion.')
    } finally {
      setIsSubmitting(false)
    }
  }

  async function handleLookup(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setMessage(null)

    if (!lookupId.trim()) {
      setMessage('Enter an audit identifier to look up a stored conversion.')
      return
    }

    try {
      const response = await fetch(`${conversionsUrl}/${encodeURIComponent(lookupId.trim())}`)
      if (response.status === 404) {
        setMessage('No audit record was found for that identifier.')
        return
      }

      if (!response.ok) {
        throw new Error('Unable to load the requested audit record.')
      }

      const record = (await response.json()) as ConversionAudit
      setSelectedRecord(record)
      setRecentRecords((current) => [record, ...current.filter((item) => item.id !== record.id)].slice(0, 10))
      setMessage(`Loaded audit record ${record.id}.`)
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Unable to load the requested audit record.')
    }
  }

  return (
    <main className="layout">
      <section className="panel hero">
        <p className="eyebrow">Real-Time Currency Conversion &amp; Audit Trail</p>
        <h1>Instant conversions with auditor-ready reconstruction.</h1>
        <p>
          Submit a conversion, store the applied rate and timestamps, and reopen any audit record without recomputing it.
        </p>
      </section>

      <div className="grid">
        <section className="panel">
          <h2>New conversion</h2>
          <form className="form" onSubmit={handleSubmit}>
            <label>
              <span>Amount</span>
              <input
                type="number"
                inputMode="decimal"
                min="0"
                step="0.01"
                value={form.amount}
                onChange={(event) => setForm((current) => ({ ...current, amount: event.target.value }))}
              />
            </label>
            <label>
              <span>From</span>
              <input
                maxLength={3}
                value={form.fromCurrency}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    fromCurrency: event.target.value.toUpperCase(),
                  }))
                }
              />
            </label>
            <label>
              <span>To</span>
              <input
                maxLength={3}
                value={form.toCurrency}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    toCurrency: event.target.value.toUpperCase(),
                  }))
                }
              />
            </label>
            <button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Submitting…' : 'Convert and store audit'}
            </button>
          </form>

          <div className="sample-callout">
            <strong>Sample:</strong> 100.00 USD → EUR should display the stored rate, converted amount, provider marker, and backend execution timestamp.
          </div>
        </section>

        <section className="panel">
          <h2>Audit lookup</h2>
          <form className="form inline-form" onSubmit={handleLookup}>
            <label>
              <span>Audit ID</span>
              <input value={lookupId} onChange={(event) => setLookupId(event.target.value)} placeholder="Paste a stored audit ID" />
            </label>
            <button type="submit">Load audit</button>
          </form>

          {message ? <p className="message">{message}</p> : null}

          {selectedRecord ? <AuditDetail record={selectedRecord} /> : <p>No audit record selected yet.</p>}
        </section>
      </div>

      <section className="panel">
        <div className="section-heading">
          <h2>Recent audit records</h2>
          <button type="button" onClick={() => void loadRecent()} disabled={isLoadingRecent}>
            {isLoadingRecent ? 'Refreshing…' : 'Refresh'}
          </button>
        </div>

        {recentRecords.length === 0 && !isLoadingRecent ? <p>No audit history yet.</p> : null}

        <ul className="audit-list">
          {recentRecords.map((record) => (
            <li key={record.id}>
              <button type="button" className="audit-item" onClick={() => setSelectedRecord(record)}>
                <span>
                  {formatMoney(record.requestedAmount)} {record.sourceCurrency} → {record.targetCurrency}
                </span>
                <strong>{formatMoney(record.convertedAmount)}</strong>
                <small>{formatTimestamp(record.executionTimestampUtc)}</small>
              </button>
            </li>
          ))}
        </ul>
      </section>
    </main>
  )
}

function AuditDetail({ record }: { record: ConversionAudit }) {
  return (
    <dl className="detail-grid">
      <DetailRow label="Audit ID" value={record.id} />
      <DetailRow label="Requested amount" value={`${formatMoney(record.requestedAmount)} ${record.sourceCurrency}`} />
      <DetailRow label="Target currency" value={record.targetCurrency} />
      <DetailRow label="Applied rate" value={record.appliedRate.toFixed(4)} />
      <DetailRow label="Converted amount" value={`${formatMoney(record.convertedAmount)} ${record.targetCurrency}`} />
      <DetailRow label="Provider marker" value={record.providerMarker} />
      <DetailRow label="Execution timestamp" value={formatTimestamp(record.executionTimestampUtc)} />
    </dl>
  )
}

function DetailRow({ label, value }: { label: string; value: string }) {
  return (
    <>
      <dt>{label}</dt>
      <dd>{value}</dd>
    </>
  )
}

function normalizeBaseUrl(value: string) {
  if (!value) {
    return ''
  }

  return value.endsWith('/') ? value.slice(0, -1) : value
}

function formatMoney(value: number) {
  return value.toFixed(4)
}

function formatTimestamp(value: string) {
  return new Date(value).toLocaleString(undefined, {
    dateStyle: 'medium',
    timeStyle: 'medium',
  })
}
