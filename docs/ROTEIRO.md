# Roteiro da 4ª medição (12/11)

O sistema inteiro foi escrito como base, com autorização do professor para se basear nele, abrir outra sessão corrigindo os erros que aparecerem e enviar essa sessão a ele. Em 04/11 o Guilherme informou que está confirmado com o professor: o limite de 50% não vale para esta base, porque o projeto será refeito por ele depois.

## Falta fazer (só você consegue)

1. **Chave do OpenRouteService:** crie a conta em https://openrouteservice.org, gere a chave e coloque em `OpenRouteService:Chave`, no `App_Secrets/secrets.json` local e no de produção. Sem ela, sugestão de endereços, cotação e pedido de corrida não funcionam. Esse caminho só foi testado contra um servidor simulado; o primeiro teste com o serviço real é seu.
2. **Conta de motorista:** os usuários do script não conseguem entrar (têm um texto no lugar do hash). Cadastre sua conta pelo site e promova uma única vez, no Run T-SQL do painel e também no banco local:
   `update Usuarios set Papel = 'Administrador' where Email = 'seu@email';`
3. **Publicar:** depois da 3ª medição, rode `publicar-hospedagem.ps1`. Depois disso, `/Tarifas` passa a exigir login de motorista.
4. **Testar no celular de verdade**, com o site publicado.
5. **Decidir as pendências de negócio** e pedir a implementação:
   - antecedência mínima para o cliente cancelar (hoje pode cancelar a qualquer momento);
   - prazo para o motorista responder (hoje a solicitação fica pendente sem limite);
   - retorno vazio (hoje o motorista lança manualmente como item adicional);
   - valores reais de tarifa e o ponto de partida.

## Onde está cada requisito

| Requisito | Código |
| --- | --- |
| RF01, RF02 login e papéis | `ContaController`, `UsuarioNegocio`, `Program.cs` |
| RF03 tarifas | `TarifasController`, `TarifaNegocio` |
| RF04, RF08, RF10 solicitar e mapa | `CorridasController.Nova`, `wwwroot/js/mapa.js` |
| RF05, RF12 agenda | `AgendaNegocio` |
| RF06, RF07 endereços e rotas | `IRotaServico`, `OpenRouteServiceRotaServico` |
| RF09 preço | `PrecoNegocio` |
| RF11, RF14 a RF16 ciclo da corrida | `CorridaNegocio` |
| RF13 bloqueios | `BloqueiosController`, `BloqueioNegocio` |
| RF17 Waze | `Views/Corridas/Detalhes.cshtml` |
| RF18 relatório | `CorridasController.Solicitacoes` |
| RNF05 API | `Controllers/Api/ApiControllers.cs` |

## Para estudar antes da apresentação

Saiba explicar, sem consulta:

- **Cookie de login:** o que são claims e por que `UseAuthentication` vem antes de `UseAuthorization`.
- **Senha:** por que hash e não criptografia, e por que o `Papel` nunca vem do formulário.
- **RN06:** por que `SolicitacaoViewModel` não tem campo de preço.
- **Conflito de agenda:** a condição `A.inicio < B.fim && B.inicio < A.fim`.
- **Transação Serializable:** o que acontece quando dois pedidos do mesmo horário são aprovados juntos (`AgendaNegocio.ExecutarExclusivoAsync`).
- **Antifalsificação:** por que o `fetch` envia o cabeçalho `RequestVerificationToken`.
- **`IRotaServico`:** por que existe a interface e como trocar de provedor.
- **Corrida guarda os valores:** como mudar a tarifa não altera corridas antigas (RN02, RN10).

## Antes do PDF final (19/11)

A documentação entregue diverge do que foi construído, e o enunciado prevalece:

- Seção 6: três projetos e `Servicos` → projeto único com `Models/Entidades` e `Models/Negocio`.
- Seção 6: hospedagem "[definir]" → MonsterASP.NET; logs com `ILogger` → arquivo em `App_Data/logs`.
- RNF03: classes geradas por Scaffold → entidades escritas espelhando o script.
- RNF08: User Secrets e variáveis de ambiente → `App_Secrets`.
- RNF09: `ILogger` → log em arquivo em `App_Data/logs`.
- Uso de IA: declarar que a IA gerou o código-base, com a autorização do professor, e anexar o histórico das conversas, **incluindo as do Claude Code**, com a senha coberta.
