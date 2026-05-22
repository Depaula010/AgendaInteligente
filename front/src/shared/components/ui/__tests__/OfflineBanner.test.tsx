import { render, screen, act } from '@testing-library/react'
import { OfflineBanner } from '@/shared/components/ui/OfflineBanner'

function setOnlineStatus(isOnline: boolean) {
  Object.defineProperty(navigator, 'onLine', {
    configurable: true,
    get: () => isOnline,
  })
}

describe('OfflineBanner', () => {
  afterEach(() => {
    setOnlineStatus(true)
  })

  it('renders nothing when online', () => {
    setOnlineStatus(true)
    render(<OfflineBanner />)
    expect(screen.queryByText(/offline/i)).not.toBeInTheDocument()
  })

  it('renders banner when offline', () => {
    setOnlineStatus(false)
    render(<OfflineBanner />)
    expect(screen.getByText(/você está offline/i)).toBeInTheDocument()
  })

  it('shows banner when offline event fires', () => {
    setOnlineStatus(true)
    render(<OfflineBanner />)
    expect(screen.queryByText(/offline/i)).not.toBeInTheDocument()

    act(() => {
      setOnlineStatus(false)
      window.dispatchEvent(new Event('offline'))
    })

    expect(screen.getByText(/você está offline/i)).toBeInTheDocument()
  })

  it('hides banner when online event fires', () => {
    setOnlineStatus(false)
    render(<OfflineBanner />)
    expect(screen.getByText(/você está offline/i)).toBeInTheDocument()

    act(() => {
      setOnlineStatus(true)
      window.dispatchEvent(new Event('online'))
    })

    expect(screen.queryByText(/offline/i)).not.toBeInTheDocument()
  })

  it('removes event listeners on unmount', () => {
    setOnlineStatus(true)
    const removeEventListener = vi.spyOn(window, 'removeEventListener')
    const { unmount } = render(<OfflineBanner />)
    unmount()
    expect(removeEventListener).toHaveBeenCalledWith('online', expect.any(Function))
    expect(removeEventListener).toHaveBeenCalledWith('offline', expect.any(Function))
    removeEventListener.mockRestore()
  })
})
