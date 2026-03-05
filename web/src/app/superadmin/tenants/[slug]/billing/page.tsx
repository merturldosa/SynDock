"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { ChevronLeft, FileText, CheckCircle } from "lucide-react";
import { useTranslations } from "next-intl";
import toast from "react-hot-toast";
import { formatPrice, formatDateShort } from "@/lib/format";
import {
  getTenantBilling,
  updateTenantBilling,
  getTenantInvoices,
  generateInvoice,
  markInvoicePaid,
  getPlatformPlans,
  type TenantBilling,
  type InvoiceDto,
  type PlanInfoDto,
} from "@/lib/platformApi";

const STATUSES = ["Active", "Trial", "Suspended", "Cancelled"];

const INV_STATUS_COLORS: Record<string, string> = {
  Pending: "bg-yellow-100 text-yellow-700",
  Paid: "bg-emerald-100 text-emerald-700",
  Failed: "bg-red-100 text-red-700",
  Cancelled: "bg-gray-100 text-gray-500",
};

export default function TenantBillingPage() {
  const t = useTranslations();
  const params = useParams();
  const router = useRouter();
  const slug = params.slug as string;

  const [billing, setBilling] = useState<TenantBilling | null>(null);
  const [invoices, setInvoices] = useState<InvoiceDto[]>([]);
  const [plans, setPlans] = useState<PlanInfoDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [selectedPlan, setSelectedPlan] = useState("Free");
  const [selectedStatus, setSelectedStatus] = useState("Trial");
  const [tab, setTab] = useState<"plan" | "invoices">("plan");

  useEffect(() => {
    Promise.all([
      getTenantBilling(slug).then((b) => {
        setBilling(b);
        setSelectedPlan(b.planType);
        setSelectedStatus(b.billingStatus);
      }),
      getTenantInvoices(slug).then(setInvoices),
      getPlatformPlans().then(setPlans),
    ])
      .catch(() => { toast.error(t("common.fetchError")); })
      .finally(() => setLoading(false));
  }, [slug]);

  const handleSave = async () => {
    const plan = plans.find((p) => p.planType === selectedPlan);
    setSaving(true);
    try {
      await updateTenantBilling(slug, {
        planType: selectedPlan,
        monthlyPrice: plan?.monthlyPrice ?? 0,
        billingStatus: selectedStatus,
      });
      toast.success(t("superadmin.billing.updateSuccess"));
      router.push("/superadmin/billing");
    } catch {
      toast.error(t("superadmin.billing.updateFailed"));
    }
    setSaving(false);
  };

  const handleGenerateInvoice = async () => {
    const period = new Date().toISOString().slice(0, 7); // yyyy-MM
    try {
      await generateInvoice(slug, period);
      const updated = await getTenantInvoices(slug);
      setInvoices(updated);
      toast.success(t("superadmin.billing.invoiceGenerated"));
    } catch {
      toast.error(t("superadmin.billing.invoiceGenerateFailed"));
    }
  };

  const handleMarkPaid = async (id: number) => {
    if (!confirm(t("superadmin.billing.markPaidConfirm"))) return;
    try {
      await markInvoicePaid(id, `TXN-${Date.now()}`, "Manual");
      const updated = await getTenantInvoices(slug);
      setInvoices(updated);
    } catch {
      toast.error(t("superadmin.billing.markPaidFailed"));
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-emerald-500 border-t-transparent" />
      </div>
    );
  }

  return (
    <div className="max-w-3xl">
      <button
        onClick={() => router.back()}
        className="flex items-center gap-1 text-sm text-gray-500 hover:text-gray-700 mb-4"
      >
        <ChevronLeft size={16} /> {t("superadmin.billing.goBack")}
      </button>

      <h1 className="text-2xl font-bold text-gray-900 mb-6">
        {billing?.tenantName || slug} {t("superadmin.billing.title")}
      </h1>

      {/* Tabs */}
      <div className="flex gap-1 mb-6 bg-gray-100 rounded-lg p-1">
        <button
          onClick={() => setTab("plan")}
          className={`flex-1 py-2 rounded-md text-sm font-medium transition-colors ${
            tab === "plan" ? "bg-white text-gray-900 shadow-sm" : "text-gray-500"
          }`}
        >
          {t("superadmin.billing.planManagement")}
        </button>
        <button
          onClick={() => setTab("invoices")}
          className={`flex-1 py-2 rounded-md text-sm font-medium transition-colors ${
            tab === "invoices" ? "bg-white text-gray-900 shadow-sm" : "text-gray-500"
          }`}
        >
          {t("superadmin.billing.invoices")} ({invoices.length})
        </button>
      </div>

      {tab === "plan" && (
        <>
          {/* Current status */}
          {billing && (
            <div className="bg-white rounded-xl shadow-sm p-6 mb-6">
              <h2 className="font-semibold text-gray-900 mb-4">{t("superadmin.billing.currentStatus")}</h2>
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <p className="text-gray-400">{t("superadmin.billing.plan")}</p>
                  <p className="font-medium">{billing.planType}</p>
                </div>
                <div>
                  <p className="text-gray-400">{t("superadmin.billing.status")}</p>
                  <p className="font-medium">{billing.billingStatus}</p>
                </div>
                <div>
                  <p className="text-gray-400">{t("superadmin.billing.monthlyFee")}</p>
                  <p className="font-medium">
                    {billing.monthlyPrice > 0
                      ? formatPrice(billing.monthlyPrice)
                      : t("superadmin.billing.free")}
                  </p>
                </div>
                <div>
                  <p className="text-gray-400">{t("superadmin.billing.nextPayment")}</p>
                  <p className="font-medium">
                    {billing.nextBillingAt
                      ? formatDateShort(billing.nextBillingAt)
                      : "-"}
                  </p>
                </div>
              </div>
            </div>
          )}

          {/* Edit */}
          <div className="bg-white rounded-xl shadow-sm p-6 space-y-4">
            <h2 className="font-semibold text-gray-900 mb-4">{t("superadmin.billing.change")}</h2>
            <div>
              <label className="block text-sm text-gray-500 mb-1">{t("superadmin.billing.plan")}</label>
              <select
                value={selectedPlan}
                onChange={(e) => setSelectedPlan(e.target.value)}
                className="w-full px-3 py-2.5 border rounded-lg text-sm"
              >
                {plans.map((p) => (
                  <option key={p.planType} value={p.planType}>
                    {p.planType} {p.monthlyPrice > 0 ? `(${formatPrice(p.monthlyPrice)}/${t("superadmin.billing.month")})` : ""}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm text-gray-500 mb-1">{t("superadmin.billing.status")}</label>
              <select
                value={selectedStatus}
                onChange={(e) => setSelectedStatus(e.target.value)}
                className="w-full px-3 py-2.5 border rounded-lg text-sm"
              >
                {STATUSES.map((s) => (
                  <option key={s} value={s}>
                    {s}
                  </option>
                ))}
              </select>
            </div>
            <div className="flex gap-3 pt-4">
              <button
                onClick={() => router.back()}
                className="flex-1 py-3 border rounded-lg text-sm font-medium text-gray-500 hover:bg-gray-50"
              >
                {t("superadmin.billing.cancel")}
              </button>
              <button
                onClick={handleSave}
                disabled={saving}
                className="flex-1 py-3 bg-emerald-600 text-white rounded-lg text-sm font-medium hover:bg-emerald-700 disabled:opacity-60"
              >
                {saving ? t("superadmin.billing.saving") : t("superadmin.billing.save")}
              </button>
            </div>
          </div>
        </>
      )}

      {tab === "invoices" && (
        <div>
          <div className="flex justify-end mb-4">
            <button
              onClick={handleGenerateInvoice}
              className="flex items-center gap-2 px-4 py-2 bg-emerald-600 text-white rounded-lg text-sm font-medium hover:bg-emerald-700"
            >
              <FileText size={14} /> {t("superadmin.billing.generateInvoice")}
            </button>
          </div>

          {invoices.length === 0 ? (
            <div className="bg-white rounded-xl shadow-sm p-12 text-center text-gray-400 text-sm">
              {t("superadmin.billing.noInvoices")}
            </div>
          ) : (
            <div className="bg-white rounded-xl shadow-sm overflow-hidden">
              <table className="w-full text-sm">
                <thead className="bg-gray-50 text-gray-500 text-xs">
                  <tr>
                    <th className="text-left px-4 py-3 font-medium">{t("superadmin.billing.invoiceNumber")}</th>
                    <th className="text-left px-4 py-3 font-medium">{t("superadmin.billing.invoicePeriod")}</th>
                    <th className="text-right px-4 py-3 font-medium">{t("superadmin.billing.amount")}</th>
                    <th className="text-left px-4 py-3 font-medium">{t("superadmin.billing.invoiceStatus")}</th>
                    <th className="text-left px-4 py-3 font-medium">{t("superadmin.billing.issuedAt")}</th>
                    <th className="px-4 py-3"></th>
                  </tr>
                </thead>
                <tbody className="divide-y">
                  {invoices.map((inv) => (
                    <tr key={inv.id} className="hover:bg-gray-50">
                      <td className="px-4 py-3 font-mono text-xs">
                        {inv.invoiceNumber}
                      </td>
                      <td className="px-4 py-3">{inv.billingPeriod}</td>
                      <td className="px-4 py-3 text-right font-medium">
                        {formatPrice(inv.amount)}
                      </td>
                      <td className="px-4 py-3">
                        <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${INV_STATUS_COLORS[inv.status] || "bg-gray-100 text-gray-500"}`}>
                          {inv.status}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-gray-500">
                        {formatDateShort(inv.issuedAt)}
                      </td>
                      <td className="px-4 py-3 text-right">
                        {inv.status === "Pending" && (
                          <button
                            onClick={() => handleMarkPaid(inv.id)}
                            className="inline-flex items-center gap-1 text-xs text-emerald-600 hover:underline"
                          >
                            <CheckCircle size={12} /> {t("superadmin.billing.markPaid")}
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
