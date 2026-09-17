using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projeto1_IF.Data;
using Projeto1_IF.Models;
using Projeto1_IF.Security;

namespace Projeto1_IF.Areas.Identity.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly DB_IFContext _context;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager, DB_IFContext context,
        ILogger<RegisterModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; private set; }
    public IList<AuthenticationScheme> ExternalLogins { get; private set; } = [];
    public SelectList Cidades { get; private set; } = default!;
    public SelectList Planos { get; private set; } = default!;

    public class InputModel
    {
        [Required, EmailAddress, Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password), Display(Name = "Senha")]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Compare(nameof(Password)), Display(Name = "Confirme a senha")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required, Display(Name = "Profissão")]
        public string Profissao { get; set; } = string.Empty;

        [Required, StringLength(100), Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Required, StringLength(15), Display(Name = "CPF")]
        public string Cpf { get; set; } = string.Empty;

        [StringLength(20), Display(Name = "CRM/CRN")]
        public string? CrmCrn { get; set; }

        [StringLength(100), Display(Name = "Especialidade")]
        public string? Especialidade { get; set; }

        [StringLength(100), Display(Name = "Logradouro")]
        public string? Logradouro { get; set; }

        [Required, StringLength(10), Display(Name = "Número")]
        public string Numero { get; set; } = string.Empty;

        [Required, StringLength(100), Display(Name = "Bairro")]
        public string Bairro { get; set; } = string.Empty;

        [Required, StringLength(10), Display(Name = "CEP")]
        public string Cep { get; set; } = string.Empty;

        [Required, Range(1, int.MaxValue), Display(Name = "Cidade")]
        public int IdCidade { get; set; }

        [Required, Range(1, int.MaxValue), Display(Name = "Plano")]
        public int IdPlano { get; set; }

        [StringLength(2), Display(Name = "DDD")]
        public string? Ddd1 { get; set; }

        [StringLength(25), Display(Name = "Telefone")]
        public string? Telefone1 { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        await CarregarOpcoesAsync();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        if (Input.Profissao is not (AppRoles.Medico or AppRoles.Nutricionista))
            ModelState.AddModelError("Input.Profissao", "Escolha Médico ou Nutricionista.");

        var cidadeExiste = await _context.TbCidade.AnyAsync(c => c.IdCidade == Input.IdCidade);
        if (!cidadeExiste)
            ModelState.AddModelError("Input.IdCidade", "Selecione uma cidade válida.");

        var planoExiste = await _context.TbPlano.AnyAsync(p => p.IdPlano == Input.IdPlano);
        if (!planoExiste)
            ModelState.AddModelError("Input.IdPlano", "Selecione um plano válido.");

        if (!ModelState.IsValid)
        {
            await CarregarOpcoesAsync();
            return Page();
        }

        var user = new ApplicationUser { UserName = Input.Email, Email = Input.Email };
        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var erro in result.Errors)
                ModelState.AddModelError(string.Empty, erro.Description);
            await CarregarOpcoesAsync();
            return Page();
        }

        try
        {
            var roleResult = await _userManager.AddToRoleAsync(user, Input.Profissao);
            if (!roleResult.Succeeded)
                throw new InvalidOperationException("Não foi possível atribuir a profissão à conta.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            var tipoNome = Input.Profissao == AppRoles.Medico ? "Médico" : "Nutricionista";
            var tipo = await _context.TbTipoProfissional.FirstOrDefaultAsync(t => t.Nome == tipoNome);
            if (tipo == null)
            {
                tipo = new TbTipoProfissional { Nome = tipoNome };
                _context.TbTipoProfissional.Add(tipo);
                await _context.SaveChangesAsync();
            }

            var inicio = DateTime.Now;
            var contrato = new TbContrato
            {
                IdPlano = Input.IdPlano,
                DataInicio = inicio,
                DataFim = inicio.AddMonths(1)
            };
            _context.TbContrato.Add(contrato);
            await _context.SaveChangesAsync();

            _context.TbProfissional.Add(new TbProfissional
            {
                IdUser = user.Id,
                IdTipoProfissional = tipo.IdTipoProfissional,
                IdContrato = contrato.IdContrato,
                IdCidade = Input.IdCidade,
                Nome = Input.Nome.Trim(),
                Cpf = Input.Cpf.Trim(),
                CrmCrn = Input.CrmCrn?.Trim(),
                Especialidade = Input.Especialidade?.Trim(),
                Logradouro = Input.Logradouro?.Trim(),
                Numero = Input.Numero.Trim(),
                Bairro = Input.Bairro.Trim(),
                Cep = Input.Cep.Trim(),
                Ddd1 = Input.Ddd1?.Trim(),
                Telefone1 = Input.Telefone1?.Trim()
            });
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            // Identity e os dados do profissional usam contextos diferentes.
            // Se a segunda etapa falhar, a conta recém-criada é removida.
            _logger.LogError(ex, "Falha ao registrar profissional {UserId}", user.Id);
            var rollback = await _userManager.DeleteAsync(user);
            if (!rollback.Succeeded)
                _logger.LogError("Não foi possível remover a conta incompleta {UserId}", user.Id);
            ModelState.AddModelError(string.Empty,
                "Não foi possível concluir o cadastro. Por favor, tente novamente.");
            await CarregarOpcoesAsync();
            return Page();
        }

        _logger.LogInformation("Profissional registrado: {UserId}", user.Id);
        await _signInManager.SignInAsync(user, isPersistent: false);
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Content("~/TbProfissionais"));
    }

    private async Task CarregarOpcoesAsync()
    {
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        var cidades = await _context.TbCidade.AsNoTracking().OrderBy(c => c.Nome).ToListAsync();
        var planos = await _context.TbPlano.AsNoTracking().OrderBy(p => p.Nome).ToListAsync();
        Cidades = new SelectList(cidades, "IdCidade", "Nome", Input.IdCidade);
        Planos = new SelectList(planos, "IdPlano", "Nome", Input.IdPlano);
    }
}
