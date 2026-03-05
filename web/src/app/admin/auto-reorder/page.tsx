"use client";

import { useState, useEffect, useCallback } from "react";
import { useTranslations } from "next-intl";
import toast from "react-hot-toast";
import { formatDateShort } from "@/lib/format";
import {
  RefreshCcw, Plus, Trash2, Power, PowerOff, Send, X,
  AlertTriangle, CheckCircle, Clock, Package
} from "lucide-react";
import {
  getAutoReorderStats, getAutoReorderRules, getPurchaseOrders,
  upsertAutoReorderRule, deleteAutoReorderRule, toggleAutoReorderRule,
  bulkCreateAutoReorderRules, forwardPurchaseOrder, cancelPurchaseOrder,
  type AutoReorderStatsDto, type AutoReorderRuleDto,
  type PurchaseOrderDto, type PurchaseOrderListDto
} from "@/lib/adminApi";
import api from "@/lib/api";

type Tab = "rules" | "orders";

interface ProductOption { id: number; name: string; }

export default function AutoReorderPage() {
  const t = useTranslations();
  const [tab, setTab] = useState<Tab>("rules");
  const [stats, setStats] = useState<AutoReorderStatsDto | null>(null);
  const [rules, setRules] = useState<AutoReorderRuleDto[]>([]);
  const [orders, setOrders] = useState<PurchaseOrderListDto | null>(null);
  const [orderStatus, setOrderStatus] = useState("");
  const [loading, setLoading] = useState(true);
  const [showAddRule, setShowAddRule] = useState(false);
  const [showBulk, setShowBulk] = useState(false);
  const [products, setProducts] = useState<ProductOption[]>([]);
  const [expandedPO, setExpandedPO] = useState<number | null>(null);

  // Add rule form
  const [ruleForm, setRuleForm] = useState({
    productId: 0,
    reorderThreshold: 10,
    reorderQuantity: 0,
    maxStockLevel: 0,
    isEnabled: true,
    autoForwardToMes: true,
    minIntervalHours: 24,
  });

  // Bulk form
  const [bulkForm, setBulkForm] = useState({
    reorderThreshold: 10,
    minIntervalHours: 24,
    autoForwardToMes: true,
  });

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [s, r] = await Promise.all([
        getAutoReorderStats(),
        getAutoReorderRules(),
      ]);
      setStats(s);
      setRules(r);
    } catch { toast.error(t("common.fetchError")); }
    setLoading(false);
  }, []);

  const loadOrders = useCallback(async () => {
    try {
      const o = await getPurchaseOrders(orderStatus || undefined);
      setOrders(o);
    } catch { toast.error(t("common.fetchError")); }
  }, [orderStatus]);

  useEffect(() => { load(); }, [load]);
  useEffect(() => { if (tab === "orders") loadOrders(); }, [tab, loadOrders]);

  const loadProducts = async () => {
    try {
      const { data } = await api.get("/api/products", { params: { pageSize: 200 } });
      const items = data.items || data;
      setProducts(items.map((p: { id: number; name: string }) => ({ id: p.id, name: p.name })));
    } catch { toast.error(t("common.fetchError")); }
  };

  const handleAddRule = async () => {
    if (!ruleForm.productId) return;
    try {
      await upsertAutoReorderRule(ruleForm);
      setShowAddRule(false);
      setRuleForm({ productId: 0, reorderThreshold: 10, reorderQuantity: 0, maxStockLevel: 0, isEnabled: true, autoForwardToMes: true, minIntervalHours: 24 });
      load();
    } catch { toast.error(t("admin.autoReorder.saveFailed")); }
  };

  const handleBulkCreate = async () => {
    try {
      const result = await bulkCreateAutoReorderRules(bulkForm);
      toast.success(t("admin.autoReorder.bulkCreatedMessage", { count: result.createdCount }));
      setShowBulk(false);
      load();
    } catch { toast.error(t("admin.autoReorder.bulkFailed")); }
  };

  const handleToggle = async (id: number, enabled: boolean) => {
    try {
      await toggleAutoReorderRule(id, enabled);
      load();
    } catch { toast.error(t("common.fetchError")); }
  };

  const handleDelete = async (id: number) => {
    if (!window.confirm(t("admin.autoReorder.confirmDeleteRule"))) return;
    try {
      await deleteAutoReorderRule(id);
      load();
    } catch { toast.error(t("admin.autoReorder.deleteFailed")); }
  };

  const handleForward = async (id: number) => {
    try {
      await forwardPurchaseOrder(id);
      loadOrders();
    } catch { toast.error(t("admin.autoReorder.forwardFailed")); }
  };

  const handleCancel = async (id: number) => {
    if (!window.confirm(t("admin.autoReorder.confirmCancelOrder"))) return;
    try {
      await cancelPurchaseOrder(id);
      loadOrders();
    } catch { toast.error(t("admin.autoReorder.cancelFailed")); }
  };

  const StatCard = ({ label, value, icon: Icon, color }: { label: string; value: number | string; icon: typeof Package; color: string }) => (
    <div className="bg-white rounded-xl shadow-sm p-4 flex items-center gap-3">
      <div className={`w-10 h-10 rounded-lg flex items-center justify-center ${color}`}>
        <Icon size={20} className="text-white" />
      </div>
      <div>
        <p className="text-xs text-gray-500">{label}</p>
        <p className="text-lg font-bold">{value}</p>
      </div>
    </div>
  );

  const statusBadge = (status: string) => {
    const colors: Record<string, string> = {
      Created: "bg-blue-100 text-blue-800",
      Forwarded: "bg-yellow-100 text-yellow-800",
      Confirmed: "bg-green-100 text-green-800",
      Received: "bg-emerald-100 text-emerald-800",
      Cancelled: "bg-red-100 text-red-800",
    };
    return <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${colors[status] || "bg-gray-100 text-gray-800"}`}>{status}</span>;
  };

  if (loading) return (
    <div className="flex items-center justify-center min-h-[40vh]">
      <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
    </div>
  );

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">{t("admin.autoReorder.title")}</h1>
      </div>

      {/* Stats */}
      {stats && (
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-3">
          <StatCard label={t("admin.autoReorder.totalRules")} value={stats.totalRules} icon={RefreshCcw} color="bg-blue-500" />
          <StatCard label={t("admin.autoReorder.enabledRules")} value={stats.enabledRules} icon={Power} color="bg-green-500" />
          <StatCard label={t("admin.autoReorder.belowThreshold")} value={stats.productsBelowThreshold} icon={AlertTriangle} color="bg-red-500" />
          <StatCard label={t("admin.autoReorder.totalPOs")} value={stats.totalPurchaseOrders} icon={Package} color="bg-purple-500" />
          <StatCard label={t("admin.autoReorder.pendingPOs")} value={stats.pendingOrders} icon={Clock} color="bg-orange-500" />
          <StatCard label={t("admin.autoReorder.forwardedPOs")} value={stats.forwardedOrders} icon={Send} color="bg-teal-500" />
        </div>
      )}

      {/* Tabs */}
      <div className="flex gap-2 border-b">
        {(["rules", "orders"] as Tab[]).map((t2) => (
          <button key={t2} onClick={() => setTab(t2)}
            className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${tab === t2 ? "border-[var(--color-primary)] text-[var(--color-primary)]" : "border-transparent text-gray-500 hover:text-gray-700"}`}>
            {t(`admin.autoReorder.tab_${t2}`)}
          </button>
        ))}
      </div>

      {/* Rules Tab */}
      {tab === "rules" && (
        <div className="space-y-4">
          <div className="flex gap-2">
            <button onClick={() => { setShowAddRule(true); loadProducts(); }}
              className="px-4 py-2 bg-[var(--color-primary)] text-white rounded-lg text-sm flex items-center gap-2 hover:opacity-90">
              <Plus size={16} /> {t("admin.autoReorder.addRule")}
            </button>
            <button onClick={() => setShowBulk(true)}
              className="px-4 py-2 bg-gray-600 text-white rounded-lg text-sm flex items-center gap-2 hover:opacity-90">
              <RefreshCcw size={16} /> {t("admin.autoReorder.bulkCreate")}
            </button>
          </div>

          {/* Add Rule Modal */}
          {showAddRule && (
            <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50" onClick={() => setShowAddRule(false)}>
              <div className="bg-white rounded-xl p-6 w-full max-w-md space-y-4" onClick={(e) => e.stopPropagation()}>
                <div className="flex justify-between items-center">
                  <h3 className="font-bold">{t("admin.autoReorder.addRule")}</h3>
                  <button onClick={() => setShowAddRule(false)} aria-label="Close dialog"><X size={20} /></button>
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">{t("admin.autoReorder.product")}</label>
                  <select value={ruleForm.productId} onChange={(e) => setRuleForm({ ...ruleForm, productId: +e.target.value })}
                    className="w-full border rounded-lg px-3 py-2 text-sm">
                    <option value={0}>{t("admin.autoReorder.selectProduct")}</option>
                    {products.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
                  </select>
                </div>
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-sm font-medium mb-1">{t("admin.autoReorder.threshold")}</label>
                    <input type="number" value={ruleForm.reorderThreshold} onChange={(e) => setRuleForm({ ...ruleForm, reorderThreshold: +e.target.value })}
                      className="w-full border rounded-lg px-3 py-2 text-sm" />
                  </div>
                  <div>
                    <label className="block text-sm font-medium mb-1">{t("admin.autoReorder.quantity")}</label>
                    <input type="number" value={ruleForm.reorderQuantity} onChange={(e) => setRuleForm({ ...ruleForm, reorderQuantity: +e.target.value })}
                      className="w-full border rounded-lg px-3 py-2 text-sm" placeholder="0=자동" />
                  </div>
                  <div>
                    <label className="block text-sm font-medium mb-1">{t("admin.autoReorder.maxStock")}</label>
                    <input type="number" value={ruleForm.maxStockLevel} onChange={(e) => setRuleForm({ ...ruleForm, maxStockLevel: +e.target.value })}
                      className="w-full border rounded-lg px-3 py-2 text-sm" placeholder="0=무제한" />
                  </div>
                  <div>
                    <label className="block text-sm font-medium mb-1">{t("admin.autoReorder.minInterval")}</label>
                    <input type="number" value={ruleForm.minIntervalHours} onChange={(e) => setRuleForm({ ...ruleForm, minIntervalHours: +e.target.value })}
                      className="w-full border rounded-lg px-3 py-2 text-sm" />
                  </div>
                </div>
                <div className="flex gap-4">
                  <label className="flex items-center gap-2 text-sm">
                    <input type="checkbox" checked={ruleForm.autoForwardToMes} onChange={(e) => setRuleForm({ ...ruleForm, autoForwardToMes: e.target.checked })} />
                    {t("admin.autoReorder.autoForward")}
                  </label>
                </div>
                <button onClick={handleAddRule} disabled={!ruleForm.productId}
                  className="w-full py-2 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium disabled:opacity-50">
                  {t("admin.autoReorder.save")}
                </button>
              </div>
            </div>
          )}

          {/* Bulk Create Modal */}
          {showBulk && (
            <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50" onClick={() => setShowBulk(false)}>
              <div className="bg-white rounded-xl p-6 w-full max-w-sm space-y-4" onClick={(e) => e.stopPropagation()}>
                <div className="flex justify-between items-center">
                  <h3 className="font-bold">{t("admin.autoReorder.bulkCreate")}</h3>
                  <button onClick={() => setShowBulk(false)} aria-label="Close dialog"><X size={20} /></button>
                </div>
                <p className="text-sm text-gray-600">{t("admin.autoReorder.bulkDesc")}</p>
                <div>
                  <label className="block text-sm font-medium mb-1">{t("admin.autoReorder.threshold")}</label>
                  <input type="number" value={bulkForm.reorderThreshold} onChange={(e) => setBulkForm({ ...bulkForm, reorderThreshold: +e.target.value })}
                    className="w-full border rounded-lg px-3 py-2 text-sm" />
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">{t("admin.autoReorder.minInterval")}</label>
                  <input type="number" value={bulkForm.minIntervalHours} onChange={(e) => setBulkForm({ ...bulkForm, minIntervalHours: +e.target.value })}
                    className="w-full border rounded-lg px-3 py-2 text-sm" />
                </div>
                <label className="flex items-center gap-2 text-sm">
                  <input type="checkbox" checked={bulkForm.autoForwardToMes} onChange={(e) => setBulkForm({ ...bulkForm, autoForwardToMes: e.target.checked })} />
                  {t("admin.autoReorder.autoForward")}
                </label>
                <button onClick={handleBulkCreate}
                  className="w-full py-2 bg-gray-700 text-white rounded-lg text-sm font-medium">
                  {t("admin.autoReorder.createAll")}
                </button>
              </div>
            </div>
          )}

          {/* Rules Table */}
          <div className="bg-white rounded-xl shadow-sm overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 text-gray-600">
                <tr>
                  <th className="text-left px-4 py-3">{t("admin.autoReorder.product")}</th>
                  <th className="text-center px-3 py-3">{t("admin.autoReorder.currentStock")}</th>
                  <th className="text-center px-3 py-3">{t("admin.autoReorder.threshold")}</th>
                  <th className="text-center px-3 py-3">{t("admin.autoReorder.quantity")}</th>
                  <th className="text-center px-3 py-3">{t("admin.autoReorder.autoForward")}</th>
                  <th className="text-center px-3 py-3">{t("admin.autoReorder.status")}</th>
                  <th className="text-center px-3 py-3">{t("admin.autoReorder.lastTriggered")}</th>
                  <th className="text-center px-3 py-3">{t("admin.autoReorder.actions")}</th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {rules.map((rule) => (
                  <tr key={rule.id} className={rule.currentStock <= rule.reorderThreshold ? "bg-red-50" : ""}>
                    <td className="px-4 py-3 font-medium">{rule.productName}</td>
                    <td className="text-center px-3 py-3">
                      <span className={rule.currentStock <= rule.reorderThreshold ? "text-red-600 font-bold" : ""}>
                        {rule.currentStock}
                      </span>
                    </td>
                    <td className="text-center px-3 py-3">{rule.reorderThreshold}</td>
                    <td className="text-center px-3 py-3">{rule.reorderQuantity || "자동"}</td>
                    <td className="text-center px-3 py-3">
                      {rule.autoForwardToMes ? <CheckCircle size={16} className="text-green-500 mx-auto" /> : <X size={16} className="text-gray-400 mx-auto" />}
                    </td>
                    <td className="text-center px-3 py-3">
                      <button onClick={() => handleToggle(rule.id, !rule.isEnabled)}
                        className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium ${rule.isEnabled ? "bg-green-100 text-green-800" : "bg-gray-100 text-gray-500"}`}>
                        {rule.isEnabled ? <Power size={12} /> : <PowerOff size={12} />}
                        {rule.isEnabled ? "ON" : "OFF"}
                      </button>
                    </td>
                    <td className="text-center px-3 py-3 text-xs text-gray-500">
                      {rule.lastTriggeredAt ? formatDateShort(rule.lastTriggeredAt) : "-"}
                    </td>
                    <td className="text-center px-3 py-3">
                      <button onClick={() => handleDelete(rule.id)} className="text-red-500 hover:text-red-700" aria-label="Delete rule">
                        <Trash2 size={16} />
                      </button>
                    </td>
                  </tr>
                ))}
                {rules.length === 0 && (
                  <tr><td colSpan={8} className="text-center py-8 text-gray-400">{t("admin.autoReorder.noRules")}</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Orders Tab */}
      {tab === "orders" && (
        <div className="space-y-4">
          <div className="flex gap-2">
            {["", "Created", "Forwarded", "Confirmed", "Received", "Cancelled"].map((s) => (
              <button key={s} onClick={() => setOrderStatus(s)}
                className={`px-3 py-1.5 rounded-lg text-sm ${orderStatus === s ? "bg-[var(--color-primary)] text-white" : "bg-white border text-gray-600 hover:bg-gray-50"}`}>
                {s || t("admin.autoReorder.allOrders")}
              </button>
            ))}
          </div>

          {orders && (
            <div className="space-y-3">
              {orders.items.map((po) => (
                <div key={po.id} className="bg-white rounded-xl shadow-sm overflow-hidden">
                  <div className="p-4 flex items-center justify-between cursor-pointer" onClick={() => setExpandedPO(expandedPO === po.id ? null : po.id)}>
                    <div className="flex items-center gap-4">
                      <div>
                        <p className="font-medium">{po.orderNumber}</p>
                        <p className="text-xs text-gray-500">{formatDateShort(po.createdAt)}</p>
                      </div>
                      {statusBadge(po.status)}
                      <span className="text-xs px-2 py-0.5 rounded bg-gray-100">{po.triggerType}</span>
                    </div>
                    <div className="flex items-center gap-4">
                      <div className="text-right text-sm">
                        <p>{po.itemCount}개 품목 / {po.totalQuantity}개</p>
                        {po.mesOrderId && <p className="text-xs text-gray-500">MES: {po.mesOrderId}</p>}
                      </div>
                      <div className="flex gap-1">
                        {po.status === "Created" && (
                          <>
                            <button onClick={(e) => { e.stopPropagation(); handleForward(po.id); }}
                              className="px-3 py-1.5 bg-blue-500 text-white rounded text-xs flex items-center gap-1 hover:bg-blue-600">
                              <Send size={12} /> MES
                            </button>
                            <button onClick={(e) => { e.stopPropagation(); handleCancel(po.id); }}
                              className="px-3 py-1.5 bg-red-500 text-white rounded text-xs hover:bg-red-600"
                              aria-label="Cancel order">
                              <X size={12} />
                            </button>
                          </>
                        )}
                      </div>
                    </div>
                  </div>
                  {expandedPO === po.id && po.items.length > 0 && (
                    <div className="border-t px-4 py-3 bg-gray-50">
                      <table className="w-full text-xs">
                        <thead className="text-gray-500">
                          <tr>
                            <th className="text-left py-1">{t("admin.autoReorder.product")}</th>
                            <th className="text-center py-1">MES Code</th>
                            <th className="text-center py-1">{t("admin.autoReorder.currentStock")}</th>
                            <th className="text-center py-1">{t("admin.autoReorder.threshold")}</th>
                            <th className="text-center py-1">{t("admin.autoReorder.orderedQty")}</th>
                            <th className="text-left py-1">{t("admin.autoReorder.reason")}</th>
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-gray-200">
                          {po.items.map((item) => (
                            <tr key={item.id}>
                              <td className="py-1.5 font-medium">{item.productName}</td>
                              <td className="text-center py-1.5 text-gray-500">{item.mesProductCode || "-"}</td>
                              <td className="text-center py-1.5">{item.currentStock}</td>
                              <td className="text-center py-1.5">{item.reorderThreshold}</td>
                              <td className="text-center py-1.5 font-bold text-blue-600">{item.orderedQuantity}</td>
                              <td className="py-1.5 text-gray-500">{item.reason}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                      {po.notes && <p className="text-xs text-gray-500 mt-2">{po.notes}</p>}
                    </div>
                  )}
                </div>
              ))}
              {orders.items.length === 0 && (
                <div className="text-center py-12 text-gray-400">{t("admin.autoReorder.noOrders")}</div>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
