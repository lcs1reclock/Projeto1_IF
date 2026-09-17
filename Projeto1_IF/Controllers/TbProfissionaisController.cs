using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projeto1_IF.Data;
using Projeto1_IF.Models;
using Projeto1_IF.Security;

// Lucas Pedroso do Bomdespacho
[Authorize(Roles = AppRoles.Todos)]
public class TbProfissionaisController : Controller
{
    private readonly DB_IFContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TbProfissionaisController(DB_IFContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var profissionais = await (await ProfissionaisPermitidosAsync())
            .AsNoTracking().OrderBy(p => p.Nome).ToListAsync();
        return View(profissionais);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) 
        {
            return NotFound();
        } 
        var profissional = await (await ProfissionaisPermitidosAsync())
            .AsNoTracking().FirstOrDefaultAsync(p => p.IdProfissional == id);
        return profissional == null ? NotFound() : View(profissional);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        } 
        var profissional = await (await ProfissionaisPermitidosAsync())
            .AsNoTracking().FirstOrDefaultAsync(p => p.IdProfissional == id);
        if (profissional == null) 
        {
            return NotFound();
        } 
        await CarregarCidadesAsync(profissional.IdCidade);
        return View(profissional);
    }

    [HttpPost, ActionName("Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPost(int? id)
    {
        if (id == null)
        {
            return NotFound();
        } 
        var profissional = await (await ProfissionaisPermitidosAsync())
            .FirstOrDefaultAsync(p => p.IdProfissional == id);
        if (profissional == null) return NotFound();

        // O CPF só entra na lista de atualização para os gerentes.
        var valido = EhGerente
            ? await TryUpdateModelAsync(profissional, "",
                p => p.Nome, p => p.Cpf, p => p.CrmCrn, p => p.Especialidade,
                p => p.Logradouro, p => p.Numero, p => p.Bairro, p => p.Cep,
                p => p.IdCidade, p => p.Cidade, p => p.Estado,
                p => p.Ddd1, p => p.Ddd2, p => p.Telefone1, p => p.Telefone2,
                p => p.Salario)
            : await TryUpdateModelAsync(profissional, "",
                p => p.Nome, p => p.CrmCrn, p => p.Especialidade,
                p => p.Logradouro, p => p.Numero, p => p.Bairro, p => p.Cep,
                p => p.IdCidade, p => p.Cidade, p => p.Estado,
                p => p.Ddd1, p => p.Ddd2, p => p.Telefone1, p => p.Telefone2,
                p => p.Salario);

        if (valido)
        {
            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.TbProfissional.AnyAsync(p => p.IdProfissional == id))
                    return NotFound();
                ModelState.AddModelError(string.Empty, "O profissional foi alterado. Recarregue a página e tente novamente.");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Não foi possível salvar as alterações.");
            }
        }

        await CarregarCidadesAsync(profissional.IdCidade);
        return View(profissional);
    }

    [Authorize(Roles = AppRoles.Gerentes)]
    public async Task<IActionResult> Delete(int? id, bool saveChangesError = false)
    {
        if (id == null) return NotFound();
        var profissional = await (await ProfissionaisPermitidosAsync())
            .AsNoTracking().FirstOrDefaultAsync(p => p.IdProfissional == id);
        if (profissional == null) 
        {
            return NotFound();
        }

        ViewData["TemPacientes"] = await _context.TbMedicoPaciente
            .AnyAsync(v => v.IdProfissional == id);
        if (saveChangesError)
        {
            ViewData["ErrorMessage"] = "Não foi possível excluir o profissional porque há registros relacionados.";
        }
        return View(profissional);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Gerentes)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var profissional = await (await ProfissionaisPermitidosAsync())
            .FirstOrDefaultAsync(p => p.IdProfissional == id);
        if (profissional == null) 
        {
            return NotFound();
        }

        if (await _context.TbMedicoPaciente.AnyAsync(v => v.IdProfissional == id))
        {
            ModelState.AddModelError(string.Empty, "Profissionais com pacientes cadastrados não podem ser excluídos.");
            ViewData["TemPacientes"] = true;
            return View("Delete", profissional);
        }

        try
        {
            _context.TbProfissional.Remove(profissional);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            return RedirectToAction(nameof(Delete), new { id, saveChangesError = true });
        }
    }

    private bool EhGerente => User.IsInRole(AppRoles.GerenteGeral)
        || User.IsInRole(AppRoles.GerenteMedico)
        || User.IsInRole(AppRoles.GerenteNutricionista);

    private async Task<IQueryable<TbProfissional>> ProfissionaisPermitidosAsync()
    {
        var consulta = _context.TbProfissional.AsQueryable();
        if (User.IsInRole(AppRoles.GerenteGeral)) return consulta;

        if (User.IsInRole(AppRoles.GerenteMedico))
        {
            var medicos = await _userManager.GetUsersInRoleAsync(AppRoles.Medico);
            var ids = medicos.Select(u => u.Id).ToArray();
            return consulta.Where(p => ids.Contains(p.IdUser));
        }
        if (User.IsInRole(AppRoles.GerenteNutricionista))
        {
            var nutricionistas = await _userManager.GetUsersInRoleAsync(AppRoles.Nutricionista);
            var ids = nutricionistas.Select(u => u.Id).ToArray();
            return consulta.Where(p => ids.Contains(p.IdUser));
        }

        var idAtual = _userManager.GetUserId(User);
        return consulta.Where(p => p.IdUser == idAtual);
    }

    private async Task CarregarCidadesAsync(int? selecionada)
    {
        var cidades = await _context.TbCidade.AsNoTracking().OrderBy(c => c.Nome).ToListAsync();
        ViewData["IdCidade"] = new SelectList(cidades, "IdCidade", "Nome", selecionada);
    }
}
