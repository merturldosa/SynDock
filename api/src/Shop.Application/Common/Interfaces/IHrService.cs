using Shop.Domain.Entities;

namespace Shop.Application.Common.Interfaces;

public interface IHrService
{
    // Employees
    Task<List<Employee>> GetEmployeesAsync(int tenantId, string? department = null, string? status = null, CancellationToken ct = default);
    Task<Employee?> GetEmployeeAsync(int tenantId, int employeeId, CancellationToken ct = default);
    Task<Employee> CreateEmployeeAsync(int tenantId, string employeeNumber, string name, string? email, string? phone, string department, string position, DateTime hireDate, decimal baseSalary, string payType, int? userId, string createdBy, CancellationToken ct = default);
    Task UpdateEmployeeAsync(int tenantId, int employeeId, string? department, string? position, decimal? baseSalary, string? status, string updatedBy, CancellationToken ct = default);

    // Attendance
    Task<Attendance> CheckInAsync(int tenantId, int employeeId, string createdBy, CancellationToken ct = default);
    Task CheckOutAsync(int tenantId, int employeeId, string updatedBy, CancellationToken ct = default);
    Task<List<Attendance>> GetAttendanceAsync(int tenantId, int? employeeId, DateTime? from, DateTime? to, CancellationToken ct = default);
    Task<Attendance> RecordLeaveAsync(int tenantId, int employeeId, DateTime workDate, string leaveType, string createdBy, CancellationToken ct = default);

    // Payroll
    Task<List<Payroll>> GeneratePayrollAsync(int tenantId, string payPeriod, string createdBy, CancellationToken ct = default);
    Task<List<Payroll>> GetPayrollsAsync(int tenantId, string? payPeriod = null, int? employeeId = null, string? status = null, CancellationToken ct = default);
    Task ApprovePayrollAsync(int tenantId, int payrollId, string updatedBy, CancellationToken ct = default);
    Task MarkPayrollPaidAsync(int tenantId, int payrollId, string updatedBy, CancellationToken ct = default);

    // Analytics
    Task<object> GetHrDashboardAsync(int tenantId, CancellationToken ct = default);
}
