import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Activity,
  AlertTriangle,
  CheckCircle2,
  Clock,
  Loader2,
  MessageSquare,
  RefreshCw,
  Send,
  Smartphone,
  WifiOff,
  Zap,
} from 'lucide-react'
import { whatsappService } from '@/features/whatsapp/services/whatsapp.service'
import { Button } from '@/shared/components/ui/Button'
import { Badge } from '@/shared/components/ui/Badge'
import { appToast } from '@/shared/lib/toast'

const STATUS_LABEL: Record<string, string> = {
  connected: 'Conectado',
  connecting: 'Conectando...',
  not_configured: 'Não configurado',
  unknown: 'Status desconhecido',
}

const STATUS_BADGE: Record<string, 'success' | 'warning' | 'danger' | 'default'> = {
  connected: 'success',
  connecting: 'warning',
  not_configured: 'danger',
  unknown: 'default',
}

function formatUptime(seconds: number | null): string {
  if (seconds === null) return '—'
  if (seconds < 60) return `${seconds}s`
  if (seconds < 3600) return `${Math.floor(seconds / 60)}min`
  const h = Math.floor(seconds / 3600)
  const m = Math.floor((seconds % 3600) / 60)
  return m > 0 ? `${h}h ${m}min` : `${h}h`
}

export function WhatsAppPage() {
  const queryClient = useQueryClient()

  const {
    data: status,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ['whatsapp-status'],
    queryFn: whatsappService.getStatus,
    refetchInterval: (query) => {
      const data = query.state.data
      return data?.isConnected ? 30_000 : 3_000
    },
    retry: 1,
  })

  const { data: stats } = useQuery({
    queryKey: ['whatsapp-stats'],
    queryFn: whatsappService.getStats,
    enabled: status?.isConnected === true,
    refetchInterval: 30_000,
    retry: false,
  })

  const connectMutation = useMutation({
    mutationFn: whatsappService.connect,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['whatsapp-status'] })
      appToast.success('Iniciando conexão com o WhatsApp...')
    },
    onError: (err: unknown) => {
      appToast.error(appToast.apiError(err, 'Não foi possível iniciar a conexão.'))
    },
  })

  const reconnectMutation = useMutation({
    mutationFn: whatsappService.reconnect,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['whatsapp-status'] })
      queryClient.invalidateQueries({ queryKey: ['whatsapp-stats'] })
      appToast.success('Reconexão iniciada. Aguarde...')
    },
    onError: (err: unknown) => {
      appToast.error(appToast.apiError(err, 'Não foi possível reconectar.'))
    },
  })

  if (isLoading) {
    return (
      <div className="flex-1 flex items-center justify-center p-8">
        <Loader2 className="h-8 w-8 text-brand-400 animate-spin" aria-label="Carregando" />
      </div>
    )
  }

  if (isError || !status) {
    return (
      <div className="p-6 max-w-md mx-auto">
        <div className="rounded-2xl border border-red-500/20 bg-red-500/5 p-6 text-center">
          <WifiOff className="h-10 w-10 text-red-400 mx-auto mb-3" aria-hidden="true" />
          <p className="text-white font-medium mb-1">Não foi possível conectar ao servidor</p>
          <p className="text-sm text-slate-500">Verifique se o backend está rodando.</p>
          <Button
            className="mt-4"
            variant="ghost"
            onClick={() => queryClient.invalidateQueries({ queryKey: ['whatsapp-status'] })}
          >
            Tentar novamente
          </Button>
        </div>
      </div>
    )
  }

  return (
    <div className="p-4 md:p-6 max-w-lg mx-auto">
      <div className="mb-6">
        <h1 className="font-display text-xl font-bold text-white">WhatsApp</h1>
        <p className="text-sm text-slate-500 mt-1">
          Conecte o bot para receber agendamentos via WhatsApp.
        </p>
      </div>

      {/* Status card */}
      <div className="rounded-2xl border border-white/10 bg-white/5 p-6 mb-4">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-3">
            <div
              className={`h-10 w-10 rounded-xl flex items-center justify-center ${
                status.isConnected ? 'bg-green-500/20' : 'bg-slate-700/50'
              }`}
            >
              <Smartphone
                className={`h-5 w-5 ${status.isConnected ? 'text-green-400' : 'text-slate-400'}`}
                aria-hidden="true"
              />
            </div>
            <div>
              <p className="text-sm font-medium text-white">Bot WhatsApp</p>
              <p className="text-xs text-slate-500">Agenda Inteligente</p>
            </div>
          </div>
          <Badge variant={STATUS_BADGE[status.status] ?? 'default'}>
            {STATUS_LABEL[status.status] ?? status.status}
          </Badge>
        </div>

        {/* Connected state */}
        {status.isConnected && (
          <>
            <div className="flex items-center gap-3 rounded-xl bg-green-500/10 border border-green-500/20 p-4">
              <CheckCircle2 className="h-5 w-5 text-green-400 flex-shrink-0" aria-hidden="true" />
              <div>
                <p className="text-sm font-medium text-white">Bot conectado com sucesso</p>
                <p className="text-xs text-slate-400 mt-0.5">
                  Clientes podem agendar pelo WhatsApp.
                </p>
              </div>
            </div>
            <div className="mt-4 flex justify-end">
              <Button
                variant="ghost"
                size="sm"
                onClick={() => reconnectMutation.mutate()}
                isLoading={reconnectMutation.isPending}
                leftIcon={<RefreshCw className="h-4 w-4" aria-hidden="true" />}
              >
                Forçar reconexão
              </Button>
            </div>
          </>
        )}

        {/* QR code state */}
        {!status.isConnected && status.qrCode && (
          <div className="flex flex-col items-center gap-4">
            <div className="rounded-2xl bg-white p-4 inline-block">
              <img
                src={`data:image/png;base64,${status.qrCode}`}
                alt="QR code para conectar o WhatsApp"
                className="h-52 w-52 block"
              />
            </div>
            <div className="text-center">
              <p className="text-sm font-medium text-white">Escaneie com o WhatsApp</p>
              <p className="text-xs text-slate-500 mt-1">
                Abra o WhatsApp → Dispositivos conectados → Conectar dispositivo
              </p>
            </div>
            <div className="flex items-center gap-2 text-xs text-slate-500">
              <Loader2 className="h-3 w-3 animate-spin" aria-hidden="true" />
              Aguardando leitura do QR code...
            </div>
          </div>
        )}

        {/* Not configured / generic disconnected */}
        {!status.isConnected && !status.qrCode && (
          <div className="text-center py-4">
            <p className="text-sm text-slate-400 mb-4">
              {status.status === 'not_configured'
                ? 'O bot ainda não foi configurado. Clique para gerar o QR code.'
                : 'O bot foi desconectado. Reconecte para retomar os agendamentos.'}
            </p>
            <Button
              onClick={() => connectMutation.mutate()}
              isLoading={connectMutation.isPending}
              leftIcon={<RefreshCw className="h-4 w-4" aria-hidden="true" />}
            >
              {status.status === 'not_configured' ? 'Iniciar conexão' : 'Reconectar'}
            </Button>
          </div>
        )}
      </div>

      {/* Stats card — visível apenas quando conectado */}
      {status.isConnected && stats && (
        <div className="rounded-2xl border border-white/10 bg-white/5 p-5 mb-4">
          <p className="text-xs font-semibold text-slate-400 uppercase tracking-wider mb-4">
            Métricas da sessão
          </p>

          <div className="grid grid-cols-2 gap-3">
            <StatItem
              icon={<MessageSquare className="h-4 w-4 text-brand-400" />}
              label="Recebidas"
              value={stats.messagesReceived}
            />
            <StatItem
              icon={<Send className="h-4 w-4 text-green-400" />}
              label="Enviadas"
              value={stats.messagesSent}
            />
            <StatItem
              icon={<AlertTriangle className="h-4 w-4 text-amber-400" />}
              label="Erros webhook"
              value={stats.webhookErrors}
              alert={stats.webhookErrors > 0}
            />
            <StatItem
              icon={<Zap className="h-4 w-4 text-red-400" />}
              label="Circuit trips"
              value={stats.circuitBreakerTrips}
              alert={stats.circuitBreakerTrips > 0}
            />
            <StatItem
              icon={<Activity className="h-4 w-4 text-slate-400" />}
              label="Reconexões"
              value={stats.reconnectCount}
            />
            <StatItem
              icon={<Clock className="h-4 w-4 text-slate-400" />}
              label="Uptime"
              value={formatUptime(stats.uptimeSeconds)}
            />
          </div>
        </div>
      )}

      {/* Instructions */}
      {!status.isConnected && (
        <div className="rounded-2xl border border-white/10 bg-white/5 p-5">
          <p className="text-xs font-semibold text-slate-400 uppercase tracking-wider mb-3">
            Como conectar
          </p>
          <ol className="flex flex-col gap-2.5">
            {[
              'Clique em "Iniciar conexão" para gerar o QR code.',
              'Abra o WhatsApp no celular do estabelecimento.',
              'Vá em Configurações → Dispositivos conectados → Conectar dispositivo.',
              'Escaneie o QR code exibido na tela.',
            ].map((step, i) => (
              <li key={i} className="flex items-start gap-3 text-sm text-slate-400">
                <span className="flex-shrink-0 flex items-center justify-center h-5 w-5 rounded-full bg-brand-500/20 text-brand-400 text-xs font-semibold">
                  {i + 1}
                </span>
                {step}
              </li>
            ))}
          </ol>
        </div>
      )}
    </div>
  )
}

interface StatItemProps {
  icon: React.ReactNode
  label: string
  value: number | string
  alert?: boolean
}

function StatItem({ icon, label, value, alert = false }: StatItemProps) {
  return (
    <div className="flex items-center gap-3 rounded-xl bg-white/5 p-3">
      <div className="flex-shrink-0">{icon}</div>
      <div className="min-w-0">
        <p className="text-xs text-slate-500 truncate">{label}</p>
        <p className={`text-sm font-semibold ${alert ? 'text-amber-400' : 'text-white'}`}>
          {value}
        </p>
      </div>
    </div>
  )
}
