export interface EventSummary {
  id: number
  title: string
  startsAt: string
  location: string
  capacity: number
  registered: number
}

export interface Registration {
  id: number
  name: string
  email: string
  company: string | null
  registeredAt: string
}

export interface EventInput {
  title: string
  startsAt: string
  location: string
  capacity: number
}

export interface RegistrationInput {
  name: string
  email: string
  company: string
}

async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, {
    ...init,
    headers: init?.body ? { 'Content-Type': 'application/json' } : undefined,
  })
  if (!response.ok) {
    const body = await response.json().catch(() => null)
    const details = body?.errors ? Object.values(body.errors).flat().join(' ') : null
    throw new Error(body?.message ?? details ?? 'Die Anfrage ist fehlgeschlagen.')
  }
  return response.status === 204 ? (undefined as T) : response.json()
}

export const getEvents = () => request<EventSummary[]>('/api/events')

export const createEvent = (input: EventInput) =>
  request<EventSummary>('/api/events', { method: 'POST', body: JSON.stringify(input) })

export const getRegistrations = (eventId: number) =>
  request<Registration[]>(`/api/events/${eventId}/registrations`)

export const createRegistration = (eventId: number, input: RegistrationInput) =>
  request<Registration>(`/api/events/${eventId}/registrations`, {
    method: 'POST',
    body: JSON.stringify(input),
  })

export const deleteRegistration = (eventId: number, registrationId: number) =>
  request<void>(`/api/events/${eventId}/registrations/${registrationId}`, { method: 'DELETE' })

export const participantsPdfUrl = (eventId: number) => `/api/events/${eventId}/participants.pdf`
