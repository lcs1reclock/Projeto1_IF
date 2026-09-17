using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projeto1_IF.Models;

// Lucas Pedroso do Bomdespacho
[Authorize]
public class TbPacientesController : Controller
{
    private readonly DB_IFContext _context;

    public TbPacientesController(DB_IFContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var pacientes = await _context.TbPaciente
            .Include(p => p.IdCidadeNavigation)
            .AsNoTracking()
            .OrderBy(p => p.Nome)
            .ToListAsync();
        return View(pacientes);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) {
            return NotFound();
        } 
        var paciente = await _context.TbPaciente
            .Include(p => p.IdCidadeNavigation)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdPaciente == id);
        return paciente == null ? NotFound() : View(paciente);
    }

    public async Task<IActionResult> Create()
    {
        await CarregarCidades();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Nome,Rg,Cpf,DataNascimento,NomeResponsavel,Sexo,Etnia,Endereco,Bairro,IdCidade,TelResidencial,TelComercial,TelCelular,Profissao,FlgAtleta,FlgGestante")] TbPaciente paciente)
    {
        if (ModelState.IsValid)
        {
            try
            {
                _context.TbPaciente.Add(paciente);
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
        var paciente = await _context.TbPaciente.AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdPaciente == id);
        if (paciente == null) {
            return NotFound();
        } 
        await CarregarCidades(paciente.IdCidade);
        return View(paciente);
    }

    // Atualiza apenas os campos do formulário, como no tutorial de CRUD da Microsoft.
    [HttpPost, ActionName("Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPost(int? id)
    {
        if (id == null) {
            return NotFound();
        } 
        var paciente = await _context.TbPaciente.FindAsync(id.Value);
        if (paciente == null) {
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
                if (!await _context.TbPaciente.AnyAsync(p => p.IdPaciente == id.Value)){
                    return NotFound();
                }
                ModelState.AddModelError(string.Empty, "O paciente foi alterado por outra operação. Recarregue a página e tente novamente.");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Não foi possível atualizar o paciente. Verifique os dados e tente novamente.");
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
        var paciente = await _context.TbPaciente
            .Include(p => p.IdCidadeNavigation)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdPaciente == id);
        if (paciente == null) {
            return NotFound();
        } 
        if (saveChangesError) {
            ViewData["ErrorMessage"] = "Não foi possível excluir o paciente. Verifique se há registros vinculados a ele.";
        }
        return View(paciente);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var paciente = await _context.TbPaciente.FindAsync(id);
        if (paciente == null) {
            return RedirectToAction(nameof(Index));
        } 
        try
        {
            _context.TbPaciente.Remove(paciente);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            return RedirectToAction(nameof(Delete), new { id, saveChangesError = true });
        }
    }

    private async Task CarregarCidades(int? cidadeSelecionada = null)
    {
        var cidades = await _context.TbCidade.AsNoTracking()
            .OrderBy(c => c.Nome).ToListAsync();
        ViewData["IdCidade"] = new SelectList(cidades, "IdCidade", "Nome", cidadeSelecionada);
    }
}
