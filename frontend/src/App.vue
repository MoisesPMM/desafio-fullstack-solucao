<template>
  <div class="app-shell">
    <header class="hero">
      <div>
        <span class="eyebrow">Desafio FullStack</span>
        <h1>WeatherApp</h1>
        <p>Registre temperaturas por cidade ou coordenadas e acompanhe o histórico dos últimos 30 dias.</p>
      </div>
      <div class="hero-actions">
        <button v-if="autenticado" class="logout-button" type="button" @click="sair">Sair</button>
        <span v-else class="auth-status">Não autenticado</span>
        <div class="hero-card" aria-label="Resumo do último registro">
          <span>Último registro</span>
          <strong>{{ ultimoResultado ? formatarTemperatura(ultimoResultado.temperaturaCelsius) : '--°C' }}</strong>
          <small>{{ ultimoResultado?.cidade ?? 'Nenhuma consulta ainda' }}</small>
        </div>
      </div>
    </header>

    <section v-if="!autenticado" class="login-panel" aria-labelledby="login-title">
      <div>
        <span class="eyebrow">Acesso necessário</span>
        <h2 id="login-title">Entre para usar o dashboard</h2>
        <p>Você pode visualizar a tela principal, mas precisa fazer login para registrar temperaturas ou consultar histórico.</p>
      </div>

      <form class="form-grid login-form" @submit.prevent="modoAuth === 'login' ? fazerLogin() : fazerCadastro()">
        <div class="segmented-control auth-mode" role="tablist" aria-label="Tipo de acesso">
          <button :class="{ active: modoAuth === 'login' }" type="button" @click="alternarModoAuth('login')">
            Entrar
          </button>
          <button :class="{ active: modoAuth === 'cadastro' }" type="button" @click="alternarModoAuth('cadastro')">
            Cadastrar
          </button>
        </div>
        <label>
          Usuário
          <input v-model="usuario" type="text" placeholder="Digite seu usuário" autocomplete="username" />
        </label>
        <label>
          Senha
          <input v-model="senha" type="password" placeholder="Digite sua senha" autocomplete="current-password" />
        </label>
        <button class="primary-button" type="submit" :disabled="carregandoAuth || !usuario.trim() || !senha">
          {{ textoBotaoAuth }}
        </button>
      </form>

      <p v-if="mensagemCadastro" class="login-success" role="status">{{ mensagemCadastro }}</p>
      <p v-if="erroLogin" class="login-error" role="alert">{{ erroLogin }}</p>
    </section>

    <main class="dashboard" :class="{ 'dashboard-locked': !autenticado }">
      <section class="panel register-panel" aria-labelledby="register-title">
        <div class="section-heading">
          <span class="icon">🌤️</span>
          <div>
            <h2 id="register-title">Registrar temperatura</h2>
            <p>Escolha como deseja consultar o clima atual.</p>
          </div>
        </div>

        <p v-if="!autenticado" class="auth-warning" role="status">
          Faça login para habilitar o método de registrar cidade ou coordenadas.
        </p>

        <div class="source-selector">
          <span>Fonte dos dados</span>
          <div class="segmented-control" role="tablist" aria-label="Fonte dos dados climáticos">
            <button :class="{ active: fonteSelecionada === 'Simulado' }" type="button" @click="fonteSelecionada = 'Simulado'">
              Simulado
            </button>
            <button :class="{ active: fonteSelecionada === 'OpenWeatherMap' }" type="button" @click="fonteSelecionada = 'OpenWeatherMap'">
              OpenWeatherMap
            </button>
          </div>
          <small>Use o modo simulado para testes sem chave externa ou OpenWeatherMap para registrar dados reais.</small>
        </div>

        <div class="segmented-control" role="tablist" aria-label="Tipo de registro">
          <button :class="{ active: modoRegistro === 'cidade' }" type="button" @click="modoRegistro = 'cidade'">
            Cidade
          </button>
          <button :class="{ active: modoRegistro === 'coordenadas' }" type="button" @click="modoRegistro = 'coordenadas'">
            Coordenadas
          </button>
        </div>

        <form v-if="modoRegistro === 'cidade'" class="form-grid" @submit.prevent="registrarPorCidade">
          <label>
            Cidade
            <input v-model="cidadeInput" type="text" placeholder="Ex.: Cascavel" autocomplete="address-level2" />
          </label>
          <button class="primary-button" type="submit" :disabled="!autenticado || carregandoRegistro || !cidadeInput.trim()">
            {{ carregandoRegistro ? 'Registrando...' : 'Registrar cidade' }}
          </button>
        </form>

        <form v-else class="form-grid two-columns" @submit.prevent="registrarPorCoordenadas">
          <label>
            Latitude
            <input v-model.number="latitudeInput" type="number" step="0.000001" min="-90" max="90" placeholder="-24.9555" />
          </label>
          <label>
            Longitude
            <input v-model.number="longitudeInput" type="number" step="0.000001" min="-180" max="180" placeholder="-53.4552" />
          </label>
          <button class="primary-button full-width" type="submit" :disabled="!autenticado || carregandoRegistro || !temCoordenadasValidas">
            {{ carregandoRegistro ? 'Registrando...' : 'Registrar coordenadas' }}
          </button>
        </form>

        <article v-if="ultimoResultado" class="weather-result">
          <div>
            <span class="result-label">{{ ultimoResultado.cidade }}</span>
            <strong>{{ formatarTemperatura(ultimoResultado.temperaturaCelsius) }}</strong>
          </div>
          <div>
            <span>{{ ultimoResultado.descricaoTempo }}</span>
            <small>{{ formatarData(ultimoResultado.registradoEm) }}</small>
          </div>
        </article>
      </section>

      <section class="panel history-panel" aria-labelledby="historico-title">
        <div class="section-heading">
          <span class="icon">📈</span>
          <div>
            <h2 id="historico-title">Histórico</h2>
            <p>Consulte dados registrados recentemente.</p>
          </div>
        </div>

        <p v-if="!autenticado" class="auth-warning" role="status">
          Faça login para consultar o histórico de temperaturas.
        </p>

        <SavedCities
          :cidades-registradas="cidadesRegistradas"
          :carregando-cidades="carregandoCidades"
          @selecionar-cidade="selecionarCidade"
        />

        <div class="segmented-control" role="tablist" aria-label="Tipo de histórico">
          <button :class="{ active: modoHistorico === 'cidade' }" type="button" @click="modoHistorico = 'cidade'">
            Cidade
          </button>
          <button :class="{ active: modoHistorico === 'coordenadas' }" type="button" @click="modoHistorico = 'coordenadas'">
            Coordenadas
          </button>
        </div>

        <form v-if="modoHistorico === 'cidade'" class="form-grid" @submit.prevent="carregarHistoricoPorCidade">
          <label>
            Cidade
            <input v-model="historicoCidade" type="text" placeholder="Ex.: Cascavel" autocomplete="address-level2" />
          </label>
          <button class="secondary-button" type="submit" :disabled="!autenticado || carregandoHistorico || !historicoCidade.trim()">
            {{ carregandoHistorico ? 'Consultando...' : 'Consultar histórico' }}
          </button>
        </form>

        <form v-else class="form-grid two-columns" @submit.prevent="carregarHistoricoPorCoordenadas">
          <label>
            Latitude
            <input v-model.number="historicoLatitude" type="number" step="0.000001" min="-90" max="90" placeholder="-24.9555" />
          </label>
          <label>
            Longitude
            <input v-model.number="historicoLongitude" type="number" step="0.000001" min="-180" max="180" placeholder="-53.4552" />
          </label>
          <button class="secondary-button full-width" type="submit" :disabled="!autenticado || carregandoHistorico || !temCoordenadasHistoricoValidas">
            {{ carregandoHistorico ? 'Consultando...' : 'Consultar coordenadas' }}
          </button>
        </form>

        <div v-if="historico.length" class="history-content">
          <div class="chart-card">
            <canvas ref="chartCanvas" aria-label="Gráfico de temperaturas" role="img"></canvas>
          </div>

          <div class="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>Data/Hora</th>
                  <th>Local</th>
                  <th>Temperatura</th>
                  <th>Descrição</th>
                </tr>
              </thead>
              <tbody>
                <tr
                  v-for="record in historico"
                  :key="record.id"
                  class="clickable-row"
                  tabindex="0"
                  title="Clique para consultar novamente esta cidade"
                  @click="selecionarRegistro(record)"
                  @keyup.enter="selecionarRegistro(record)"
                >
                  <td>{{ formatarData(record.registradoEm) }}</td>
                  <td>{{ record.cidade }}</td>
                  <td>{{ formatarTemperatura(record.temperaturaCelsius) }}</td>
                  <td>{{ record.descricaoTempo }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <div v-else-if="buscou" class="empty-state">
          <strong>Nenhum registro encontrado</strong>
          <span>Faça um novo registro e consulte novamente.</span>
        </div>
      </section>
    </main>

    <p v-if="erro" class="toast" role="alert">{{ erro }}</p>
  </div>
</template>

<script setup lang="ts">
import Chart from 'chart.js/auto'
import { computed, nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import SavedCities from './components/SavedCities.vue'
import { WeatherApiError, type FonteClima, type TemperaturaResponse, weatherService } from './services/weatherService'

type ModoBusca = 'cidade' | 'coordenadas'
type ModoAuth = 'login' | 'cadastro'

const modoRegistro = ref<ModoBusca>('cidade')
const modoHistorico = ref<ModoBusca>('cidade')
const fonteSelecionada = ref<FonteClima>('Simulado')
const cidadeInput = ref('')
const historicoCidade = ref('')
const latitudeInput = ref<number | null>(null)
const longitudeInput = ref<number | null>(null)
const historicoLatitude = ref<number | null>(null)
const historicoLongitude = ref<number | null>(null)
const autenticado = ref(weatherService.isAuthenticated())
const modoAuth = ref<ModoAuth>('login')
const usuario = ref('')
const senha = ref('')
const carregandoLogin = ref(false)
const carregandoCadastro = ref(false)
const erroLogin = ref('')
const mensagemCadastro = ref('')
const carregandoRegistro = ref(false)
const carregandoHistorico = ref(false)
const ultimoResultado = ref<TemperaturaResponse | null>(null)
const historico = ref<TemperaturaResponse[]>([])
const cidadesRegistradas = ref<string[]>([])
const carregandoCidades = ref(false)
const erro = ref('')
const buscou = ref(false)
const chartCanvas = ref<HTMLCanvasElement | null>(null)
let chartInstance: Chart | null = null

const temCoordenadasValidas = computed(() => coordenadaValida(latitudeInput.value, -90, 90) && coordenadaValida(longitudeInput.value, -180, 180))
const temCoordenadasHistoricoValidas = computed(
  () => coordenadaValida(historicoLatitude.value, -90, 90) && coordenadaValida(historicoLongitude.value, -180, 180),
)
const carregandoAuth = computed(() => carregandoLogin.value || carregandoCadastro.value)
const textoBotaoAuth = computed(() => {
  if (carregandoLogin.value) return 'Entrando...'
  if (carregandoCadastro.value) return 'Cadastrando...'

  return modoAuth.value === 'login' ? 'Entrar' : 'Cadastrar'
})

function alternarModoAuth(modo: ModoAuth) {
  modoAuth.value = modo
  erroLogin.value = ''
  mensagemCadastro.value = ''
}

async function fazerLogin() {
  const usuarioInformado = usuario.value.trim()
  if (!usuarioInformado || !senha.value) return

  carregandoLogin.value = true
  erroLogin.value = ''
  mensagemCadastro.value = ''

  try {
    await weatherService.login(usuarioInformado, senha.value)
    autenticado.value = true
    usuario.value = ''
    senha.value = ''
    erro.value = ''
    await carregarCidadesRegistradas()
  } catch (exception) {
    erroLogin.value = obterMensagemErroLogin(exception)
  } finally {
    carregandoLogin.value = false
  }
}

async function fazerCadastro() {
  const usuarioInformado = usuario.value.trim()
  if (!usuarioInformado || !senha.value) return

  carregandoCadastro.value = true
  erroLogin.value = ''
  mensagemCadastro.value = ''

  try {
    await weatherService.cadastrar(usuarioInformado, senha.value)
    mensagemCadastro.value = 'Cadastro realizado com sucesso. Você já pode entrar.'
    modoAuth.value = 'login'
    senha.value = ''
  } catch (exception) {
    erroLogin.value = obterMensagemErro(exception, 'Não foi possível realizar o cadastro agora.')
  } finally {
    carregandoCadastro.value = false
  }
}

function sair() {
  weatherService.clearAuthToken()
  autenticado.value = false
  usuario.value = ''
  senha.value = ''
  erroLogin.value = ''
  mensagemCadastro.value = ''
  modoAuth.value = 'login'
  limparDadosDashboard()
}

function limparDadosDashboard() {
  cidadeInput.value = ''
  historicoCidade.value = ''
  latitudeInput.value = null
  longitudeInput.value = null
  historicoLatitude.value = null
  historicoLongitude.value = null
  ultimoResultado.value = null
  historico.value = []
  cidadesRegistradas.value = []
  erro.value = ''
  buscou.value = false
  chartInstance?.destroy()
  chartInstance = null
}

function tratarErroOperacao(exception: unknown) {
  const mensagem = obterMensagemErro(exception)

  if (exception instanceof WeatherApiError && exception.status === 401) {
    sair()
    erroLogin.value = mensagem
    return
  }

  erro.value = mensagem
}

function garantirAutenticacao(acao: string) {
  if (autenticado.value) return true

  erro.value = ''
  erroLogin.value = `Faça login para ${acao}.`
  return false
}

async function registrarPorCidade() {
  if (!garantirAutenticacao('registrar uma cidade')) return

  const cidade = cidadeInput.value.trim()
  if (!cidade) return

  const resultado = await registrarTemperatura(() => weatherService.registrarPorCidade(cidade, fonteSelecionada.value))
  if (!resultado) return

  const cidadeRegistrada = resultado.cidade || cidade
  cidadeInput.value = cidadeRegistrada
  historicoCidade.value = cidadeRegistrada
  modoHistorico.value = 'cidade'
  await carregarCidadesRegistradas()
  await carregarHistorico(() => weatherService.obterHistoricoPorCidade(cidadeRegistrada))
}

async function registrarPorCoordenadas() {
  if (!garantirAutenticacao('registrar coordenadas')) return

  if (!temCoordenadasValidas.value || latitudeInput.value === null || longitudeInput.value === null) return

  const latitude = latitudeInput.value
  const longitude = longitudeInput.value
  const resultado = await registrarTemperatura(() => weatherService.registrarPorCoordenadas(latitude!, longitude!, fonteSelecionada.value))
  if (!resultado) return

  historicoLatitude.value = latitude
  historicoLongitude.value = longitude
  modoHistorico.value = 'coordenadas'
  await carregarCidadesRegistradas()
  await carregarHistorico(() => weatherService.obterHistoricoPorCoordenadas(latitude!, longitude!))
}

async function registrarTemperatura(action: () => Promise<TemperaturaResponse>) {
  carregandoRegistro.value = true
  erro.value = ''

  try {
    ultimoResultado.value = await action()
    return ultimoResultado.value
  } catch (exception) {
    tratarErroOperacao(exception)
    return null
  } finally {
    carregandoRegistro.value = false
  }
}


async function carregarCidadesRegistradas() {
  if (!autenticado.value) return

  carregandoCidades.value = true

  try {
    cidadesRegistradas.value = await weatherService.obterCidadesRegistradas()
  } catch (exception) {
    tratarErroOperacao(exception)
  } finally {
    carregandoCidades.value = false
  }
}

async function selecionarCidade(cidade: string) {
  if (!cidade) return

  cidadeInput.value = cidade
  historicoCidade.value = cidade
  modoHistorico.value = 'cidade'
  await carregarHistoricoPorCidade()
}

async function selecionarRegistro(record: TemperaturaResponse) {
  await selecionarCidade(record.cidade)
}

async function carregarHistoricoPorCidade() {
  if (!garantirAutenticacao('consultar o histórico por cidade')) return

  const cidade = historicoCidade.value.trim()
  if (!cidade) return

  await carregarHistorico(() => weatherService.obterHistoricoPorCidade(cidade))
}

async function carregarHistoricoPorCoordenadas() {
  if (!garantirAutenticacao('consultar o histórico por coordenadas')) return

  if (!temCoordenadasHistoricoValidas.value || historicoLatitude.value === null || historicoLongitude.value === null) return

  await carregarHistorico(() => weatherService.obterHistoricoPorCoordenadas(historicoLatitude.value!, historicoLongitude.value!))
}

async function carregarHistorico(action: () => Promise<TemperaturaResponse[]>) {
  carregandoHistorico.value = true
  buscou.value = false
  erro.value = ''

  try {
    historico.value = await action()
    buscou.value = true
    await nextTick()
    renderizarGrafico()
  } catch (exception) {
    tratarErroOperacao(exception)
    historico.value = []
    chartInstance?.destroy()
    chartInstance = null
  } finally {
    carregandoHistorico.value = false
  }
}

function renderizarGrafico() {
  if (!chartCanvas.value || !historico.value.length) return

  chartInstance?.destroy()

  const orderedHistory = [...historico.value].reverse()

  chartInstance = new Chart(chartCanvas.value, {
    type: 'line',
    data: {
      labels: orderedHistory.map((record) => formatarData(record.registradoEm)),
      datasets: [
        {
          label: 'Temperatura (°C)',
          data: orderedHistory.map((record) => record.temperaturaCelsius),
          borderColor: '#2563eb',
          backgroundColor: 'rgba(37, 99, 235, 0.14)',
          pointBackgroundColor: '#0f172a',
          pointRadius: 4,
          tension: 0.35,
          fill: true,
        },
      ],
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          display: false,
        },
      },
      scales: {
        y: {
          ticks: {
            callback: (value) => `${value}°C`,
          },
        },
      },
    },
  })
}

