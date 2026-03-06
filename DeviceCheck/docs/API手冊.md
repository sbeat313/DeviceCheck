# DeviceCheck API 手冊

## 1. DeviceCheck 主服務
Base URL 範例：`http://localhost:5000`

### 1.1 心跳回報
- **Method**: `POST`
- **Path**: `/api/devices/{uid}/heartbeat`
- **用途**: 設備主動回報存活，刷新 `LastSeenUtc` 並延後下次檢查。

成功（200）：
```json
{
  "uid": 1001,
  "message": "heartbeat accepted"
}
```

失敗（404）：
```json
{
  "uid": 9999,
  "message": "uid not tracked"
}
```

---

### 1.2 查詢全部設備
- **Method**: `GET`
- **Path**: `/api/devices`
- **用途**: 取得所有列管 UID 狀態（含 `alias`）。

---

### 1.3 查詢單一設備
- **Method**: `GET`
- **Path**: `/api/devices/{uid}`
- **用途**: 查詢指定 UID 狀態（含 `alias`）。

---

### 1.4 更新設備別名
- **Method**: `PUT`
- **Path**: `/api/devices/{uid}/alias`
- **用途**: 更新設備中文別名，並同步回寫 `config.json`。

Request Body：
```json
{
  "alias": "機台A"
}
```

成功（200）：
```json
{
  "uid": 1001,
  "alias": "機台A",
  "message": "alias updated"
}
```

---

## 2. DeviceCheck.Notifier
Base URL（預設）：`http://127.0.0.1:5058`

### 2.1 接收狀態轉換通知
- **Method**: `POST`
- **Path**: `/api/notifications`
- **用途**: 接收主服務送出的 Dead 邊界通知。

Request Body：
```json
{
  "uid": 1001,
  "alias": "機台A",
  "fromStatus": "Alive",
  "toStatus": "Dead",
  "message": "設備 1001（機台A）狀態由 Alive 變更為 Dead，探測結果：503 ServiceUnavailable",
  "occurredAtUtc": "2026-01-01T10:00:00Z"
}
```

成功（200）：
```json
{
  "message": "notification accepted"
}
```

### 2.2 健康檢查
- **Method**: `GET`
- **Path**: `/`
- **用途**: 確認 Notifier 服務是否正在運行。

---

## 3. 通知與狀態補充
- `Dead` 必須連續達到 `DeadConsecutiveThreshold`。
- 未達門檻前為 `Unknown`。
- 通知只看 Dead 邊界，不看一般狀態互轉。
- 通知中的非 Dead 狀態統一顯示為 `Alive`。
