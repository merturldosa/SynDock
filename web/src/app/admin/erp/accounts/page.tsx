"use client";

import { useEffect, useState } from "react";
import api from "@/lib/api";
import toast from "react-hot-toast";
import {
  ChevronRight,
  ChevronDown,
  Plus,
  X,
  FolderTree,
  Landmark,
  TrendingUp,
  TrendingDown,
  Wallet,
} from "lucide-react";

interface Account {
  id: number;
  accountCode: string;
  name: string;
  accountType: string;
  parentAccountCode: string | null;
  level: number;
  isActive: boolean;
  description: string | null;
}

const ACCOUNT_TYPES = ["Asset", "Liability", "Equity", "Revenue", "Expense"] as const;

const typeIcons: Record<string, typeof Landmark> = {
  Asset: Landmark,
  Liability: TrendingDown,
  Equity: Wallet,
  Revenue: TrendingUp,
  Expense: TrendingDown,
};

const typeColors: Record<string, string> = {
  Asset: "bg-blue-50 text-blue-600",
  Liability: "bg-red-50 text-red-600",
  Equity: "bg-purple-50 text-purple-600",
  Revenue: "bg-green-50 text-green-600",
  Expense: "bg-orange-50 text-orange-600",
};

function AccountNode({
  account,
  children,
}: {
  account: Account;
  children: Account[];
}) {
  const [open, setOpen] = useState(true);
  const subs = children.filter((c) => c.parentAccountCode === account.accountCode);

  return (
    <div className="ml-4">
      <div className="flex items-center gap-2 py-1.5 px-2 rounded-lg hover:bg-gray-50 group">
        {subs.length > 0 ? (
          <button onClick={() => setOpen(!open)} className="text-gray-400">
            {open ? <ChevronDown size={16} /> : <ChevronRight size={16} />}
          </button>
        ) : (
          <span className="w-4" />
        )}
        <span className="text-xs font-mono text-gray-400">{account.accountCode}</span>
        <span className="text-sm font-medium text-gray-800">{account.name}</span>
        <span className="text-xs text-gray-400">Lv.{account.level}</span>
        {!account.isActive && (
          <span className="px-1.5 py-0.5 text-xs rounded bg-gray-100 text-gray-500">Inactive</span>
        )}
      </div>
      {open &&
        subs.map((s) => (
          <AccountNode key={s.id} account={s} children={children} />
        ))}
    </div>
  );
}

export default function AccountsPage() {
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [form, setForm] = useState({
    accountCode: "",
    name: "",
    accountType: "Asset",
    parentAccountCode: "",
    description: "",
  });
  const [submitting, setSubmitting] = useState(false);

  const fetchAccounts = async () => {
    try {
      const { data } = await api.get("/accounting/accounts");
      setAccounts(Array.isArray(data) ? data : data.items ?? []);
    } catch {
      toast.error("Failed to load accounts");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchAccounts();
  }, []);

  const handleCreate = async () => {
    if (!form.accountCode || !form.name) {
      toast.error("Code and Name are required");
      return;
    }
    setSubmitting(true);
    try {
      await api.post("/accounting/accounts", {
        ...form,
        parentAccountCode: form.parentAccountCode || null,
      });
      toast.success("Account created");
      setShowModal(false);
      setForm({ accountCode: "", name: "", accountType: "Asset", parentAccountCode: "", description: "" });
      fetchAccounts();
    } catch {
      toast.error("Failed to create account");
    } finally {
      setSubmitting(false);
    }
  };

  const grouped = ACCOUNT_TYPES.map((type) => ({
    type,
    accounts: accounts.filter((a) => a.accountType === type),
  }));

  if (loading) {
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
          <h1 className="text-2xl font-bold text-gray-900">Chart of Accounts</h1>
          <p className="text-sm text-gray-500 mt-1">Manage your accounting chart of accounts</p>
        </div>
        <button
          onClick={() => setShowModal(true)}
          className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors"
        >
          <Plus size={16} />
          New Account
        </button>
      </div>

      {/* Summary */}
      <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
        {grouped.map(({ type, accounts: accs }) => {
          const Icon = typeIcons[type];
          return (
            <div key={type} className="bg-white rounded-xl border border-gray-200 p-4">
              <div className="flex items-center gap-2 mb-1">
                <div className={`w-8 h-8 rounded-lg flex items-center justify-center ${typeColors[type]}`}>
                  <Icon size={16} />
                </div>
                <span className="text-sm font-medium text-gray-700">{type}</span>
              </div>
              <p className="text-xl font-bold text-gray-900">{accs.length}</p>
            </div>
          );
        })}
      </div>

      {/* Tree View */}
      {grouped.map(({ type, accounts: accs }) => {
        const roots = accs.filter((a) => !a.parentAccountCode);
        if (accs.length === 0) return null;
        return (
          <div key={type} className="bg-white rounded-xl border border-gray-200 overflow-hidden">
            <div className="px-6 py-4 border-b border-gray-200 flex items-center gap-2">
              <FolderTree size={18} className="text-gray-400" />
              <h2 className="font-semibold text-gray-900">{type}</h2>
              <span className="text-xs text-gray-400 ml-1">({accs.length} accounts)</span>
            </div>
            <div className="p-4">
              {roots.map((root) => (
                <AccountNode key={root.id} account={root} children={accs} />
              ))}
            </div>
          </div>
        );
      })}

      {/* Create Modal */}
      {showModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-white rounded-xl shadow-xl w-full max-w-md mx-4">
            <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200">
              <h3 className="text-lg font-semibold text-gray-900">Create Account</h3>
              <button onClick={() => setShowModal(false)} className="text-gray-400 hover:text-gray-600">
                <X size={20} />
              </button>
            </div>
            <div className="p-6 space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Account Code *</label>
                <input
                  value={form.accountCode}
                  onChange={(e) => setForm({ ...form, accountCode: e.target.value })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                  placeholder="e.g. 1010"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Name *</label>
                <input
                  value={form.name}
                  onChange={(e) => setForm({ ...form, name: e.target.value })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                  placeholder="e.g. Cash"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Account Type</label>
                <select
                  value={form.accountType}
                  onChange={(e) => setForm({ ...form, accountType: e.target.value })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                >
                  {ACCOUNT_TYPES.map((t) => (
                    <option key={t} value={t}>{t}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Parent Account Code</label>
                <input
                  value={form.parentAccountCode}
                  onChange={(e) => setForm({ ...form, parentAccountCode: e.target.value })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                  placeholder="Leave empty for root account"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
                <textarea
                  value={form.description}
                  onChange={(e) => setForm({ ...form, description: e.target.value })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                  rows={3}
                />
              </div>
            </div>
            <div className="flex justify-end gap-3 px-6 py-4 border-t border-gray-200">
              <button
                onClick={() => setShowModal(false)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-gray-100 rounded-lg hover:bg-gray-200 transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={handleCreate}
                disabled={submitting}
                className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-50"
              >
                {submitting ? "Creating..." : "Create"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
