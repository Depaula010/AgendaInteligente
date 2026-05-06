# Acompanhamento do Projeto: Agenda Inteligente (SaaS)

Este documento serve para rastrear o progresso do desenvolvimento da plataforma, registrando o que está sendo feito no momento, o que já foi concluído e os próximos passos do roadmap.

---

## 🚧 1. Etapa Atual em Desenvolvimento

(Aguardando próximo comando do roadmap)

---

## ✅ 2. O que já foi desenvolvido

* [x] Definição da Arquitetura Geral do Sistema (Backend, Node.js WhatsApp, Gemini IA, PWA).
* [x] Elaboração das Diretrizes e Documentações (`BACKEND_GUIDELINES.md`, `ARCHITECTURE.md`, `BUSINESS_RULES.md`, `SECURITY_GUIDELINES.md`, etc.).
* [x] Configuração do Setup Inicial do Backend (.NET 8/9) e Clean Architecture (pastas Domain, Data, Services, Repositories, Endpoints).
* [x] Suporte base a Multi-Tenant (provedor de contexto via HTTP, interface).
* [x] Criação de Entidades e configurações Iniciais (`Tenant`, `Customer`).
* [x] Endpoints e Serviços para controle de Tenants.
* [x] **Criação dos Modelos de Domínio restantes:**
  * [x] Enums: `ScheduleStatus`, `ProfessionalRole`, `WaitlistStatus`.
  * [x] Entidade `Professional` (Equipe — Staff/Owner, com cor de calendário e hash de senha).
  * [x] Entidade `Service` (Serviços — duração, preço, cor de calendário).
  * [x] Entidade `Schedule` (Agendamentos — recorrência via RRULE, sincronização com Google Calendar).
  * [x] Entidade `TenantSettings` (Configurações — horários em JSON, lembretes, reengajamento).
  * [x] Entidade `Waitlist` (Lista de Espera inteligente — notificação e conversão em agendamento).
  * [x] Configurações EF Core (`IEntityTypeConfiguration`) para todas as novas entidades.
  * [x] `AppDbContext` atualizado com novos `DbSet<T>` e `HasQueryFilter` (isolamento multi-tenant).
  * [x] Correção do `using Xunit;` faltante no `TenantServiceTests.cs` (build da solution: 0 erros).
* [x] **Migrations Iniciais do Entity Framework Core:**
  * [x] `AppDbContextFactory` atualizada para ler `appsettings.Development.json` (sem hardcode de senha).
  * [x] Migration `InitialCreate` gerada (`Data/Migrations/20260429174309_InitialCreate.cs`).
  * [x] 7 tabelas mapeadas: `tenants`, `customers`, `professionals`, `services`, `schedules`, `tenant_settings`, `waitlist`.
  * [x] Índices críticos gerados: conflito de horário `ix_schedules_tenant_professional_datetime`, `customers(tenant_id, phone)` único, `professionals(tenant_id, email)` único.
  * [x] Enums mapeados como `integer` (performance).
  * [x] Pacotes EF Core 9.0.4 fixados no projeto de testes (0 warnings de conflito de versão).
  * [x] Build da solution: **0 erros, 0 warnings**.
* [x] **Infraestrutura:**
  * [x] Setup do `docker-compose.yml` para rodar o `Redis` localmente (porta 6379, senha, AOF).
  * [x] `appsettings.Development.json` configurado com `RedisConnection`.
* [x] **Repositories e Services — Professional, Service, Schedule, TenantSettings:**
  * [x] Interface `IMustHaveTenant` criada em `Domain/Interfaces/`. Entidades `Professional`, `Service`, `Schedule` e `TenantSettings` a implementam.
  * [x] Override de `SaveChangesAsync` no `AppDbContext`: preenche `TenantId` automaticamente em qualquer `IMustHaveTenant` com `Guid.Empty` ao salvar — Services e Repositories 100% agnósticos ao TenantId.
  * [x] Interfaces de Repository: `IProfessionalRepository`, `IServiceCatalogRepository`, `IScheduleRepository`, `ITenantSettingsRepository`.
  * [x] Implementações de Repository: `ProfessionalRepository`, `ServiceCatalogRepository`, `ScheduleRepository`, `TenantSettingsRepository`. Soft-delete via `IsActive=false` para `Professional` e `Service`.
  * [x] Interfaces de Service: `IProfessionalService`, `IServiceCatalogService`, `IScheduleService`, `ITenantSettingsService`.
  * [x] Implementações de Service com regras de negócio:
    * `ProfessionalService`: Valida e-mail duplicado dentro do Tenant.
    * `ServiceCatalogService`: Valida duração > 0 e preço ≥ 0.
    * `ScheduleService`: **Regra de anti-conflito de horário** — usa `GetConflictingAsync` (algoritmo de sobreposição de intervalos) antes de criar ou atualizar agendamento. Calcula `EndDateTime` automaticamente via `DurationMinutes` do serviço. Normaliza `StartDateTime` para UTC.
    * `TenantSettingsService`: Garante invariante 1:1 — impede criação de configurações duplicadas.
  * [x] `Program.cs` atualizado com todos os `AddScoped<>` para os novos Repositories e Services.
  * [x] Build da solution: **0 erros, 0 warnings**.
