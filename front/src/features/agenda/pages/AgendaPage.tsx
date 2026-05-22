import { useMemo, useRef, useState } from 'react'
import FullCalendar from '@fullcalendar/react'
import dayGridPlugin from '@fullcalendar/daygrid'
import timeGridPlugin from '@fullcalendar/timegrid'
import interactionPlugin from '@fullcalendar/interaction'
import type { DateSelectArg, EventClickArg, DatesSetArg } from '@fullcalendar/core'
import ptBrLocale from '@fullcalendar/core/locales/pt-br'
import { useQuery } from '@tanstack/react-query'
import { cn } from '@/shared/utils/cn'
import { Skeleton } from '@/shared/components/ui/Skeleton'
import { agendaService } from '@/features/agenda/services/agenda.service'
import {
  ScheduleStatus,
  type ProfessionalResponse,
  type ScheduleResponse,
  type ServiceCatalogResponse,
} from '@/features/agenda/types/agenda.types'
import { EventDetailModal } from '@/features/agenda/components/EventDetailModal'
import { CreateScheduleModal } from '@/features/agenda/components/CreateScheduleModal'

const DEFAULT_COLOR = '#0ea5e9'

interface SelectedEvent {
  schedule: ScheduleResponse
  professional: ProfessionalResponse | undefined
  service: ServiceCatalogResponse | undefined
}

const isMobile = typeof window !== 'undefined' && window.innerWidth < 640

export function AgendaPage() {
  const calendarRef = useRef<FullCalendar>(null)
  const [dateRange, setDateRange] = useState({ start: '', end: '' })
  const [selectedEvent, setSelectedEvent] = useState<SelectedEvent | null>(null)
  const [createModal, setCreateModal] = useState<{ open: boolean; initialStart?: Date }>({
    open: false,
  })

  const { data: schedules = [], isLoading: loadingSchedules } = useQuery({
    queryKey: ['schedules', dateRange],
    queryFn: () =>
      dateRange.start
        ? agendaService.getSchedules(dateRange.start, dateRange.end)
        : Promise.resolve([]),
    enabled: !!dateRange.start,
  })

  const { data: professionals = [] } = useQuery({
    queryKey: ['professionals'],
    queryFn: agendaService.getProfessionals,
    staleTime: 5 * 60 * 1000,
  })

  const { data: services = [] } = useQuery({
    queryKey: ['services'],
    queryFn: agendaService.getServices,
    staleTime: 5 * 60 * 1000,
  })

  const events = useMemo(
    () =>
      schedules
        .filter((s) => s.status !== ScheduleStatus.Cancelled)
        .map((s) => {
          const professional = professionals.find((p) => p.id === s.professionalId)
          const service = services.find((sv) => sv.id === s.serviceId)
          const color =
            service?.calendarColor ?? professional?.calendarColor ?? DEFAULT_COLOR
          return {
            id: s.id,
            title: service?.name ?? 'Agendamento',
            start: s.startDateTime,
            end: s.endDateTime,
            backgroundColor: color,
            borderColor: 'transparent',
            textColor: '#ffffff',
            extendedProps: { schedule: s, professional, service },
          }
        }),
    [schedules, professionals, services],
  )

  function handleDatesSet(info: DatesSetArg) {
    setDateRange({ start: info.startStr, end: info.endStr })
  }

  function handleEventClick(info: EventClickArg) {
    const { schedule, professional, service } = info.event.extendedProps as {
      schedule: ScheduleResponse
      professional: ProfessionalResponse | undefined
      service: ServiceCatalogResponse | undefined
    }
    setSelectedEvent({ schedule, professional, service })
  }

  function handleDateSelect(info: DateSelectArg) {
    setCreateModal({ open: true, initialStart: info.start })
  }

  return (
    <div className="flex flex-col h-full">
      {/* Loading indicator */}
      {loadingSchedules && (
        <div className="px-4 py-2.5 border-b border-white/5 flex items-center gap-3" aria-label="Carregando agendamentos">
          <Skeleton className="h-2 w-2 rounded-full" />
          <Skeleton className="h-2 w-36 rounded-full" />
          <Skeleton className="h-2 w-20 rounded-full" />
        </div>
      )}

      {/* FullCalendar */}
      <div
        className={cn('fc-dark flex-1 overflow-hidden', loadingSchedules && 'opacity-70')}
        style={{ minHeight: 0 }}
      >
        <FullCalendar
          ref={calendarRef}
          plugins={[dayGridPlugin, timeGridPlugin, interactionPlugin]}
          initialView={isMobile ? 'timeGridDay' : 'timeGridWeek'}
          headerToolbar={
            isMobile
              ? { left: 'prev,next', center: 'title', right: 'timeGridDay,timeGridWeek' }
              : { left: 'prev,next today', center: 'title', right: 'dayGridMonth,timeGridWeek,timeGridDay' }
          }
          locale={ptBrLocale}
          events={events}
          selectable
          selectMirror
          select={handleDateSelect}
          eventClick={handleEventClick}
          datesSet={handleDatesSet}
          height="100%"
          slotMinTime="06:00:00"
          slotMaxTime="22:00:00"
          allDaySlot={false}
          nowIndicator
          slotDuration="00:30:00"
          eventDisplay="block"
          dayMaxEvents={3}
        />
      </div>

      {/* Modals */}
      {selectedEvent && (
        <EventDetailModal
          {...selectedEvent}
          onClose={() => setSelectedEvent(null)}
        />
      )}

      <CreateScheduleModal
        isOpen={createModal.open}
        initialStart={createModal.initialStart}
        onClose={() => setCreateModal({ open: false })}
      />
    </div>
  )
}
