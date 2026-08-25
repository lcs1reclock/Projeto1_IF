
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto1_IF.Models;

// Lucas Pedroso do Bomdespacho
[Authorize]
public class TbCidadesController : Controller
{
    private readonly DB_IFContext _context;

    public TbCidadesController(DB_IFContext context)
    {
        _context = context;
    }

    // GET: TBCIDADES
    public async Task<IActionResult> Index()    
    {
        return View(await _context.TbCidade.ToListAsync());
    }

    // GET: TBCIDADES/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var tbcidade = await _context.TbCidade
            .FirstOrDefaultAsync(m => m.IdCidade == id);
        if (tbcidade == null)
        {
            return NotFound();
        }

        return View(tbcidade);
    }

    // GET: TBCIDADES/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: TBCIDADES/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("IdCidade,IdEstado,Nome,IdEstadoNavigation,TbPaciente,TbProfissional")] TbCidade tbcidade)
    {
        if (ModelState.IsValid)
        {
            _context.Add(tbcidade);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(tbcidade);
    }

    // GET: TBCIDADES/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var tbcidade = await _context.TbCidade.FindAsync(id);
        if (tbcidade == null)
        {
            return NotFound();
        }
        return View(tbcidade);
    }

    // POST: TBCIDADES/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("IdCidade,IdEstado,Nome,IdEstadoNavigation,TbPaciente,TbProfissional")] TbCidade tbcidade)
    {
        if (id != tbcidade.IdCidade)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(tbcidade);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TbCidadeExists(tbcidade.IdCidade))
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
        return View(tbcidade);
    }

    // GET: TBCIDADES/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var tbcidade = await _context.TbCidade
            .FirstOrDefaultAsync(m => m.IdCidade == id);
        if (tbcidade == null)
        {
            return NotFound();
        }

        return View(tbcidade);
    }

    // POST: TBCIDADES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var tbcidade = await _context.TbCidade.FindAsync(id);
        if (tbcidade != null)
        {
            _context.TbCidade.Remove(tbcidade);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool TbCidadeExists(int? id)
    {
        return _context.TbCidade.Any(e => e.IdCidade == id);
    }
}