* [x] **Testes Unitários (xUnit + Moq) — Services:**
  * [x] `ScheduleServiceTests` (14 testes): happy-path, cálculo de EndDateTime, normalização UTC, 4 cenários de sobreposição de intervalos (Allen's Interval Algebra), exclusão de auto-conflito no Update, serviço não encontrado.
  * [x] `ProfessionalServiceTests` (8 testes): e-mail duplicado bloqueado, criar com cor, update, delete soft, get delegação ao repository.
  * [x] `ServiceCatalogServiceTests` (11 testes): guard clauses duração ≤ 0, preço negativo, preço zero válido, update, delete, get delegação.
  * [x] `TenantSettingsServiceTests` (6 testes): invariante 1:1, settings inexistente retorna null, create/update delegação.
  * [x] **Total: 51 testes — 51 aprovados, 0 falhas, 0 avisos.**


* [x] **Autenticação e Autorização (JWT) com `TenantId` nos claims — `ProfessionalRole`:**
  * [x] Integração com os pacotes `Microsoft.AspNetCore.Authentication.JwtBearer` e `BCrypt.Net-Next`.
  * [x] Endpoint Minimal API `POST /api/v1/auth/login` (anônimo).
  * [x] Serviço de Autenticação gerando o token com claims `sub` (Id), `tenant_id` e `role`.
  * [x] Autorização global configurada e Policy "RequireOwnerRole" criada.
  * [x] **Testes Unitários:** `AuthServiceTests` (4 testes: happy-path validando token e claims, sad-path email inexistente, senha incorreta, inativo).
  * [x] **Total: 55 testes — 55 aprovados, 0 falhas, 0 avisos.**

* [x] **Endpoints CRUD (Minimal API):**
  * [x] `ProfessionalEndpoints` com DTOs (Create, Update, Get, GetAll, Delete).
  * [x] `ServiceCatalogEndpoints` com DTOs.
  * [x] `ScheduleEndpoints` com DTOs.
  * [x] `TenantSettingsEndpoints` com DTOs e lógica de Create/Update híbrida.
  * [x] Rotas protegidas por `[Authorize]` e validação de `RequireOwnerRole` em ações destrutivas.

* [x] **Integração Node.js (Webhooks):**
  * [x] Contratos (`WebhookMessageRequest`).
  * [x] Filtro de Segurança `ApiKeyAuthFilter` via `X-Api-Key` lido do `appsettings.json`.
  * [x] Serviço `WebhookService` para recepção primária (validação e log).
  * [x] Endpoint Minimal API `POST /api/v1/webhooks/whatsapp`.
  * [x] **Testes Unitários:** `ApiKeyAuthFilterTests` (3 testes), `WebhookServiceTests` (12 testes).
  * [x] **Total: 70 testes — 70 aprovados, 0 falhas, 0 avisos.**

* [x] **Integração IA (Google Gemini):**
  * [x] Configurações no `TenantSettings` (`GeminiApiKey`, `GeminiModel` com fallback via `appsettings.json`).
  * [x] Migration do Entity Framework e banco atualizado.
  * [x] Implementação de `IGeminiService` e `GeminiService` usando `HttpClient` e tipagem estruturada (JSON Schema).
  * [x] Implementação de `IAiOrchestratorService` para construção do Contexto Dinâmico a partir das regras de negócio do Tenant.
  * [x] Endpoint Minimal API protegido para extração de intenções (`POST /api/v1/ai/extract-intent`).
  * [x] **Testes Unitários:** `GeminiServiceTests` (2 testes: success e error rates), `AiOrchestratorServiceTests` (2 testes: success orquestração e fallback/exception).
  * [x] **Total: 74 testes — 74 aprovados, 0 falhas, 0 avisos.**

* [x] **Sincronização Google Calendar (BackgroundService):**
  * [x] Adição das propriedades `GoogleCalendarRefreshToken` e `GoogleCalendarEmail` em `Professional`.
  * [x] Criação da `ICalendarSyncQueue` (in-memory channel queue) para desacoplamento.
  * [x] Implementação de `IGoogleCalendarApiService` via Google.Apis.Calendar.v3 (OAuth2).
  * [x] Criação do `GoogleCalendarSyncBackgroundService` para processamento assíncrono.
  * [x] Disparo automático de eventos via `ScheduleService` (Create/Update/Delete).
  * [x] Nova migration aplicada: `AddGoogleCalendarToProfessional`.
  * [x] **Testes Unitários:** `CalendarSyncQueueTests` (1 teste), `GoogleCalendarSyncBackgroundServiceTests` (1 teste), `ScheduleServiceTests` (atualizado para cobrir a fila).
  * [x] **Total Geral: 78 testes — 78 aprovados, 0 falhas, 0 avisos.**

---

## 📅 3. O que será desenvolvido (Roadmap Futuro)

### Backend (.NET Core / Minimal APIs)
* [ ] Lógica de Sugestão Inteligente de horários alternativos (quando slot está ocupado).
* [ ] Lógica de Lista de Espera: trigger de cancelamento → notificação proativa via WhatsApp.
* [ ] Testes Unitários (`xUnit` + `Moq`) para `ScheduleService` (conflito de horários).


### Serviço Mensageria (Node.js + Baileys)
* [ ] Setup do projeto Node.js e biblioteca Baileys.
* [ ] Implementação do fluxo de QR Code e persistência de sessão.
* [ ] Envio de eventos (mensagens recebidas) para o Webhook do .NET.
* [ ] Endpoint para o .NET disparar o envio de mensagens para o WhatsApp.

### Frontend (PWA)
* [ ] Setup do projeto Frontend (React/Vite) como Progressive Web App.
* [ ] Telas de Autenticação (Login/Cadastro do Profissional).
* [ ] Dashboard/Painel principal para visualização e gestão de Agendamentos (estilo Google Calendar).
* [ ] Telas de configurações do Tenant (horários, serviços, folgas).
