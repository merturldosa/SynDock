"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { MapPin, Plus, Pencil, Trash2, X } from "lucide-react";
import { getAddresses, createAddress, updateAddress, deleteAddress, type Address } from "@/lib/addressApi";

interface AddressForm {
  recipientName: string;
  phone: string;
  zipCode: string;
  address1: string;
  address2: string;
  isDefault: boolean;
}

const emptyForm: AddressForm = {
  recipientName: "",
  phone: "",
  zipCode: "",
  address1: "",
  address2: "",
  isDefault: false,
};

export default function AddressesPage() {
  const t = useTranslations();
  const [addresses, setAddresses] = useState<Address[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<AddressForm>(emptyForm);
  const [submitting, setSubmitting] = useState(false);

  const load = () => {
    setLoading(true);
    getAddresses()
      .then(setAddresses)
      .catch(() => {})
      .finally(() => setLoading(false));
  };

  useEffect(load, []);

  const openCreate = () => {
    setEditingId(null);
    setForm(emptyForm);
    setModalOpen(true);
  };

  const openEdit = (addr: Address) => {
    setEditingId(addr.id);
    setForm({
      recipientName: addr.recipientName,
      phone: addr.phone,
      zipCode: addr.zipCode,
      address1: addr.address1,
      address2: addr.address2 || "",
      isDefault: addr.isDefault,
    });
    setModalOpen(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    try {
      const payload = {
        recipientName: form.recipientName,
        phone: form.phone,
        zipCode: form.zipCode,
        address1: form.address1,
        address2: form.address2 || null,
        isDefault: form.isDefault,
      };
      if (editingId) {
        await updateAddress(editingId, payload);
      } else {
        await createAddress(payload);
      }
      setModalOpen(false);
      load();
    } catch {
      alert(t("mypage.addresses.saveFailed"));
    }
    setSubmitting(false);
  };

  const handleDelete = async (id: number) => {
    if (!confirm(t("mypage.addresses.deleteConfirm"))) return;
    try {
      await deleteAddress(id);
      load();
    } catch {
      alert(t("mypage.addresses.deleteFailed"));
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    );
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-[var(--color-secondary)]">{t("mypage.addresses.title")}</h1>
        <button
          onClick={openCreate}
          disabled={addresses.length >= 10}
          className="flex items-center gap-1.5 px-4 py-2 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90 disabled:opacity-50"
        >
          <Plus size={16} />
          {t("mypage.addresses.addNew")}
        </button>
      </div>

      {addresses.length >= 10 && (
        <p className="text-sm text-amber-600 mb-4">{t("mypage.addresses.maxLimit")}</p>
      )}

      {addresses.length === 0 ? (
        <div className="text-center py-20">
          <MapPin size={64} className="mx-auto text-gray-300 mb-6" />
          <p className="text-gray-500 mb-4">{t("mypage.addresses.empty")}</p>
          <button
            onClick={openCreate}
            className="px-6 py-3 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90"
          >
            {t("mypage.addresses.addNew")}
          </button>
        </div>
      ) : (
        <div className="space-y-3">
          {addresses.map((addr) => (
            <div
              key={addr.id}
              className="bg-white rounded-xl shadow-sm p-5 flex items-start gap-4"
            >
              <MapPin size={20} className="text-[var(--color-primary)] mt-0.5 shrink-0" />
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 mb-1">
                  <span className="font-medium text-[var(--color-secondary)]">{addr.recipientName}</span>
                  {addr.isDefault && (
                    <span className="px-2 py-0.5 text-xs rounded-full bg-[var(--color-primary)] text-white">
                      {t("mypage.addresses.defaultLabel")}
                    </span>
                  )}
                </div>
                <p className="text-sm text-gray-600">{addr.phone}</p>
                <p className="text-sm text-gray-500 mt-1">
                  [{addr.zipCode}] {addr.address1}
                  {addr.address2 && ` ${addr.address2}`}
                </p>
              </div>
              <div className="flex gap-1 shrink-0">
                <button
                  onClick={() => openEdit(addr)}
                  className="p-2 text-gray-400 hover:text-[var(--color-primary)] transition-colors"
                >
                  <Pencil size={16} />
                </button>
                <button
                  onClick={() => handleDelete(addr.id)}
                  className="p-2 text-gray-400 hover:text-red-500 transition-colors"
                >
                  <Trash2 size={16} />
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Modal */}
      {modalOpen && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md">
            <div className="flex items-center justify-between p-5 border-b">
              <h2 className="font-semibold text-[var(--color-secondary)]">
                {editingId ? t("mypage.addresses.editAddress") : t("mypage.addresses.addNew")}
              </h2>
              <button onClick={() => setModalOpen(false)} className="text-gray-400 hover:text-gray-600">
                <X size={20} />
              </button>
            </div>
            <form onSubmit={handleSubmit} className="p-5 space-y-4">
              <div>
                <label className="block text-sm text-gray-500 mb-1">{t("mypage.addresses.recipientName")}</label>
                <input
                  type="text"
                  required
                  value={form.recipientName}
                  onChange={(e) => setForm((f) => ({ ...f, recipientName: e.target.value }))}
                  className="w-full px-3 py-2.5 border rounded-lg text-sm"
                />
              </div>
              <div>
                <label className="block text-sm text-gray-500 mb-1">{t("mypage.addresses.phoneNumber")}</label>
                <input
                  type="tel"
                  required
                  value={form.phone}
                  onChange={(e) => setForm((f) => ({ ...f, phone: e.target.value }))}
                  placeholder="010-0000-0000"
                  className="w-full px-3 py-2.5 border rounded-lg text-sm"
                />
              </div>
              <div>
                <label className="block text-sm text-gray-500 mb-1">{t("mypage.addresses.zipCode")}</label>
                <input
                  type="text"
                  required
                  value={form.zipCode}
                  onChange={(e) => setForm((f) => ({ ...f, zipCode: e.target.value }))}
                  className="w-full px-3 py-2.5 border rounded-lg text-sm"
                />
              </div>
              <div>
                <label className="block text-sm text-gray-500 mb-1">{t("mypage.addresses.address")}</label>
                <input
                  type="text"
                  required
                  value={form.address1}
                  onChange={(e) => setForm((f) => ({ ...f, address1: e.target.value }))}
                  className="w-full px-3 py-2.5 border rounded-lg text-sm"
                />
              </div>
              <div>
                <label className="block text-sm text-gray-500 mb-1">{t("mypage.addresses.addressDetail")}</label>
                <input
                  type="text"
                  value={form.address2}
                  onChange={(e) => setForm((f) => ({ ...f, address2: e.target.value }))}
                  className="w-full px-3 py-2.5 border rounded-lg text-sm"
                />
              </div>
              <label className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={form.isDefault}
                  onChange={(e) => setForm((f) => ({ ...f, isDefault: e.target.checked }))}
                />
                {t("mypage.addresses.setDefault")}
              </label>
              <div className="flex gap-3 pt-2">
                <button
                  type="button"
                  onClick={() => setModalOpen(false)}
                  className="flex-1 py-2.5 border rounded-lg text-sm text-gray-500 hover:bg-gray-50"
                >
                  {t("common.cancel")}
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className="flex-1 py-2.5 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90 disabled:opacity-60"
                >
                  {submitting ? t("common.saving") : t("common.save")}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
