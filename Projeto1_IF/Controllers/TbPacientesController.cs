using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projeto1_IF.Data;
using Projeto1_IF.Models;
using Projeto1_IF.Security;

// Lucas Pedroso do Bomdespacho
[Authorize(Roles = AppRoles.Profissionais)]
public class TbPacientesController : Controller
{
    private readonly DB_IFContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TbPacientesController(DB_IFContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var profissionalId = await ProfissionalAtualIdAsync();
        if (profissionalId == null) 
        {
            return Forbid();
        } 
        var pacientes = await PacientesPermitidos(profissionalId.Value)
            .Include(p => p.IdCidadeNavigation).AsNoTracking()
            .OrderBy(p => p.Nome).ToListAsync();
        return View(pacientes);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) {
            return NotFound();
        } 
        var profissionalId = await ProfissionalAtualIdAsync();
        if (profissionalId == null) 
        {
            return Forbid();
        } 
        var paciente = await PacientesPermitidos(profissionalId.Value)
            .Include(p => p.IdCidadeNavigation).AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdPaciente == id);
        return paciente == null ? NotFound() : View(paciente);
    }

    public async Task<IActionResult> Create()
    {
        if (await ProfissionalAtualIdAsync() == null) return Forbid();
        await CarregarCidades();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Nome,Rg,Cpf,DataNascimento,NomeResponsavel,Sexo,Etnia,Endereco,Bairro,IdCidade,TelResidencial,TelComercial,TelCelular,Profissao,FlgAtleta,FlgGestante")] TbPaciente paciente)
    {
        var profissionalId = await ProfissionalAtualIdAsync();
        if (profissionalId == null) 
        {
            return Forbid();
        } 
        if (ModelState.IsValid)
        {
            try
            {
                _context.TbMedicoPaciente.Add(new TbMedicoPaciente
                {
                    IdProfissional = profissionalId.Value,
                    IdPacienteNavigation = paciente
                });
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Não foi possível salvar o paciente. Verifique os dados e tente novamente.");
            }
        }
        await CarregarCidades(paciente.IdCidade);
        return View(paciente);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) {
            return NotFound();
        } 
        var profissionalId = await ProfissionalAtualIdAsync();
        if (profissionalId == null) 
        {
            return Forbid();
        } 
        var paciente = await PacientesPermitidos(profissionalId.Value).AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdPaciente == id);
        if (paciente == null) return NotFound();
        await CarregarCidades(paciente.IdCidade);
        return View(paciente);
    }

    [HttpPost, ActionName("Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPost(int? id)
    {
        if (id == null) {
            return NotFound();
        } 
        var profissionalId = await ProfissionalAtualIdAsync();
        if (profissionalId == null) 
        {
            return Forbid();
        } 
        var paciente = await PacientesPermitidos(profissionalId.Value)
            .FirstOrDefaultAsync(p => p.IdPaciente == id);
        if (paciente == null) 
        {
            return NotFound();
        } 

        if (await TryUpdateModelAsync(paciente, "",
            p => p.Nome, p => p.Rg, p => p.Cpf, p => p.DataNascimento,
            p => p.NomeResponsavel, p => p.Sexo, p => p.Etnia,
            p => p.Endereco, p => p.Bairro, p => p.IdCidade,
            p => p.TelResidencial, p => p.TelComercial, p => p.TelCelular,
            p => p.Profissao, p => p.FlgAtleta, p => p.FlgGestante))
        {
            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.TbPaciente.AnyAsync(p => p.IdPaciente == id.Value))
                    return NotFound();
                ModelState.AddModelError(string.Empty, "O paciente foi alterado. Recarregue a página e tente novamente.");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Não foi possível atualizar o paciente.");
            }
        }
        await CarregarCidades(paciente.IdCidade);
        return View("Edit", paciente);
    }

    public async Task<IActionResult> Delete(int? id, bool saveChangesError = false)
    {
        if (id == null) {
            return NotFound();
        } 
        var profissionalId = await ProfissionalAtualIdAsync();
        if (profissionalId == null) 
        {
            return Forbid();
        } 
        var paciente = await PacientesPermitidos(profissionalId.Value)
            .Include(p => p.IdCidadeNavigation).AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdPaciente == id);
        if (paciente == null) {
            return NotFound();
        } 
        if (saveChangesError) {
            ViewData["ErrorMessage"] = "Não foi possível excluir o paciente. Há outros registros vinculados a ele.";
        }
        return View(paciente);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var profissionalId = await ProfissionalAtualIdAsync();
        if (profissionalId == null) 
        {
            return Forbid();
        } 
        var vinculos = await _context.TbMedicoPaciente
            .Where(v => v.IdPaciente == id && v.IdProfissional == profissionalId)
            .ToListAsync();
        if (vinculos.Count == 0) 
        {
            return NotFound();
        } 

        var outroProfissional = await _context.TbMedicoPaciente
            .AnyAsync(v => v.IdPaciente == id && v.IdProfissional != profissionalId);
        _context.TbMedicoPaciente.RemoveRange(vinculos);
        if (!outroProfissional)
        {
            var paciente = await _context.TbPaciente.FindAsync(id);
            if (paciente != null) 
            {
                _context.TbPaciente.Remove(paciente);
            } 
        }
        try
        {
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            return RedirectToAction(nameof(Delete), new { id, saveChangesError = true });
        }
    }

    private async Task<int?> ProfissionalAtualIdAsync()
    {
        var userId = _userManager.GetUserId(User);
        return await _context.TbProfissional.Where(p => p.IdUser == userId)
            .Select(p => (int?)p.IdProfissional).FirstOrDefaultAsync();
    }

    private IQueryable<TbPaciente> PacientesPermitidos(int profissionalId) =>
        _context.TbPaciente.Where(p => p.TbMedicoPaciente
            .Any(v => v.IdProfissional == profissionalId));

    private async Task CarregarCidades(int? selecionada = null)
    {
        var cidades = await _context.TbCidade.AsNoTracking()
            .OrderBy(c => c.Nome).ToListAsync();
        ViewData["IdCidade"] = new SelectList(cidades, "IdCidade", "Nome", selecionada);
    }
}
