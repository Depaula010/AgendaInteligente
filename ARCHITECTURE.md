# GUIDELINES DE ARQUITETURA E DESENVOLVIMENTO: AGENDAMENTO SAAS (BARBEARIAS)

## 1. Visão Geral do Projeto
Sistema de agendamento inteligente para barbearias e serviços via WhatsApp. O cliente interage com um bot no WhatsApp, a IA (Gemini) entende a intenção (marcar, cancelar, reagendar, consultar) e o sistema gerencia a agenda usando o banco de dados local sincronizado com o Google Calendar. O barbeiro gerencia tudo via um PWA (Progressive Web App). O sistema é um SaaS (Software as a Service) desenhado para atender múltiplas barbearias simultaneamente de forma isolada e segura.

## 2. Stack Tecnológica
* **Backend API**: C# .NET 8/9 com Minimal APIs.
* **Testes**: xUnit + Moq (Test-Driven Development e Unit Tests obrigatórios para lógicas complexas).
* **Banco de Dados Primário**: PostgreSQL.
* **Cache**: Redis (para evitar requisições redundantes à LLM e chamadas repetidas ao BD).
* **Inteligência Artificial**: Google Gemini API (para Processamento de Linguagem Natural das mensagens).
* **Frontend PWA**: React + Vite (ou Angular) configurado como Progressive Web App (mobile-first, UI responsiva parecendo app nativo).
* **Serviço de Mensageria**: Serviço Node.js externo (Baileys) consumido via chamadas HTTP (REST) e Webhooks.
* **Infraestrutura**: Docker e Docker Compose, hospedado em VPS Ubuntu (Contabo).
* **Segurança**: Autenticação via JWT (PWA) e API Keys (Webhooks Node <-> .NET).
* **Arquitetura de Dados**: Multi-tenant (isolamento lógico por `TenantId` no PostgreSQL).

## 3. Decisões de Arquitetura

### 3.1. Abordagem Híbrida do Google Calendar
* O PostgreSQL é a **fonte da verdade (Source of Truth)** da aplicação.
* O Google Calendar atua como um refletor/espelho.
* Quando a IA agenda um horário, ele é salvo no Postgres primeiro, e uma task em background (ex: IHostedService ou Hangfire/Quartz) sincroniza o evento com o Google Calendar do barbeiro. 
* Isso protege a aplicação de rate-limits da API do Google e de instabilidades temporárias.

### 3.2. Fluxo de Integração com WhatsApp (Node.js API)
* **Entrada**: O serviço Node.js recebe a mensagem do WhatsApp e dispara um POST para o webhook exposto por esta API .NET.
* **Filtro e Cache**: A API .NET recebe a mensagem, verifica no Redis se é um "debounce" ou se a mesma intenção exata já está em cache nos últimos segundos.
* **Processamento**: A API envia o histórico da conversa e a nova mensagem para o Gemini, extraindo intenções (ex: `{"intent": "schedule", "date": "2026-03-14", "time": "14:00"}`).
* **Ação**: A API .NET valida a disponibilidade no Postgres, reserva o horário e responde.
* **Saída**: A API .NET faz uma chamada HTTP POST para `/api/v1/sessions/{id}/send-message` do serviço Node.js, que entrega a mensagem final ao cliente.

### 3.3. PWA Frontend
* O aplicativo deve ter design "Mobile First". O barbeiro instalará o PWA em seu celular Android/iOS via opção "Adicionar à Tela Inicial".
* Foco em performance, uso de ícones claros, skeleton screens durante carregamentos e suporte a offline-first básico (visualização de agenda via cache do service worker).

### 3.4. Arquitetura Multi-Tenant (SaaS) e Segurança
* O sistema deve ser projetado desde o dia 1 para suportar múltiplas barbearias.
* As entidades do banco de dados (Schedules, Customers, Services, Settings) devem possuir um `TenantId` (Guid ou Int).
* **Autenticação PWA:** O painel do barbeiro usará autenticação via JWT. O `TenantId` deve ser extraído dos claims do token JWT logado.
* **Segurança Interna:** A comunicação entre o serviço Node.js e a API .NET deve ser protegida por headers de autorização (ex: `x-api-key`), garantindo que os webhooks não sejam forjados.

### 3.5. Contexto Dinâmico para a IA (Gemini)
* A IA não deve usar um "System Prompt" estático. 
* Quando uma mensagem chegar via WhatsApp, a API .NET deve identificar de qual sessão/barbearia a mensagem pertence.
* O backend montará um prompt dinâmico injetando as configurações do barbeiro: serviços oferecidos, tempo de duração de cada serviço, dias de folga, horários de funcionamento e a agenda disponível do dia. Somente com esse contexto a mensagem será enviada ao Gemini para extração da intenção.

## 4. Metodologia de Desenvolvimento ("Vibecoding" Rules)
Ao atuar neste projeto, você (IA) deve obedecer rigorosamente às seguintes regras de desenvolvimento iterativo:

1.  **Pequenos Passos**: Nunca gere a aplicação inteira. Vamos focar em uma feature/módulo por vez.
2.  **Começar pelas Fundações**: Inicie sempre pelos Modelos de Domínio, Interfaces e Banco de Dados (Entity Framework Core com Code-First Migrations).
3.  **TDD (Test-Driven Development)**: Ao criar lógicas de negócio (ex: O serviço que decide se um horário está disponível), crie os testes unitários da classe primeiro ou logo após a implementação do serviço.
4.  **Injeção de Dependência e Clean Architecture**: Separe as responsabilidades. Minimal API routes apenas roteiam, Services lidam com a lógica e Repositories lidam com dados.
5.  **Resiliência**: Sempre trate exceções, adicione logs estruturados (ex: Serilog) e nunca deixe o app quebrar por falha na comunicação com o Gemini ou com a API do WhatsApp.
6.  **Código Limpo**: Retorne códigos prontos para uso, sem placeholders desnecessários. Use tipagem forte no .NET.
7.  **SaaS First**: Sempre considere o parâmetro `TenantId` (ou equivalente) nas consultas (Queries) e comandos (Commands) do banco de dados para garantir que um cliente não veja dados de outra barbearia.

## 5. Próximo Passo
Aguarde o comando do usuário indicando qual módulo será iniciado. (Geralmente começa com o Setup do Backend, EF Core Models e Dockerfile base).