# DeviceCheck 系統說明

## 架構
- `DeviceCheck`：監控設備、做狀態判定、發送通知。
- `NotificationReceiver`：獨立 Web API，接收通知並保存。

## 判定流程
1. Probe 結果先累積連續計數。
2. 未達 `DecisionThresholdCount` 時狀態維持 `Unknown`。
3. 達門檻後最終判定 `Alive` 或 `Busy/Dead`。
4. 當最終判定跨越正常/異常邊界時，DeviceCheck 透過 HTTP POST 發通知到 `NotificationEndpoints`。

## 為什麼要獨立接收端
- 可獨立部署/擴充。
- 可替換成真實企業通知系統（Message Queue、Webhook、告警平台）。
- 與 DeviceCheck 解耦，符合實際上線架構。
