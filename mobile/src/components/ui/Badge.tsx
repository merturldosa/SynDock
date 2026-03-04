import { View, Text, StyleSheet } from "react-native";
import { useTheme } from "../../hooks/useTheme";

interface BadgeProps {
  count: number;
  size?: number;
}

export function Badge({ count, size = 18 }: BadgeProps) {
  const theme = useTheme();

  if (count <= 0) return null;

  return (
    <View
      style={[
        styles.badge,
        {
          backgroundColor: theme.error,
          width: size,
          height: size,
          borderRadius: size / 2,
        },
      ]}
    >
      <Text style={[styles.text, { fontSize: size * 0.6 }]}>
        {count > 99 ? "99+" : count}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  badge: {
    position: "absolute",
    top: -6,
    right: -6,
    alignItems: "center",
    justifyContent: "center",
  },
  text: {
    color: "#FFFFFF",
    fontWeight: "700",
  },
});
