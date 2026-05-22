import toast from 'react-hot-toast'

type ToastId = string | undefined

/** Mostra um toast de sucesso verde */
function success(message: string, id?: ToastId) {
  return toast.success(message, { id })
}

/** Mostra um toast de erro vermelho com a mensagem da API ou fallback */
function error(message: string, id?: ToastId) {
  return toast.error(message, { id })
}

/** Mostra um toast de loading — retorna o id para fechar depois */
function loading(message: string, id?: ToastId) {
  return toast.loading(message, { id })
}

/** Extrai a mensagem de erro de uma resposta Axios ou usa o fallback */
function apiError(err: unknown, fallback: string): string {
  if (
    err !== null &&
    typeof err === 'object' &&
    'response' in err &&
    err.response !== null &&
    typeof err.response === 'object' &&
    'data' in err.response &&
    err.response.data !== null &&
    typeof err.response.data === 'object' &&
    'message' in err.response.data &&
    typeof err.response.data.message === 'string'
  ) {
    return err.response.data.message
  }
  return fallback
}

/** Fecha um toast pelo id */
function dismiss(id?: ToastId) {
  toast.dismiss(id)
}

export const appToast = { success, error, loading, apiError, dismiss }
