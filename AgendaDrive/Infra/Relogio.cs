namespace AgendaDrive.Infra;

// Hora oficial do sistema: sempre a de Brasília.
// O servidor do MonsterASP fica na Europa, então DateTime.Now daria a hora de lá e
// a regra "não pode pedir corrida no passado" erraria por algumas horas.
public static class Relogio
{
    private static readonly TimeZoneInfo Brasilia = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public static DateTime Agora => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Brasilia);
}
