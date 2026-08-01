import { fireEvent, render, screen } from '@testing-library/react'
import App from './App'

describe('App', () => {
  it('renders conversion form fields', () => {
    render(<App />)
    expect(screen.getByText('Real-Time Currency Conversion')).toBeInTheDocument()
    expect(screen.getByText('Amount')).toBeInTheDocument()
    expect(screen.getByText('From')).toBeInTheDocument()
    expect(screen.getByText('To')).toBeInTheDocument()
  })

  it('handles amount input', () => {
    render(<App />)
    const amountInput = screen.getByPlaceholderText('100.00') as HTMLInputElement
    fireEvent.change(amountInput, { target: { value: '12.34' } })
    expect(amountInput.value).toBe('12.34')
  })
})
