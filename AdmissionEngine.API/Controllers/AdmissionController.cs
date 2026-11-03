using AdmissionEngine.API.Models.DTOS;
using AdmissionEngine.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdmissionEngine.API.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AdmissionController : ControllerBase
    {
        private readonly IAdmissionEngineService _admissionEngineService;

        public AdmissionController(IAdmissionEngineService admissionEngineService)
        {
            _admissionEngineService = admissionEngineService;
        }

        /// <summary>
        /// Evaluates a candidate's eligibility for their chosen course.
        /// </summary>
        /// <param name="studentId">The unique string ID (MongoDB ObjectId) of the student applicant.</param>
        /// <returns>An evaluation result containing eligibility status, remarks, and alternative course recommendations if ineligible.</returns>
        /// <response code="200">Evaluation completed successfully.</response>
        /// <response code="400">Invalid student ID format supplied.</response>
        /// <response code="404">Student candidate record not found.</response>
        /// <response code="500">Internal evaluation server error.</response>
        [HttpPost("evaluate/{studentId}")]
        [ProducesResponseType(typeof(EvaluationResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Evaluate(string studentId)
        {
            if (string.IsNullOrWhiteSpace(studentId) || !MongoDB.Bson.ObjectId.TryParse(studentId, out _))
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid Student ID",
                    Detail = "Student ID must be a valid 24-character hexadecimal MongoDB ObjectId."
                });
            }

            var result = await _admissionEngineService.EvaluateApplicantAsync(studentId);
            return Ok(result);
        }

        /// <summary>
        /// Processes a student's acceptance or rejection response to an admission offer.
        /// </summary>
        /// <param name="responseDto">The offer response payload containing student ID, course ID, and acceptance status.</param>
        /// <returns>A boolean indicating whether the offer response was successfully recorded.</returns>
        /// <response code="200">Offer response processed successfully.</response>
        /// <response code="400">Invalid payload or validation failure.</response>
        /// <response code="500">Internal offer processing error.</response>
        //[Authorize]
        [HttpPost("offer-response")]
        public async Task<IActionResult> RespondToOffer([FromBody] OfferResponseDto responseDto)
        {
            var message = await _admissionEngineService.RespondToCourseOfferAsync(responseDto);
            return Ok(new { message });
        }
    }
}