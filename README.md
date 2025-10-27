# Distributed Order Saga Demo

> Parte dessa documentação foi escrita com o auxílio do chatGPT

### O que é uma SAGA?

Quando você lê a palavra SAGA deve imaginar que é um acrônimo. Ledo engano. A palavra SAGA remete a uma narrativa épica, cheia de eventos interligados e personagens distintos. De forma semelhante, no contexto de sistemas distribuídos, uma SAGA representa uma série de operações coordenadas que juntas compõem uma transação maior, envolvendo múltiplos serviços ou componentes. (Que Odin salve o chatGPT por essa analogia!)

Uma SAGA é um padrão arquitetural utilizado para gerenciar transações distribuídas em ambientes de microserviços. Diferente das transações tradicionais, que garantem atomicidade em um único banco de dados, a SAGA coordena uma série de operações independentes, cada uma executada por um serviço diferente. 

Cada etapa da SAGA é composta por uma ação e, caso ocorra uma falha, um rollback (ação de compensação) é disparado para desfazer os efeitos da etapa anterior.

Existem dois tipos principais de implementação do padrão SAGA:

**1. Orquestrado:**
Um serviço central (orquestrador) é responsável por coordenar o fluxo das etapas, enviando comandos e reagindo a eventos. Esse serviço central decide qual serviço deve executar a próxima ação e gerencia explicitamente as compensações em caso de falha. Esse modelo facilita o rastreamento do fluxo pelo fato de toda sua gestão estar centralizada.

**2. Coreografado:**
Não há um serviço central. Cada microserviço reage a eventos publicados por outros serviços e decide de forma autônoma qual ação tomar. O fluxo é distribuído, tornando o rastreamento mais complexo, porém favorecendo maior desacoplamento e escalabilidade.


#### Importante:

A orquestração não é melhor nem pior que a coreografia. Cada abordagem - como tudo na computação - possui vantagens e desvantagens, e a escolha depende do contexto e dos objetivos do sistema:

- **Orquestração** oferece maior controle centralizado, facilita o rastreamento e o debug, e é mais simples para cenários onde se deseja ter uma visão clara do fluxo. Por outro lado, pode criar um ponto único de falha e um alto acoplamento ao orquestrador.

- **Coreografia** favorece o desacoplamento entre serviços, escalabilidade e flexibilidade, pois cada serviço reage de forma autônoma aos eventos. Porém, o rastreamento do fluxo e o entendimento do estado global podem ser mais complexos, exigindo ferramentas de observabilidade e monitoramento mais avançadas.

> Neste projeto, foi adotado o padrão SAGA Orquestrado para facilitar o entendimento do fluxo completo, centralizar a lógica de coordenação e tornar mais didática a demonstração dos conceitos. O orquestrador permite visualizar claramente cada etapa, os comandos e eventos envolvidos, além de simplificar o tratamento de compensações e erros para quem está estudando o padrão.

---

## Observabilidade

Todo e qualquer sistema precisa ter o mínimo de observabilidade. Ponto final. 

Em sistemas distribuídos, especialmente ao utilizar o padrão SAGA, a observabilidade é fundamental para garantir o rastreamento, diagnóstico e auditoria dos fluxos de eventos e compensações. Como cada etapa pode ocorrer em diferentes serviços, é essencial ter mecanismos que permitam acompanhar o ciclo de vida das transações, identificar falhas rapidamente e entender o estado global do sistema. Sem isso, esquece microserviços distribuídos, esquece SAGA, esquece tudo.

### OpenTelemetry
O OpenTelemetry é uma solução open source que permite coletar métricas, logs e traces distribuídos de aplicações. Integrar OpenTelemetry aos microserviços facilita o rastreamento de cada evento, comando e ação de compensação, permitindo visualizar o caminho percorrido por cada pedido e identificar gargalos ou falhas. Nosso sistema gera esses dados de observabilidade e os exporta para ferramentas de análise (Dashboard do Aspire).

### Aspire
Aspire é uma ferramenta fantástica. Não me canso de dizer isso. No nosso exemplo, é possível executar todos os microserviços e visualizar o fluxo completo da SAGA no seu dashboard, facilitando a compreensão do processo e a identificação de possíveis problemas.
Os traces distribuídos coletados via OpenTelemetry são enviados para o Aspire, onde podem ser analisados e monitorados em tempo real. Isso é o máximo.
No mundo real, não devemos utilizar o dashboard do Aspire. Geralmente se usa ferramentas como Grafana, Kibana, Jaeger, etc. Mas para desenvolvimento, estudos e demonstrações, o Aspire beira o estado da arte.

---

### Tecnologias Envolvidas

- .NET 9 com C# 13
- RabbitMQ
- OpenTelemetry
- Aspire

### Serviços Envolvidos

