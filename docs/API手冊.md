# DeviceCheck API 手冊

## 1. DeviceCheck 主服務 API

Base URL 範例：`http://localhost:5000`

### 1.1 心跳回報
- **Method**: `POST`
- **Path**: `/api/devices/{uid}/heartbeat`
- **說明**: 設備主動回報存活，會更新 `LastSeenUtc` 並延後下次檢查時間。

**成功回應 (200)**
```json
{
  "uid": 1001,
  "message": "heartbeat accepted"
}
```

**失敗回應 (404)**
```json
{
  "uid": 9999,
  "message": "uid not tracked"
}
```

### 1.2 取得所有設備狀態
- **Method**: `GET`
- **Path**: `/api/devices`
- **說明**: 取得目前所有列管 UID 狀態。

### 1.3 取得單一設備狀態
- **Method**: `GET`
- **Path**: `/api/devices/{uid}`
- **說明**: 查詢指定 UID 狀態。

## 2. DeviceCheck.Notifier 通知接收 API

Base URL 範例：`http://localhost:5058`

### 2.1 接收狀態轉換通知
- **Method**: `POST`
- **Path**: `/api/notifications`
- **說明**: 接收 DeviceCheck 發出的正常/異常狀態切換通知。

**Request Body**
```json
{
  "uid": 1001,
  "fromStatus": "Alive",
  "toStatus": "Dead",
  "message": "設備 1001 狀態由 Alive 變更為 Dead，探測結果：503 ServiceUnavailable",
  "occurredAtUtc": "2026-01-01T10:00:00Z",
  "recipients": ["ops-team@company.local", "oncall@company.local"]
}
```

**成功回應 (200)**
```json
{
  "message": "notification accepted",
  "receivedRecipients": 2
}
```
