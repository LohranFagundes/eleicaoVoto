# Sistema de Votação FUNCORSAN

Sistema web de votação eletrônica desenvolvido em .NET 8 com Bootstrap para as eleições dos conselhos da FUNCORSAN.

## Características Principais

- **Controle de Período**: Sistema verifica automaticamente se está no período de votação
- **Autenticação Segura**: Login com CPF e senha
- **Votação em 2 Etapas**: Conselho Deliberativo e Conselho Fiscal
- **Confirmação de Voto**: Tela de revisão antes da confirmação final
- **Comprovante**: Geração de comprovante de votação com número único
- **Interface Responsiva**: Design adaptável para desktop e mobile

## Estrutura Implementada

### Controllers
- `VoteController.cs` - Controlador principal do sistema de votação
- `HomeController.cs` - Controlador padrão (existente)

### Models
- `ElectionInfo.cs` - Informações da eleição
- `Candidate.cs` - Dados dos candidatos
- `VoteRequest.cs` - Requisição de voto
- `VoteConfirmation.cs` - Comprovante de votação
- `LoginModel.cs` - Modelo de login

### Services
- `ElectionService.cs` - Serviços de eleição com interface para integração com API

### Views
- `Vote/Welcome.cshtml` - Tela de boas-vindas (fora do período)
- `Vote/Login.cshtml` - Tela de login
- `Vote/Vote.cshtml` - Interface de votação (Passo 1 e 2)
- `Vote/Confirm.cshtml` - Confirmação de voto
- `Vote/Success.cshtml` - Comprovante de votação

## Funcionalidades

### 1. Controle de Período de Votação
- Verifica automaticamente se está no período habilitado
- Exibe tela de boas-vindas quando fora do período
- Auto-refresh para verificar início da votação

### 2. Sistema de Login
- Autenticação por CPF e senha
- Cookies seguros com timeout
- Redirecionamento automático

### 3. Processo de Votação
- **Passo 1**: Conselho Deliberativo (6 candidatos)
- **Passo 2**: Conselho Fiscal (6 candidatos)
- Opções: Candidatos, Voto Branco, Voto Nulo
- Timer de segurança (5 minutos por etapa)

### 4. Confirmação e Comprovante
- Tela de revisão das escolhas
- Confirmação final
- Comprovante com número único
- Opção de impressão

## Configuração

### Período de Votação
No arquivo `Services/ElectionService.cs`, método `GetElectionInfoAsync()`:
```csharp
StartDate = new DateTime(2025, 8, 4, 8, 0, 0),
EndDate = new DateTime(2025, 8, 4, 18, 0, 0)
```

### Candidatos
Configure os candidatos no método `GetCandidatesAsync()` do `ElectionService.cs`.

### Autenticação
Para integrar com sistema real, implemente o método `ValidateUserAsync()` no `ElectionService.cs`.

## Como Executar

1. Navegue até o diretório do projeto:
```bash
cd VoteHomWebApp
```

2. Restaure os pacotes:
```bash
dotnet restore
```

3. Execute o projeto:
```bash
dotnet run
```

4. Acesse no navegador:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`

## Interface Visual

O sistema replica fielmente o design dos exemplos fornecidos:
- Logo FUNCORSAN com slogan
- Cards dos candidatos com avatares azuis
- Botões para Branco e Nulo com ícones específicos
- Timer de segurança
- Indicador "Homologação"
- Layout responsivo com Bootstrap

## Integração com API

Para integrar com APIs reais, substitua os métodos simulados no `ElectionService.cs`:

- `GetElectionInfoAsync()` - Buscar dados da eleição
- `GetCandidatesAsync()` - Buscar candidatos
- `ValidateUserAsync()` - Validar usuário
- `SubmitVoteAsync()` - Enviar voto

## Tecnologias

- .NET 8.0
- ASP.NET Core MVC
- Bootstrap 5
- jQuery
- Newtonsoft.Json
- Cookie Authentication
- Session State

## Segurança

- Autenticação por cookies
- Controle de sessão
- Validação de período
- Sanitização de entrada
- Timeout automático