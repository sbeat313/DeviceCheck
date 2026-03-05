# DeviceCheck 系統說明

## 架構
- `DeviceCheck`：監控設備、做狀態判定、發送通知。
- `NotificationReceiver.UnitTests`：獨立測試方案，用測試內建 HTTP 接收端驗證通知送達。

## 判定流程
1. Probe 結果先累積連續計數。
2. 未達 `DecisionThresholdCount` 時狀態維持 `Unknown`。
3. 達門檻後最終判定 `Alive` 或 `Busy/Dead`。
4. 當最終判定跨越正常/異常邊界時，DeviceCheck 透過 HTTP POST 發通知到 `NotificationEndpoints`。

## 為什麼改成 UnitTest 接收端
- 你要求「獨立方案 + UnitTest」。
- 測試可直接驗證主程式發送內容，不需額外部署常駐接收服務。
- 後續若要接真實系統，只要把 `NotificationEndpoints` 改成正式 URL。
