-- na hospedagem o banco ja vem criado: pule as 4 linhas abaixo e rode o resto no banco de la
create database AgendaDrive;
go
use AgendaDrive;
go

-- exigido pelo indice filtrado UX_Tarifas_Ativa (o SSMS ja liga por padrao, o sqlcmd nao)
set ansi_nulls on;
set quoted_identifier on;
go

create table Usuarios (
    ID int identity primary key,
    Nome nvarchar(100) not null,
    Email nvarchar(150) not null unique,
    Telefone nvarchar(20) null,
    SenhaHash nvarchar(255) not null,
    PalavraPasseHash nvarchar(255) null,
    Papel nvarchar(20) not null check (Papel in ('Administrador', 'Usuario')),
    Ativo bit not null default 1,
    CriadoEm datetime2 not null default sysdatetime()
);

create table Tarifas (
    ID int identity primary key,
    TarifaBase decimal(10,2) not null check (TarifaBase >= 0),
    ValorKm decimal(10,2) not null check (ValorKm >= 0),
    ValorMinuto decimal(10,2) not null check (ValorMinuto >= 0),
    ValorKmDeslocamento decimal(10,2) not null default 0,
    PrecoMinimo decimal(10,2) not null default 0,
    MargemSegurancaMin int not null default 30,
    BaseEndereco nvarchar(250) not null,
    BaseLatitude decimal(9,6) not null,
    BaseLongitude decimal(9,6) not null,
    Ativo bit not null default 1,
    CriadoEm datetime2 not null default sysdatetime()
);

-- apenas uma tarifa ativa por vez
create unique index UX_Tarifas_Ativa on Tarifas (Ativo) where Ativo = 1;

create table Corridas (
    ID int identity primary key,
    IDUsuario int not null foreign key references Usuarios(ID),
    IDTarifa int not null foreign key references Tarifas(ID),
    Origem nvarchar(250) not null,
    OrigemLatitude decimal(9,6) not null,
    OrigemLongitude decimal(9,6) not null,
    Destino nvarchar(250) not null,
    DestinoLatitude decimal(9,6) not null,
    DestinoLongitude decimal(9,6) not null,
    DataHora datetime2 not null,
    DuracaoMin int not null check (DuracaoMin > 0),
    DistanciaKm decimal(8,2) not null check (DistanciaKm >= 0),
    DeslocamentoKm decimal(8,2) not null default 0,
    Intermunicipal bit not null default 0,
    PrecoEstimado decimal(10,2) not null,
    Status nvarchar(20) not null default 'Pendente'
        check (Status in ('Pendente', 'Confirmada', 'Recusada', 'Cancelada', 'Concluida')),
    Observacoes nvarchar(500) null,
    CriadoEm datetime2 not null default sysdatetime()
);

create table Bloqueios (
    ID int identity primary key,
    Inicio datetime2 not null,
    Fim datetime2 not null,
    MotivoPrivado nvarchar(200) null,
    Ativo bit not null default 1,
    check (Fim > Inicio)
);

create table ItensAdicionais (
    ID int identity primary key,
    IDCorrida int not null foreign key references Corridas(ID),
    Tipo nvarchar(30) not null
        check (Tipo in ('Pedagio', 'Estacionamento', 'Espera', 'RetornoVazio', 'Outro')),
    Descricao nvarchar(200) null,
    Valor decimal(10,2) not null check (Valor >= 0),
    Incluso bit not null default 1
);

create table HistoricosStatus (
    ID int identity primary key,
    IDCorrida int not null foreign key references Corridas(ID),
    IDUsuario int not null foreign key references Usuarios(ID),
    StatusAnterior nvarchar(20) null,
    StatusNovo nvarchar(20) not null,
    DataHora datetime2 not null default sysdatetime()
);
go

-- dados de exemplo: valores e coordenadas ilustrativos; o hash real das senhas e gerado pela aplicacao
insert into Usuarios (Nome, Email, Telefone, SenhaHash, Papel) values
('Motorista Administrador', 'admin@agendadrive.local', '(17) 99999-0001', 'HASH_GERADO_PELA_APLICACAO', 'Administrador'),
('Cliente Exemplo', 'cliente@agendadrive.local', '(17) 99999-0002', 'HASH_GERADO_PELA_APLICACAO', 'Usuario');

insert into Tarifas (TarifaBase, ValorKm, ValorMinuto, ValorKmDeslocamento, PrecoMinimo, MargemSegurancaMin,
                     BaseEndereco, BaseLatitude, BaseLongitude)
values (5.00, 2.00, 0.20, 0.90, 15.00, 30, 'Centro, Votuporanga - SP', -20.423700, -49.978100);

insert into Corridas (IDUsuario, IDTarifa, Origem, OrigemLatitude, OrigemLongitude,
                      Destino, DestinoLatitude, DestinoLongitude, DataHora, DuracaoMin, DistanciaKm,
                      DeslocamentoKm, Intermunicipal, PrecoEstimado, Status, Observacoes) values
(2, 1, 'Endereco do cliente (exemplo)', -20.410000, -49.970000, 'Rodoviaria (exemplo)', -20.440000, -49.990000,
 '2026-10-20T08:00:00', 15, 8.00, 2.00, 0, 25.80, 'Concluida', null),
(2, 1, 'Endereco do cliente (exemplo)', -20.410000, -49.970000, 'Shopping (exemplo)', -20.400000, -50.010000,
 '2026-11-03T09:00:00', 30, 20.00, 3.00, 0, 53.70, 'Confirmada', null),
(2, 1, 'Endereco do cliente (exemplo)', -20.410000, -49.970000, 'Aeroporto em outra cidade (exemplo)', -20.816600, -49.406500,
 '2026-11-10T05:00:00', 70, 60.00, 5.00, 1, 143.50, 'Pendente', 'Duas malas');

insert into ItensAdicionais (IDCorrida, Tipo, Descricao, Valor, Incluso) values
(3, 'Pedagio', 'Pedagio da rodovia', 12.40, 0);

insert into Bloqueios (Inicio, Fim, MotivoPrivado) values
('2026-11-05T13:00:00', '2026-11-05T17:00:00', 'Revisao do veiculo');

insert into HistoricosStatus (IDCorrida, IDUsuario, StatusAnterior, StatusNovo, DataHora) values
(1, 2, null, 'Pendente', '2026-10-15T10:00:00'),
(1, 1, 'Pendente', 'Confirmada', '2026-10-15T12:00:00'),
(1, 1, 'Confirmada', 'Concluida', '2026-10-20T08:20:00'),
(2, 2, null, 'Pendente', '2026-10-27T18:00:00'),
(2, 1, 'Pendente', 'Confirmada', '2026-10-27T19:00:00'),
(3, 2, null, 'Pendente', '2026-10-28T09:00:00');
go

select * from Usuarios;
select * from Tarifas;
select * from Corridas;
select * from ItensAdicionais;
select * from Bloqueios;
select * from HistoricosStatus;
