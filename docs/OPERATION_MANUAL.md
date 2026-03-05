# DeviceCheck 操作手冊

## 1. 設定檔
主設定檔：`appsettings.json`，重點在 `DeviceCheck` 區段。

```json
"DeviceCheck": {
  "BaseUrl": "http://192.168.0.58:5099",
  "CheckIntervalSeconds": 10,
  "BusyRetryDelaySeconds": 3,
  "RequestTimeoutSeconds": 10,
  "Uids": [1001, 1002, 1003],
  "NotificationRecipients": [
    "ops-team@example.com",
    "line-group:device-alert"
  ]
}
```

### 1.1 參數說明
- `BaseUrl`：設備 API 主機位址。
- `CheckIntervalSeconds`：一般輪詢間隔。
- `BusyRetryDelaySeconds`：當設備回傳 Busy (486) 時重試延遲。
- `RequestTimeoutSeconds`：呼叫設備 API 的逾時秒數。
- `Uids`：列管設備 UID 清單（不可為空）。
- `NotificationRecipients`：通知對象清單（不可包含空白字串）。

---

## 2. 通知行為
當設備狀態跨越正常/異常邊界時發送通知：
- 正常 → 異常（`Alive` → `Busy/Dead/...`）
- 異常 → 正常（`Busy/Dead/Unknown` → `Alive`）

通知目前以日誌輸出，格式含：收件對象、UID、轉換方向、前後狀態、觸發來源（`probe` 或 `heartbeat`）、結果與時間。

---

## 3. 服務啟動與日常操作
1. 調整 `appsettings.json`。
2. 啟動服務。
3. 使用 `GET /api/devices` 檢查設備狀態。
4. 設備端可定期呼叫 `POST /api/devices/{uid}/heartbeat`。
5. 監看 NLog 輸出中的 `[Notify]` 記錄以掌握狀態切換通知。

---

## 4. 排錯建議
- 若啟動失敗，先檢查 `DeviceCheck` 設定驗證錯誤訊息。
- 若大量 `Dead`，確認 `BaseUrl`、網路連線、設備服務狀態。
- 若沒有通知，確認 `NotificationRecipients` 是否有設定。
