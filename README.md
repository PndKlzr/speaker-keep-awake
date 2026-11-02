# Speaker Keep Awake

Utilitário leve para impedir que uma saída Bluetooth entre em repouso durante pausas curtas de áudio.

## Como usar

1. Extraia o ZIP para uma pasta permanente.
2. Abra `SpeakerKeepAwake.exe`.
3. Deixe **Somente quando for a saída padrão** marcado para acompanhar automaticamente a saída atual do Windows. Se você alternar entre FXSound e WZ BT5, o app acompanha a troca.
4. Para fixar uma saída específica, desmarque essa opção e escolha o dispositivo em **Saída de áudio**.
5. Escolha o tempo sem áudio e clique em **Salvar alterações**.
6. Feche a janela; o aplicativo continuará na bandeja do Windows.

Se você clonou o repositório em vez de baixar um ZIP pronto, instale o SDK do .NET 10 e gere o executável no PowerShell:

```powershell
dotnet publish .\src\SpeakerKeepAwake\SpeakerKeepAwake.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\publish\win-x64
```

Depois, copie `publish\win-x64\SpeakerKeepAwake.exe` para uma pasta permanente e abra-o. Suas preferências ficam em `%LOCALAPPDATA%\SpeakerKeepAwake\settings.json`.

O padrão é 1 minuto. A opção **Iniciar com o Windows** registra o caminho atual do executável no seu perfil, sem exigir administrador. Se mover o programa, abra-o novamente para que o caminho seja corrigido.

O interruptor no cartão de status liga e desliga a proteção imediatamente. Enquanto a janela estiver aberta, ela aparece na barra de tarefas e no Alt+Tab. Minimizar ou fechar a janela envia o aplicativo para a bandeja.

A interface da versão 2.3.6 organiza as opções em cartões, acompanha automaticamente o tema claro ou escuro do Windows e mostra o estado atual na parte superior. Durante uma pausa, o cartão de status exibe quanto tempo falta para o sinal ser desligado. A detecção também ignora a sessão de processamento do FxSound para que o próprio sinal de manutenção não seja confundido com áudio real. O ícone e o menu da bandeja permanecem legíveis nos dois temas.

## Calibrar para o ZK-502MT e placas semelhantes

Abra **Ajuste fino**, comece em **Muito suave** e use **Testar proteção**. O aplicativo mantém o sinal escolhido por 8 segundos e depois toca dois avisos audíveis. Se os dois saírem completos, sem atraso nem começo cortado, o nível funcionou; caso contrário, aumente somente um nível e repita.

Durante a reprodução, o sinal permanece contínuo para evitar o atraso ao reabrir o áudio. Quando o áudio real parar — inclusive ao pausar um vídeo — ele continua pelo tempo configurado e então para.

## Encerrar ou remover

Clique com o botão direito no ícone da bandeja e escolha **Sair**. Para impedir a inicialização automática, desmarque **Iniciar com o Windows** e clique em **Salvar** antes de excluir a pasta.

O executável é portátil e não instala componentes do .NET. Como esta versão não possui assinatura digital, o Windows pode mostrar um aviso do SmartScreen na primeira abertura.
