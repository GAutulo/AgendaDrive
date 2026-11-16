-- Redefinição de senha por palavra-passe (16/11/2026).
-- Rode UMA vez nos bancos que já existem (LocalDB e MonsterASP, pelo Run T-SQL do painel).
-- Só acrescenta uma coluna opcional: nenhum dado é apagado ou alterado.
-- Se a coluna já existir, não faz nada.
if col_length('Usuarios', 'PalavraPasseHash') is null
    alter table Usuarios add PalavraPasseHash nvarchar(255) null;
