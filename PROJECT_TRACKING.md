# Acompanhamento do Projeto: Agenda Inteligente (SaaS)

Este documento serve para rastrear o progresso do desenvolvimento da plataforma, registrando o que está sendo feito no momento, o que já foi concluído e os próximos passos do roadmap.

---

## 🚧 1. Etapa Atual em Desenvolvimento

**Frontend PWA — Dashboard (Painel Principal de Agendamentos)**
- Próximo passo: implementar a tela de Dashboard com visualização de agenda estilo Google Calendar.

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
  * [x] Migrations secundárias: `AddGeminiConfigToTenantSettings`, `AddGoogleCalendarToProfessional`, `AddBlockoutToSchedule`.
  * [x] 7 tabelas mapeadas: `tenants`, `customers`, `professionals`, `services`, `schedules`, `tenant_settings`, `waitlist`.
  * [x] Índices críticos gerados: conflito de horário `ix_schedules_tenant_professional_datetime`, bloqueios `ix_schedules_blockouts`, `customers(tenant_id, phone)` único, `professionals(tenant_id, email)` único.
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

* [x] **B17 — Loop Completo bot↔backend:**
  * [x] Pacote `Microsoft.Extensions.Caching.StackExchangeRedis 9.0.4` adicionado. Versão `Google.Apis.Calendar.v3` corrigida para `1.69.0.3667` (eliminou warning NU1603 pré-existente).
  * [x] `IConversationHistoryService` + `ConversationHistoryService`: Redis `chat:{tenantId}:{phone}` (TTL 24h) e debounce `debounce:{messageId}` (TTL 30s). Degradação graciosa se Redis offline.
  * [x] `IWhatsAppSendService` + `WhatsAppSendService`: canal único de saída para bot Node.js (POST `{BotUrl}/api/v1/whatsapp/send`). Stub via logger quando `BotUrl` não configurado.
  * [x] `WhatsAppBotOptions` para binding de `WhatsAppBot:BotUrl`.
  * [x] `WebhookService` completo — fluxo: debounce → load history → `AiOrchestratorService` → save history (nova lista imutável) → `IWhatsAppSendService`.
  * [x] `Program.cs`: `AddStackExchangeRedisCache` com fallback `AddDistributedMemoryCache`. `appsettings.json` com seção `WhatsAppBot`.
  * [x] **Novos testes (13):** `ConversationHistoryServiceTests` (7) + `WebhookServiceTests` (+7 novos aos 5 existentes).
  * [x] **Total Geral: 110 testes — 110 aprovados, 0 falhas, 0 warnings.**

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

* [x] **Sugestão Inteligente de Horários Alternativos:**
  * [x] Nova exception de domínio tipada `ScheduleConflictException` com propriedade `SuggestedAlternatives` (`IReadOnlyList<DateTime>`).
  * [x] Método `GetAlternativeTimesAsync` adicionado à interface `IScheduleService` e implementado em `ScheduleService`.
  * [x] Algoritmo de busca: varre slots de 30 em 30 min no horário comercial (08h–18h UTC) do dia solicitado e dos D+1..D+7 dias seguintes, retornando os 3 mais próximos ao horário pedido.
  * [x] `CreateAsync` refatorado: ao detectar conflito, busca alternativas e lança `ScheduleConflictException` tipada (em vez de `InvalidOperationException` genérica).
  * [x] `UpdateAsync` mantém `InvalidOperationException` (sem busca de alternativas — comportamento intencional para reagendamento pelo profissional).
  * [x] **Novos testes (6 adicionados):** `CreateAsync_WhenConflictExists_ThrowsScheduleConflictException`, `CreateAsync_WhenConflictExists_SuggestedAlternatives_DoNotIncludeConflictedSlot_WhenItRemainsBlocked`, `CreateAsync_WhenConflictExists_DoesNotCallRepositoryCreate` (atualizado), `GetAlternativeTimesAsync_WhenDayHasAvailableSlots_ReturnsClosestAlternatives`, `GetAlternativeTimesAsync_WhenCurrentDayIsFullyBooked_ReturnsFromNextDays`, `GetAlternativeTimesAsync_WhenNoSlotsFoundInWindow_ReturnsEmptyList`, `GetAlternativeTimesAsync_ShouldNotSuggestPastSlots`, `GetAlternativeTimesAsync_WhenServiceNotFound_ThrowsKeyNotFoundException`.
  * [x] Testes de sobreposição (Theory `OverlappingIntervals`) atualizados para `ScheduleConflictException`.
  * [x] **Total Geral: 84 testes — 84 aprovados, 0 falhas, 0 avisos.**

