# WeatherApp — Desafio Fullstack

Aplicação full-stack para registro e consulta de temperaturas por cidade ou coordenadas geográficas.

**Stack:** .NET 8 (C#) · Vue 3 + TypeScript · PostgreSQL · Docker

## Visão geral

O projeto entrega uma API em .NET, um frontend em Vue e um banco PostgreSQL. A forma recomendada de executar a aplicação é **somente com Docker Compose**, sem instalar dependências do backend ou frontend na máquina.

A solução original foi feita em outro repositório no meu Perfil do github, localizado aqui `https://github.com/MoisesPMM/Desafio-FullStack`
Devido a alguns problemas na hora de fazer o PR, eu fiz um fork limpo e subi os arquivos, mas nesse projeto mostra o histórico de commits que são cerca de 35 ou 40, e a evolução do projeto


| Serviço | Porta local | Descrição |
|---|---:|---|
| Frontend | `3000` | Interface web para autenticação, cadastro e consulta de temperaturas |
| API | `8080` | API REST em .NET 8 |
| PostgreSQL | `5432` | Banco de dados usado pela API |

---

## Pré-requisitos

- Docker
- Docker Compose
- .NET SDK 8 somente se você quiser executar os testes diretamente na máquina com `dotnet test tests/`

> Para subir a aplicação, apenas Docker e Docker Compose são necessários.

## Imagens Docker Hub:

### API: https://hub.docker.com/r/moisespedro/desafioappclima-api
### Frontend: https://hub.docker.com/r/moisespedro/desafioappclima-frontend

## Rodando a aplicação apenas com Docker

### 1. Clone o repositório e acesse a pasta

```bash
git clone <url-do-repositorio>
cd Desafio
```

### 2. Configure as variáveis de ambiente (obrigatório)

Esse passo é muito importante, pois sem um .env na raiz do projeto contendo as variaveis como `JWT_SENHA` E `JWT_KEY`, o docker não vai conseguir subir as imagens, alegando erro de dependencias.

O `docker-compose.yml` já possui valores padrão para subir a aplicação com o provedor simulado de clima (`USE_FAKE_PROVIDER=true`). Assim, é possível testar o projeto sem chave externa da OpenWeatherMap.

`Observação:` Caso tente utilizar a função de consulta de clima tempo com o OpenWeatherAPI sem a chave, vai retornar erro de comunicação com o servidor/api.

Se quiser personalizar credenciais ou usar dados reais da OpenWeatherMap, crie um arquivo `.env` na raiz do projeto:

```bash
cp .env.example .env
```

Exemplo de `.env` para continuar usando dados simulados:

```env
WEATHER_API_KEY=fake
USE_FAKE_PROVIDER=true
JWT_KEY=sua_chave_jwt_com_32_bytes_ou_mais
JWT_USUARIO=admin
JWT_SENHA=senha_segura_para_jwt32bytes
```

Exemplo de `.env` para usar OpenWeatherMap:

```env
WEATHER_API_KEY=sua_chave_openweathermap
USE_FAKE_PROVIDER=false
JWT_KEY=sua_chave_jwt_com_32_bytes_ou_mais
JWT_USUARIO=admin
JWT_SENHA=senha_segura_para_jwt32bytes
```

> Obtenha uma API key gratuita em: https://openweathermap.org/api

### 3. Suba os containers

```bash
docker compose up -d
```

Esse comando inicia:

- `db`: PostgreSQL 16
- `api`: backend publicado em imagem Docker
- `frontend`: aplicação Vue servida pelo Nginx

### 4. Acompanhe a inicialização

```bash
docker compose ps
docker compose logs -f api
```

A API aplica/cria a estrutura do banco automaticamente durante a inicialização.

### 5. Acesse a aplicação

| Recurso | URL |
|---|---|
| Frontend | http://localhost:3000 |
| API | http://localhost:8080 |
| Swagger | http://localhost:8080/swagger |
| Health Check | http://localhost:8080/health |

### 6. Parar a aplicação

```bash
docker compose down
```

Para remover também o volume do PostgreSQL e limpar os dados salvos:

```bash
docker compose down -v
```

---

## Autenticação

Os endpoints de clima exigem JWT. Pelo frontend, o login usa as credenciais configuradas nas variáveis:

- `JWT_USUARIO`
- `JWT_SENHA`

Também é possível usar a API diretamente:

1. Criar/cadastrar credenciais em `POST /api/auth/cadastro`.
2. Autenticar em `POST /api/auth/login`.
3. Enviar o token retornado no header `Authorization: Bearer <token>`.

---

## Fonte da temperatura por registro

Além da configuração global `USE_FAKE_PROVIDER`, o dashboard e a API aceitam a fonte desejada em cada novo registro.

Exemplo para registrar temperatura por cidade usando OpenWeatherMap:

```json
{
  "cidade": "Cascavel",
  "fonte": "OpenWeatherMap"
}
```

Valores aceitos para `fonte`:

- `Simulado`: usa o provedor fake, sem chave externa.
- `OpenWeatherMap`: consulta dados reais na OpenWeatherMap.

Se `fonte` não for enviada, a API usa o padrão definido por `FeatureFlags:UseFakeProvider` / `USE_FAKE_PROVIDER`.

---

## Testes

Para executar a suíte de testes a partir da raiz do repositório:

```bash
dotnet test tests/
```

Se você quiser manter a execução também dentro de Docker, use a imagem do SDK .NET e execute o mesmo comando dentro do container:

```bash
docker run --rm \
  -v "$PWD:/workspace" \
  -w /workspace \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  dotnet test tests/
```

---

## Arquitetura

```text
backend/src/         (Padrão comum de desenvolvimento em c#)
├── Domain/          # Entidades e interfaces, sem dependências externas
├── Application/     # Casos de uso, DTOs e contratos de serviço
├── Infrastructure/  # EF Core, repositórios e providers de clima
└── Api/             # Controllers, Program.cs, Swagger e filtros

tests/
├── Unit/            # Testes unitários dos serviços
└── Integration/     # Testes de API com WebApplicationFactory

frontend/
└── src/
    ├── components/  # Componentes Vue
    ├── services/    # Cliente HTTP da API
    └── App.vue      # Componente principal
```

### Decisões de design

- **Clean Architecture** com separação entre domínio, aplicação, infraestrutura e API.
- **Docker Compose como caminho principal de execução**, orquestrando frontend, API e banco de dados.
- **Feature Flag** para definir o provedor padrão de clima sem recompilação (`FeatureFlags:UseFakeProvider`).
- **JWT Bearer** para proteger endpoints de clima.
- **BruteForceFilter** aplicado aos registros de clima sem autenticação, bloqueando excesso de tentativas em um endpoint, teria sido interessante em outras funcionalidades adicionais ou novas, somando com as que foram propostas no desafio.
- **Inicialização automática do banco**, aplicando migrations quando existirem ou criando o schema automaticamente.
- **FakeWeatherProvider** como padrão para permitir execução sem API key externa, mas se tiver uma chave ele também funciona com a comunicação com o OpenWeatherAPI.
- **Seleção por requisição** para registrar temperatura com `fonte: "Simulado"` ou `fonte: "OpenWeatherMap"`.

---

## Endpoints principais

| Método | Rota | Descrição |
|---|---|---|
| POST | `/api/auth/cadastro` | Cadastra usuário/senha para acesso ao sistema |
| POST | `/api/auth/login` | Autentica usuário/senha e retorna JWT |
| POST | `/api/clima/cidade` | Registra temperatura por cidade. Aceita `fonte` opcional: `Simulado` ou `OpenWeatherMap` |
| POST | `/api/clima/coordenadas` | Registra temperatura por latitude/longitude. Aceita `fonte` opcional: `Simulado` ou `OpenWeatherMap` |
| GET | `/api/clima/cidades` | Lista cidades com registros salvos para reutilização no dashboard |
| GET | `/api/clima/historico/cidade/{cidade}` | Retorna histórico por cidade |
| GET | `/api/clima/historico/coordenadas?latitude=&longitude=` | Retorna histórico por coordenadas |
| GET | `/health` | Health check da API |
| GET | `/swagger` | Documentação interativa da API |

---

## Solução de problemas

### Porta já está em uso

Se alguma porta estiver ocupada, pare o processo local ou ajuste o mapeamento no `docker-compose.yml`.

### API não ficou saudável

Verifique os logs:

```bash
docker compose logs api
docker compose logs db
```

### Quero resetar o banco

```bash
docker compose down -v
docker compose up -d
```
