using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BookStore_API.Contracts;
using BookStore_API.Data;
using BookStore_API.DTOs;
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
        public async Task<IActionResult> GetAuthors()
        {
            try
            {
                _logger.LogInfo("Attempted Get All Authors");
                var authors = await _authorRepository.FindAll();
                var response = _mapper.Map<IList<AuthorDTO>>(authors);
                return Ok(response);
            }
            catch (Exception ex)
            {                
                Debug.WriteLine(ex.Message);
                return InternalError(ex.Message);
            }
        }

        /// <summary>
        /// gets one author
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAuthor(int id)
        {
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
                return InternalError(ex.Message);
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
                return Created("Create", new { author });
            }
            catch (Exception ex)
            {
                return InternalError($"Error executing CreateAuthor: {ex.Message}" );                
            }
        }

        private ObjectResult InternalError(string message)
        {
            _logger.LogError($"{message} when attempting to ");
            return StatusCode(500, $"Something went wrong : {message} . Please contact the guy who wrote the thing");
        }
    }
}
