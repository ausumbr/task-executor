# TaskExecutor

Uma simples biblioteca que controla a quantidade de Tasks executando em paralelo.

### Execução de ação

A execução de uma ação, cria uma tarefa que a encapsula, e em seguida tenta **adicioná-la**.

### Adição de tarefa

Ao adicionar uma tarefa, a biblioteca irá enfileirar a **Task**, caso esteja trabalhando acima da capacidade, e a executará assim que possível.
O tipo do método é **fire and forget**, você irá chamar e imediatamente ele irá te retornar um OK, iniciando ou não a execução da **Task**.

### Fluxo na finalização (Dispose)

Ao realizar o *Dispose*, a biblioteca tentará aguardar a conclusão das tarefas em execução por até
**100ms**, e em seguida limpará a lista de controle.

