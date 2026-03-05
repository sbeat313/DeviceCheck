# DeviceCheck 系統說明

## 架構概念
- `DeviceRegistry`：記憶體內狀態儲存與排程控制。
- `DeviceMonitorService`：背景輪詢引擎，每秒撈取到期設備並探測。
- `DeviceProbeClient`：呼叫外部 `.../Ctrl/{uid}/RadioCheck` API 並映射狀態。
- `DeviceNotificationService`：當狀態跨越正常/異常邊界時，對設定收件者發送通知（寫入日誌 + 投遞到模擬接收端）。
- `SimulatedNotificationReceiver`：In-Memory 模擬接收端，保存已收到通知，供 API 查詢。
- Minimal API (`Program.cs`)：提供心跳與狀態查詢端點。

## 狀態流
1. 啟動時將 `Uids` 載入，初始 `Unknown`。
2. 心跳可直接將設備設為 `Alive` 並延後下次檢查。
3. 背景輪詢探測後，依 HTTP 結果更新狀態：
   - `200` => `Alive`
   - `486` => `Busy`
   - 其他 / timeout / exception => `Dead`
4. 若狀態從 `Alive` 與「非 Alive」之間切換，觸發通知。


## 模擬接收端資料流
1. `DeviceNotificationService` 針對每位收件者產生一筆通知。
2. 通知先被寫入 `SimulatedNotificationReceiver` 的記憶體佇列。
3. 維運可透過 `/api/notifications/simulated` 讀取、清除，或手動注入測試通知。
