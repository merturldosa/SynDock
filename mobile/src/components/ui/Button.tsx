import {
  TouchableOpacity,
  Text,
  ActivityIndicator,
  StyleSheet,
  type ViewStyle,
  type TextStyle,
} from "react-native";
import { useTheme } from "../../hooks/useTheme";

interface ButtonProps {
  title: string;
  onPress: () => void;
  variant?: "primary" | "secondary" | "outline" | "ghost";
  size?: "sm" | "md" | "lg";
  isLoading?: boolean;
  disabled?: boolean;
  style?: ViewStyle;
}

export function Button({
  title,
  onPress,
  variant = "primary",
  size = "md",
  isLoading = false,
  disabled = false,
  style,
}: ButtonProps) {
  const theme = useTheme();

  const bgColors: Record<string, string> = {
    primary: theme.secondary,
    secondary: theme.primary,
    outline: "transparent",
    ghost: "transparent",
  };

  const textColors: Record<string, string> = {
    primary: "#FFFFFF",
    secondary: "#FFFFFF",
    outline: theme.secondary,
    ghost: theme.secondary,
  };

  const heights: Record<string, number> = { sm: 36, md: 44, lg: 52 };
  const fontSizes: Record<string, number> = { sm: 14, md: 16, lg: 18 };

  return (
    <TouchableOpacity
      onPress={onPress}
      disabled={disabled || isLoading}
      style={[
        styles.button,
        {
          backgroundColor: bgColors[variant],
          height: heights[size],
          borderWidth: variant === "outline" ? 1 : 0,
          borderColor: theme.secondary,
          opacity: disabled ? 0.5 : 1,
        },
        style,
      ]}
    >
      {isLoading ? (
        <ActivityIndicator color={textColors[variant]} />
      ) : (
        <Text
          style={[
            styles.text,
            { color: textColors[variant], fontSize: fontSizes[size] },
          ]}
        >
          {title}
        </Text>
      )}
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  button: {
    borderRadius: 8,
    paddingHorizontal: 16,
    alignItems: "center",
    justifyContent: "center",
  },
  text: {
    fontWeight: "600",
  },
});