function formatarData(iso: string) {
  return new Intl.DateTimeFormat('pt-BR', {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(new Date(iso))
}

function formatarTemperatura(temperature: number) {
  return `${temperature.toFixed(1)}°C`
}

function coordenadaValida(value: number | null, min: number, max: number) {
  return typeof value === 'number' && Number.isFinite(value) && value >= min && value <= max
}

function obterMensagemErroLogin(exception: unknown) {
  if (exception instanceof WeatherApiError && exception.status === 401) {
    return 'Usuário ou senha inválidos. Verifique suas credenciais e tente novamente.'
  }

  return obterMensagemErro(exception, 'Não foi possível fazer login agora. Tente novamente em instantes.')
}

function obterMensagemErro(exception: unknown, fallback = 'Não foi possível concluir a operação.') {
  if (exception instanceof WeatherApiError) {
    if (exception.status === 401) {
      return 'Sua sessão expirou ou você não está autenticado. Faça login para continuar.'
    }

    if (exception.status >= 500) {
      return 'A API está indisponível no momento. Tente novamente em instantes.'
    }

    return exception.message || fallback
  }

  if (exception instanceof TypeError) {
    return 'Não foi possível conectar à API. Verifique sua conexão ou a URL configurada.'
  }

  return exception instanceof Error && exception.message ? exception.message : fallback
}

onMounted(() => {
  if (autenticado.value) {
    void carregarCidadesRegistradas()
  }
})

onBeforeUnmount(() => {
  chartInstance?.destroy()
})
</script>
