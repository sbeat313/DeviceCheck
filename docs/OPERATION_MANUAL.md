# DeviceCheck 操作手冊

## 1. 設定
`appsettings.json` 的 `DeviceCheck`：
- `DecisionThresholdCount`：連續判定門檻。
- `NotificationEndpoints`：通知接收端 URL 清單（例如 `http://localhost:6001/api/notifications`）。

## 2. 啟動方式（兩個服務）
1. 啟動通知接收端：
   - `cd NotificationReceiver`
   - `dotnet run --urls http://0.0.0.0:6001`
2. 啟動 DeviceCheck：
   - `dotnet run --urls http://0.0.0.0:5000`

## 3. 驗證流程
1. 用 `GET /api/devices` 觀察設備狀態，未達門檻前應為 `Unknown`。
2. 製造連續異常（或連續恢復）直到達門檻。
3. 呼叫通知接收端 `GET /api/notifications`，確認收到通知。

## 4. 說明
目前僅保留「獨立 NotificationReceiver 服務」作為通知接收端，專案內已移除舊的模擬接收程式碼。
