using Microsoft.EntityFrameworkCore;

namespace AgendaDrive.Models.Entidades;

// Ponte entre o C# e o banco (Entity Framework Core, Database First: o banco vem do database/script.sql).
// Cada DbSet e uma tabela; o nome do DbSet e o nome da tabela.
// Quem usa o context sao SO as classes de Models/Negocio, recebendo-o por injecao de dependencia.
public class AgendaDriveContext : DbContext
{
    public AgendaDriveContext(DbContextOptions<AgendaDriveContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios { get; set; }

    public DbSet<Tarifa> Tarifas { get; set; }

    public DbSet<Corrida> Corridas { get; set; }

    public DbSet<Bloqueio> Bloqueios { get; set; }

    public DbSet<ItemAdicional> ItensAdicionais { get; set; }

    public DbSet<HistoricoStatus> HistoricosStatus { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Colunas preenchidas pelo proprio banco (default sysdatetime()) quando o C# nao manda valor.
        modelBuilder.Entity<Usuario>().Property(u => u.CriadoEm).HasDefaultValueSql("sysdatetime()");
        modelBuilder.Entity<Tarifa>().Property(t => t.CriadoEm).HasDefaultValueSql("sysdatetime()");
        modelBuilder.Entity<Corrida>().Property(c => c.CriadoEm).HasDefaultValueSql("sysdatetime()");
        modelBuilder.Entity<HistoricoStatus>().Property(h => h.DataHora).HasDefaultValueSql("sysdatetime()");

        // Os campos bool/int com default no banco (Ativo, Incluso, MargemSegurancaMin) NAO usam HasDefaultValue aqui
        // de proposito: se usassem, o EF deixaria de enviar false/0 e o banco gravaria o default (1/30) no lugar.

        modelBuilder.Entity<Usuario>().HasIndex(u => u.Email).IsUnique();

        modelBuilder.Entity<Tarifa>()
            .HasIndex(t => t.Ativo, "UX_Tarifas_Ativa")
            .IsUnique()
            .HasFilter("[Ativo] = 1");

        // Nenhuma exclusao em cascata: o banco recusa apagar um registro que ainda e usado por outro.
        foreach (var relacao in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            relacao.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