* [x] **Exposição de `ScheduleConflictException.SuggestedAlternatives` no endpoint `POST /api/v1/schedules`:**
  * [x] `ScheduleEndpoints.cs` atualizado para capturar `ScheduleConflictException` de forma específica (antes de qualquer fallback genérico).
  * [x] Endpoint retorna **HTTP 409** com body JSON `{ "error": "...", "suggestedAlternatives": [...] }`, expondo a lista de slots disponíveis ao cliente/bot.
  * [x] `KeyNotFoundException` mapeada para **HTTP 404** no mesmo handler.

* [x] **Lista de Espera Inteligente — Trigger por Código (cancela → notifica via WhatsApp):**
  * [x] `IWaitlistRepository` e `WaitlistRepository` criados com busca FIFO por data/profissional filtrada pelo Global Query Filter Multi-Tenant.
  * [x] `IWhatsAppNotificationService` e `WhatsAppNotificationService` criados como abstração de envio (stub via logger — integração com bot Node.js plugável futuramente).
  * [x] `IWaitlistService` e `WaitlistService` implementados com resiliência em dois níveis: falha no repositório não propaga ao cancelamento; falha em uma notificação não impede as demais.
  * [x] `ScheduleService` injetado com `IWaitlistService`: aciona `ProcessCancellationAsync` em `DeleteAsync` e em `UpdateStatusAsync` quando status = `Cancelled`.
  * [x] `Program.cs` atualizado com os 3 novos `AddScoped`.
  * [x] **Novos testes (9 adicionados):** `WaitlistServiceTests` (6): happy-path notifica todos, atualiza status Notified, fila vazia não envia, repo lança exceção não propaga, cliente sem telefone ignorado, WhatsApp falha no 1º não impede 2º. `ScheduleServiceTests` (3): `DeleteAsync_TriggersWaitlistProcessing`, `UpdateStatusAsync_WhenCancelled_TriggersWaitlist`, `UpdateStatusAsync_WhenNotCancelled_DoesNotTriggerWaitlist`.
  * [x] **Total Geral: 93 testes — 93 aprovados, 0 falhas, 0 avisos.**

* [x] **Setup Frontend PWA (React + Vite) e Telas de Autenticação:**
  * [x] Projeto React 19 + TypeScript inicializado com Vite 6 na pasta `front/`.
  * [x] `vite-plugin-pwa` configurado com Service Worker (`autoUpdate`), Manifest completo e estratégias de cache offline (CacheFirst para fonts, StaleWhileRevalidate para API).
  * [x] Design System premium no Tailwind CSS: paleta dark slate, glassmorphism, animações `slide-up`/`fade-in`, tipografia Inter + Plus Jakarta Sans.
  * [x] `index.html` com meta tags PWA completas para iOS/Android (viewport-fit, apple-mobile-web-app-capable, theme-color).
  * [x] Ícones PWA gerados e servidos em `/public/icons/`.
  * [x] **Tipos TypeScript:** `auth.types.ts` com `LoginRequest`, `RegisterRequest`, `AuthResponse`, `JwtClaims`, `AuthUser`.
  * [x] **Serviços:** `api.ts` (Axios + interceptors JWT/401-logout), `auth.service.ts` (login/register).
  * [x] **Store Zustand (`authStore`):** persistência no localStorage, `TenantId` derivado exclusivamente do JWT (segurança SaaS), rehidratação automática ao recarregar.
  * [x] **Componentes UI Atômicos:** `Button` (variantes + loading spinner), `Input` (ícones + toggle senha + aria), `Skeleton` e `SkeletonCard` (placeholders de carregamento).
  * [x] **Layouts:** `AuthLayout` com fundo gradiente, orbs decorativos e card glassmorphism animado.
  * [x] **Telas:** `LoginPage` e `RegisterPage` com validação client-side, toasts de sucesso/erro (react-hot-toast) e botão com estado de loading.
  * [x] **Roteamento:** `react-router-dom` v7 com `ProtectedRoute` (guarda rotas autenticadas), redirect automático para `/login`.
  * [x] **Dev server:** `http://localhost:5173` — iniciado em 363ms, zero erros de compilação.

