# REGRAS DE NEGÓCIO E FUNCIONALIDADES (PRODUCT BACKLOG)

Este documento descreve as regras de negócio, fluxos de usuários e funcionalidades (features) que devem ser implementadas no SaaS de Agendamento. Ele dita "o que" o sistema deve fazer.

## 1. Escopo Multi-Nicho e Fluxo de Agendamento Inteligente
* **Objetivo:** O sistema deve ser agnóstico de nicho, funcionando para Barbearias, Manicures, Clínicas de Estética, Consultórios, etc.
* **Regras de Agendamento:**
  * O cliente pode interagir via WhatsApp e escolher: o **Profissional** (se houver mais de um), o **Serviço**, a **Data** e o **Horário**.
  * **Sugestão Inteligente (Smart Alternatives):** Caso o cliente solicite um horário que já está ocupado, a IA não deve apenas dizer "não tem". Ela DEVE procurar na agenda e oferecer ativamente os horários disponíveis mais próximos (ex: *"Às 15h o João está ocupado, mas ele tem um horário livre às 14h ou às 16h30. Algum desses fica bom para você?"*).
  * O tempo total bloqueado na agenda deve ser a soma da duração de todos os serviços solicitados.

## 2. Horários Recorrentes (Agendamentos "Infinitos")
* **Objetivo:** Fidelizar clientes que possuem rotinas fixas.
* **Regras:**
  * Um cliente pode solicitar um horário fixo (ex: "Toda terça-feira às 15h").
  * O sistema deve ser capaz de criar esse agendamento com recorrência "infinita" no banco de dados.
  * Esse bloqueio recorrente se mantém ativo até que o profissional ou o dono do estabelecimento cancele/remova a recorrência pelo painel.

## 3. Lista de Espera Inteligente (Waitlist)
* **Objetivo:** Maximizar o lucro do estabelecimento garantindo que buracos na agenda sejam preenchidos automaticamente.
* **Regras:**
  * Se um cliente tentar agendar um dia/horário lotado, a IA deve perguntar se ele deseja entrar na lista de espera.
  * **Gatilho (Trigger):** Quando qualquer cliente cancelar um agendamento, o backend verifica se há alguém na `Waitlist` para aquele período.
  * O sistema envia uma mensagem proativa aos clientes da lista: *"Olá! Surgiu uma vaga agora na sexta às 15h. Você ainda tem interesse? Responda SIM para confirmar."* O primeiro a confirmar ganha a vaga.

## 4. Lembretes, Confirmações e Reengajamento Automático
* **Objetivo:** Reduzir No-show (não comparecimento) e aumentar a recorrência de visitas.
* **Regras de Confirmação:**
  * O sistema deve enviar uma mensagem de confirmação de horário via WhatsApp antes do agendamento (antecedência configurável pelo dono).
* **Regras de Reengajamento (Retenção):**
  * O sistema monitora a frequência dos clientes.
  * O dono pode configurar gatilhos de ausência (ex: a cada 15 dias, ou a cada 30 dias sem agendar).
  * O bot enviará uma mensagem proativa: *"Olá [Nome], você não agendou seu serviço este mês. Gostaria de agendar para o mesmo horário e dia da semana da sua última visita?"*.

## 5. Gestão de Equipe e Permissões (Owner vs. Staff)
* **Objetivo:** Permitir que estabelecimentos com múltiplos profissionais operem na mesma conta de forma organizada.
* **Regras:**
  * O dono do estabelecimento (Owner) possui acesso total (Admin).
  * O Owner pode convidar/cadastrar profissionais (Staff) e configurar os níveis de acesso de cada um.
  * O Owner pode visualizar a agenda de TODOS os profissionais no painel PWA.
  * O profissional (Staff) pode ter acesso restrito para ver apenas a própria agenda ou a agenda geral (dependendo da configuração do Owner).

## 6. Interface Visual e Google Calendar
* **Objetivo:** Facilitar a rápida identificação visual dos agendamentos no painel PWA.
* **Regras:**
  * O frontend deve utilizar o padrão de **cores do Google Calendar** (ou permitir que o Owner defina cores).
  * Diferentes serviços, status (confirmado, pendente, cancelado) ou profissionais diferentes devem ser facilmente distinguíveis por essas marcações de cores, tornando a interface limpa e intuitiva para o uso no dia a dia.