import { cn } from '@/shared/utils/cn'

interface CardProps {
  children: React.ReactNode
  className?: string
  as?: React.ElementType
  onClick?: () => void
}

export function Card({ children, className, as: Tag = 'div', onClick }: CardProps) {
  return (
    <Tag
      onClick={onClick}
      className={cn(
        'rounded-2xl border border-white/10 bg-white/5 backdrop-blur-sm shadow-glass',
        onClick && 'cursor-pointer hover:border-white/20 hover:bg-white/8 transition-all duration-200',
        className,
      )}
    >
      {children}
    </Tag>
  )
}

interface CardHeaderProps { children: React.ReactNode; className?: string }
interface CardBodyProps   { children: React.ReactNode; className?: string }
interface CardFooterProps { children: React.ReactNode; className?: string }

export function CardHeader({ children, className }: CardHeaderProps) {
  return <div className={cn('px-5 pt-5 pb-3', className)}>{children}</div>
}

export function CardBody({ children, className }: CardBodyProps) {
  return <div className={cn('px-5 py-3', className)}>{children}</div>
}

export function CardFooter({ children, className }: CardFooterProps) {
  return (
    <div className={cn('px-5 pt-3 pb-5 border-t border-white/10', className)}>{children}</div>
  )
}
