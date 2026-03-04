import { useState } from "react";
import {
  View,
  Text,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
  TouchableOpacity,
  StyleSheet,
  Alert,
} from "react-native";
import { useRouter } from "expo-router";
import { useTheme } from "../../src/hooks/useTheme";
import { Input } from "../../src/components/ui/Input";
import { Button } from "../../src/components/ui/Button";
import { register as registerApi } from "../../src/lib/api";

export default function RegisterScreen() {
  const theme = useTheme();
  const router = useRouter();
  const [form, setForm] = useState({
    email: "",
    password: "",
    confirmPassword: "",
    username: "",
    name: "",
  });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isLoading, setIsLoading] = useState(false);

  const validate = () => {
    const e: Record<string, string> = {};
    if (!form.username || form.username.length < 4)
      e.username = "4자 이상 입력해주세요";
    if (!form.email || !form.email.includes("@"))
      e.email = "올바른 이메일을 입력해주세요";
    if (!form.password || form.password.length < 8)
      e.password = "8자 이상 입력해주세요";
    if (form.password !== form.confirmPassword)
      e.confirmPassword = "비밀번호가 일치하지 않습니다";
    if (!form.name) e.name = "이름을 입력해주세요";
    setErrors(e);
    return Object.keys(e).length === 0;
  };

  const handleRegister = async () => {
    if (!validate()) return;
    setIsLoading(true);
    try {
      await registerApi({
        email: form.email,
        password: form.password,
        username: form.username,
        name: form.name,
      });
      Alert.alert("회원가입 완료", "로그인해주세요", [
        { text: "확인", onPress: () => router.replace("/(auth)/login" as never) },
      ]);
    } catch (err: any) {
      Alert.alert(
        "회원가입 실패",
        err.response?.data?.error || "다시 시도해주세요"
      );
    }
    setIsLoading(false);
  };

  const update = (key: string, value: string) =>
    setForm((prev) => ({ ...prev, [key]: value }));

  return (
    <KeyboardAvoidingView
      style={{ flex: 1 }}
      behavior={Platform.OS === "ios" ? "padding" : undefined}
    >
      <ScrollView
        contentContainerStyle={[
          styles.container,
          { backgroundColor: theme.background },
        ]}
        keyboardShouldPersistTaps="handled"
      >
        <View style={styles.header}>
          <Text style={[styles.logo, { color: theme.secondary }]}>SynDock</Text>
          <Text style={[styles.subtitle, { color: theme.textSecondary }]}>
            회원가입
          </Text>
        </View>

        <View style={styles.form}>
          <Input
            label="사용자명"
            placeholder="4자 이상"
            value={form.username}
            onChangeText={(v) => update("username", v)}
            error={errors.username}
            autoCapitalize="none"
          />
          <Input
            label="이메일"
            placeholder="example@email.com"
            value={form.email}
            onChangeText={(v) => update("email", v)}
            error={errors.email}
            keyboardType="email-address"
            autoCapitalize="none"
          />
          <Input
            label="비밀번호"
            placeholder="8자 이상"
            value={form.password}
            onChangeText={(v) => update("password", v)}
            error={errors.password}
            secureTextEntry
          />
          <Input
            label="비밀번호 확인"
            placeholder="비밀번호를 다시 입력하세요"
            value={form.confirmPassword}
            onChangeText={(v) => update("confirmPassword", v)}
            error={errors.confirmPassword}
            secureTextEntry
          />
          <Input
            label="이름"
            placeholder="이름을 입력하세요"
            value={form.name}
            onChangeText={(v) => update("name", v)}
            error={errors.name}
          />
          <Button
            title="회원가입"
            onPress={handleRegister}
            isLoading={isLoading}
            size="lg"
          />
        </View>

        <View style={styles.footer}>
          <Text style={{ color: theme.textSecondary }}>
            이미 계정이 있으신가요?{" "}
          </Text>
          <TouchableOpacity
            onPress={() => router.push("/(auth)/login" as never)}
          >
            <Text style={{ color: theme.primary, fontWeight: "600" }}>
              로그인
            </Text>
          </TouchableOpacity>
        </View>
      </ScrollView>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  container: {
    flexGrow: 1,
    justifyContent: "center",
    padding: 24,
  },
  header: {
    alignItems: "center",
    marginBottom: 32,
  },
  logo: {
    fontSize: 32,
    fontWeight: "700",
  },
  subtitle: {
    fontSize: 16,
    marginTop: 8,
  },
  form: {
    gap: 4,
  },
  footer: {
    flexDirection: "row",
    justifyContent: "center",
    marginTop: 24,
  },
});
