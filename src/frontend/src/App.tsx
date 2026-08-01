import { FormEvent, useCallback, useEffect, useMemo, useState } from 'react'

type LogEntry = {
  TimestampIST?: string
  Message?: string
  ContainerAppName?: string
}

type LogsResponse = {
  enrollmentNumber: number
  containerAppName: string
  count: number
  logs: LogEntry[]
}

type ProblemDetails = {
  title?: string
  detail?: string
}

type AppProps = {
  apiBaseUrl: string
}

export default function App({ apiBaseUrl }: AppProps) {
  const [enrollmentId, setEnrollmentId] = useState('1000')
  const [hours, setHours] = useState(1)
  const [search, setSearch] = useState('')
  const [limit, setLimit] = useState(100)
  const [logs, setLogs] = useState<LogEntry[]>([])
  const [containerAppName, setContainerAppName] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const logsUrl = useMemo(() => `${normalizeBaseUrl(apiBaseUrl)}/api/logs`, [apiBaseUrl])

  const loadLogs = useCallback(
    async (id: string, windowHours: number, filter: string, rowLimit: number) => {
      setIsLoading(true)
      setError(null)
      setContainerAppName(null)

      try {
        const params = new URLSearchParams({ enrollmentId: id.trim() })
        if (windowHours > 0) {
          params.set('hours', String(windowHours))
        }
        if (filter.trim()) {
          params.set('search', filter.trim())
        }
        params.set('limit', String(rowLimit))

        const response = await fetch(`${logsUrl}?${params.toString()}`)
        if (!response.ok) {
          const problem = (await response.json().catch(() => null)) as ProblemDetails | null
          throw new Error(problem?.detail ?? problem?.title ?? 'Unable to load logs.')
        }

        const data = (await response.json()) as LogsResponse
        setLogs(data.logs)
        setContainerAppName(data.containerAppName)
      } catch (caught) {
        setError(caught instanceof Error ? caught.message : 'Unable to load logs.')
        setLogs([])
      } finally {
        setIsLoading(false)
      }
    },
    [logsUrl],
  )

  useEffect(() => {
    void loadLogs('1000', 1, '', 100)
  }, [loadLogs])

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    await loadLogs(enrollmentId, hours, search, limit)
  }

  return (
    <main className="layout">
      <section className="panel hero">
        <p className="eyebrow">Container App Log Viewer</p>
        <h1>Container logs with an enrollment ID filter.</h1>
        <p>
          Enter an enrollment ID to view recent console logs for its container app. Timestamps are shown in India
          Standard Time (IST).
        </p>
      </section>

      <section className="panel">
        <form className="form filter-form" onSubmit={(event) => void handleSubmit(event)}>
          <label>
            <span>Enrollment ID</span>
            <input
              value={enrollmentId}
              onChange={(event) => setEnrollmentId(event.target.value)}
              placeholder="e.g. 1000 or adlc-1000"
            />
          </label>
          <label>
            <span>Window (hours)</span>
            <input
              type="number"
              min="1"
              max="720"
              value={hours}
              onChange={(event) => setHours(Number(event.target.value))}
            />
          </label>
          <label>
            <span>Search message</span>
            <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="e.g. error, warn, exception" />
            <small className="field-note">Filters log messages by text — useful to spot errors, warnings, or a specific pattern.</small>
          </label>
          <label>
            <span>Rows (max 500)</span>
            <select value={limit} onChange={(event) => setLimit(Number(event.target.value))}>
              <option value={50}>50</option>
              <option value={100}>100</option>
              <option value={200}>200</option>
              <option value={500}>500</option>
            </select>
          </label>
          <button type="submit" disabled={isLoading}>
            {isLoading ? 'Loading…' : 'Show logs'}
          </button>
        </form>

        {error ? <p className="error-message">{error}</p> : null}
        {containerAppName ? (
          <p className="scope-note">
            Showing logs for <strong>{containerAppName}</strong> ({logs.length} rows)
          </p>
        ) : null}
      </section>

      <section className="panel">
        <div className="section-heading">
          <h2>Log entries</h2>
          <button type="button" onClick={() => void loadLogs(enrollmentId, hours, search, limit)} disabled={isLoading}>
            {isLoading ? 'Refreshing…' : 'Refresh'}
          </button>
        </div>

        {isLoading ? <p>Loading container logs…</p> : null}

        {!isLoading && logs.length === 0 && !error ? <p>No log entries in the selected window.</p> : null}

        {logs.length > 0 ? (
          <div className="table-wrap">
            <table className="log-table">
              <thead>
                <tr>
                  <th>Timestamp (IST)</th>
                  <th>Message</th>
                  <th>Container App</th>
                </tr>
              </thead>
              <tbody>
                {logs.map((entry, index) => (
                  <tr key={`${entry.TimestampIST}-${index}`}>
                    <td className="time-cell">{entry.TimestampIST ?? ''}</td>
                    <td className="message-cell">{entry.Message ?? ''}</td>
                    <td className="app-cell">{entry.ContainerAppName ?? ''}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : null}
      </section>
    </main>
  )
}

function normalizeBaseUrl(value: string) {
  if (!value) {
    return ''
  }

  return value.endsWith('/') ? value.slice(0, -1) : value
}
