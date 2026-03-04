import {
  TextInput,
  Text,
  View,
  StyleSheet,
  type TextInputProps,
} from "react-native";
import { useTheme } from "../../hooks/useTheme";

interface InputProps extends TextInputProps {
  label?: string;
  error?: string;
}

export function Input({ label, error, style, ...props }: InputProps) {
  const theme = useTheme();

  return (
    <View style={styles.container}>
      {label && (
        <Text style={[styles.label, { color: theme.text }]}>{label}</Text>
      )}
      <TextInput
        style={[
          styles.input,
          {
            borderColor: error ? theme.error : theme.border,
            color: theme.text,
          },
          style,
        ]}
        placeholderTextColor={theme.textSecondary}
        {...props}
      />
      {error && (
        <Text style={[styles.error, { color: theme.error }]}>{error}</Text>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    marginBottom: 16,
  },
  label: {
    fontSize: 14,
    fontWeight: "500",
    marginBottom: 6,
  },
  input: {
    height: 44,
    borderWidth: 1,
    borderRadius: 8,
    paddingHorizontal: 12,
    fontSize: 16,
    backgroundColor: "#FFFFFF",
  },
  error: {
    fontSize: 12,
    marginTop: 4,
  },
});
