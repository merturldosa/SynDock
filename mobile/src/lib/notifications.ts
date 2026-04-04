import * as Notifications from "expo-notifications";
import * as Device from "expo-device";
import { Platform } from "react-native";
import { router } from "expo-router";
import { registerPushToken } from "./api";

Notifications.setNotificationHandler({
  handleNotification: async () => ({
    shouldShowAlert: true,
    shouldPlaySound: true,
    shouldSetBadge: true,
  }),
});

export async function registerForPushNotifications(): Promise<string | null> {
  if (!Device.isDevice) {
    console.log("Push notifications require a physical device");
    return null;
  }

  const { status: existingStatus } = await Notifications.getPermissionsAsync();
  let finalStatus = existingStatus;

  if (existingStatus !== "granted") {
    const { status } = await Notifications.requestPermissionsAsync();
    finalStatus = status;
  }

  if (finalStatus !== "granted") {
    console.log("Push notification permission denied");
    return null;
  }

  const tokenData = await Notifications.getExpoPushTokenAsync();
  const token = tokenData.data;

  // Register token with backend
  try {
    await registerPushToken(token, Platform.OS);
  } catch (error) {
    console.log("Failed to register push token with backend:", error);
  }

  // Android notification channel
  if (Platform.OS === "android") {
    await Notifications.setNotificationChannelAsync("default", {
      name: "기본 알림",
      importance: Notifications.AndroidImportance.MAX,
      vibrationPattern: [0, 250, 250, 250],
    });

    await Notifications.setNotificationChannelAsync("delivery", {
      name: "배송 알림",
      importance: Notifications.AndroidImportance.HIGH,
      vibrationPattern: [0, 250, 250, 250],
    });

    await Notifications.setNotificationChannelAsync("order", {
      name: "주문 알림",
      importance: Notifications.AndroidImportance.HIGH,
    });
  }

  return token;
}

export function setupNotificationListeners() {
  // Handle notification received while app is in foreground
  const receivedSubscription = Notifications.addNotificationReceivedListener(
    (notification) => {
      console.log("Notification received:", notification);
    }
  );

  // Handle notification tap (opens app)
  const responseSubscription =
    Notifications.addNotificationResponseReceivedListener((response) => {
      const data = response.notification.request.content.data;

      if (data?.orderId) {
        router.push(`/mypage/order-detail?id=${data.orderId}`);
      } else if (data?.assignmentId) {
        router.push(
          `/delivery-tracking?assignmentId=${data.assignmentId}`
        );
      } else if (data?.type === "Order") {
        router.push("/mypage/orders");
      } else {
        router.push("/mypage/notifications");
      }
    });

  return () => {
    receivedSubscription.remove();
    responseSubscription.remove();
  };
}
