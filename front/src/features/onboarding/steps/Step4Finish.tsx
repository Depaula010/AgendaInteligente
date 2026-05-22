import { useNavigate } from 'react-router-dom'
import { CheckCircle2 } from 'lucide-react'

import { Button } from '@/shared/components/ui/Button'
import { ROUTES } from '@/app/routes'

interface Step4Props {
  tenantName: string
}

export function Step4Finish({ tenantName }: Step4Props) {
  const navigate = useNavigate()

  return (
    <div className="flex flex-col items-center text-center gap-6">
      <div className="flex items-center justify-center h-16 w-16 rounded-full bg-green-500/20 border border-green-500/30">
        <CheckCircle2 className="h-8 w-8 text-green-400" aria-hidden="true" />
      </div>

      <div>
        <h3 className="font-display text-lg font-bold text-white">Tudo pronto!</h3>
        <p className="text-sm text-slate-400 mt-2">
          <span className="text-white font-medium">{tenantName}</span> está configurada e pronta
          para receber agendamentos.
        </p>
      </div>

      <ul className="w-full text-left flex flex-col gap-2">
        {[
          'Conta criada com sucesso',
          'Acesso ao painel liberado',
          'Agenda disponível para clientes',
        ].map((item) => (
          <li key={item} className="flex items-center gap-2 text-sm text-slate-400">
            <CheckCircle2 className="h-4 w-4 text-green-400 flex-shrink-0" aria-hidden="true" />
            {item}
          </li>
        ))}
      </ul>

      <Button
        className="w-full mt-2"
        onClick={() => navigate(ROUTES.DASHBOARD, { replace: true })}
      >
        Ir para o painel
      </Button>
    </div>
  )
}
