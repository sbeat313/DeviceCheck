# DeviceCheck API 手冊

## 1. 服務目的
DeviceCheck 提供設備狀態監控 API，支援：
- 設備心跳回報。
- 查詢所有設備狀態。
- 背景輪詢設備可用性。
- 當設備最終判定跨越正常/異常邊界時，發送 HTTP 通知到外部接收端。

## 2. 狀態與判定機制
`DeviceHealthStatus`：`Unknown` / `Alive` / `Busy` / `Dead`。

- 正常：`Alive`
- 異常：`Busy`、`Dead`
- 過渡：`Unknown`（尚未達到連續判定次數）

`DeviceCheck:DecisionThresholdCount` 決定連續次數門檻。
- 未達門檻時，API 查詢狀態為 `Unknown`
- 達門檻才最終判定為 `Alive` 或 `Busy/Dead`

## 3. DeviceCheck API
### 3.1 POST `/api/devices/{uid}/heartbeat`
設備心跳。成功時會直接將設備視為 `Alive`，並重置排程。

### 3.2 GET `/api/devices`
查詢所有設備狀態。

### 3.3 GET `/api/devices/{uid}`
查詢單一設備狀態。

## 4. 通知接收端 API（NotificationReceiver 專案）
此為獨立服務，預設接收路由：

### 4.1 POST `/api/notifications`
接收 DeviceCheck 發出的通知。

Request body（對應 `DeviceStatusTransition`）：
```json
{
  "uid": 1001,
  "from": "Alive",
  "to": "Dead",
  "trigger": "probe",
  "result": "503 ServiceUnavailable",
  "occurredUtc": "2026-01-01T08:00:03+00:00"
}
```

### 4.2 GET `/api/notifications`
查看接收端已收到的通知。

### 4.3 DELETE `/api/notifications`
清空接收端通知。
