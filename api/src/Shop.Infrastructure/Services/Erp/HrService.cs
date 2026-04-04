using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;

namespace Shop.Infrastructure.Services;

public class HrService : IHrService
{
    private readonly IShopDbContext _db;
    public HrService(IShopDbContext db) => _db = db;

    public async Task<List<Employee>> GetEmployeesAsync(int tenantId, string? department = null, string? status = null, CancellationToken ct = default)
    {
        var query = _db.Employees.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(department)) query = query.Where(e => e.Department == department);
        if (!string.IsNullOrEmpty(status)) query = query.Where(e => e.Status == status);
        return await query.OrderBy(e => e.Name).ToListAsync(ct);
    }

    public async Task<Employee?> GetEmployeeAsync(int tenantId, int employeeId, CancellationToken ct = default)
        => await _db.Employees.AsNoTracking().Include(e => e.Attendances.OrderByDescending(a => a.WorkDate).Take(30)).Include(e => e.Payrolls.OrderByDescending(p => p.PayPeriod).Take(12)).FirstOrDefaultAsync(e => e.Id == employeeId, ct);

    public async Task<Employee> CreateEmployeeAsync(int tenantId, string employeeNumber, string name, string? email, string? phone, string department, string position, DateTime hireDate, decimal baseSalary, string payType, int? userId, string createdBy, CancellationToken ct = default)
    {
        var employee = new Employee
        {
            TenantId = tenantId, EmployeeNumber = employeeNumber, Name = name,
            Email = email, Phone = phone, Department = department, Position = position,
            HireDate = hireDate, BaseSalary = baseSalary, PayType = payType,
            UserId = userId, CreatedBy = createdBy
        };
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync(ct);
        return employee;
    }

    public async Task UpdateEmployeeAsync(int tenantId, int employeeId, string? department, string? position, decimal? baseSalary, string? status, string updatedBy, CancellationToken ct = default)
    {
        var emp = await _db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, ct) ?? throw new InvalidOperationException("Employee not found");
        if (department != null) emp.Department = department;
        if (position != null) emp.Position = position;
        if (baseSalary.HasValue) emp.BaseSalary = baseSalary.Value;
        if (status != null) emp.Status = status;
        emp.UpdatedBy = updatedBy;
        emp.UpdatedAt = DateTime.UtcNow;
        if (status == "Terminated") emp.TerminationDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<Attendance> CheckInAsync(int tenantId, int employeeId, string createdBy, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var existing = await _db.Attendances.FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.WorkDate == today, ct);
        if (existing != null) throw new InvalidOperationException("Already checked in today");

        var standardStart = today.AddHours(9); // 9 AM
        var now = DateTime.UtcNow;
        var attendance = new Attendance
        {
            TenantId = tenantId, EmployeeId = employeeId, WorkDate = today,
            CheckInAt = now, Status = now > standardStart.AddMinutes(10) ? "Late" : "Present",
            CreatedBy = createdBy
        };
        _db.Attendances.Add(attendance);
        await _db.SaveChangesAsync(ct);
        return attendance;
    }

    public async Task CheckOutAsync(int tenantId, int employeeId, string updatedBy, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var attendance = await _db.Attendances.FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.WorkDate == today, ct) ?? throw new InvalidOperationException("No check-in found for today");
        attendance.CheckOutAt = DateTime.UtcNow;
        if (attendance.CheckInAt.HasValue)
        {
            var hours = (attendance.CheckOutAt.Value - attendance.CheckInAt.Value).TotalHours;
            attendance.WorkHours = Math.Min(hours, 8);
            attendance.OvertimeHours = Math.Max(0, hours - 8);
        }
        attendance.UpdatedBy = updatedBy;
        attendance.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<Attendance>> GetAttendanceAsync(int tenantId, int? employeeId, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var query = _db.Attendances.AsNoTracking().Include(a => a.Employee).AsQueryable();
        if (employeeId.HasValue) query = query.Where(a => a.EmployeeId == employeeId.Value);
        if (from.HasValue) query = query.Where(a => a.WorkDate >= from.Value);
        if (to.HasValue) query = query.Where(a => a.WorkDate <= to.Value);
        return await query.OrderByDescending(a => a.WorkDate).ToListAsync(ct);
    }

    public async Task<Attendance> RecordLeaveAsync(int tenantId, int employeeId, DateTime workDate, string leaveType, string createdBy, CancellationToken ct = default)
    {
        var attendance = new Attendance
        {
            TenantId = tenantId, EmployeeId = employeeId, WorkDate = workDate.Date,
            Status = "Leave", LeaveType = leaveType, CreatedBy = createdBy
        };
        _db.Attendances.Add(attendance);
        await _db.SaveChangesAsync(ct);
        return attendance;
    }

    public async Task<List<Payroll>> GeneratePayrollAsync(int tenantId, string payPeriod, string createdBy, CancellationToken ct = default)
    {
        var employees = await _db.Employees.Where(e => e.Status == "Active").ToListAsync(ct);
        var payrolls = new List<Payroll>();

        foreach (var emp in employees)
        {
            var existingPayroll = await _db.Payrolls.AnyAsync(p => p.EmployeeId == emp.Id && p.PayPeriod == payPeriod, ct);
            if (existingPayroll) continue;

            // Calculate from attendance
            var periodParts = payPeriod.Split('-');
            var periodYear = int.Parse(periodParts[0]);
            var periodMonth = int.Parse(periodParts[1]);
            var attendances = await _db.Attendances.Where(a => a.EmployeeId == emp.Id && a.WorkDate.Month == periodMonth && a.WorkDate.Year == periodYear).ToListAsync(ct);

            var overtimeHours = attendances.Sum(a => a.OvertimeHours);
            var hourlyRate = emp.BaseSalary / 209; // Korean standard monthly hours
            var overtimePay = (decimal)overtimeHours * hourlyRate * 1.5m;

            var grossPay = emp.BaseSalary + overtimePay;
            var taxAmount = grossPay * 0.033m; // Simplified income tax
            var insuranceAmount = grossPay * 0.0945m; // 4대보험 approximate

            var payroll = new Payroll
            {
                TenantId = tenantId, EmployeeId = emp.Id, PayPeriod = payPeriod,
                BasePay = emp.BaseSalary, OvertimePay = overtimePay,
                TaxAmount = Math.Round(taxAmount), InsuranceAmount = Math.Round(insuranceAmount),
                Deductions = Math.Round(taxAmount + insuranceAmount),
                NetPay = Math.Round(grossPay - taxAmount - insuranceAmount),
                CreatedBy = createdBy
            };
            _db.Payrolls.Add(payroll);
            payrolls.Add(payroll);
        }

        await _db.SaveChangesAsync(ct);
        return payrolls;
    }

    public async Task<List<Payroll>> GetPayrollsAsync(int tenantId, string? payPeriod = null, int? employeeId = null, string? status = null, CancellationToken ct = default)
    {
        var query = _db.Payrolls.AsNoTracking().Include(p => p.Employee).AsQueryable();
        if (!string.IsNullOrEmpty(payPeriod)) query = query.Where(p => p.PayPeriod == payPeriod);
        if (employeeId.HasValue) query = query.Where(p => p.EmployeeId == employeeId.Value);
        if (!string.IsNullOrEmpty(status)) query = query.Where(p => p.Status == status);
        return await query.OrderByDescending(p => p.PayPeriod).ThenBy(p => p.Employee.Name).ToListAsync(ct);
    }

    public async Task ApprovePayrollAsync(int tenantId, int payrollId, string updatedBy, CancellationToken ct = default)
    {
        var payroll = await _db.Payrolls.FirstOrDefaultAsync(p => p.Id == payrollId, ct) ?? throw new InvalidOperationException("Payroll not found");
        payroll.Status = "Approved";
        payroll.UpdatedBy = updatedBy;
        payroll.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkPayrollPaidAsync(int tenantId, int payrollId, string updatedBy, CancellationToken ct = default)
    {
        var payroll = await _db.Payrolls.FirstOrDefaultAsync(p => p.Id == payrollId, ct) ?? throw new InvalidOperationException("Payroll not found");
        payroll.Status = "Paid";
        payroll.PaidAt = DateTime.UtcNow;
        payroll.UpdatedBy = updatedBy;
        payroll.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<object> GetHrDashboardAsync(int tenantId, CancellationToken ct = default)
    {
        var totalEmployees = await _db.Employees.CountAsync(e => e.Status == "Active", ct);
        var byDepartment = await _db.Employees.Where(e => e.Status == "Active").GroupBy(e => e.Department).Select(g => new { Department = g.Key, Count = g.Count() }).ToListAsync(ct);
        var today = DateTime.UtcNow.Date;
        var todayAttendance = await _db.Attendances.CountAsync(a => a.WorkDate == today && a.Status == "Present", ct);
        var onLeave = await _db.Attendances.CountAsync(a => a.WorkDate == today && a.Status == "Leave", ct);
        var totalPayroll = await _db.Payrolls.Where(p => p.Status == "Paid").SumAsync(p => (decimal?)p.NetPay, ct) ?? 0;

        return new { totalEmployees, todayAttendance, onLeave, byDepartment, totalPayrollPaid = totalPayroll };
    }
}
