using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VoteHomWebApp.Models;
using VoteHomWebApp.Services;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Linq;

namespace VoteHomWebApp.Controllers
{
    public class VoteController : Controller
    {
        private readonly IElectionService _electionService;
        private readonly ILogger<VoteController> _logger;

        public VoteController(IElectionService electionService, ILogger<VoteController> logger)
        {
            _electionService = electionService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            // Redirecionar sempre para a tela pré-login
            return await PreLogin();
        }

        [HttpGet]
        public async Task<IActionResult> PreLogin()
        {
            _logger.LogInformation("Accessing PreLogin - checking election status");
            
            var electionInfo = await _electionService.GetElectionInfoAsync();
            
            if (electionInfo == null)
            {
                _logger.LogWarning("No election found");
                ViewBag.StatusMessage = "Nenhuma eleição disponível para votação no momento.";
                ViewBag.ElectionStatus = "not_found";
                return View("PreLogin", new ElectionInfo { Name = "Sistema de Votação", Id = 0, StartDate = DateTime.Now, EndDate = DateTime.Now });
            }

            // Obter status detalhado da eleição
            var (status, message) = await _electionService.GetElectionStatusMessageAsync(electionInfo.Id);
            ViewBag.StatusMessage = message;
            ViewBag.ElectionStatus = status;
            
            _logger.LogInformation("Election {ElectionId} status: {Status}", electionInfo.Id, status);
            
            return View("PreLogin", electionInfo);
        }

        [HttpGet]
        public async Task<IActionResult> Login()
        {
            var electionInfo = await _electionService.GetElectionInfoAsync();
            
            // Se não há eleição ou não está ativa, redirecionar para PreLogin
            if (electionInfo == null || !electionInfo.IsVotingPeriod)
            {
                _logger.LogInformation("Login access denied - redirecting to PreLogin. Election active: {IsActive}", 
                    electionInfo?.IsVotingPeriod ?? false);
                return RedirectToAction("PreLogin");
            }

            // Eleição ativa - mostrar formulário de login
            _logger.LogInformation("Election active - showing login form. ElectionId: {ElectionId}", electionInfo.Id);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            try
            {
                _logger.LogInformation("Starting login process for user");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model state is invalid");
                    return View(model);
                }

                _logger.LogInformation("Getting election info for login POST");
                var electionInfo = await _electionService.GetElectionInfoAsync();
                
                // Se não há eleição ou não está ativa, redirecionar para PreLogin
                if (electionInfo == null || !electionInfo.IsVotingPeriod)
                {
                    _logger.LogWarning("Login POST blocked - redirecting to PreLogin. Election active: {IsActive}", 
                        electionInfo?.IsVotingPeriod ?? false);
                    return RedirectToAction("PreLogin");
                }

                _logger.LogInformation("Validating election {ElectionId}", electionInfo.Id);
                // Validar se a eleição está disponível para votação
                var (isElectionValid, electionErrorMessage) = await _electionService.ValidateElectionAsync(electionInfo.Id);
                if (!isElectionValid)
                {
                    _logger.LogWarning("Election {ElectionId} is not valid for voting: {ErrorMessage}", electionInfo.Id, electionErrorMessage);
                    ModelState.AddModelError("", electionErrorMessage);
                    return View(model);
                }

                _logger.LogInformation("Validating user credentials");
                var (isUserValid, token, voterName, userErrorMessage) = await _electionService.ValidateUserAsync(model.CPF, model.Password, electionInfo.Id);
                if (!isUserValid)
                {
                    _logger.LogWarning("User validation failed: {ErrorMessage}", userErrorMessage);
                    
                    // Special handling for "election expired" messages that might be timezone issues
                    if (userErrorMessage.Contains("encerrada") || userErrorMessage.Contains("expirou") || userErrorMessage.Contains("sincronização"))
                    {
                        // Double-check the election status
                        bool isActuallyExpired = await _electionService.IsElectionExpiredAsync(electionInfo.Id);
                        _logger.LogInformation("Double-checking election expiration: API says expired, actual check = {IsExpired}", isActuallyExpired);
                        
                        if (!isActuallyExpired)
                        {
                            ModelState.AddModelError("", "Detectado possível problema de sincronização de horário no servidor. A eleição parece estar ativa. Aguarde alguns minutos e tente novamente. Se o problema persistir, contate o suporte técnico.");
                        }
                        else
                        {
                            ModelState.AddModelError("", userErrorMessage);
                        }
                    }
                    // Check if the election has expired for generic errors
                    else if (userErrorMessage.Contains("CPF ou senha") || userErrorMessage.Contains("Resposta inválida") || userErrorMessage.Contains("servidor"))
                    {
                        bool isExpired = await _electionService.IsElectionExpiredAsync(electionInfo.Id);
                        if (isExpired)
                        {
                            var (status, message) = await _electionService.GetElectionStatusMessageAsync(electionInfo.Id);
                            ModelState.AddModelError("", message);
                        }
                        else
                        {
                            ModelState.AddModelError("", userErrorMessage);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", userErrorMessage);
                    }
                    
                    return View(model);
                }

                _logger.LogInformation("Creating claims for user {VoterName}", voterName);
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, voterName),
                    new Claim("CPF", model.CPF),
                    new Claim("access_token", token),
                    new Claim("voter_name", voterName)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10) // Token de votação expira em 10 minutos
                };