---

## 📅 3. O que será desenvolvido (Roadmap Futuro)

### Backend (.NET Core / Minimal APIs)
* [x] ~~Lógica de Sugestão Inteligente de horários alternativos (quando slot está ocupado).~~ ✅ Concluído
* [x] ~~Testes Unitários (`xUnit` + `Moq`) para `ScheduleService` (conflito de horários).~~ ✅ Concluído
* [x] **Implementação de Folgas (Blockouts):**
  * [x] O modelo `Schedule` foi atualizado com as propriedades `IsBlocked`, `BlockReason` e `IsAllDay`.
  * [x] `CustomerId` e `ServiceId` agora são opcionais (`Guid?`) no banco (gerada nova migration `AddBlockoutToSchedule`).
  * [x] `ScheduleRepository` ignora blockouts nas buscas de agendamentos normais (`!s.IsBlocked`), mas o detector de conflitos (`GetConflictingAsync`) continua cruzando tudo (agendamentos + folgas).
  * [x] `ScheduleService` lida com bloqueios, retornando mensagens contextualizadas (ex: *"O profissional já possui uma folga/bloqueio neste horário..."*) durante o `CreateAsync`/`UpdateAsync`.
  * [x] Integração com **Google Calendar** atualizada: exibe folgas com o título "🔒 Bloqueado", em cor diferente (Tomato/11) e usa eventos de dia-inteiro (All-Day) se aplicável.
  * [x] Endpoints CRUD via `/api/v1/schedules/block` criados (GET, POST, PUT, DELETE).
  * [x] **Novos testes (4 adicionados):** `CreateBlockoutAsync_WithValidData_ReturnsBlockoutAndEnqueuesSync`, `CreateBlockoutAsync_WhenConflictExists_ThrowsInvalidOperationException`, `UpdateBlockoutAsync_WithValidData_UpdatesBlockoutAndEnqueuesSync`, `UpdateBlockoutAsync_WhenTargetIsNotBlockout_ThrowsInvalidOperationException`.
  * [x] **Total Geral: 97 testes — 97 aprovados, 0 falhas, 0 avisos.**

---

## 📅 3. O que será desenvolvido (Roadmap Futuro)

### Serviço Mensageria (Node.js + Baileys)
* [ ] Setup do projeto Node.js e biblioteca Baileys.
* [ ] Implementação do fluxo de QR Code e persistência de sessão.
* [ ] Envio de eventos (mensagens recebidas) para o Webhook do .NET.
* [ ] Endpoint para o .NET disparar o envio de mensagens para o WhatsApp.

### Frontend (PWA)
* [x] ~~Setup do projeto Frontend (React/Vite) como Progressive Web App.~~ ✅ Concluído
* [x] ~~Telas de Autenticação (Login/Cadastro do Profissional).~~ ✅ Concluído
* [ ] Dashboard/Painel principal para visualização e gestão de Agendamentos (estilo Google Calendar).
* [ ] Telas de configurações do Tenant (horários, serviços, folgas).
