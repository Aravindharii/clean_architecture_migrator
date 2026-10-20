using LibraryManagement.Api.Common.Responses;
using LibraryManagement.Api.DTOs;
using LibraryManagement.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.Controllers;

[ApiController]
[Route("api/admin/webhooks")]
[Authorize(Roles = "Admin")]
public class WebhooksController : ControllerBase
{
    private readonly IWebhookSubscriptionService _webhookSubscriptionService;

    public WebhooksController(IWebhookSubscriptionService webhookSubscriptionService)
    {
        _webhookSubscriptionService = webhookSubscriptionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var subscriptions = await _webhookSubscriptionService.GetAllAsync();

        return Ok(new ApiResponse<IEnumerable<WebhookSubscriptionDto>>
        {
            Success = true,
            Message = "Webhook subscriptions retrieved successfully.",
            Data = subscriptions
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var subscription = await _webhookSubscriptionService.GetByIdAsync(id);

        return Ok(new ApiResponse<WebhookSubscriptionDto>
        {
            Success = true,
            Message = "Webhook subscription retrieved successfully.",
            Data = subscription
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WebhookSubscriptionCreateRequest request)
    {
        var result = await _webhookSubscriptionService.CreateAsync(request);

        return CreatedAtAction(nameof(GetById), new { id = result.Subscription.Id }, new ApiResponse<WebhookSubscriptionCreatedResponse>
        {
            Success = true,
            Message = "Webhook subscription created successfully.",
            Data = result
        });
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] WebhookSubscriptionUpdateRequest request)
    {
        var subscription = await _webhookSubscriptionService.UpdateAsync(id, request);

        return Ok(new ApiResponse<WebhookSubscriptionDto>
        {
            Success = true,
            Message = "Webhook subscription updated successfully.",
            Data = subscription
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _webhookSubscriptionService.DeleteAsync(id);

        return NoContent();
    }
}
