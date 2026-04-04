"use client";

import { useEffect, useState } from "react";
import api from "@/lib/api";
import toast from "react-hot-toast";
import {
  X,
  Clock,
  LogIn,
  LogOut,
  CalendarDays,
  UserMinus,
} from "lucide-react";

interface AttendanceRecord {
  id: number;
  employeeId: number;
  employeeName?: string;
  employeeNumber?: string;
  workDate: string;
  checkIn: string | null;
  checkOut: string | null;
  workHours: number;
  overtimeHours: number;
  status: string;
  leaveType: string | null;
}

interface Employee {
  id: number;
  employeeNumber: string;
  name: string;
}

const STATUS_COLORS: Record<string, string> = {
  Present: "bg-green-100 text-green-700",
  Absent: "bg-red-100 text-red-700",
  Late: "bg-yellow-100 text-yellow-700",
  Leave: "bg-purple-100 text-purple-700",
};

const LEAVE_TYPES = ["Annual", "Sick", "Personal", "Maternity"];

export default function AttendancePage() {
  const [records, setRecords] = useState<AttendanceRecord[]>([]);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedDate, setSelectedDate] = useState(new Date().toISOString().slice(0, 10));
  const [showLeaveModal, setShowLeaveModal] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const [leaveForm, setLeaveForm] = useState({
    employeeId: "",
    workDate: new Date().toISOString().slice(0, 10),
    leaveType: "Annual",
  });

  const fetchRecords = async () => {
    try {
      const { data } = await api.get("/hr/attendance", { params: { date: selectedDate } });
      setRecords(Array.isArray(data) ? data : data.items ?? []);
    } catch {
      toast.error("Failed to load attendance");
    } finally {
      setLoading(false);
    }
  };

  const fetchEmployees = async () => {
    try {
      const { data } = await api.get("/hr/employees");
      setEmployees(Array.isArray(data) ? data : data.items ?? []);
    } catch {
      /* silent */
    }
  };

  useEffect(() => {
    fetchEmployees();
  }, []);

  useEffect(() => {
    setLoading(true);
    fetchRecords();
  }, [selectedDate]);

  const handleCheckIn = async (employeeId: number) => {
    try {
      await api.post("/hr/attendance/check-in", { employeeId, workDate: selectedDate });
      toast.success("Checked in");
      fetchRecords();
    } catch {
      toast.error("Check-in failed");
    }
  };

  const handleCheckOut = async (employeeId: number) => {
    try {
      await api.post("/hr/attendance/check-out", { employeeId, workDate: selectedDate });
      toast.success("Checked out");
      fetchRecords();
    } catch {
      toast.error("Check-out failed");
    }
  };

  const handleRecordLeave = async () => {
    if (!leaveForm.employeeId) {
      toast.error("Select an employee");
      return;
    }
    setSubmitting(true);
    try {
      await api.post("/hr/attendance/leave", {
        employeeId: Number(leaveForm.employeeId),
        workDate: leaveForm.workDate,
        leaveType: leaveForm.leaveType,
      });
      toast.success("Leave recorded");
      setShowLeaveModal(false);
      setLeaveForm({ employeeId: "", workDate: new Date().toISOString().slice(0, 10), leaveType: "Annual" });
      fetchRecords();
    } catch {
      toast.error("Failed to record leave");
    } finally {
      setSubmitting(false);
    }
  };

  const presentCount = records.filter((r) => r.status === "Present").length;
  const lateCount = records.filter((r) => r.status === "Late").length;
  const absentCount = records.filter((r) => r.status === "Absent").length;
  const leaveCount = records.filter((r) => r.status === "Leave").length;

  if (loading && records.length === 0) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-blue-600 border-t-transparent" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Attendance</h1>
          <p className="text-sm text-gray-500 mt-1">Track employee attendance and leave</p>
        </div>
        <button
          onClick={() => setShowLeaveModal(true)}
          className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors"
        >
          <UserMinus size={16} />
          Record Leave
        </button>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center gap-3 mb-2">
            <div className="w-10 h-10 rounded-lg bg-green-50 flex items-center justify-center">
              <LogIn className="w-5 h-5 text-green-600" />
            </div>
            <span className="text-sm text-gray-500">Present</span>
          </div>
          <p className="text-2xl font-bold text-green-600">{presentCount}</p>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center gap-3 mb-2">
            <div className="w-10 h-10 rounded-lg bg-yellow-50 flex items-center justify-center">
              <Clock className="w-5 h-5 text-yellow-600" />
            </div>
            <span className="text-sm text-gray-500">Late</span>
          </div>
          <p className="text-2xl font-bold text-yellow-600">{lateCount}</p>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center gap-3 mb-2">
            <div className="w-10 h-10 rounded-lg bg-red-50 flex items-center justify-center">
              <LogOut className="w-5 h-5 text-red-600" />
            </div>
            <span className="text-sm text-gray-500">Absent</span>
          </div>
          <p className="text-2xl font-bold text-red-600">{absentCount}</p>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center gap-3 mb-2">
            <div className="w-10 h-10 rounded-lg bg-purple-50 flex items-center justify-center">
              <CalendarDays className="w-5 h-5 text-purple-600" />
            </div>
            <span className="text-sm text-gray-500">On Leave</span>
          </div>
          <p className="text-2xl font-bold text-purple-600">{leaveCount}</p>
        </div>
      </div>

      {/* Date Picker */}
      <div className="flex items-center gap-3">
        <CalendarDays size={18} className="text-gray-400" />
        <input
          type="date"
          value={selectedDate}
          onChange={(e) => setSelectedDate(e.target.value)}
          className="px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 outline-none"
        />
        <span className="text-sm text-gray-500">
          {new Date(selectedDate).toLocaleDateString("en-US", { weekday: "long", year: "numeric", month: "long", day: "numeric" })}
        </span>
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-4 py-3 text-left font-medium text-gray-500">Employee</th>
                <th className="px-4 py-3 text-left font-medium text-gray-500">Date</th>
                <th className="px-4 py-3 text-center font-medium text-gray-500">Check In</th>
                <th className="px-4 py-3 text-center font-medium text-gray-500">Check Out</th>
                <th className="px-4 py-3 text-right font-medium text-gray-500">Work Hours</th>
                <th className="px-4 py-3 text-right font-medium text-gray-500">Overtime</th>
                <th className="px-4 py-3 text-center font-medium text-gray-500">Status</th>
                <th className="px-4 py-3 text-center font-medium text-gray-500">Actions</th>
              </tr>
            </thead>
            <tbody>
              {records.length === 0 ? (
                <tr>
                  <td colSpan={8} className="px-4 py-8 text-center text-gray-400">No attendance records for this date</td>
                </tr>
              ) : (
                records.map((rec) => (
                  <tr key={rec.id} className="border-t border-gray-100 hover:bg-gray-50">
                    <td className="px-4 py-3">
                      <div className="font-medium text-gray-800">{rec.employeeName || `Employee #${rec.employeeId}`}</div>
                      {rec.employeeNumber && <div className="text-xs text-gray-400">{rec.employeeNumber}</div>}
                    </td>
                    <td className="px-4 py-3 text-gray-600">{rec.workDate?.slice(0, 10)}</td>
                    <td className="px-4 py-3 text-center text-gray-600">{rec.checkIn?.slice(11, 16) || "-"}</td>
                    <td className="px-4 py-3 text-center text-gray-600">{rec.checkOut?.slice(11, 16) || "-"}</td>
                    <td className="px-4 py-3 text-right font-medium">{rec.workHours.toFixed(1)}h</td>
                    <td className="px-4 py-3 text-right text-orange-600">{rec.overtimeHours > 0 ? `${rec.overtimeHours.toFixed(1)}h` : "-"}</td>
                    <td className="px-4 py-3 text-center">
                      <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${STATUS_COLORS[rec.status] || "bg-gray-100 text-gray-600"}`}>
                        {rec.status}
                        {rec.leaveType && ` (${rec.leaveType})`}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-center">
                      <div className="flex items-center justify-center gap-1">
                        {!rec.checkIn && rec.status !== "Leave" && (
                          <button
                            onClick={() => handleCheckIn(rec.employeeId)}
                            className="px-2 py-1 text-xs font-medium text-green-600 bg-green-50 rounded-lg hover:bg-green-100 transition-colors"
                          >
                            Check In
                          </button>
                        )}
                        {rec.checkIn && !rec.checkOut && rec.status !== "Leave" && (
                          <button
                            onClick={() => handleCheckOut(rec.employeeId)}
                            className="px-2 py-1 text-xs font-medium text-blue-600 bg-blue-50 rounded-lg hover:bg-blue-100 transition-colors"
                          >
                            Check Out
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Leave Modal */}
      {showLeaveModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-white rounded-xl shadow-xl w-full max-w-md mx-4">
            <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200">
              <h3 className="text-lg font-semibold text-gray-900">Record Leave</h3>
              <button onClick={() => setShowLeaveModal(false)} className="text-gray-400 hover:text-gray-600">
                <X size={20} />
              </button>
            </div>
            <div className="p-6 space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Employee *</label>
                <select
                  value={leaveForm.employeeId}
                  onChange={(e) => setLeaveForm({ ...leaveForm, employeeId: e.target.value })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                >
                  <option value="">Select Employee</option>
                  {employees.map((emp) => (
                    <option key={emp.id} value={emp.id}>{emp.employeeNumber} - {emp.name}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Date</label>
                <input
                  type="date"
                  value={leaveForm.workDate}
                  onChange={(e) => setLeaveForm({ ...leaveForm, workDate: e.target.value })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Leave Type</label>
                <select
                  value={leaveForm.leaveType}
                  onChange={(e) => setLeaveForm({ ...leaveForm, leaveType: e.target.value })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                >
                  {LEAVE_TYPES.map((t) => (
                    <option key={t} value={t}>{t}</option>
                  ))}
                </select>
              </div>
            </div>
            <div className="flex justify-end gap-3 px-6 py-4 border-t border-gray-200">
              <button
                onClick={() => setShowLeaveModal(false)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-gray-100 rounded-lg hover:bg-gray-200 transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={handleRecordLeave}
                disabled={submitting}
                className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-50"
              >
                {submitting ? "Recording..." : "Record Leave"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
