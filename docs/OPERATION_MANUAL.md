# DeviceCheck 操作手冊

## 1. 設定
`appsettings.json` 的 `DeviceCheck`：
- `DecisionThresholdCount`：連續判定門檻。
- `NotificationEndpoints`：通知接收 URL 清單（可設定真實外部系統 Endpoint）。

## 2. 啟動主程式
- `dotnet run --urls http://0.0.0.0:5000`

## 3. 驗證通知（用獨立 UnitTest 方案）
- 方案：`NotificationReceiver.UnitTests/NotificationReceiver.UnitTests.sln`
- 測試：`DeviceNotificationServiceReceiverTests`
- 測試會自行啟本機 HTTP 接收端，檢查主程式通知 payload 是否成功送達。

## 4. 說明
你要求的接收端已改為「獨立 UnitTest 方案」，不再額外維護另一個常駐服務專案。
