# Prompt Padrão para Criação de Novas Funcionalidades (Frontend)

**Instruções de uso:** Copie e cole o texto abaixo sempre que for iniciar o desenvolvimento de uma nova funcionalidade (feature) no Frontend deste projeto. Substitua `[DESCRIÇÃO DA FUNCIONALIDADE]` pelos detalhes do que você deseja criar.

---

### Copie o bloco abaixo:

```text
Por favor, atue como um Especialista Frontend Sênior (React/Angular, Tailwind CSS, PWA) e ajude a implementar a seguinte funcionalidade:

**Funcionalidade:** [DESCRIÇÃO DA FUNCIONALIDADE]

Antes de iniciar qualquer código ou propor uma solução, você DEVE seguir rigorosamente este fluxo de trabalho:

1. **Leitura de Contexto:**
   - Leia o arquivo `ARCHITECTURE.md` para entender as restrições da nossa arquitetura SaaS.
   - Leia o arquivo `front/FRONTEND_GUIDELINES.md` para garantir que a feature respeita as Regras de Ouro do Frontend (Mobile First, PWA, Comportamento Nativo, Feedback Visual).
   - Leia o arquivo `docs/SECURITY_GUIDELINES.md` para entender a proteção de rotas, o gerenciamento do token JWT e como obter o TenantId de forma segura.
   - Se houver regras de negócio pertinentes, leia `docs/BUSINESS_RULES.md`.

2. **Planejamento (Planning Mode):**
   - Com base nos documentos lidos, estruture um plano de implementação (`implementation_plan.md`) considerando a estrutura de componentes e serviços do frontend.
   - O plano DEVE listar os novos componentes atômicos/reutilizáveis que serão criados, a estrutura do estado global ou local da página e as integrações necessárias com as APIs do backend.
   - Solicite minha aprovação antes de gerar o código.

3. **Execução (Código Limpo e Iterativo):**
   - Siga pequenos passos iterativos: Criação de Tipos/Interfaces → Componentes Reutilizáveis (UI) → Lógica e Estado (Hooks/Zustand) → Montagem da Página (View).
   - Priorize o "Comportamento de App Nativo": interface focada em touch, sem barras de rolagem horizontais e navegação muito fluida.
   - Implemente obrigatoriamente Feedbacks Visuais: Skeleton Screens para requisições assíncronas em andamento e Toasts de notificação para sucessos/erros.
   - Construa um design esteticamente premium, focado em usabilidade e com animações/transições sutis.

4. **Resiliência e Cache (PWA):**
   - Considere abordagens "offline-first". Em caso de perda de conexão temporária ou lentidão de rede, a UI deve lidar com isso de forma suave, potencialmente resgatando dados em cache do Service Worker.

5. **Atualização de Tracking:**
   - Após a validação de que a feature está rodando e com UI responsiva sem falhas, atualize obrigatoriamente o arquivo `PROJECT_TRACKING.md`:
     - Mova a feature construída para a seção "O que já foi desenvolvido".
     - Ajuste ou remova os itens correspondentes do Roadmap Futuro.
```
