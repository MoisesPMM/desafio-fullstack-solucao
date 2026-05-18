export type FonteClima = 'Simulado' | 'OpenWeatherMap'

export interface TemperaturaResponse {
  id: number
  cidade: string
  latitude: number | null
  longitude: number | null
  temperaturaCelsius: number
  descricaoTempo: string
  registradoEm: string
}

export interface LoginResponse {
  token: string
  expiraEm: string
  tipo: string
}

export interface CadastroResponse {
  id: number
  usuario: string
  criadoEm: string
}

export class WeatherApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
  ) {
    super(message)
    this.name = 'WeatherApiError'
  }
}

const API_BASE = (import.meta.env.VITE_API_URL ?? '').replace(/\/$/, '')
const TOKEN_KEY = 'weatherapp.jwt'
const LOGIN_PATH = '/api/auth/login'
const CADASTRO_PATH = '/api/auth/cadastro'

export function setAuthToken(token: string) {
  localStorage.setItem(TOKEN_KEY, token)
}

export function clearAuthToken() {
  localStorage.removeItem(TOKEN_KEY)
}

function getAuthToken() {
  return localStorage.getItem(TOKEN_KEY)
}

export function isAuthenticated() {
  return Boolean(getAuthToken())
}

async function parseErrorMessage(response: Response, fallback: string) {
  const contentType = response.headers.get('content-type') ?? ''

  if (contentType.includes('application/json')) {
    const body = (await response.json().catch(() => null)) as { mensagem?: string; message?: string; title?: string } | null
    return body?.mensagem ?? body?.message ?? body?.title ?? fallback
  }

  const body = await response.text().catch(() => '')
  return body || fallback
}

async function login(usuario: string, senha: string): Promise<LoginResponse> {
  const response = await request<LoginResponse>(LOGIN_PATH, {
    method: 'POST',
    body: JSON.stringify({ usuario, senha }),
  })

  setAuthToken(response.token)
  return response
}

async function cadastrar(usuario: string, senha: string): Promise<CadastroResponse> {
  return request<CadastroResponse>(CADASTRO_PATH, {
    method: 'POST',
    body: JSON.stringify({ usuario, senha }),
  })
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const requiresAuth = path !== LOGIN_PATH && path !== CADASTRO_PATH

  const token = getAuthToken()

  if (requiresAuth && !token) {
    throw new WeatherApiError('Faça login para acessar este recurso.', 401)
  }

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(init?.headers as Record<string, string> | undefined),
  }

  if (token) {
    headers.Authorization = `Bearer ${token}`
  }

  const response = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers,
  })

  if (response.status === 401 && requiresAuth) {
    clearAuthToken()
  }

  if (!response.ok) {
    const message = await parseErrorMessage(response, `Erro ${response.status} ao comunicar com a API.`)
    throw new WeatherApiError(message, response.status)
  }

  return response.json() as Promise<T>
}

export const weatherService = {
  login,
  cadastrar,
  isAuthenticated,
  clearAuthToken,

  registrarPorCidade(cidade: string, fonte?: FonteClima): Promise<TemperaturaResponse> {
    return request<TemperaturaResponse>('/api/clima/cidade', {
      method: 'POST',
      body: JSON.stringify({ cidade, fonte }),
    })
  },

  registrarPorCoordenadas(latitude: number, longitude: number, fonte?: FonteClima): Promise<TemperaturaResponse> {
    return request<TemperaturaResponse>('/api/clima/coordenadas', {
      method: 'POST',
      body: JSON.stringify({ latitude, longitude, fonte }),
    })
  },

  obterCidadesRegistradas(): Promise<string[]> {
    return request<string[]>('/api/clima/cidades')
  },

  obterHistoricoPorCidade(cidade: string): Promise<TemperaturaResponse[]> {
    const cidadeCodificada = encodeURIComponent(cidade)

    return request<TemperaturaResponse[]>(`/api/clima/historico/cidade/${cidadeCodificada}`)
  },

  obterHistoricoPorCoordenadas(latitude: number, longitude: number): Promise<TemperaturaResponse[]> {
    const params = new URLSearchParams({
      latitude: String(latitude),
      longitude: String(longitude),
    })

    return request<TemperaturaResponse[]>(`/api/clima/historico/coordenadas?${params.toString()}`)
  },
}
