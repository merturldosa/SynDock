"use client";

import { useEffect, useState } from "react";
import {
  Search,
  Eye,
  ShoppingCart,
  CreditCard,
  Star,
  RotateCcw,
  Ticket,
  Gift,
  Monitor,
  Users,
  Tag,
  TrendingUp,
  BarChart3,
} from "lucide-react";
import api from "@/lib/api";

interface JourneyEvent {
  id: number;
  type:
    | "PageView"
    | "ProductView"
    | "AddToCart"
    | "Purchase"
    | "Review"
    | "Return"
    | "CsTicket"
    | "CouponUsed";
  description: string;
  metadata?: Record<string, string>;
  createdAt: string;
}

interface CustomerAnalytics {
  totalCustomers: number;
  activeCustomers: number;
  totalSegments: number;
  totalTags: number;
}

const EVENT_CONFIG: Record<
  string,
  { icon: React.ReactNode; color: string; bgColor: string }
> = {
  PageView: {
    icon: <Monitor size={14} />,
    color: "text-gray-600",
    bgColor: "bg-gray-100",
  },
  ProductView: {
    icon: <Eye size={14} />,
    color: "text-blue-600",
    bgColor: "bg-blue-100",
  },
  AddToCart: {
    icon: <ShoppingCart size={14} />,
    color: "text-orange-600",
    bgColor: "bg-orange-100",
  },
  Purchase: {
    icon: <CreditCard size={14} />,
    color: "text-emerald-600",
    bgColor: "bg-emerald-100",
  },
  Review: {
    icon: <Star size={14} />,
    color: "text-yellow-600",
    bgColor: "bg-yellow-100",
  },
  Return: {
    icon: <RotateCcw size={14} />,
    color: "text-red-600",
    bgColor: "bg-red-100",
  },
  CsTicket: {
    icon: <Ticket size={14} />,
    color: "text-purple-600",
    bgColor: "bg-purple-100",
  },
  CouponUsed: {
    icon: <Gift size={14} />,
    color: "text-pink-600",
    bgColor: "bg-pink-100",
  },
};

