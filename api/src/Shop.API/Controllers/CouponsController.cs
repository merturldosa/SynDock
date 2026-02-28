using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Coupons.Commands;
using Shop.Application.Coupons.Queries;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CouponsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CouponsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Admin: Get all coupons
    [HttpGet]
    public async Task<IActionResult> GetCoupons([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetCouponsQuery(page, pageSize));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    // Admin: Create coupon
    [HttpPost]
    public async Task<IActionResult> CreateCoupon([FromBody] CreateCouponRequest request)
    {
        var result = await _mediator.Send(new CreateCouponCommand(
            request.Code, request.Name, request.Description,
            request.DiscountType, request.DiscountValue,
            request.MinOrderAmount, request.MaxDiscountAmount,
            request.StartDate, request.EndDate, request.MaxUsageCount));

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { couponId = result.Data });
    }

    // Admin: Update coupon
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCoupon(int id, [FromBody] UpdateCouponRequest request)
    {
        var result = await _mediator.Send(new UpdateCouponCommand(
            id, request.Name, request.Description,
            request.DiscountType, request.DiscountValue,
            request.MinOrderAmount, request.MaxDiscountAmount,
            request.StartDate, request.EndDate, request.MaxUsageCount, request.IsActive));

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    // Admin: Delete coupon
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCoupon(int id)
    {
        var result = await _mediator.Send(new DeleteCouponCommand(id));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    // Admin: Issue coupon to users
    [HttpPost("{id:int}/issue")]
    public async Task<IActionResult> IssueCoupon(int id, [FromBody] IssueCouponRequest request)
    {
        var result = await _mediator.Send(new IssueCouponCommand(id, request.UserIds));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { issuedCount = result.Data });
    }

    // User: My available coupons
    [HttpGet("my")]
    public async Task<IActionResult> GetMyCoupons()
    {
        var result = await _mediator.Send(new GetMyCouponsQuery());
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    // User: Validate coupon code
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateCoupon([FromBody] ValidateCouponRequest request)
    {
        var result = await _mediator.Send(new ValidateCouponQuery(request.Code, request.OrderAmount));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }
}

public record CreateCouponRequest(
    string Code, string Name, string? Description,
    string DiscountType, decimal DiscountValue,
    decimal MinOrderAmount, decimal? MaxDiscountAmount,
    DateTime StartDate, DateTime EndDate, int MaxUsageCount);

public record UpdateCouponRequest(
    string Name, string? Description,
    string DiscountType, decimal DiscountValue,
    decimal MinOrderAmount, decimal? MaxDiscountAmount,
    DateTime StartDate, DateTime EndDate, int MaxUsageCount, bool IsActive);

public record IssueCouponRequest(List<int>? UserIds);

public record ValidateCouponRequest(string Code, decimal OrderAmount);
