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
import { useAuthStore } from "../../src/stores/authStore";

export default function LoginScreen() {
  const theme = useTheme();
  const router = useRouter();
  const login = useAuthStore((s) => s.login);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const handleLogin = async () => {
    if (!email || !password) {
      Alert.alert("오류", "이메일과 비밀번호를 입력해주세요");
      return;
    }
    setIsLoading(true);
    const success = await login(email, password);
    setIsLoading(false);
    if (success) {
      router.replace("/(tabs)/" as never);
    } else {
      Alert.alert("로그인 실패", "이메일 또는 비밀번호를 확인해주세요");
    }
  };

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
            로그인
          </Text>
        </View>

        <View style={styles.form}>
          <Input
            label="이메일"
            placeholder="example@email.com"
            value={email}
            onChangeText={setEmail}
            keyboardType="email-address"
            autoCapitalize="none"
          />
          <Input
            label="비밀번호"
            placeholder="비밀번호를 입력하세요"
            value={password}
            onChangeText={setPassword}
            secureTextEntry
          />
          <Button
            title="로그인"
            onPress={handleLogin}
            isLoading={isLoading}
            size="lg"
          />
        </View>

        <View style={styles.footer}>
          <Text style={{ color: theme.textSecondary }}>
            계정이 없으신가요?{" "}
          </Text>
          <TouchableOpacity
            onPress={() => router.push("/(auth)/register" as never)}
          >
            <Text style={{ color: theme.primary, fontWeight: "600" }}>
              회원가입
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
    marginBottom: 40,
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