export default function CrmJourneyPage() {
  const [userId, setUserId] = useState("");
  const [searchInput, setSearchInput] = useState("");
  const [events, setEvents] = useState<JourneyEvent[]>([]);
  const [loading, setLoading] = useState(false);
  const [searched, setSearched] = useState(false);
  const [analytics, setAnalytics] = useState<CustomerAnalytics | null>(null);
  const [analyticsLoading, setAnalyticsLoading] = useState(true);

  useEffect(() => {
    setAnalyticsLoading(true);
    api
      .get("/crm/analytics/customers")
      .then((res) => setAnalytics(res.data))
      .catch(() => {})
      .finally(() => setAnalyticsLoading(false));
  }, []);

  const handleSearch = () => {
    const id = searchInput.trim();
    if (!id) return;
    setUserId(id);
    setSearched(true);
    setLoading(true);
    api
      .get(`/crm/journey/${id}`)
      .then((res) => setEvents(res.data))
      .catch(() => setEvents([]))
      .finally(() => setLoading(false));
  };

  const formatDate = (d: string) =>
    new Date(d).toLocaleDateString("ko-KR", {
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
    });

  const groupEventsByDate = (evts: JourneyEvent[]) => {
    const groups: Record<string, JourneyEvent[]> = {};
    evts.forEach((e) => {
      const dateKey = new Date(e.createdAt).toLocaleDateString("ko-KR", {
        year: "numeric",
        month: "long",
        day: "numeric",
      });
      if (!groups[dateKey]) groups[dateKey] = [];
      groups[dateKey].push(e);
    });
    return groups;
  };

  const grouped = groupEventsByDate(events);

  return (
    <div>
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">
        Customer Journey
      </h1>

      {/* Analytics Panel */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        {[
          {
            label: "Total Customers",
            value: analytics?.totalCustomers ?? "-",
            icon: <Users size={20} />,
            color: "text-blue-600",
            bg: "bg-blue-50",
          },
          {
            label: "Active Customers",
            value: analytics?.activeCustomers ?? "-",
            icon: <TrendingUp size={20} />,
            color: "text-emerald-600",
            bg: "bg-emerald-50",
          },
          {
            label: "Segments",
            value: analytics?.totalSegments ?? "-",
            icon: <BarChart3 size={20} />,
            color: "text-purple-600",
            bg: "bg-purple-50",
          },
          {
            label: "Tags",
            value: analytics?.totalTags ?? "-",
            icon: <Tag size={20} />,
            color: "text-orange-600",
            bg: "bg-orange-50",
          },
        ].map((stat) => (
          <div
            key={stat.label}
            className="bg-white rounded-xl shadow-sm border border-gray-100 p-4"
          >
            <div className="flex items-center gap-3">
              <div className={`p-2 rounded-lg ${stat.bg} ${stat.color}`}>
                {stat.icon}
              </div>
              <div>
                <p className="text-xs text-gray-500">{stat.label}</p>
                <p className="text-xl font-bold text-[var(--color-secondary)]">
                  {analyticsLoading ? (
                    <span className="inline-block w-8 h-5 bg-gray-200 rounded animate-pulse" />
                  ) : typeof stat.value === "number" ? (
                    stat.value.toLocaleString()
                  ) : (
                    stat.value
                  )}
                </p>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* User Search */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-4 mb-6">
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Search Customer Journey
        </label>
        <div className="flex gap-2">
          <div className="relative flex-1">
            <Search
              size={16}
              className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
            />
            <input
              type="text"
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && handleSearch()}
              placeholder="Enter User ID"
              className="w-full pl-9 pr-4 py-2.5 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-[var(--color-primary)] focus:ring-1 focus:ring-[var(--color-primary)]"
            />
          </div>
          <button
            onClick={handleSearch}
            disabled={!searchInput.trim()}
            className="px-5 py-2.5 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90 disabled:opacity-50 transition-colors"
          >
            Search
          </button>
        </div>
      </div>

      {/* Journey Timeline */}
      {loading ? (
        <div className="flex items-center justify-center py-20">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
        </div>
      ) : searched && events.length === 0 ? (
        <div className="text-center py-20 text-gray-400">
          <Monitor size={48} className="mx-auto mb-3 opacity-40" />
          <p>No journey events found for User #{userId}.</p>
        </div>
      ) : searched ? (
        <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-6">
          <h2 className="text-sm font-medium text-gray-500 mb-4">
            User #{userId} &mdash; {events.length} events
          </h2>

          <div className="space-y-6">
            {Object.entries(grouped).map(([dateLabel, dayEvents]) => (
              <div key={dateLabel}>
                <p className="text-xs font-medium text-gray-400 mb-3 sticky top-0 bg-white">
                  {dateLabel}
                </p>
                <div className="relative pl-6 border-l-2 border-gray-100 space-y-4">
                  {dayEvents.map((event) => {
                    const cfg = EVENT_CONFIG[event.type] || {
                      icon: <Monitor size={14} />,
                      color: "text-gray-600",
                      bgColor: "bg-gray-100",
                    };
                    return (
                      <div key={event.id} className="relative">
                        {/* Timeline dot */}
                        <div
                          className={`absolute -left-[1.625rem] w-5 h-5 rounded-full flex items-center justify-center ${cfg.bgColor} ${cfg.color}`}
                        >
                          {cfg.icon}
                        </div>

                        <div className="ml-2">
                          <div className="flex items-center gap-2">
                            <span
                              className={`text-xs font-medium px-1.5 py-0.5 rounded ${cfg.bgColor} ${cfg.color}`}
                            >
                              {event.type}
                            </span>
                            <span className="text-[10px] text-gray-400">
                              {new Date(event.createdAt).toLocaleTimeString(
                                "ko-KR",
                                {
                                  hour: "2-digit",
                                  minute: "2-digit",
                                  second: "2-digit",
                                }
                              )}
                            </span>
                          </div>
                          <p className="text-sm text-gray-700 mt-0.5">
                            {event.description}
                          </p>
                          {event.metadata &&
                            Object.keys(event.metadata).length > 0 && (
                              <div className="flex flex-wrap gap-1.5 mt-1">
                                {Object.entries(event.metadata).map(
                                  ([k, v]) => (
                                    <span
                                      key={k}
                                      className="text-[10px] px-1.5 py-0.5 bg-gray-50 text-gray-500 rounded"
                                    >
                                      {k}: {v}
                                    </span>
                                  )
                                )}
                              </div>
                            )}
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            ))}
          </div>
        </div>
      ) : null}
    </div>
  );
}
