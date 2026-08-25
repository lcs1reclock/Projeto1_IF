using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto1_IF.Models;
// Lucas Pedroso do Bomdespacho
[Authorize]
public class TbAlimentosController : Controller
{
    private readonly DB_IFContext _context;

    public TbAlimentosController(DB_IFContext context)
    {
        _context = context;
    }

    // GET: TBALIMENTOS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.TbAlimento.ToListAsync());
    }

    // GET: TBALIMENTOS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var tbalimento = await _context.TbAlimento
            .FirstOrDefaultAsync(m => m.IdAlimento == id);
        if (tbalimento == null)
        {
            return NotFound();
        }

        return View(tbalimento);
    }

    // GET: TBALIMENTOS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: TBALIMENTOS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("IdAlimento,IdTipoQuantidade,Nome,Carboidrato,VitaminaA,VitaminaB,TbReceitaAlimentarPadraoXAlimento")] TbAlimento tbalimento)
    {
        if (ModelState.IsValid)
        {
            _context.Add(tbalimento);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(tbalimento);
    }

    // GET: TBALIMENTOS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var tbalimento = await _context.TbAlimento.FindAsync(id);
        if (tbalimento == null)
        {
            return NotFound();
        }
        return View(tbalimento);
    }

    // POST: TBALIMENTOS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("IdAlimento,IdTipoQuantidade,Nome,Carboidrato,VitaminaA,VitaminaB,TbReceitaAlimentarPadraoXAlimento")] TbAlimento tbalimento)
    {
        if (id != tbalimento.IdAlimento)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(tbalimento);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TbAlimentoExists(tbalimento.IdAlimento))
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
        return View(tbalimento);
    }

    // GET: TBALIMENTOS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var tbalimento = await _context.TbAlimento
            .FirstOrDefaultAsync(m => m.IdAlimento == id);
        if (tbalimento == null)
        {
            return NotFound();
        }

        return View(tbalimento);
    }

    // POST: TBALIMENTOS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var tbalimento = await _context.TbAlimento.FindAsync(id);
        if (tbalimento != null)
        {
            _context.TbAlimento.Remove(tbalimento);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool TbAlimentoExists(int? id)
    {
        return _context.TbAlimento.Any(e => e.IdAlimento == id);
    }
}