- **OrderService**: Cria pedidos e publica eventos de criação.
- **PaymentService**: Processa pagamentos e publica eventos de aprovação/rejeição.
- **ShippingService**: Realiza envio e publica eventos de sucesso/falha.
- **Orchestration**: Orquestra o fluxo, mantém o estado da SAGA e executa compensações.


### Outros componentes

- **AppHost**: Responsável por hospedar e iniciar os microserviços, centralizando configurações comuns e facilitando o gerenciamento do ciclo de vida das aplicações.
- **ServiceDefaults**: Biblioteca utilitária que padroniza configurações de logs, métricas, health checks e práticas recomendadas para os serviços.
- **Messaging**: Implementa abstrações e utilitários para comunicação via RabbitMQ, incluindo publishers, consumers e mecanismos de tracing para eventos e comandos.
- **Contracts**: Define os contratos de mensagens (eventos e comandos) utilizados na comunicação entre os microserviços, garantindo tipagem forte e padronização dos dados trafegados.

---

A ideia do projeto é simular um sistema de pedidos, onde cada serviço é responsável por uma parte do processo. A SAGA orquestrada gerencia o fluxo completo, garantindo que cada etapa seja concluída com sucesso ou que as compensações sejam executadas em caso de falhas.

O foco principal é demonstrar como o padrão SAGA pode ser implementado em um ambiente de microserviços, utilizando eventos e comandos para coordenar as ações entre os serviços envolvidos, sendo assim, a estrutura dos projetos, implementações - por mais que tenham sido cuidadosamente pensadas - servem apenas para fins didáticos. Tentei manter o código o mais simples e didático possível, priorizando a clareza dos conceitos.

Sobre o uso do RabbitMQ, tentei deixar o mais próximo possível do que seria um cenário real de produção, porém no mundo real, toda a gestão da infraestrutura (clusters, filas, exchanges, dead letter queues, etc) deveria ser feita de tal forma que fosse possível sua replicação entre vários ambientes (Infra as Code? talvez) e não manualmente.
Gerenciar uma ferramenta como essa é um grande desafio e foge do escopo deste projeto. Mas uma coisa eu posso garantir, o RabbitMQ é poderosíssimo... quando bem configurado... e quando você sabe o que está fazendo...

### Fluxo Principal

1. **Criação do Pedido**
   - Evento: `OrderCreatedEvent`
   - Consumidor: `OrderCreatedConsumer`
   - Ação: Cria o estado inicial da SAGA e envia comando para processar pagamento (`ProcessPaymentCommand`).

2. **Processamento do Pagamento**
   - Evento: `PaymentApprovedEvent` ou `PaymentRejectedEvent`
   - Consumidor: `PaymentApprovedConsumer` ou `PaymentRejectedConsumer`
   - Ação:
     - Se aprovado: Atualiza o estado da SAGA e envia comando para envio do pedido (`ShipOrderCommand`).
     - Se rejeitado: Atualiza o estado da SAGA para cancelado.

3. **Envio do Pedido**
    - Evento: `OrderShippedEvent` ou `OrderShippingFailedEvent`
    - Consumidor: `OrderShippedConsumer` ou `OrderShippingFailedConsumer`
    - Ação:
       - Se enviado: Finaliza a SAGA com sucesso.
       - Se falha: Atualiza o estado da SAGA para compensação, aciona comando de reembolso (`RefundPaymentCommand`) e envia comando de cancelamento de pedido (`CancelOrderCommand`).
       - O comando `CancelOrderCommand` é publicado para garantir que o pedido seja cancelado em todos os serviços envolvidos, registrando o motivo do cancelamento.

4. **Confirmação do Reembolso**
    - Evento: `PaymentRefundedEvent`
    - Consumidor: `PaymentRefundedConsumer`
    - Ação:
       - Confirma que o reembolso foi processado com sucesso.
       - Atualiza o estado da SAGA para "Compensated" (Compensada).
       - Finaliza o fluxo de compensação garantindo rastreabilidade completa.


### Exemplos de Fluxos Possíveis

#### 1. Fluxo de Sucesso

- Pedido criado → Pagamento aprovado → Pedido enviado → SAGA concluída com sucesso.

#### 2. Pagamento Reprovado

- Pedido criado → Pagamento reprovado → SAGA cancelada.
   - O consumidor de pagamento rejeitado atualiza o estado da SAGA para cancelado e registra o motivo.

#### 3. Erro na Entrega (Falha no Envio)

- Pedido criado → Pagamento aprovado → Falha no envio → SAGA entra em compensação.
   - O consumidor de falha no envio publica:
      - Comando de reembolso (`RefundPaymentCommand`)
      - Comando de cancelamento de pedido (`CancelOrderCommand`)
   - O `PaymentService` processa o reembolso e publica `PaymentRefundedEvent`.
   - O `PaymentRefundedConsumer` confirma o reembolso e atualiza o estado da SAGA para "Compensated".
   - O motivo do cancelamento é registrado em toda a cadeia.
