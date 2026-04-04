"use client";

import { useEffect, useState } from "react";
import {
  Ticket,
  Send,
  ChevronRight,
  X,
  AlertCircle,
  Clock,
  CheckCircle2,
  Star,
} from "lucide-react";
import api from "@/lib/api";

interface TicketMessage {
  id: number;
  sender: string;
  senderRole: "Customer" | "Agent";
  content: string;
  createdAt: string;
}

interface CsTicket {
  id: number;
  ticketNumber: string;
  customerId: number;
  customerName: string;
  customerEmail: string;
  subject: string;
  category: string;
  priority: "Urgent" | "High" | "Normal" | "Low";
  status: "Open" | "InProgress" | "WaitingCustomer" | "Resolved" | "Closed";
  assignedTo?: string;
  satisfactionRating?: number;
  messages: TicketMessage[];
  createdAt: string;
  updatedAt: string;
}

const STATUS_TABS = [
  "All",
  "Open",
  "InProgress",
  "WaitingCustomer",
  "Resolved",
  "Closed",
] as const;

const STATUS_LABELS: Record<string, string> = {
  All: "All",
  Open: "Open",
  InProgress: "In Progress",
  WaitingCustomer: "Waiting Customer",
  Resolved: "Resolved",
  Closed: "Closed",
};

const STATUS_COLORS: Record<string, string> = {
  Open: "bg-blue-100 text-blue-700",
  InProgress: "bg-yellow-100 text-yellow-700",
  WaitingCustomer: "bg-orange-100 text-orange-700",
  Resolved: "bg-emerald-100 text-emerald-700",
  Closed: "bg-gray-100 text-gray-700",
};

const PRIORITY_COLORS: Record<string, string> = {
  Urgent: "bg-red-100 text-red-700",
  High: "bg-orange-100 text-orange-700",
  Normal: "bg-blue-100 text-blue-700",
  Low: "bg-gray-100 text-gray-500",
};

const STATUS_ICONS: Record<string, React.ReactNode> = {
  Open: <AlertCircle size={12} />,
  InProgress: <Clock size={12} />,
  WaitingCustomer: <Clock size={12} />,
  Resolved: <CheckCircle2 size={12} />,
  Closed: <CheckCircle2 size={12} />,
};

