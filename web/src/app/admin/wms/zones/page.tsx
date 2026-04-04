"use client";
import { useEffect, useState } from "react";
import { Warehouse, Plus, ChevronDown, ChevronUp } from "lucide-react";
import api from "@/lib/api";

interface Zone { id: number; name: string; code: string; type: string; description?: string; isActive: boolean; sortOrder: number; locations: Location[]; }
interface Location { id: number; code: string; type: string; productId?: number; currentQuantity: number; maxCapacity: number; isActive: boolean; isOccupied: boolean; product?: { name: string }; }

export default function WmsZonesPage() {
  const [zones, setZones] = useState<Zone[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreate, setShowCreate] = useState(false);
  const [expandedZone, setExpandedZone] = useState<number | null>(null);
  const [locations, setLocations] = useState<Location[]>([]);
  const [form, setForm] = useState({ name: "", code: "", type: "General", description: "" });
  const [locForm, setLocForm] = useState({ zoneId: 0, code: "", type: "Shelf", maxCapacity: 1000 });
  const [showLocCreate, setShowLocCreate] = useState(false);

  const fetchZones = () => {
    setLoading(true);
    api.get("/wms/zones").then(r => setZones(r.data)).catch(() => {}).finally(() => setLoading(false));
  };
  useEffect(() => { fetchZones(); }, []);

  const toggleZone = async (zoneId: number) => {
    if (expandedZone === zoneId) { setExpandedZone(null); return; }
    setExpandedZone(zoneId);
    const r = await api.get(`/wms/locations?zoneId=${zoneId}`);
    setLocations(r.data);
  };

  const createZone = async () => {
    await api.post("/wms/zones", form);
    setShowCreate(false); setForm({ name: "", code: "", type: "General", description: "" }); fetchZones();
  };

  const createLocation = async () => {
    await api.post("/wms/locations", locForm);
    setShowLocCreate(false); toggleZone(locForm.zoneId);
  };

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 flex items-center gap-2"><Warehouse size={24} /> Warehouse Zones</h1>
          <p className="text-gray-500 text-sm mt-1">{zones.length} zones</p>
        </div>
        <button onClick={() => setShowCreate(true)} className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"><Plus size={16} /> Add Zone</button>
      </div>

      <div className="bg-white rounded-xl border overflow-hidden">
        {loading ? <div className="p-12 text-center text-gray-400">Loading...</div> : zones.length === 0 ? <div className="p-12 text-center text-gray-400">No zones yet</div> : (
          <div>
            {zones.map(zone => (
              <div key={zone.id} className="border-b last:border-b-0">
                <button onClick={() => toggleZone(zone.id)} className="w-full flex items-center justify-between p-4 hover:bg-gray-50 text-left">
                  <div className="flex items-center gap-3">
                    <div className="w-10 h-10 bg-blue-50 rounded-lg flex items-center justify-center"><Warehouse className="w-5 h-5 text-blue-600" /></div>
                    <div>
                      <p className="font-medium text-gray-900">{zone.name}</p>
                      <p className="text-xs text-gray-500">Code: {zone.code} | Type: {zone.type}</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-4">
                    <span className={`px-2 py-0.5 rounded text-xs ${zone.isActive ? "bg-green-100 text-green-700" : "bg-gray-100 text-gray-500"}`}>{zone.isActive ? "Active" : "Inactive"}</span>
                    <span className="text-sm text-gray-500">{zone.locations?.length || 0} locations</span>
                    {expandedZone === zone.id ? <ChevronUp size={16} className="text-gray-400" /> : <ChevronDown size={16} className="text-gray-400" />}
                  </div>
                </button>
                {expandedZone === zone.id && (
                  <div className="bg-gray-50 p-4 border-t">
                    <div className="flex justify-between mb-3">
                      <h3 className="text-sm font-medium text-gray-700">Locations</h3>
                      <button onClick={() => { setLocForm({ ...locForm, zoneId: zone.id }); setShowLocCreate(true); }} className="text-xs text-blue-600 hover:underline flex items-center gap-1"><Plus size={12} /> Add Location</button>
                    </div>
                    {locations.length === 0 ? <p className="text-sm text-gray-400">No locations</p> : (
                      <table className="w-full text-xs">
                        <thead><tr className="text-gray-500"><th className="text-left py-1">Code</th><th className="text-left py-1">Type</th><th className="text-left py-1">Product</th><th className="text-right py-1">Qty</th><th className="text-right py-1">Max</th><th className="text-center py-1">Status</th></tr></thead>
                        <tbody>{locations.map(loc => (
                          <tr key={loc.id} className="border-t border-gray-200"><td className="py-1.5 font-mono">{loc.code}</td><td>{loc.type}</td><td>{loc.product?.name || "-"}</td><td className="text-right">{loc.currentQuantity}</td><td className="text-right text-gray-400">{loc.maxCapacity}</td><td className="text-center"><span className={`w-2 h-2 rounded-full inline-block ${loc.isOccupied ? "bg-blue-500" : "bg-gray-300"}`} /></td></tr>
                        ))}</tbody>
                      </table>
                    )}
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
      </div>

      {showCreate && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl p-6 w-full max-w-md">
            <h3 className="text-lg font-bold mb-4">New Zone</h3>
            <div className="space-y-3">
              <input value={form.name} onChange={e => setForm({...form, name: e.target.value})} placeholder="Zone Name" className="w-full px-3 py-2 border rounded-lg text-sm" />
              <input value={form.code} onChange={e => setForm({...form, code: e.target.value.toUpperCase()})} placeholder="Code (e.g. MAIN)" className="w-full px-3 py-2 border rounded-lg text-sm font-mono" />
              <select value={form.type} onChange={e => setForm({...form, type: e.target.value})} className="w-full px-3 py-2 border rounded-lg text-sm">
                {["General","Cold","Hazardous","Quarantine","Shipping","Receiving"].map(t => <option key={t} value={t}>{t}</option>)}
              </select>
              <textarea value={form.description} onChange={e => setForm({...form, description: e.target.value})} placeholder="Description" className="w-full px-3 py-2 border rounded-lg text-sm" rows={2} />
            </div>
            <div className="flex gap-2 mt-4 justify-end">
              <button onClick={() => setShowCreate(false)} className="px-4 py-2 text-gray-600 hover:bg-gray-100 rounded-lg text-sm">Cancel</button>
              <button onClick={createZone} className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm hover:bg-blue-700">Create</button>
            </div>
          </div>
        </div>
      )}

      {showLocCreate && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl p-6 w-full max-w-md">
            <h3 className="text-lg font-bold mb-4">New Location</h3>
            <div className="space-y-3">
              <input value={locForm.code} onChange={e => setLocForm({...locForm, code: e.target.value.toUpperCase()})} placeholder="Code (e.g. A-01-02)" className="w-full px-3 py-2 border rounded-lg text-sm font-mono" />
              <select value={locForm.type} onChange={e => setLocForm({...locForm, type: e.target.value})} className="w-full px-3 py-2 border rounded-lg text-sm">
                {["Shelf","Bin","Floor","Pallet"].map(t => <option key={t} value={t}>{t}</option>)}
              </select>
              <input type="number" value={locForm.maxCapacity} onChange={e => setLocForm({...locForm, maxCapacity: parseInt(e.target.value)})} placeholder="Max Capacity" className="w-full px-3 py-2 border rounded-lg text-sm" />
            </div>
            <div className="flex gap-2 mt-4 justify-end">
              <button onClick={() => setShowLocCreate(false)} className="px-4 py-2 text-gray-600 hover:bg-gray-100 rounded-lg text-sm">Cancel</button>
              <button onClick={createLocation} className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm hover:bg-blue-700">Create</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
