using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoteHomWebApp.Models;
using VoteHomWebApp.Services;
using Newtonsoft.Json;

namespace VoteHomWebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VotingTestController : ControllerBase
    {
        private readonly IElectionService _electionService;
        private readonly ILogger<VotingTestController> _logger;

        public VotingTestController(IElectionService electionService, ILogger<VotingTestController> logger)
        {
            _electionService = electionService;
            _logger = logger;
        }

        [HttpGet("election/{electionId}/multiple-positions")]
        public async Task<IActionResult> CheckMultiplePositions(int electionId)
        {
            try
            {
                var (hasMultiple, method, message) = await _electionService.CheckMultiplePositionsAsync(electionId);
                
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        electionId = electionId,
                        hasMultiplePositions = hasMultiple,
                        requiredVotingMethod = method,
                        message = message
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking multiple positions for election {ElectionId}", electionId);
                return StatusCode(500, new { success = false, message = "Erro interno do servidor" });
            }
        }

        [HttpPost("election/{electionId}/validate-votes")]
        public async Task<IActionResult> ValidateVotes(int electionId, [FromBody] List<SingleVoteChoice> votes)
        {
            try
            {
                if (votes == null || !votes.Any())
                {
                    return BadRequest(new { success = false, message = "Lista de votos é obrigatória" });
                }

                var (isValid, message) = await _electionService.ValidateVotesAsync(electionId, votes);
                
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        electionId = electionId,
                        isValid = isValid,
                        validationMessage = message,
                        votesCount = votes.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating votes for election {ElectionId}", electionId);
                return StatusCode(500, new { success = false, message = "Erro interno do servidor" });
            }
        }

        [HttpGet("election/{electionId}/system-integrity")]
        public async Task<IActionResult> RunSystemIntegrityTest(int electionId)
        {
            try
            {
                var (success, status, message) = await _electionService.RunSystemIntegrityTestAsync(electionId);
                
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        electionId = electionId,
                        overallStatus = status,
                        testPassed = success,
                        message = message,
                        testedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running system integrity test for election {ElectionId}", electionId);
                return StatusCode(500, new { success = false, message = "Erro interno do servidor" });
            }
        }

        [HttpGet("election/{electionId}/info")]
        public async Task<IActionResult> GetElectionInfo(int electionId)
        {
            try
            {
                var electionInfo = await _electionService.GetElectionInfoAsync();
                var positions = await _electionService.GetAllPositionsWithCandidatesAsync(electionId);
                var (hasMultiple, method, message) = await _electionService.CheckMultiplePositionsAsync(electionId);
                
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        election = electionInfo,
                        positionsCount = positions.Count,
                        positions = positions.Select(p => new
                        {
                            id = p.Id,
                            name = p.Name,
                            candidatesCount = p.Candidates.Count
                        }),
                        multiplePositionsTest = new
                        {
                            hasMultiplePositions = hasMultiple,
                            requiredMethod = method,
                            message = message
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting election info for {ElectionId}", electionId);
                return StatusCode(500, new { success = false, message = "Erro interno do servidor" });
            }
        }
    }
}