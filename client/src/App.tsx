import { useCallback, useEffect, useState, type FormEvent } from 'react'
import {
  createEvent,
  createRegistration,
  deleteRegistration,
  getEvents,
  getRegistrations,
  participantsPdfUrl,
  type EventSummary,
  type Registration,
} from './api'

const dateFormat = new Intl.DateTimeFormat('de-DE', { dateStyle: 'medium', timeStyle: 'short' })
const shortDate = new Intl.DateTimeFormat('de-DE', { dateStyle: 'medium' })
const formatDate = (value: string) => dateFormat.format(new Date(value))

const message = (error: unknown) => (error instanceof Error ? error.message : 'Unbekannter Fehler.')

function App() {
  const [events, setEvents] = useState<EventSummary[]>([])
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)

  const loadEvents = useCallback(async () => {
    try {
      const loaded = await getEvents()
      setEvents(loaded)
      setSelectedId((current) => current ?? loaded[0]?.id ?? null)
    } catch (e) {
      setError(message(e))
    }
  }, [])

  useEffect(() => {
    void loadEvents()
  }, [loadEvents])

  const selected = events.find((e) => e.id === selectedId)

  return (
    <>
      <header className="topbar">
        <h1>EventReg</h1>
      </header>

      {error && <p className="error">{error}</p>}

      <main>
        <aside>
          <ul className="events">
            {events.map((e) => (
              <li key={e.id}>
                <button
                  className={e.id === selectedId ? 'event active' : 'event'}
                  onClick={() => setSelectedId(e.id)}
                >
                  {e.title}
                  <small>
                    {shortDate.format(new Date(e.startsAt))}, {e.registered}/{e.capacity}
                  </small>
                </button>
              </li>
            ))}
          </ul>
          <NewEventForm
            onCreated={async (id) => {
              await loadEvents()
              setSelectedId(id)
            }}
          />
        </aside>

        {selected ? (
          <EventDetail key={selected.id} event={selected} onChanged={loadEvents} />
        ) : (
          <p className="empty">Noch keine Veranstaltung vorhanden.</p>
        )}
      </main>
    </>
  )
}

function NewEventForm({ onCreated }: { onCreated: (id: number) => Promise<void> }) {
  const empty = { title: '', startsAt: '', location: '', capacity: '20' }
  const [form, setForm] = useState(empty)
  const [error, setError] = useState<string | null>(null)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    try {
      const created = await createEvent({ ...form, capacity: Number(form.capacity) })
      setForm(empty)
      await onCreated(created.id)
    } catch (e) {
      setError(message(e))
    }
  }

  return (
    <details>
      <summary>Neue Veranstaltung</summary>
      <form onSubmit={submit}>
        <label>
          Titel
          <input required minLength={3} value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} />
        </label>
        <label>
          Beginn
          <input required type="datetime-local" value={form.startsAt} onChange={(e) => setForm({ ...form, startsAt: e.target.value })} />
        </label>
        <label>
          Ort
          <input required value={form.location} onChange={(e) => setForm({ ...form, location: e.target.value })} />
        </label>
        <label>
          Plätze
          <input required type="number" min={1} value={form.capacity} onChange={(e) => setForm({ ...form, capacity: e.target.value })} />
        </label>
        {error && <p className="error">{error}</p>}
        <button type="submit">Anlegen</button>
      </form>
    </details>
  )
}

function EventDetail({ event, onChanged }: { event: EventSummary; onChanged: () => Promise<void> }) {
  const empty = { name: '', email: '', company: '' }
  const [registrations, setRegistrations] = useState<Registration[]>([])
  const [form, setForm] = useState(empty)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    getRegistrations(event.id).then(setRegistrations).catch((e) => setError(message(e)))
  }, [event.id])

  async function refresh() {
    setRegistrations(await getRegistrations(event.id))
    await onChanged()
  }

  async function submit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    try {
      await createRegistration(event.id, form)
      setForm(empty)
      await refresh()
    } catch (e) {
      setError(message(e))
    }
  }

  async function remove(id: number) {
    setError(null)
    try {
      await deleteRegistration(event.id, id)
      await refresh()
    } catch (e) {
      setError(message(e))
    }
  }

  const full = registrations.length >= event.capacity

  return (
    <section>
      <div className="detail-header">
        <div>
          <h2>{event.title}</h2>
          <p>
            {formatDate(event.startsAt)}, {event.location}
            <br />
            {registrations.length} von {event.capacity} Plätzen belegt
          </p>
        </div>
        <a className="button" href={participantsPdfUrl(event.id)} target="_blank" rel="noreferrer">
          Teilnehmerliste als PDF
        </a>
      </div>

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Name</th>
              <th>E-Mail</th>
              <th>Firma</th>
              <th>Angemeldet</th>
              <th aria-label="Aktionen" />
            </tr>
          </thead>
          <tbody>
            {registrations.map((r) => (
              <tr key={r.id}>
                <td>{r.name}</td>
                <td>{r.email}</td>
                <td>{r.company ?? '-'}</td>
                <td className="date">{formatDate(r.registeredAt)}</td>
                <td className="action">
                  <button className="link" onClick={() => remove(r.id)}>
                    entfernen
                  </button>
                </td>
              </tr>
            ))}
            {registrations.length === 0 && (
              <tr>
                <td colSpan={5} className="empty">
                  Noch keine Anmeldungen.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <h3>Person anmelden</h3>
      <form className="inline" onSubmit={submit}>
        <label>
          Name
          <input required minLength={2} value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
        </label>
        <label>
          E-Mail
          <input required type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
        </label>
        <label>
          Firma (optional)
          <input value={form.company} onChange={(e) => setForm({ ...form, company: e.target.value })} />
        </label>
        <button type="submit" disabled={full}>
          {full ? 'Ausgebucht' : 'Anmelden'}
        </button>
      </form>
      {error && <p className="error">{error}</p>}
    </section>
  )
}

export default App
