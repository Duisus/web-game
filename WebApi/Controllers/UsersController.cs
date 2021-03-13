using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
        private readonly IUserRepository userRepository;
        private readonly IMapper mapper;
        private readonly LinkGenerator linkGenerator;

        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        public UsersController(
            IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
        {
            this.userRepository = userRepository;
            this.mapper = mapper;
            this.linkGenerator = linkGenerator;
        }

        [HttpOptions]
        public ActionResult OptionsForUsers()
        {
            Response.Headers.Add("Allow", "GET, POST, OPTIONS");
            return Ok();
        }

        [HttpGet(Name = nameof(GetUsers))]
        [Produces("application/json", "application/xml")]
        public ActionResult<IEnumerable<UserDto>> GetUsers(
            [FromQuery] int pageNumber=1, [FromQuery, Range(1, 20)] int pageSize=10)
        {
            if (pageNumber < 1)
                pageNumber = 1;

            if (pageSize > 20)
                pageSize = 20;
            else if (pageSize < 1)
                pageSize = 1;

            var pageList = userRepository.GetPage(pageNumber, pageSize);
            var users = mapper.Map<IEnumerable<UserDto>>(pageList);

            var paginationHeader = new
            {
                previousPageLink = pageList.HasPrevious
                    ? linkGenerator.GetUriByRouteValues(
                        HttpContext, nameof(GetUsers), new {pageNumber = pageNumber - 1, pageSize = pageSize})
                    : null,

                nextPageLink = pageList.HasNext
                    ? linkGenerator.GetUriByRouteValues(
                        HttpContext, nameof(GetUsers), new {pageNumber = pageNumber + 1, pageSize = pageSize})
                    : null,
                
                totalCount = pageList.TotalCount,
                pageSize = pageList.PageSize,
                currentPage = pageList.CurrentPage,
                totalPages = pageList.TotalPages,
            };
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

            return Ok(users);
        }

        [HttpHead("{userId}")]
        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var foundUser = userRepository.FindById(userId);
            if (foundUser == null || userId == Guid.Empty)
                return NotFound();

            return Ok(mapper.Map<UserDto>(foundUser));
        }

        [HttpPost]
        [Produces("application/json", "application/xml")]
        public ActionResult<Guid> CreateUser([FromBody] UserToCreateDto user)
        {
            if (user == null)
                return BadRequest();
            if (user.Login != null && !IsValidUserLogin(user.Login))
                ModelState.AddModelError(
                    nameof(user.Login),
                    "Login must contains only letters or digits");
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var userEntity = mapper.Map<UserEntity>(user);
            var createdUserEntity = userRepository.Insert(userEntity);

            return CreatedAtRoute(
                nameof(GetUserById),
                new {userId = createdUserEntity.Id},
                createdUserEntity.Id);
        }

        [HttpPut("{userId}")]
        [Produces("application/json", "application/xml")]
        public ActionResult<Guid> UpdateUser(
            [FromRoute] Guid userId, [FromBody] UserToUpdateDto updateUser)
        {
            if (updateUser == null || userId == Guid.Empty)
                return BadRequest();

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var userEntity = mapper.Map(updateUser, new UserEntity(userId));
            userRepository.UpdateOrInsert(userEntity, out var isInserted);

            if (isInserted)
                return CreatedAtRoute(
                    nameof(GetUserById), new {userId = userId}, userId);

            return NoContent();
        }

        [HttpPatch("{userId}")]
        [Produces("application/json", "application/xml")]
        public ActionResult PartiallyUpdateUser(
            [FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserToUpdateDto> patchDoc)
        {
            if (patchDoc == null)
                return BadRequest();

            var user = userRepository.FindById(userId);
            if (user == null)
                return NotFound();
            var updateUser = mapper.Map<UserToUpdateDto>(user);

            patchDoc.ApplyTo(updateUser, ModelState);
            if (!TryValidateModel(updateUser))
                return UnprocessableEntity(ModelState);

            userRepository.Update(
                mapper.Map(updateUser, user));

            return NoContent();
        }

        [HttpDelete("{userId}")]
        public ActionResult DeleteUser([FromRoute] Guid userId)
        {
            if (userRepository.FindById(userId) == null)
                return NotFound();

            userRepository.Delete(userId);
            return NoContent();
        }

        private bool IsValidUserLogin(string login)
        {
            return login.All(char.IsLetterOrDigit);
        }
    }
}