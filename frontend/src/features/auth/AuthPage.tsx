import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, Navigate } from 'react-router-dom'
import { z } from 'zod'
import { errorMessage } from '../../api/client'
import { Button } from '../../components/Button'
import { ErrorNote } from '../../components/Feedback'
import { SelectField, TextField } from '../../components/Field'
import { homePath, useAuth } from './useAuth'

const loginSchema = z.object({
  email: z.email('Enter a valid email address'),
  password: z.string().min(1, 'Password is required'),
})

const registerSchema = loginSchema.extend({
  name: z.string().trim().min(1, 'Name is required').max(100),
  password: z.string().min(8, 'At least 8 characters'),
  role: z.enum(['Owner', 'Consumer']),
})

type Values = z.infer<typeof registerSchema>

const copy = {
  login: { title: 'Sign in', submit: 'Sign in', other: { to: '/register', text: 'Create an account' } },
  register: { title: 'Create an account', submit: 'Create account', other: { to: '/login', text: 'I already have an account' } },
}

export function AuthPage({ mode }: { mode: 'login' | 'register' }) {
  const { user, login, register: registerAccount } = useAuth()
  const [serverError, setServerError] = useState<string | null>(null)
  const isRegister = mode === 'register'

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<Values>({
    resolver: zodResolver(isRegister ? registerSchema : (loginSchema as unknown as typeof registerSchema)),
    defaultValues: { name: '', email: '', password: '', role: 'Owner' },
  })

  if (user) {
    return <Navigate to={homePath[user.role]} replace />
  }

  const submit = handleSubmit(async (values) => {
    setServerError(null)
    try {
      await (isRegister ? registerAccount(values) : login({ email: values.email, password: values.password }))
    } catch (error) {
      setServerError(errorMessage(error))
    }
  })

  return (
    <div className="flex min-h-screen items-center justify-center p-4">
      <div className="w-full max-w-sm rounded-lg border border-line bg-surface p-6">
        <p className="text-sm font-semibold text-ink-2">API Gateway</p>
        <h1 className="mt-1 mb-5 text-2xl font-semibold text-ink">{copy[mode].title}</h1>

        <form onSubmit={submit} className="flex flex-col gap-4" noValidate>
          {isRegister && <TextField label="Name" autoComplete="name" error={errors.name?.message} {...register('name')} />}
          <TextField label="Email" type="email" autoComplete="email" error={errors.email?.message} {...register('email')} />
          <TextField
            label="Password"
            type="password"
            autoComplete={isRegister ? 'new-password' : 'current-password'}
            error={errors.password?.message}
            {...register('password')}
          />
          {isRegister && (
            <SelectField label="I want to" error={errors.role?.message} {...register('role')}>
              <option value="Owner">Publish APIs (API owner)</option>
              <option value="Consumer">Use APIs (API consumer)</option>
            </SelectField>
          )}

          {serverError && <ErrorNote message={serverError} />}

          <Button type="submit" variant="primary" disabled={isSubmitting}>
            {isSubmitting ? 'Please wait…' : copy[mode].submit}
          </Button>
        </form>

        <p className="mt-4 text-center text-sm">
          <Link to={copy[mode].other.to} className="text-accent hover:underline">
            {copy[mode].other.text}
          </Link>
        </p>
      </div>
    </div>
  )
}