                _logger.LogInformation("Signing in user");
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                    new ClaimsPrincipal(claimsIdentity), authProperties);

                _logger.LogInformation("Login successful, redirecting to SelectPosition");
                return RedirectToAction("SelectPosition");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during login process");
                throw; // Re-throw to let the developer exception page catch it
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SelectPosition()
        {
            try
            {
                var electionInfo = await _electionService.GetElectionInfoAsync();
                
                // Bloquear acesso se a eleição não estiver ativa
                if (electionInfo == null || !electionInfo.IsVotingPeriod)
                {
                    _logger.LogWarning("SelectPosition access blocked - election not active");
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction("PreLogin");
                }
                var token = User.FindFirst("access_token")?.Value ?? "";
                var voterCpf = User.FindFirst("CPF")?.Value ?? "";
                
                _logger.LogInformation("Checking if user has already voted in election {ElectionId}", electionInfo.Id);
                
                // Verificar se o usuário já votou
                if (!string.IsNullOrEmpty(token))
                {
                    bool hasVoted = await _electionService.HasVotedAsync(electionInfo.Id, token);
                    
                    if (hasVoted)
                    {
                        _logger.LogInformation("User has already voted, showing receipt");
                        
                        // Buscar voto local armazenado
                        var localVotes = _electionService.GetLocalVotes();
                        var userVote = localVotes.FirstOrDefault(v => v.VoterCpf == voterCpf && v.ElectionId == electionInfo.Id);
                        
                        if (userVote != null)
                        {
                            // Criar confirmação baseada no voto salvo
                            var confirmation = new VoteConfirmation
                            {
                                VoterName = User.FindFirst("voter_name")?.Value ?? "Eleitor",
                                ElectionName = electionInfo.Name,
                                VoteId = userVote.ReceiptToken,
                                VoteDateTime = userVote.VotedAt,
                                CPF = voterCpf.Length >= 11 ? $"{voterCpf.Substring(0, 3)}.***.**{voterCpf.Substring(9, 2)}" : voterCpf
                            };
                            
                            ViewBag.AlreadyVoted = true;
                            return View("Success", confirmation);
                        }
                        else
                        {
                            // Voto existe na API mas não localmente - mostrar mensagem genérica
                            ViewBag.AlreadyVoted = true;
                            ViewBag.Message = "Você já votou nesta eleição.";
                            return View("AlreadyVoted");
                        }
                    }
                }
                
                // Se não votou, procede com a votação normal
                
                // Check with API if this election requires multiple voting
                var (hasMultiplePositions, requiredMethod, methodMessage) = await _electionService.CheckMultiplePositionsAsync(electionInfo.Id);
                
                var positions = await _electionService.GetAllPositionsWithCandidatesAsync(electionInfo.Id);
                
                if (!positions.Any())
                {
                    ViewBag.ErrorMessage = "Nenhuma posição disponível para votação.";
                    return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
                }

                // Pass election info to the view
                ViewBag.ElectionName = electionInfo.Name;
                ViewBag.ElectionTitle = electionInfo.Title;
                ViewBag.HasMultiplePositions = hasMultiplePositions;
                ViewBag.RequiredVotingMethod = requiredMethod;
                ViewBag.ApiMessage = methodMessage;
                
                _logger.LogInformation("Election {ElectionId}: HasMultiple={HasMultiple}, Method={Method}, Message={Message}", 
                    electionInfo.Id, hasMultiplePositions, requiredMethod, methodMessage);

                return View(positions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SelectPosition while checking vote status");
                // Em caso de erro na verificação, permite prosseguir com a votação
                var electionInfo = await _electionService.GetElectionInfoAsync();
                
                // Check with API if this election requires multiple voting
                var (hasMultiplePositions, requiredMethod, methodMessage) = await _electionService.CheckMultiplePositionsAsync(electionInfo.Id);
                
                var positions = await _electionService.GetAllPositionsWithCandidatesAsync(electionInfo.Id);
                
                ViewBag.ElectionName = electionInfo.Name;
                ViewBag.ElectionTitle = electionInfo.Title;
                ViewBag.HasMultiplePositions = hasMultiplePositions;
                ViewBag.RequiredVotingMethod = requiredMethod;
                ViewBag.ApiMessage = methodMessage;
                
                _logger.LogInformation("Election {ElectionId} (fallback): HasMultiple={HasMultiple}, Method={Method}, Message={Message}", 
                    electionInfo.Id, hasMultiplePositions, requiredMethod, methodMessage);
                
                return View(positions);
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Vote(int positionId, int currentStep = 1)
        {
            var electionInfo = await _electionService.GetElectionInfoAsync();
            
            // Bloquear acesso se a eleição não estiver ativa
            if (electionInfo == null || !electionInfo.IsVotingPeriod)
            {
                _logger.LogWarning("Vote access blocked - election not active");
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("PreLogin");
            }
            
            // Check with API if this election requires multiple voting
            var (hasMultiplePositions, requiredMethod, methodMessage) = await _electionService.CheckMultiplePositionsAsync(electionInfo.Id);
            
            // If API detects multiple positions, redirect to multiple voting
            if (hasMultiplePositions)
            {
                TempData["ApiMessage"] = "A API detectou múltiplos cargos. Redirecionando para votação múltipla obrigatória.";
                return RedirectToAction("StartMultipleVote");
            }
            
            var positions = await _electionService.GetAllPositionsWithCandidatesAsync(electionInfo.Id);
            
            var currentPosition = positions.FirstOrDefault(p => p.Id == positionId);
            if (currentPosition == null)
            {
                return RedirectToAction("SelectPosition");
            }

            // Use candidates from the position instead of calling a separate API
            var candidates = currentPosition.Candidates;

            // Verificar se candidatos têm fotos e carregá-las
            foreach (var candidate in candidates)
            {
                if (string.IsNullOrEmpty(candidate.PhotoBase64) && string.IsNullOrEmpty(candidate.PhotoUrl))
                {
                    var photoData = await _electionService.GetCandidatePhotoAsync(candidate.Id);
                    if (photoData != null)
                    {
                        candidate.PhotoUrl = photoData.PhotoUrl;
                        candidate.PhotoBase64 = photoData.PhotoBase64;
                    }
                }
            }
            
            ViewBag.PositionName = currentPosition.Name;
            ViewBag.ElectionName = electionInfo.Name;
            ViewBag.CurrentStep = currentStep;
            ViewBag.TotalSteps = positions.Count;
            ViewBag.PositionId = positionId;

            return View(candidates);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SubmitVote(int? candidateId, string voteType, int positionId)
        {
            try
            {
                _logger.LogInformation("Starting vote submission - positionId: {PositionId}, candidateId: {CandidateId}, voteType: {VoteType}", 
                    positionId, candidateId, voteType);

                var electionInfo = await _electionService.GetElectionInfoAsync();
                if (electionInfo == null)
                {
                    _logger.LogError("Election info is null during vote submission");
                    ViewBag.ErrorMessage = "Erro: Informações da eleição não encontradas.";
                    return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
                }
                
                // Check with API if this election requires multiple voting - prevent individual votes
                var (hasMultiplePositions, requiredMethod, methodMessage) = await _electionService.CheckMultiplePositionsAsync(electionInfo.Id);
                
                if (hasMultiplePositions)
                {
                    _logger.LogWarning("Attempted individual vote submission for multiple position election {ElectionId}", electionInfo.Id);
                    TempData["ErrorMessage"] = "Esta eleição possui múltiplos cargos. Use a votação múltipla obrigatória.";
                    return RedirectToAction("StartMultipleVote");
                }

                var token = User.FindFirst("access_token")?.Value ?? "";
                var voterCpf = User.FindFirst("CPF")?.Value ?? "";
                var voterName = User.FindFirst("voter_name")?.Value ?? "";
                
                _logger.LogInformation("Token available: {HasToken}, VoterName: {VoterName}", !string.IsNullOrEmpty(token), voterName);

                _logger.LogInformation("Election info retrieved - ID: {ElectionId}, Name: {ElectionName}", 
                    electionInfo.Id, electionInfo.Name);

                // Criar voto
                var vote = new VoteChoice
                {
                    PositionId = positionId,
                    CandidateId = candidateId,
                    IsBlankVote = voteType == "blank",
                    IsNullVote = voteType == "null",
                    Justification = null
                };

                _logger.LogInformation("Vote object created successfully");

                // Submeter voto à API
                var receipt = await _electionService.SubmitVoteAsync(vote, token, electionInfo.Id);
                
                if (receipt != null)
                {
                    _logger.LogInformation("Vote submitted successfully, receipt token: {ReceiptToken}", receipt.ReceiptToken);
                    
                    // Salvar voto localmente para auditoria
                    var voteStorage = new VoteStorage
                    {
                        VoterCpf = voterCpf,
                        ElectionId = electionInfo.Id,
                        VotedAt = DateTime.Now,
                        ReceiptToken = receipt.ReceiptToken,
                        VoteHash = receipt.VoteHash,
                        Votes = receipt.VoteDetails.Select(vd => new VoteChoice
                        {
                            PositionName = vd.PositionName,
                            CandidateName = vd.CandidateName,
                            CandidateNumber = vd.CandidateNumber,
                            IsBlankVote = vd.IsBlankVote,
                            IsNullVote = vd.IsNullVote
                        }).ToList()
                    };

                    _electionService.SaveVoteLocally(voteStorage);

                    // Criar confirmação
                    var confirmation = new VoteConfirmation
                    {
                        VoterName = voterName,
                        ElectionName = receipt.ElectionTitle,
                        VoteId = receipt.ReceiptToken,
                        VoteDateTime = receipt.VotedAt,
                        CPF = voterCpf.Length >= 11 ? $"{voterCpf.Substring(0, 3)}.***.**{voterCpf.Substring(9, 2)}" : voterCpf
                    };

                    return View("Success", confirmation);
                }
                else
                {
                    _logger.LogError("Vote submission returned null receipt");
                    ViewBag.ErrorMessage = "Erro ao processar voto. A API não retornou um comprovante válido.";
                    return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during vote submission process");
                ViewBag.ErrorMessage = $"Erro interno: {ex.Message}";
                return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
            }
        }


        [Authorize]
        [HttpGet]
        public IActionResult VoteHistory()
        {
            var votes = _electionService.GetLocalVotes();
            return View(votes);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> StartMultipleVote()
        {
            try
            {
                var electionInfo = await _electionService.GetElectionInfoAsync();
                if (electionInfo == null)
                {
                    TempData["ErrorMessage"] = "Nenhuma eleição disponível.";
                    return RedirectToAction("Index");
                }

                var positions = await _electionService.GetAllPositionsWithCandidatesAsync(electionInfo.Id);
                if (positions.Count <= 1)
                {
                    TempData["ErrorMessage"] = "Votação múltipla não é necessária para apenas um cargo.";
                    return RedirectToAction("SelectPosition");
                }

                // Load candidate photos for all positions
                foreach (var position in positions)
                {
                    foreach (var candidate in position.Candidates)
                    {
                        if (string.IsNullOrEmpty(candidate.PhotoBase64) && string.IsNullOrEmpty(candidate.PhotoUrl))
                        {
                            var photoData = await _electionService.GetCandidatePhotoAsync(candidate.Id);
                            if (photoData != null)
                            {
                                candidate.PhotoUrl = photoData.PhotoUrl;
                                candidate.PhotoBase64 = photoData.PhotoBase64;
                            }
                        }
                    }
                }

                // Initialize multiple vote session
                var session = new MultipleVoteSession
                {
                    AllPositions = positions.OrderBy(p => p.OrderPosition).ToList(),
                    CurrentPositionIndex = 0
                };

                // Store in TempData for persistence across requests
                TempData["MultipleVoteSession"] = JsonConvert.SerializeObject(session);
                ViewBag.ElectionName = electionInfo.Name;

                return View("MultipleVote", session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting multiple vote");
                TempData["ErrorMessage"] = "Erro ao iniciar votação múltipla.";
                return RedirectToAction("SelectPosition");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SubmitMultiplePositionVote(int positionId, int? candidateId, string voteType)
        {
            try
            {
                // Retrieve session from TempData
                var sessionJson = TempData["MultipleVoteSession"] as string;
                if (string.IsNullOrEmpty(sessionJson))
                {
                    TempData["ErrorMessage"] = "Sessão de votação expirou. Reinicie o processo.";
                    return RedirectToAction("SelectPosition");
                }

                var session = JsonConvert.DeserializeObject<MultipleVoteSession>(sessionJson);
                if (session == null)
                {
                    TempData["ErrorMessage"] = "Erro na sessão de votação.";
                    return RedirectToAction("SelectPosition");
                }

                // Find current position and candidate info
                var currentPosition = session.AllPositions.FirstOrDefault(p => p.Id == positionId);
                if (currentPosition == null)
                {
                    TempData["ErrorMessage"] = "Cargo não encontrado.";
                    return RedirectToAction("SelectPosition");
                }

                // Create vote choice for current position
                var voteChoice = new SingleVoteChoice
                {
                    PositionId = positionId,
                    PositionName = currentPosition.Name,
                    CandidateId = candidateId,
                    IsBlankVote = voteType == "blank",
                    IsNullVote = voteType == "null",
                    VoteType = voteType
                };

                if (voteType == "candidate" && candidateId.HasValue)
                {
                    var candidate = currentPosition.Candidates.FirstOrDefault(c => c.Id == candidateId.Value);
                    voteChoice.CandidateName = candidate?.Name ?? "Candidato não encontrado";
                }
                else if (voteType == "blank")
                {
                    voteChoice.CandidateName = "Voto em Branco";
                }
                else if (voteType == "null")
                {
                    voteChoice.CandidateName = "Voto Nulo";
                }

                // Add vote to session
                session.VoteChoices[positionId] = voteChoice;

                // Check if we're done with all positions
                if (session.IsComplete)
                {
                    // Show confirmation page instead of submitting directly
                    TempData["MultipleVoteSession"] = JsonConvert.SerializeObject(session);
                    var electionInfo = await _electionService.GetElectionInfoAsync();
                    ViewBag.ElectionName = electionInfo?.Name;
                    
                    return View("MultipleVoteConfirmation", session);
                }
                else
                {
                    // Move to next position
                    session.CurrentPositionIndex++;
                    TempData["MultipleVoteSession"] = JsonConvert.SerializeObject(session);

                    var electionInfo = await _electionService.GetElectionInfoAsync();
                    ViewBag.ElectionName = electionInfo?.Name;

                    return View("MultipleVote", session);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in multiple position vote");
                TempData["ErrorMessage"] = "Erro ao processar voto.";
                return RedirectToAction("SelectPosition");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> PreviousPosition()
        {
            try
            {
                var sessionJson = TempData["MultipleVoteSession"] as string;
                if (string.IsNullOrEmpty(sessionJson))
                {
                    return RedirectToAction("SelectPosition");
                }

                var session = JsonConvert.DeserializeObject<MultipleVoteSession>(sessionJson);
                if (session == null || session.CurrentPositionIndex <= 0)
                {
                    return RedirectToAction("SelectPosition");
                }

                // Move to previous position
                session.CurrentPositionIndex--;
                
                // Remove vote for current position if it exists
                var currentPositionId = session.CurrentPosition?.Id;
                if (currentPositionId.HasValue && session.VoteChoices.ContainsKey(currentPositionId.Value))
                {
                    session.VoteChoices.Remove(currentPositionId.Value);
                }

                TempData["MultipleVoteSession"] = JsonConvert.SerializeObject(session);
                
                var electionInfo = await _electionService.GetElectionInfoAsync();
                ViewBag.ElectionName = electionInfo?.Name;

                return View("MultipleVote", session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error going to previous position");
                return RedirectToAction("SelectPosition");
            }
        }

        private async Task<IActionResult> SubmitAllVotes(MultipleVoteSession session)
        {
            try
            {
                var token = User.FindFirst("access_token")?.Value ?? "";
                var voterCpf = User.FindFirst("CPF")?.Value ?? "";
                var voterName = User.FindFirst("voter_name")?.Value ?? "";
                var electionInfo = await _electionService.GetElectionInfoAsync();

                if (electionInfo == null)
                {
                    TempData["ErrorMessage"] = "Erro: Informações da eleição não encontradas.";
                    return RedirectToAction("SelectPosition");
                }

                // Validate votes with API before submitting
                var votesToValidate = session.VoteChoices.Values.ToList();
                var (isValidVotes, validationMessage) = await _electionService.ValidateVotesAsync(electionInfo.Id, votesToValidate);
                
                if (!isValidVotes)
                {
                    _logger.LogWarning("Vote validation failed: {ValidationMessage}", validationMessage);
                    TempData["ErrorMessage"] = $"Validação dos votos falhou: {validationMessage}";
                    return RedirectToAction("SelectPosition");
                }
                
                _logger.LogInformation("Votes validated successfully by API: {ValidationMessage}", validationMessage);
                
                // Convert to API format
                var multipleVote = new MultipleVoteModel
                {
                    Votes = session.VoteChoices.Values.ToList(),
                    Justification = "Votação múltipla obrigatória - validada pela API"
                };

                // Submit to API using multiple vote endpoint
                var receipt = await _electionService.SubmitMultipleVotesAsync(multipleVote, token, electionInfo.Id);

                if (receipt != null)
                {
                    _logger.LogInformation("Multiple votes submitted successfully, receipt token: {ReceiptToken}", receipt.ReceiptToken);
                    
                    // Save votes locally for audit
                    var voteStorage = new VoteStorage
                    {
                        VoterCpf = voterCpf,
                        ElectionId = electionInfo.Id,
                        VotedAt = DateTime.Now,
                        ReceiptToken = receipt.ReceiptToken,
                        VoteHash = receipt.VoteHash,
                        Votes = receipt.VoteDetails.Select(vd => new VoteChoice
                        {
                            PositionName = vd.PositionName,
                            CandidateName = vd.CandidateName,
                            CandidateNumber = vd.CandidateNumber,
                            IsBlankVote = vd.IsBlankVote,
                            IsNullVote = vd.IsNullVote
                        }).ToList()
                    };

                    _electionService.SaveVoteLocally(voteStorage);

                    // Create confirmation
                    var confirmation = new VoteConfirmation
                    {
                        VoterName = voterName,
                        ElectionName = receipt.ElectionTitle,
                        VoteId = receipt.ReceiptToken,
                        VoteDateTime = receipt.VotedAt,
                        CPF = voterCpf.Length >= 11 ? $"{voterCpf.Substring(0, 3)}.***.**{voterCpf.Substring(9, 2)}" : voterCpf
                    };

                    return View("Success", confirmation);
                }
                else
                {
                    _logger.LogError("Multiple vote submission returned null receipt");
                    TempData["ErrorMessage"] = "Erro ao processar votação múltipla.";
                    return RedirectToAction("SelectPosition");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting multiple votes");
                TempData["ErrorMessage"] = $"Erro ao submeter votação: {ex.Message}";
                return RedirectToAction("SelectPosition");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SubmitFinalMultipleVotes()
        {
            try
            {
                // Retrieve session from TempData
                var sessionJson = TempData["MultipleVoteSession"] as string;
                if (string.IsNullOrEmpty(sessionJson))
                {
                    TempData["ErrorMessage"] = "Sessão de votação expirou. Reinicie o processo.";
                    return RedirectToAction("SelectPosition");
                }

                var session = JsonConvert.DeserializeObject<MultipleVoteSession>(sessionJson);
                if (session == null || !session.IsComplete)
                {
                    TempData["ErrorMessage"] = "Votação não está completa. Reinicie o processo.";
                    return RedirectToAction("SelectPosition");
                }

                // Submit all votes to API
                return await SubmitAllVotes(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in final multiple votes submission");
                TempData["ErrorMessage"] = "Erro ao finalizar votação múltipla.";
                return RedirectToAction("SelectPosition");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index");
        }
    }
}