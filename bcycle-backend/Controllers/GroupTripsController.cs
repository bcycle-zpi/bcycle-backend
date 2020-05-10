using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using bcycle_backend.Models;
using bcycle_backend.Models.Entities;
using bcycle_backend.Models.Requests;
using bcycle_backend.Models.Responses;
using bcycle_backend.Security;
using bcycle_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace bcycle_backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/group-trips")]
    public class GroupTripsController : ControllerBase
    {
        private readonly GroupTripService _tripService;
        private readonly UserService _userService;

        public GroupTripsController(GroupTripService tripService, UserService userService)
        {
            _tripService = tripService;
            _userService = userService;
        }

        // GET /api/group-trips
        [HttpGet]
        public async Task<ActionResult<ResultContainer<List<GroupTripResponse>>>> GetAll() {
            var rawTrips = await _tripService.FindAllUserTripsAsync(User.GetId());
            var trips = rawTrips.Select(
                async trip => await TripAsResponse(trip));
            return new ResultContainer<List<GroupTripResponse>>((await Task.WhenAll(trips)).ToList());
        }

        // POST /api/group-trips
        [HttpPost]
        public async Task<ActionResult<ResultContainer<int>>> Create([FromBody] GroupTripRequest data)
        {
            var trip = await _tripService.CreateAsync(data, User.GetId());
            return new ResultContainer<int>(trip.Id);
        }

        // GET /api/group-trips/:id
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ResultContainer<GroupTripResponse>>> Get(int id)
        {
            var trip = await _tripService.FindAsync(id, User.GetId());
            return await TransformTrip(trip);
        }

        // GET /api/group-trips/:guid
        [AllowAnonymous]
        [HttpGet("{guid:guid}")]
        public async Task<ActionResult<ResultContainer<GroupTripResponse>>> Get(Guid guid)
        {
            var trip = await _tripService.FindPublicGroupTripAsync(guid);
            return await TransformTrip(trip);
        }

        // PUT /api/group-trips/:id
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] GroupTripRequest data)
        {
            var result = await _tripService.UpdateAsync(id, data, User.GetId());
            return CreateResponse(result);
        }

        // DELETE /api/group-trips/:id
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _tripService.RemoveAsync(id, User.GetId());
            return CreateResponse(result);
        }

        // POST /api/group-trips/join/:code
        [HttpPost("join/{code}")]
        public async Task<ActionResult<ResultContainer<int>>> Join(string code)
        {
            var trip = await _tripService.Join(User.GetId(), code);
            if (trip == null) return NotFound();

            return new ResultContainer<int>(trip.Id);
        }

        // POST /group-trips/:tripId/requests/:userid
        [HttpPost("{tripId}/requests/{userId}")]
        public async Task<IActionResult> Accept(int tripId, string userId)
        {
            var result = await _tripService.AcceptRequestAsync(tripId, userId, User.GetId());
            return CreateResponse(result);
        }

        // DELETE /group-trips/:tripId/requests/:userid
        [HttpDelete("{tripId}/requests/{userId}")]
        public async Task<IActionResult> Reject(int tripId, string userId)
        {
            var result = await _tripService.RejectRequestAsync(tripId, userId, User.GetId());
            return CreateResponse(result);
        }

        // DELETE /group-trips/:tripId/participants/:userid
        [HttpDelete("{tripId}/participants/{userId}")]
        public async Task<IActionResult> RemoveParticipant(int tripId, string userId)
        {
            var result = await _tripService.RemoveParticipant(tripId, userId, User.GetId());
            return CreateResponse(result);
        }

        // POST /api/group-trips/:id/share
        [HttpPost("{id}/share")]
        public async Task<ActionResult<ResultContainer<string>>> GetSharingUrl(int id)
        {
            var sharingUrl = await _tripService.EnableSharingAsync($"{Request.Scheme}://{Request.Host}", id, User.GetId());
            if (sharingUrl == null) return BadRequest();

            return new ResultContainer<string>(sharingUrl);
        }

        // DELETE /api/group-trips/:id/share
        [HttpDelete("{id}/share")]
        public async Task<IActionResult> DeleteSharingUrl(int id) =>
            CreateResponse(await _tripService.DisableSharingAsync(id, User.GetId()));

        // We may choose better way for returning responses rather than simple null/non null if we need to
        private IActionResult CreateResponse(object obj) => obj == null ? (IActionResult)NotFound() : Ok();


        private async Task<GroupTripResponse> TripAsResponse(GroupTrip trip) =>
            await _tripService.TripAsResponseAsync(trip, _userService.GetUserInfoAsync, $"{Request.Scheme}://{Request.Host}");

        private async Task<ActionResult<ResultContainer<GroupTripResponse>>> TransformTrip(GroupTrip trip) {
            if (trip == null) return NotFound();
            var response = await TripAsResponse(trip);
            return new ResultContainer<GroupTripResponse>(response);
        }
    }
}
