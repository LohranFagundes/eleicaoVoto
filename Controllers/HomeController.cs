using Microsoft.AspNetCore.Mvc;
using VoteHomWebApp.Models;
using System.Diagnostics;
using VoteHomWebApp.Services;

namespace VoteHomWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IElectionService _electionService;

        public HomeController(ILogger<HomeController> logger, IElectionService electionService)
        {
            _logger = logger;
            _electionService = electionService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var electionInfo = await _electionService.GetElectionInfoAsync();

                if (electionInfo != null)
                {
                    DateTime now = DateTime.Now;

                    if (now >= electionInfo.StartDate && now <= electionInfo.EndDate)
                    {
                        ViewBag.ElectionActive = true;
                        ViewBag.ElectionTitle = electionInfo.Name;
                        ViewBag.ElectionId = electionInfo.Id;
                    }
                    else
                    {
                        ViewBag.ElectionActive = false;
                        ViewBag.ElectionTitle = electionInfo.Name;
                        ViewBag.ElectionStartDate = electionInfo.StartDate.ToString("dd/MM/yyyy HH:mm");
                        ViewBag.ElectionEndDate = electionInfo.EndDate.ToString("dd/MM/yyyy HH:mm");
                    }
                }
                else
                {
                    ViewBag.ElectionActive = false;
                    ViewBag.ErrorMessage = "Nenhuma eleição ativa encontrada.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao conectar à API de eleição.");
                ViewBag.ElectionActive = false;
                ViewBag.ErrorMessage = "Não foi possível conectar ao serviço de eleição. Verifique se a API está rodando.";
            }

            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            // Lógica de login simulada por enquanto
            if (username == "voter" && password == "password") // Exemplo de credenciais
            {
                // Redirecionar para a tela de votação
                return RedirectToAction("Vote", "Home");
            }
            else
            {
                ViewBag.ElectionActive = true; // Manter o formulário de login visível
                ViewBag.LoginError = "Usuário ou senha inválidos.";
                return View("Index");
            }
        }

        public IActionResult Vote()
        {
            // Esta será a tela de votação
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}