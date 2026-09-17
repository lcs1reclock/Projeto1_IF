namespace Projeto1_IF.Security;

public static class AppRoles
{
    public const string Medico = "Médico";
    public const string Nutricionista = "Nutricionista";
    public const string GerenteMedico = "GerenteMédico";
    public const string GerenteNutricionista = "GerenteNutricionista";
    public const string GerenteGeral = "GerenteGeral";

    public const string Profissionais = Medico + "," + Nutricionista;
    public const string Gerentes = GerenteMedico + "," + GerenteNutricionista + "," + GerenteGeral;
    public const string Todos = Profissionais + "," + Gerentes;

    public static readonly string[] Nomes =
    [
        Medico, Nutricionista, GerenteMedico, GerenteNutricionista, GerenteGeral
    ];
}
