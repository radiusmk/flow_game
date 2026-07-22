# Flow Game

Jogo desktop 2D para Windows inspirado no puzzle Flow. O jogador liga pontos da
mesma cor por linhas ortogonais, sem cruzar caminhos, ate preencher todo o
tabuleiro.

## Como executar

Build de desenvolvimento:

```powershell
dotnet run --project .\FlowGame.Wpf\FlowGame.Wpf.csproj
```

Build portatil dependente do runtime instalado:

```powershell
dotnet publish .\FlowGame.Wpf\FlowGame.Wpf.csproj -c Release --self-contained false -o .\publish\win-x64-framework-dependent
```

Depois abra:

```powershell
.\publish\win-x64-framework-dependent\FlowGame.Wpf.exe
```

## Testes

```powershell
dotnet run --project .\FlowGame.Tests\FlowGame.Tests.csproj
```

Testes Android unitários, a partir de uma máquina com Android Studio, JDK e SDK Android:

```powershell
gradle :FlowGame.Android:testDebugUnitTest
```

Build de APK local para Android:

```powershell
gradle :FlowGame.Android:assembleDebug
```

O APK de debug será gerado em:

```powershell
.\FlowGame.Android\build\outputs\apk\debug\FlowGame.Android-debug.apk
```

Para testes instrumentados em emulador ou aparelho conectado:

```powershell
gradle :FlowGame.Android:connectedDebugAndroidTest
```

## Estrutura

- `FlowGame.Core`: regras, tabuleiro, progresso de caminhos e geracao de puzzles.
- `FlowGame.Wpf`: interface Windows 2D.
- `FlowGame.Android`: interface Android nativa em Kotlin/Jetpack Compose.
- `FlowGame.Tests`: verificador de regras sem dependencias externas.
