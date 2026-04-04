using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Interfaces;

namespace Shop.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
public class HrController : ControllerBase
{
    private readonly IHrService _hr;
    private readonly ICurrentUserService _currentUser;

    public HrController(IHrService hr, ICurrentUserService currentUser)
    {
        _hr = hr;
        _currentUser = currentUser;
    }

    // Employees
    [HttpGet("employees")]
    public async Task<IActionResult> GetEmployees([FromQuery] string? department, [FromQuery] string? status, CancellationToken ct)
        => Ok(await _hr.GetEmployeesAsync(0, department, status, ct));

    [HttpGet("employees/{id}")]
    public async Task<IActionResult> GetEmployee(int id, CancellationToken ct)
    {
        var emp = await _hr.GetEmployeeAsync(0, id, ct);
        return emp == null ? NotFound() : Ok(emp);
    }

    [HttpPost("employees")]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequest req, CancellationToken ct)
        => Ok(await _hr.CreateEmployeeAsync(0, req.EmployeeNumber, req.Name, req.Email, req.Phone, req.Department, req.Position, req.HireDate, req.BaseSalary, req.PayType, req.UserId, _currentUser.Username ?? "system", ct));

    [HttpPut("employees/{id}")]
    public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeRequest req, CancellationToken ct)
    {
        await _hr.UpdateEmployeeAsync(0, id, req.Department, req.Position, req.BaseSalary, req.Status, _currentUser.Username ?? "system", ct);
        return Ok(new { message = "Employee updated" });
    }

    // Attendance
    [HttpPost("attendance/check-in")]
    [Authorize]
    public async Task<IActionResult> CheckIn([FromBody] CheckInRequest req, CancellationToken ct)
        => Ok(await _hr.CheckInAsync(0, req.EmployeeId, _currentUser.Username ?? "system", ct));

    [HttpPost("attendance/check-out")]
    [Authorize]
    public async Task<IActionResult> CheckOut([FromBody] CheckOutRequest req, CancellationToken ct)
    {
        await _hr.CheckOutAsync(0, req.EmployeeId, _currentUser.Username ?? "system", ct);
        return Ok(new { message = "Checked out" });
    }

    [HttpGet("attendance")]
    public async Task<IActionResult> GetAttendance([FromQuery] int? employeeId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(await _hr.GetAttendanceAsync(0, employeeId, from, to, ct));

    [HttpPost("attendance/leave")]
    public async Task<IActionResult> RecordLeave([FromBody] RecordLeaveRequest req, CancellationToken ct)
        => Ok(await _hr.RecordLeaveAsync(0, req.EmployeeId, req.WorkDate, req.LeaveType, _currentUser.Username ?? "system", ct));

    // Payroll
    [HttpPost("payroll/generate")]
    public async Task<IActionResult> GeneratePayroll([FromBody] GeneratePayrollRequest req, CancellationToken ct)
        => Ok(await _hr.GeneratePayrollAsync(0, req.PayPeriod, _currentUser.Username ?? "system", ct));

    [HttpGet("payroll")]
    public async Task<IActionResult> GetPayrolls([FromQuery] string? payPeriod, [FromQuery] int? employeeId, [FromQuery] string? status, CancellationToken ct)
        => Ok(await _hr.GetPayrollsAsync(0, payPeriod, employeeId, status, ct));

    [HttpPut("payroll/{id}/approve")]
    public async Task<IActionResult> ApprovePayroll(int id, CancellationToken ct)
    {
        await _hr.ApprovePayrollAsync(0, id, _currentUser.Username ?? "system", ct);
        return Ok(new { message = "Payroll approved" });
    }

    [HttpPut("payroll/{id}/pay")]
    public async Task<IActionResult> MarkPayrollPaid(int id, CancellationToken ct)
    {
        await _hr.MarkPayrollPaidAsync(0, id, _currentUser.Username ?? "system", ct);
        return Ok(new { message = "Payroll marked as paid" });
    }

    // Dashboard
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
        => Ok(await _hr.GetHrDashboardAsync(0, ct));
}

// Request DTOs
public record CreateEmployeeRequest(string EmployeeNumber, string Name, string? Email, string? Phone, string Department, string Position, DateTime HireDate, decimal BaseSalary, string PayType = "Monthly", int? UserId = null);
public record UpdateEmployeeRequest(string? Department = null, string? Position = null, decimal? BaseSalary = null, string? Status = null);
public record CheckInRequest(int EmployeeId);
public record CheckOutRequest(int EmployeeId);
public record RecordLeaveRequest(int EmployeeId, DateTime WorkDate, string LeaveType);
public record GeneratePayrollRequest(string PayPeriod);
