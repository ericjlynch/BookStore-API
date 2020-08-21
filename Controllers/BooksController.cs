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
    /// Interacts with Books table in SQL Server
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly IBookRepository _bookRepository;
        private readonly ILoggerService _logger;
        private readonly IMapper _mapper;
        public BooksController(IBookRepository bookRepository, ILoggerService loggerService, IMapper mapper)
        {
            _bookRepository = bookRepository;
            _logger = loggerService;
            _mapper = mapper;
        }

        /// <summary>
        /// Gets a list of all books
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetBooks()
        {
            var location = GetControllerActionNames();
            try
            {
                _logger.LogInfo($"{location} : Attempted Call");
                var books = await _bookRepository.FindAll();
                var response = _mapper.Map<IList<BookDTO>>(books);
                _logger.LogInfo($"{location} : Successful");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return InternalError($"{location} : {ex.Message} - {ex.InnerException}");
            }
        }

        /// <summary>
        /// Creates a Book in SQL Server
        /// </summary>
        /// <param name="bookDTO"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateBook([FromBody] BookCreateDTO bookDTO)
        {
            var location = GetControllerActionNames();
            try
            {
                _logger.LogInfo($"{location} attempted with {bookDTO.Title}");
                if (bookDTO == null)
                {
                    _logger.LogWarn($"{location} - Empty Request was submitted");
                    return BadRequest(ModelState);
                }
                if (!ModelState.IsValid)
                {
                    _logger.LogWarn($"{location} : Data was Incomplete {bookDTO.Title}");
                    return BadRequest(ModelState);
                }
                var book = _mapper.Map<Book>(bookDTO);
                var success = await _bookRepository.Create(book);
                if (!success)
                {
                    return InternalError("{location} : Creation failed");
                }
                _logger.LogInfo($"{location} : succeeded with {bookDTO.Title}");
                return Created("Create", new { book });

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return InternalError($"{location} - failed to create book {bookDTO.Title} - {ex.Message} - {ex.InnerException}");
            }
        }

        /// <summary>
        /// Gets the book by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetBook(int id)
        {
            var location = GetControllerActionNames();
            try
            {
                _logger.LogInfo($"{location} : Attempted Call for book id: {id}");
                var book = await _bookRepository.FindById(id);
                if (book == null)
                {
                    _logger.LogWarn($"failed to retrieve book {id}");
                    return NotFound();
                }
                var response = _mapper.Map<BookDTO>(book);
                _logger.LogInfo($"{location} : Successful for book id: {id}");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return InternalError($"{location} : {ex.Message} - {ex.InnerException}");
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateBook(int id, [FromBody] BookUpdateDTO dto)
        {
            var location = GetControllerActionNames();
            _logger.LogInfo($"{location} : Update attempt for Book {id} - {dto.Title}");
            try
            {
                if(id < 1 || dto == null)
                {
                    _logger.LogWarn($"{location} - book dto is null for id {id}");
                    return BadRequest();
                }               
               if(id != dto.Id)
                {
                    _logger.LogWarn($"{location} - book id does not match dto.id");
                    return BadRequest();
                }
                var isExists = await _bookRepository.IsExists(id);
                if (!isExists)
                {
                    _logger.LogWarn($"{location} book does not exist: {id}");
                    return NotFound();
                }
                if (!ModelState.IsValid)
                {
                    _logger.LogWarn($"{location} - ModelState is not valid");
                    return BadRequest(ModelState);
                }

                //let's try now
                var book = _mapper.Map<Book>(dto);
                var success = _bookRepository.Update(book);
                _logger.LogInfo($"{location} successfully updated book {id} - {book.Title}");
                return NoContent();
            }
            catch (Exception ex)
            {
                return InternalError("{location} failed for book {id} \n {ex.Message}");
            }
        }

        /// <summary>
        /// Removes a book by its Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(int id)
        {
            var location = GetControllerActionNames();
            try
            {
                _logger.LogInfo($"{location} - Delete attempt on book {id}");
                if(id < 1)
                {
                    _logger.LogWarn($"{location} - book dto is null for id {id}");
                    return BadRequest();
                }
                var isExists = await _bookRepository.IsExists(id);
                if (!isExists)
                {
                    _logger.LogWarn($"{location} book does not exist: {id}");
                    return NotFound();
                }

                //attempt the delete
                var book = await _bookRepository.FindById(id);
                var success = await _bookRepository.Delete(book);
                _logger.LogInfo($"{location} - Book successfully deleted: {id}");
                return NoContent();
            }
            catch (Exception ex)
            {
                return InternalError($"{location} - delete failed for book {id} - {ex.Message} - {ex.InnerException}");
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
