# DeviceCheck API 手冊

## 1. 服務目的
DeviceCheck 提供設備狀態監控 API，支援：
- 設備心跳回報。
- 查詢所有設備狀態。
- 查詢單一設備狀態。
- 背景輪詢設備可用性並在正常/異常切換時發送通知（透過日誌輸出）。

---

## 2. 狀態定義
`DeviceHealthStatus` 列舉：
- `Unknown`：尚未檢查。
- `Alive`：正常。
- `Busy`：異常（忙碌）。
- `Dead`：異常（離線、錯誤、逾時或 503/其他錯誤碼）。

> 正常/異常切換規則：
> - 正常：`Alive`
> - 異常：`Busy` / `Dead`（`Unknown` 為未達判定次數的過渡狀態）


## 2.1 判定次數機制
可透過 `DeviceCheck:DecisionThresholdCount` 設定連續判定次數（例如 3）：
- 連續 `Alive` 未達 3 次前，狀態維持 `Unknown`。
- 連續 `Busy/Dead` 未達 3 次前，狀態維持 `Unknown`。
- 達到次數後才會最終判定為 `Alive` 或 `Busy/Dead`。
- 通知僅在「最終判定」跨越正常/異常時發送。


---

## 3. API 總覽
Base URL：`http://<host>:<port>`

### 3.1 POST `/api/devices/{uid}/heartbeat`
設備主動回報心跳，將該設備狀態設為 `Alive`，並延後下次檢查時間。

#### Path Parameter
- `uid` (int): 設備 UID。

#### Response
- `200 OK`
```json
{
  "uid": 1001,
  "message": "heartbeat accepted"
}
```
- `404 Not Found`
```json
{
  "uid": 9999,
  "message": "uid not tracked"
}
```

#### 行為備註
若該設備原本處於異常狀態，且心跳使其變成正常，會觸發通知。

---

### 3.2 GET `/api/devices`
取得所有列管設備目前狀態（UID 升冪排序）。

#### Response `200 OK`
```json
[
  {
    "uid": 1001,
    "status": "Alive",
    "lastSeenUtc": "2026-01-01T08:00:00+00:00",
    "lastCheckedUtc": "2026-01-01T08:00:00+00:00",
    "nextCheckUtc": "2026-01-01T08:00:10+00:00",
    "lastResult": "200 OK"
  }
]
```

---

### 3.3 GET `/api/devices/{uid}`
取得單一設備狀態。

#### Path Parameter
- `uid` (int): 設備 UID。

#### Response
- `200 OK`：回傳設備狀態物件。
- `404 Not Found`
```json
{
  "uid": 9999,
  "message": "uid not tracked"
}
```

---


### 3.4 GET `/api/notifications/simulated`
取得「模擬通知接收端」目前收到的通知清單。

#### Response `200 OK`
```json
[
  {
    "id": 1,
    "recipient": "ops-team@example.com",
    "uid": 1001,
    "from": "Alive",
    "to": "Dead",
    "category": "正常→異常",
    "trigger": "probe",
    "result": "503 ServiceUnavailable",
    "receivedUtc": "2026-01-01T08:00:03+00:00"
  }
]
```

### 3.5 POST `/api/notifications/simulated`
手動送一筆通知到模擬接收端（整合測試用）。

#### Request Body
`DeviceStatusTransition` JSON，例如：
```json
{
  "uid": 1001,
  "from": "Alive",
  "to": "Dead",
  "trigger": "api",
  "result": "manual test",
  "occurredUtc": "2026-01-01T08:00:03+00:00"
}
```

#### Response
- `202 Accepted`

### 3.6 DELETE `/api/notifications/simulated`
清空模擬接收端佇列。

#### Response
- `204 No Content`


## 4. 狀態欄位說明（DeviceState）
- `uid`：設備 ID。
- `status`：`Unknown` / `Alive` / `Busy` / `Dead`。
- `lastSeenUtc`：最後一次確認設備存活時間（UTC）。
- `lastCheckedUtc`：最後一次主動探測時間（UTC）。
- `nextCheckUtc`：下一次預計探測時間（UTC）。
- `lastResult`：最後探測結果字串（例如 `200 OK`, `486 BusyHere`, `timeout`）。
