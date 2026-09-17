
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projeto1_IF.Models;
using Projeto1_IF.Data;

public class TbProfissionaisController : Controller
{
    private readonly DB_IFContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TbProfissionaisController(DB_IFContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: TBPROFISSIONALS
    public async Task<IActionResult> Index()
    {
        return View(await _context.TbProfissional.ToListAsync());
    }

    // GET: TBPROFISSIONALS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var tbprofissional = await _context.TbProfissional
            .FirstOrDefaultAsync(m => m.IdProfissional == id);
        if (tbprofissional == null)
        {
            return NotFound();
        }

        return View(tbprofissional);
    }

    // GET: TBPROFISSIONALS/Create
    public IActionResult Create()
    {
        ViewData["IdCidade"] = new SelectList(_context.TbCidade, "IdCidade", "Nome");
        ViewData["IdPlano"] = new SelectList(_context.TbPlano, "IdPlano", "Nome");
        ViewData["IdTipoAcesso"] = new SelectList(_context.TbTipoAcesso, "IdTipoAcesso", "Nome");
        return View();
    }

    // POST: TBPROFISSIONALS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("IdTipoProfissional,IdTipoAcesso,IdCidade,IdUser,Nome,Cpf,CrmCrn,Especialidade,Logradouro,Numero,Bairro,Cep,Cidade,Estado,Ddd1,Ddd2,Telefone1,Telefone2,Salario")] TbProfissional tbprofissional, [Bind("IdPlano")] TbContrato IdContratoNavigation)
    {
        ModelState.Remove("IdUser");
        ModelState.Remove("IdContrato");

        // repopula dropdowns sempre que for necessário retornar a View
        void PopulateSelects()
        {
            ViewData["IdCidade"] = new SelectList(_context.TbCidade, "IdCidade", "Nome", tbprofissional?.IdCidade);
            ViewData["IdPlano"] = new SelectList(_context.TbPlano, "IdPlano", "Nome", IdContratoNavigation?.IdPlano);
            ViewData["IdTipoAcesso"] = new SelectList(_context.TbTipoAcesso, "IdTipoAcesso", "Nome", tbprofissional?.IdTipoAcesso);
        }

        if (!ModelState.IsValid)
        {
            PopulateSelects();
            return View(tbprofissional);
        }

        // cria contrato e trata possíveis erros de BD
        IdContratoNavigation.DataInicio = DateTime.UtcNow;
        IdContratoNavigation.DataFim = IdContratoNavigation.DataInicio.Value.AddMonths(1);
        _context.Add(IdContratoNavigation);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            ModelState.AddModelError(string.Empty, "Erro ao salvar o contrato: " + ex.GetBaseException().Message);
            PopulateSelects();
            return View(tbprofissional);
        }

        // assegura que o contrato realmente existe antes de associar
        tbprofissional.IdContrato = IdContratoNavigation.IdContrato;
        var contratoExists = await _context.TbContrato.AnyAsync(c => c.IdContrato == tbprofissional.IdContrato);
        if (!contratoExists)
        {
            ModelState.AddModelError("IdContrato", "Contrato selecionado não existe.");
            PopulateSelects();
            return View(tbprofissional);
        }

        var userManager = HttpContext.RequestServices.GetService<UserManager<IdentityUser>>();
        if (userManager == null)
        {
            ModelState.AddModelError(string.Empty, "Serviço de usuário indisponível.");
            PopulateSelects();
            return View(tbprofissional);
        }

        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            // usuário não autenticado ou não encontrado -> retorna view com erro em vez de 404
            ModelState.AddModelError(string.Empty, "Usuário não autenticado ou não encontrado.");
            PopulateSelects();
            return View(tbprofissional);
        }

        tbprofissional.IdUser = user.Id;

        _context.Add(tbprofissional);
        try
        {
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            // registra erro de BD e mostra mensagem amigável na View
            ModelState.AddModelError(string.Empty, "Erro ao salvar no banco de dados: " + ex.GetBaseException().Message);
            PopulateSelects();
            return View(tbprofissional);
        }
    }

    // GET: TBPROFISSIONALS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var tbprofissional = await _context.TbProfissional.FindAsync(id);
        if (tbprofissional == null)
        {
            return NotFound();
        }
        return View(tbprofissional);
    }

    // POST: TBPROFISSIONALS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("IdProfissional,IdTipoProfissional,IdContrato,IdTipoAcesso,IdCidade,IdUser,Nome,Cpf,CrmCrn,Especialidade,Logradouro,Numero,Bairro,Cep,Cidade,Estado,Ddd1,Ddd2,Telefone1,Telefone2,Salario,IdCidadeNavigation,IdContratoNavigation,IdTipoAcessoNavigation,TbHoraPacienteProfissional,TbMedicoPaciente,TbPergunta,TbReceitaAlimentarPadrao,TbReceitaMedicaPadrao")] TbProfissional tbprofissional)
    {
        if (id != tbprofissional.IdProfissional)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(tbprofissional);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TbProfissionalExists(tbprofissional.IdProfissional))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(tbprofissional);
    }

    // GET: TBPROFISSIONALS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var tbprofissional = await _context.TbProfissional
            .FirstOrDefaultAsync(m => m.IdProfissional == id);
        if (tbprofissional == null)
        {
            return NotFound();
        }

        return View(tbprofissional);
    }

    // POST: TBPROFISSIONALS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var tbprofissional = await _context.TbProfissional.FindAsync(id);
        if (tbprofissional != null)
        {
            _context.TbProfissional.Remove(tbprofissional);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool TbProfissionalExists(int? id)
    {
        return _context.TbProfissional.Any(e => e.IdProfissional == id);
    }
}
