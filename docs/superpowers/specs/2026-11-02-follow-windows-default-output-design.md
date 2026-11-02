# Acompanhar a saída padrão do Windows

## Intenção

Quando a opção **Usar somente quando for a saída padrão** estiver ligada, o Speaker Keep Awake deve acompanhar a saída padrão de reprodução atual do Windows. Isso cobre mudanças como alternar entre FXSound e WZ BT5, sem deixar a proteção presa ao endpoint que estava selecionado antes.

## Estado atual

`AppSettings.SelectedDeviceId` persiste um endpoint específico. `KeepAliveEngine.Tick` sempre usa esse ID e apenas compara com o endpoint padrão quando `OnlyWhenDefault` está ativo. Se o padrão mudar, o motor deixa de considerar o endpoint elegível, mas não passa a usar o novo padrão. `SettingsForm.ReloadDevices` também preserva a seleção anterior e só é chamado ao abrir/ativar a janela ou pelo botão Atualizar.

## Abordagens consideradas

1. **Acompanhar automaticamente a saída padrão (escolhida).** Em modo padrão, resolver o endpoint padrão atual como destino tanto para atividade quanto para emissão e refletir a mudança na interface. Quando o modo estiver desligado, continuar respeitando a seleção manual. Isso atende a expectativa descrita e cobre mudanças enquanto a janela estiver na bandeja.
2. **Manter o endpoint fixo e avisar quando deixar de ser padrão.** Exigiria ação manual para trocar e reproduziria a confusão atual; não foi escolhido.

## Escopo e comportamento

- Com `OnlyWhenDefault == true`, o endpoint padrão atual é o destino efetivo; o ID salvo não deve bloquear uma troca de padrão.
- Ao detectar uma troca, interromper o renderer associado ao endpoint antigo e iniciar a proteção no novo endpoint quando as condições do motor permitirem.
- A interface deve mostrar o endpoint padrão atual e atualizar sua lista/seleção enquanto estiver aberta, inclusive quando a mudança ocorreu com a janela oculta.
- Com `OnlyWhenDefault == false`, manter o comportamento de saída escolhida manualmente.
- Sem uma saída padrão disponível, aguardar sem emitir áudio e apresentar estado compreensível.

## Fora de escopo

- Alterar a detecção de atividade de áudio ou a calibração do sinal.
- Criar configuração específica para FXSound ou para o modelo ZK-502MT.
- Mudar a inicialização automática, ícone ou menu de bandeja.

## Restrições e critérios de aceitação

- Mudanças de dispositivo não devem deixar uma sessão de áudio presa no endpoint anterior.
- O modo manual precisa continuar apontando para o dispositivo escolhido pelo usuário.
- O estado visível deve corresponder ao destino que o motor realmente usa.
- Manter a implementação leve, nativa e sem dependências adicionais.

## Decisões de implementação em aberto

O plano deve decidir como propagar mudanças do endpoint padrão para a interface e para o renderer. Preferir notificações do Core Audio se já houver uma integração segura com o loop da interface; caso contrário, usar uma consulta periódica limitada enquanto a janela estiver aberta, com o motor continuando a resolver o destino padrão no seu ciclo normal. Evitar enumerar todos os endpoints em cada ciclo de 100 ms.

## Verificação

O projeto contém cobertura automatizada de `KeepAliveEngine`, mas as instruções locais pedem que testes não sejam adicionados nem executados sem solicitação explícita. O plano deve descrever verificações manuais reproduzíveis para: FXSound padrão, troca para WZ BT5 com a janela aberta, troca com a janela na bandeja e modo manual desligado. Nenhuma dessas verificações será alegada como executada sem evidência.
