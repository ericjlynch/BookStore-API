using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BookStore_API.Contracts;
using BookStore_API.Data;
using BookStore_API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookStore_API.Controllers
{
    /// <summary>
    /// Endpoint used to interact with Authors table in SQL Server
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]   
    [ProducesResponseType(StatusCodes.Status200OK)]
    public class AuthorsController : ControllerBase
    {
        private readonly IAuthorRepository _authorRepository;
        private readonly ILoggerService _logger;
        private readonly IMapper _mapper;
        public AuthorsController(IAuthorRepository authorRepository, ILoggerService loggerService, IMapper mapper)
        {
            _authorRepository = authorRepository;
            _logger = loggerService;
            _mapper = mapper;
        }

        /// <summary>
        /// Gets all authors
        /// </summary>
        /// <returns>List of Authors</returns>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAuthors()
        {
            var location = GetControllerActionNames();
            try
            {                
                _logger.LogInfo($"{location}: Attempt to Get all authors");
                var authors = await _authorRepository.FindAll();
                var response = _mapper.Map<IList<AuthorDTO>>(authors);
                _logger.LogInfo("{location} successfully got all authors");
                return Ok(response);
            }
            catch (Exception ex)
            {                
                Debug.WriteLine(ex.Message);
                return InternalError($"{location}: failed for all authors - {ex.Message} - {ex.InnerException}");
            }
        }

        /// <summary>
        /// gets one author
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAuthor(int id)
        {
            var location = GetControllerActionNames();
            try
            {
                _logger.LogInfo("Attempted Get Single Author");
                var author = await _authorRepository.FindById(id);
                if(author == null)
                {
                    _logger.LogWarn($"No author with id of {id} was found");
                    return NotFound();
                }
                var response = _mapper.Map<AuthorDTO>(author);
                _logger.LogInfo("Successfully got an author of id : " + id);
                return Ok(response);
            }
            catch (Exception ex)
            {                
                Debug.WriteLine(ex.Message);
                return InternalError($"{location}: failed for all authors - {ex.Message} - {ex.InnerException}");
            }
        }

        /// <summary>
        /// Creates new author
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize]
        public async Task<IActionResult> CreateAuthor([FromBody] AuthorCreateDTO dto)
        {
            _logger.LogInfo("author submission attempted in CreateAuthor in {[controller]}");
            try
            {
                if(dto == null)
                {
                    _logger.LogWarn($"Empty request was submitted");
                    return BadRequest(ModelState);
                }
                if (!ModelState.IsValid)
                {
                    _logger.LogWarn($"Author data not complete");
                    return BadRequest(ModelState);
                }

                //gives you a populated author data class from the dto
                var author = _mapper.Map<Author>(dto);
                var success = await _authorRepository.Create(author);
                if (!success)
                {
                    return InternalError($"Author Creation Failed");
                }

                //now you're pretty sure it succeeded
                _logger.LogInfo("Created author successfully");

                //Created is an IActionResult.
                return Created("Create", new { author });
            }
            catch (Exception ex)
            {
                return InternalError($"Error executing CreateAuthor: {ex.Message}" );                
            }
        }

        /// <summary>
        /// Updates an author by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="author"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "Administrator, Customer")]
        public async Task<IActionResult> UpdateAuthor(int id, [FromBody] AuthorUpdateDTO dto)
        {
            _logger.LogInfo($"Update author entered with id: {id}");
            try
            {
                if (id < 1 || dto == null  || dto.Id != id )
                {
                    _logger.LogWarn($"Update author with bad data id: {id}");
                    return BadRequest();
                }
                if (!ModelState.IsValid)
                {
                    _logger.LogWarn($"Update author with bad ModelState: {ModelState}");
                    return BadRequest(ModelState);
                }

                var author = _mapper.Map<Author>(dto);
                var success = await _authorRepository.Update(author);
                if (!success)
                {
                    _logger.LogError($"Update author returned unsuccessful: {id}");
                    return InternalError($"UpdateAuthor method failed - {success}");
                }
                _logger.LogInfo($"Update author succeeded for id {id}");
                return NoContent();
            }
            catch (Exception e)
            {
                return InternalError(e.Message);
            }

        }

        /// <summary>
        /// Deletes one Author by Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if(id < 1)
                {
                    return BadRequest();
                }
                //var author = await _authorRepository.FindById(id);
                var isExists = await _authorRepository.IsExists(id);
                if(!isExists)
                {
                    return NotFound();
                }
                var author = await _authorRepository.FindById(id);
                var success = await _authorRepository.Delete(author);
                if (!success)
                {
                    return InternalError($"Author deletion did not succeed: {author.Id}");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return InternalError($"Exception deleting author {id} \n {ex.Message}");
            }
        }
        private string GetControllerActionNames()
        {
            var controller = ControllerContext.ActionDescriptor.ControllerName;
            var action = ControllerContext.ActionDescriptor.ActionName;
            return $"{controller} - {action}";
        }

        private ObjectResult InternalError(string message)
        {
            _logger.LogError($"{message} when attempting to ");
            return StatusCode(500, $"Something went wrong : {message} . Please contact the guy who wrote the thing");
        }
    }
}
