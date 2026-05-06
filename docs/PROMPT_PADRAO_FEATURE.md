# Prompt Padrão para Criação de Novas Funcionalidades

**Instruções de uso:** Copie e cole o texto abaixo sempre que for iniciar o desenvolvimento de uma nova funcionalidade (feature) neste projeto. Substitua `[DESCRIÇÃO DA FUNCIONALIDADE]` pelos detalhes do que você deseja criar.

---

### Copie o bloco abaixo:

```text
Por favor, atue como um Arquiteto e Desenvolvedor Sênior (.NET, React, Node.js) e ajude a implementar a seguinte funcionalidade:

**Funcionalidade:** [DESCRIÇÃO DA FUNCIONALIDADE]

Antes de iniciar qualquer código ou propor uma solução, você DEVE seguir rigorosamente este fluxo de trabalho:

1. **Leitura de Contexto:**
   - Leia o arquivo `ARCHITECTURE.md` para entender as restrições da nossa arquitetura SaaS.
   - Leia o arquivo `docs/BUSINESS_RULES.md` para garantir que a feature respeita nossas regras de negócio.
   - Leia o arquivo `docs/SECURITY_GUIDELINES.md` para aplicar as políticas de segurança e autenticação (JWT / API Keys / Multi-tenant).
   - Se a feature envolver o Backend, leia `back/BACKEND_GUIDELINES.md`.
   - Se a feature envolver o Frontend, leia `front/FRONTEND_GUIDELINES.md`.

2. **Planejamento (Planning Mode):**
   - Com base nos documentos lidos, estruture um plano de implementação (`implementation_plan.md`) usando os padrões do projeto (ex: Minimal APIs, Clean Architecture, Entity Framework com TenantId).
   - O plano DEVE incluir uma seção "Testes Previstos" listando quais cenários serão cobertos por testes unitários.
   - Solicite minha aprovação antes de gerar o código.

3. **Execução (Código Limpo e Iterativo):**
   - Siga pequenos passos iterativos: Model → Repository → Service → Endpoint.
   - Mantenha o código limpo: sem placeholders, tipagem forte, tratamento de exceções e logs estruturados.
   - Garanta isolamento Multi-Tenant em todas as entidades (`IMustHaveTenant`, Global Query Filters).

4. **Testes Unitários (OBRIGATÓRIO para toda lógica de negócio):**
   - Após implementar cada Service com regras de negócio, crie imediatamente os testes unitários correspondentes no projeto `AgendaInteligente.Api.Tests`.
   - **Critério de cobertura obrigatória:** Todo método de Service que contenha validação, cálculo, verificação de regra de negócio ou lançamento de exceção DEVE ter ao menos um teste de caminho feliz (happy-path) e um de falha (sad-path).
   - **Quando os testes são opcionais:** Métodos que apenas delegam ao Repository sem lógica própria (ex: `GetByIdAsync` que apenas chama `_repo.GetByIdAsync`) não precisam de teste unitário dedicado.
   - Use `Moq` para mockar Repositories e `NullLogger<T>` para loggers.
   - Ao final, execute `dotnet test` e confirme **0 falhas** antes de prosseguir.

5. **Atualização de Tracking:**
   - Somente após `dotnet test` com 0 falhas, atualize obrigatoriamente o arquivo `PROJECT_TRACKING.md`:
     - Mova o que foi concluído para a seção "O que já foi desenvolvido", incluindo o total de testes criados e aprovados.
     - Ajuste ou remova os itens correspondentes do Roadmap Futuro.
```
