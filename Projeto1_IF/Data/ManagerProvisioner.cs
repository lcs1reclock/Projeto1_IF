using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Projeto1_IF.Models;
using Projeto1_IF.Security;

namespace Projeto1_IF.Data;

public static class ManagerProvisioner
{
    private static readonly (string Role, string Prefix)[] Managers =
    [
        (AppRoles.GerenteMedico, "PROJETO_IF_GERENTE_MEDICO"),
        (AppRoles.GerenteNutricionista, "PROJETO_IF_GERENTE_NUTRICIONISTA"),
        (AppRoles.GerenteGeral, "PROJETO_IF_GERENTE_GERAL")
    ];

    public static async Task ProvisionAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var identityDb = services.GetRequiredService<ApplicationDbContext>();
        var domainDb = services.GetRequiredService<DB_IFContext>();

        // Valida todas as entradas antes de criar qualquer conta.
        var credentials = Managers.Select(m => (
            m.Role,
            Email: Environment.GetEnvironmentVariable(m.Prefix + "_EMAIL"),
            Password: Environment.GetEnvironmentVariable(m.Prefix + "_PASSWORD")))
            .ToArray();
        if (credentials.Any(c => string.IsNullOrWhiteSpace(c.Email) || string.IsNullOrWhiteSpace(c.Password)))
        {
            throw new InvalidOperationException("Defina EMAIL e PASSWORD para os três gerentes antes de usar --provision-managers.");
        }
        if (credentials.Select(c => c.Email!.Trim().ToUpperInvariant()).Distinct().Count() != credentials.Length)
        {
            throw new InvalidOperationException("Cada gerente precisa de um e-mail diferente.");
        }

        foreach (var (roleName, emailValue, password) in credentials)
        {
            var email = emailValue!.Trim();
            var user = await userManager.FindByEmailAsync(email);
            if (user != null)
            {
                if (await domainDb.TbProfissional.AnyAsync(p => p.IdUser == user.Id))
                {
                    throw new InvalidOperationException($"A conta {email} já pertence a um profissional.");
                }
                var roles = await userManager.GetRolesAsync(user);
                if (roles.Any(r => r != roleName))
                {
                    throw new InvalidOperationException($"A conta {email} já possui outro papel.");
                }
            }
            else
            {
                user = new ApplicationUser { UserName = email, Email = email };
                var creation = await userManager.CreateAsync(user, password!);
                if (!creation.Succeeded)
                {
                    throw new InvalidOperationException($"Não foi possível criar {email}: {string.Join("; ", creation.Errors.Select(e => e.Description))}");
                }
            }

            var role = await roleManager.FindByNameAsync(roleName)
                ?? throw new InvalidOperationException($"Papel {roleName} não encontrado.");

            // Exigência do projeto final: a associação do gerente ao papel diretamente no banco.
            await identityDb.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO [AspNetUserRoles] ([UserId], [RoleId])
                SELECT {user.Id}, {role.Id}
                WHERE NOT EXISTS (
                    SELECT 1 FROM [AspNetUserRoles]
                    WHERE [UserId] = {user.Id} AND [RoleId] = {role.Id}
                )
                """);
            Console.WriteLine($"Gerente {roleName}: {email}");
        }
    }
}
