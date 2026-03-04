import { useEffect, useState } from "react";
import {
  View,
  Text,
  FlatList,
  TouchableOpacity,
  StyleSheet,
  ActivityIndicator,
  Alert,
} from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { useTheme } from "../../src/hooks/useTheme";
import { Button } from "../../src/components/ui/Button";
import { Input } from "../../src/components/ui/Input";
import { getAddresses, createAddress } from "../../src/lib/api";
import { EmptyState } from "../../src/components/ui/EmptyState";
import type { Address } from "../../src/types";

export default function AddressesScreen() {
  const theme = useTheme();
  const [addresses, setAddresses] = useState<Address[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({
    recipientName: "",
    phone: "",
    zipCode: "",
    address1: "",
    address2: "",
  });
  const [isSaving, setIsSaving] = useState(false);

  const load = async () => {
    try {
      const res = await getAddresses();
      setAddresses(res.data);
    } catch {}
    setIsLoading(false);
  };

  useEffect(() => {
    load();
  }, []);

  const handleSave = async () => {
    if (!form.recipientName || !form.phone || !form.zipCode || !form.address1) {
      Alert.alert("입력 오류", "필수 항목을 입력해주세요");
      return;
    }
    setIsSaving(true);
    try {
      await createAddress({
        ...form,
        isDefault: addresses.length === 0,
      } as any);
      setShowForm(false);
      setForm({ recipientName: "", phone: "", zipCode: "", address1: "", address2: "" });
      load();
    } catch (err: any) {
      Alert.alert("저장 실패", err.response?.data?.error || "다시 시도해주세요");
    }
    setIsSaving(false);
  };

  if (isLoading) {
    return (
      <View style={styles.center}>
        <ActivityIndicator size="large" color={theme.primary} />
      </View>
    );
  }

  if (showForm) {
    return (
      <View style={[styles.container, { backgroundColor: theme.background }]}>
        <View style={[styles.formContainer, { backgroundColor: theme.surface }]}>
          <Text style={[styles.formTitle, { color: theme.text }]}>
            새 배송지 추가
          </Text>
          <Input
            label="받는 분"
            placeholder="이름을 입력하세요"
            value={form.recipientName}
            onChangeText={(v) => setForm((p) => ({ ...p, recipientName: v }))}
          />
          <Input
            label="연락처"
            placeholder="010-0000-0000"
            value={form.phone}
            onChangeText={(v) => setForm((p) => ({ ...p, phone: v }))}
            keyboardType="phone-pad"
          />
          <Input
            label="우편번호"
            placeholder="12345"
            value={form.zipCode}
            onChangeText={(v) => setForm((p) => ({ ...p, zipCode: v }))}
            keyboardType="number-pad"
          />
          <Input
            label="주소"
            placeholder="도로명 주소"
            value={form.address1}
            onChangeText={(v) => setForm((p) => ({ ...p, address1: v }))}
          />
          <Input
            label="상세 주소"
            placeholder="동/호수 (선택)"
            value={form.address2}
            onChangeText={(v) => setForm((p) => ({ ...p, address2: v }))}
          />
          <View style={styles.formButtons}>
            <Button
              title="취소"
              variant="outline"
              onPress={() => setShowForm(false)}
              style={{ flex: 1 }}
            />
            <Button
              title="저장"
              onPress={handleSave}
              isLoading={isSaving}
              style={{ flex: 1 }}
            />
          </View>
        </View>
      </View>
    );
  }

  return (
    <View style={[styles.container, { backgroundColor: theme.background }]}>
      <FlatList
        data={addresses}
        keyExtractor={(item) => String(item.id)}
        contentContainerStyle={addresses.length === 0 ? { flex: 1 } : undefined}
        ListEmptyComponent={
          <EmptyState
            icon="location-outline"
            title="등록된 배송지가 없습니다"
            description="배송지를 추가하면 주문 시 편리하게 사용할 수 있습니다"
          />
        }
        renderItem={({ item }) => (
          <View style={[styles.card, { backgroundColor: theme.surface }]}>
            <View style={styles.cardHeader}>
              <Text style={[styles.name, { color: theme.text }]}>
                {item.recipientName}
              </Text>
              {item.isDefault && (
                <View
                  style={[
                    styles.defaultBadge,
                    { backgroundColor: theme.primary + "20" },
                  ]}
                >
                  <Text style={{ color: theme.primary, fontSize: 11, fontWeight: "600" }}>
                    기본
                  </Text>
                </View>
              )}
            </View>
            <Text style={{ color: theme.textSecondary, marginTop: 4 }}>
              {item.phone}
            </Text>
            <Text style={{ color: theme.textSecondary, marginTop: 2 }}>
              [{item.zipCode}] {item.address1}
              {item.address2 && ` ${item.address2}`}
            </Text>
          </View>
        )}
      />
      <View style={[styles.bottomBar, { backgroundColor: theme.surface }]}>
        <Button
          title="배송지 추가"
          onPress={() => setShowForm(true)}
          size="lg"
          style={{ flex: 1 }}
        />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  center: { flex: 1, alignItems: "center", justifyContent: "center" },
  card: {
    margin: 16,
    marginBottom: 0,
    padding: 16,
    borderRadius: 12,
  },
  cardHeader: {
    flexDirection: "row",
    alignItems: "center",
    gap: 8,
  },
  name: { fontSize: 15, fontWeight: "600" },
  defaultBadge: {
    paddingHorizontal: 8,
    paddingVertical: 2,
    borderRadius: 8,
  },
  formContainer: { margin: 16, padding: 20, borderRadius: 12 },
  formTitle: { fontSize: 18, fontWeight: "700", marginBottom: 16 },
  formButtons: { flexDirection: "row", gap: 12, marginTop: 16 },
  bottomBar: {
    padding: 16,
    borderTopWidth: 1,
    borderTopColor: "#E5E5E5",
  },
});