export default function CrmTicketsPage() {
  const [tickets, setTickets] = useState<CsTicket[]>([]);
  const [loading, setLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState<string>("All");
  const [selectedTicket, setSelectedTicket] = useState<CsTicket | null>(null);
  const [replyText, setReplyText] = useState("");
  const [replying, setReplying] = useState(false);
  const [updatingStatus, setUpdatingStatus] = useState(false);

  const load = () => {
    setLoading(true);
    api
      .get("/crm/tickets", {
        params:
          statusFilter !== "All" ? { status: statusFilter } : undefined,
      })
      .then((res) => {
        setTickets(res.data);
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  };

  useEffect(load, [statusFilter]);

  const handleReply = async () => {
    if (!selectedTicket || !replyText.trim()) return;
    setReplying(true);
    try {
      await api.post(`/crm/tickets/${selectedTicket.id}/reply`, {
        content: replyText,
      });
      setReplyText("");
      // Refresh ticket data
      const res = await api.get("/crm/tickets");
      const allTickets: CsTicket[] = res.data;
      setTickets(allTickets);
      const updated = allTickets.find((t) => t.id === selectedTicket.id);
      if (updated) setSelectedTicket(updated);
    } catch {
      // error handled silently
    }
    setReplying(false);
  };

  const handleStatusChange = async (newStatus: string) => {
    if (!selectedTicket) return;
    setUpdatingStatus(true);
    try {
      await api.put(`/crm/tickets/${selectedTicket.id}/status`, {
        status: newStatus,
      });
      const res = await api.get("/crm/tickets");
      const allTickets: CsTicket[] = res.data;
      setTickets(allTickets);
      const updated = allTickets.find((t) => t.id === selectedTicket.id);
      if (updated) setSelectedTicket(updated);
    } catch {
      // error handled silently
    }
    setUpdatingStatus(false);
  };

  const formatDate = (d: string) =>
    new Date(d).toLocaleDateString("ko-KR", {
      month: "2-digit",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
    });

  const filteredTickets =
    statusFilter === "All"
      ? tickets
      : tickets.filter((t) => t.status === statusFilter);

  return (
    <div className="flex flex-col h-[calc(100vh-8rem)]">
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-2xl font-bold text-[var(--color-secondary)]">
          CS Tickets
        </h1>
        <span className="text-sm text-gray-500">
          {filteredTickets.length} tickets
        </span>
      </div>

      {/* Status Filter Tabs */}
      <div className="flex gap-1 mb-4 flex-wrap">
        {STATUS_TABS.map((tab) => (
          <button
            key={tab}
            onClick={() => setStatusFilter(tab)}
            className={`px-3 py-1.5 text-sm rounded-full border transition-colors ${
              statusFilter === tab
                ? "bg-[var(--color-secondary)] text-white border-[var(--color-secondary)]"
                : "hover:bg-gray-50 border-gray-200"
            }`}
          >
            {STATUS_LABELS[tab]}
          </button>
        ))}
      </div>

      {loading ? (
        <div className="flex items-center justify-center py-20">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
        </div>
      ) : (
        <div className="flex gap-4 flex-1 min-h-0">
          {/* Ticket List */}
          <div className="w-full lg:w-1/2 xl:w-3/5 overflow-auto">
            {filteredTickets.length === 0 ? (
              <div className="text-center py-20 text-gray-400">
                <Ticket size={48} className="mx-auto mb-3 opacity-40" />
                <p>No tickets found.</p>
              </div>
            ) : (
              <div className="bg-white rounded-xl shadow-sm overflow-hidden">
                <table className="w-full text-sm">
                  <thead className="bg-gray-50 border-b sticky top-0">
                    <tr>
                      <th className="text-left p-3 font-medium text-gray-500">
                        Ticket
                      </th>
                      <th className="text-left p-3 font-medium text-gray-500">
                        Customer
                      </th>
                      <th className="text-center p-3 font-medium text-gray-500">
                        Priority
                      </th>
                      <th className="text-center p-3 font-medium text-gray-500">
                        Status
                      </th>
                      <th className="text-center p-3 font-medium text-gray-500">
                        Rating
                      </th>
                      <th className="w-8" />
                    </tr>
                  </thead>
                  <tbody>
                    {filteredTickets.map((ticket) => (
                      <tr
                        key={ticket.id}
                        onClick={() => setSelectedTicket(ticket)}
                        className={`border-b last:border-0 cursor-pointer transition-colors ${
                          selectedTicket?.id === ticket.id
                            ? "bg-blue-50"
                            : "hover:bg-gray-50"
                        }`}
                      >
                        <td className="p-3">
                          <p className="font-mono text-xs text-gray-400">
                            {ticket.ticketNumber}
                          </p>
                          <p className="font-medium text-[var(--color-secondary)] text-sm truncate max-w-[200px]">
                            {ticket.subject}
                          </p>
                          <p className="text-xs text-gray-400">
                            {ticket.category}
                          </p>
                        </td>
                        <td className="p-3">
                          <p className="text-sm truncate max-w-[100px]">
                            {ticket.customerName}
                          </p>
                        </td>
                        <td className="p-3 text-center">
                          <span
                            className={`px-2 py-0.5 text-xs rounded-full ${PRIORITY_COLORS[ticket.priority]}`}
                          >
                            {ticket.priority}
                          </span>
                        </td>
                        <td className="p-3 text-center">
                          <span
                            className={`inline-flex items-center gap-1 px-2 py-0.5 text-xs rounded-full ${STATUS_COLORS[ticket.status]}`}
                          >
                            {STATUS_ICONS[ticket.status]}
                            {STATUS_LABELS[ticket.status]}
                          </span>
                        </td>
                        <td className="p-3 text-center">
                          {ticket.satisfactionRating ? (
                            <span className="inline-flex items-center gap-0.5 text-xs text-yellow-600">
                              <Star size={10} fill="currentColor" />
                              {ticket.satisfactionRating}
                            </span>
                          ) : (
                            <span className="text-xs text-gray-300">-</span>
                          )}
                        </td>
                        <td className="p-2">
                          <ChevronRight
                            size={14}
                            className="text-gray-300"
                          />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          {/* Detail Panel */}
          <div className="hidden lg:flex w-1/2 xl:w-2/5 flex-col bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
            {selectedTicket ? (
              <>
                {/* Header */}
                <div className="p-4 border-b bg-gray-50">
                  <div className="flex items-start justify-between">
                    <div>
                      <p className="font-mono text-xs text-gray-400 mb-0.5">
                        {selectedTicket.ticketNumber}
                      </p>
                      <h3 className="font-bold text-[var(--color-secondary)]">
                        {selectedTicket.subject}
                      </h3>
                      <p className="text-xs text-gray-500 mt-1">
                        {selectedTicket.customerName} &middot;{" "}
                        {selectedTicket.customerEmail}
                      </p>
                    </div>
                    <button
                      onClick={() => setSelectedTicket(null)}
                      className="p-1 hover:bg-gray-200 rounded"
                    >
                      <X size={16} />
                    </button>
                  </div>

                  <div className="flex items-center gap-2 mt-3">
                    <select
                      value={selectedTicket.status}
                      onChange={(e) => handleStatusChange(e.target.value)}
                      disabled={updatingStatus}
                      className="text-xs border rounded-lg px-2 py-1.5 bg-white disabled:opacity-50"
                    >
                      <option value="Open">Open</option>
                      <option value="InProgress">In Progress</option>
                      <option value="WaitingCustomer">
                        Waiting Customer
                      </option>
                      <option value="Resolved">Resolved</option>
                      <option value="Closed">Closed</option>
                    </select>
                    <span
                      className={`px-2 py-0.5 text-xs rounded-full ${PRIORITY_COLORS[selectedTicket.priority]}`}
                    >
                      {selectedTicket.priority}
                    </span>
                    <span className="text-xs text-gray-400">
                      {selectedTicket.category}
                    </span>
                    {selectedTicket.assignedTo && (
                      <span className="text-xs text-gray-400 ml-auto">
                        Assigned: {selectedTicket.assignedTo}
                      </span>
                    )}
                  </div>
                </div>

                {/* Messages Thread */}
                <div className="flex-1 overflow-auto p-4 space-y-3">
                  {selectedTicket.messages &&
                  selectedTicket.messages.length > 0 ? (
                    selectedTicket.messages.map((msg) => (
                      <div
                        key={msg.id}
                        className={`flex ${msg.senderRole === "Agent" ? "justify-end" : "justify-start"}`}
                      >
                        <div
                          className={`max-w-[80%] rounded-lg px-3 py-2 ${
                            msg.senderRole === "Agent"
                              ? "bg-[var(--color-primary)] text-white"
                              : "bg-gray-100 text-gray-800"
                          }`}
                        >
                          <p className="text-xs font-medium opacity-70 mb-0.5">
                            {msg.sender}
                          </p>
                          <p className="text-sm whitespace-pre-wrap">
                            {msg.content}
                          </p>
                          <p className="text-[10px] opacity-50 mt-1">
                            {formatDate(msg.createdAt)}
                          </p>
                        </div>
                      </div>
                    ))
                  ) : (
                    <div className="text-center py-10 text-gray-400 text-sm">
                      No messages yet.
                    </div>
                  )}
                </div>

                {/* Reply Input */}
                <div className="p-4 border-t bg-gray-50">
                  <div className="flex gap-2">
                    <textarea
                      value={replyText}
                      onChange={(e) => setReplyText(e.target.value)}
                      placeholder="Type your reply..."
                      rows={2}
                      className="flex-1 px-3 py-2 border border-gray-200 rounded-lg text-sm resize-none focus:outline-none focus:border-[var(--color-primary)] focus:ring-1 focus:ring-[var(--color-primary)]"
                      onKeyDown={(e) => {
                        if (e.key === "Enter" && (e.ctrlKey || e.metaKey)) {
                          handleReply();
                        }
                      }}
                    />
                    <button
                      onClick={handleReply}
                      disabled={replying || !replyText.trim()}
                      className="self-end px-4 py-2 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90 disabled:opacity-50 transition-colors"
                    >
                      <Send size={16} />
                    </button>
                  </div>
                  <p className="text-[10px] text-gray-400 mt-1">
                    Ctrl+Enter to send
                  </p>
                </div>
              </>
            ) : (
              <div className="flex items-center justify-center h-full text-gray-400">
                <div className="text-center">
                  <Ticket size={48} className="mx-auto mb-3 opacity-40" />
                  <p className="text-sm">Select a ticket to view details</p>
                </div>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
