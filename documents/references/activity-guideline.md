# OrderHub AI Agent 實作練習

這是一套用 **Claude Code（或其他 coding agent）** 完成日常開發任務的實戰練習。
專案是一個公司內部的訂單管理系統（ASP.NET Core MVC + EF Core + SQL Server），
你會依序完成：讀懂專案 → 排查修復 3 個 bug → 新增一個小功能 → 小型重構。

> 開始前請先照 `README.md` 把網站跑起來，確認可以在瀏覽器操作。
> 過程中的心得請隨手記錄到 `PROCESS.md`。

**共同原則**

- 每個練習都先**自己在頁面上操作、觀察**，再把「具體的觀察」告訴 agent——這比丟一句「幫我修 bug」有效得多。
- Agent 的回答**永遠要人工驗證**：對照程式碼、在頁面上實測、跑測試。
- 一個修復＝一個獨立 commit，commit message 說清楚「症狀 → 根因 → 修法」。

---

## 練習 1 — 讓 agent 讀懂專案, agent 初始設置

**目標**：學會用 agent 快速建立對陌生專案的理解，並練習驗證它的說法、如何初始化設定專
案。

**做法**

1. 根據你使用的工具挑一份設定指南進行設定：
   - Claude Code：[agent-configuration.md](agent-configuration.md)
   - Codex CLI：[agent-configuration-codex.md](agent-configuration-codex.md)
2. 把設定檔案 `commit` 到 git
3. `PROCESS.md` 自我驗證

---

## 練習 2 — 排查並修復 3 個 bug

系統上線後陸續接到三張客訴單。**只有症狀，沒有其他線索。**

> 建議流程（每個 bug 都一樣）：
> **① 在頁面上親手重現 → ② 把觀察到的具體現象（頁碼、金額、庫存數字）告訴 agent → ③ 和 agent 一起定位根因 → ④ 修復 → ⑤ 回到頁面確認 → ⑥ 補一個回歸測試**。
> 每個 bug 一個獨立 commit（含回歸測試）。

- 可以使用 `練習 1` 添加的 `fix-bug` skill 來試試（Claude Code 輸入 `/fix-bug`，Codex 輸入 `$fix-bug`）
- 一個bug `commit` 一次

### 客訴 1：訂單列表怪怪的

> 客服：「客戶說剛建立的訂單在列表**第一頁找不到**，要翻到後面才看得到；而且點到**最後一頁常常是空白的**。」

重現提示：開 `/Orders`，先建一筆新訂單記下編號，回列表第一頁找找看；再點分頁的最後一頁。

### 客訴 2：Gold 會員的金額對不上

> 財務：「對帳時發現 **Gold 會員**新訂單的應付總額比我們手算的**少了一截**，但 **Silver 會員完全正常**。」

重現提示：到 `/Products` 記下某商品原價 → 用 Gold 客戶（下拉顯示「金卡會員」）建一筆該商品 × 1 的訂單 → 在明細頁手算：原價 × 0.9 應該是多少？頁面顯示多少？再用 Silver 客戶做對照組。

### 客訴 3：庫存越退越少

> 倉庫：「商品頁的庫存數字**跟實際盤點對不上**，而且好像**每次退單（取消訂單）之後就更少**。」

重現提示：記下 `/Products` 某商品庫存 → 建一筆該商品的訂單（庫存應該正確減少）→ 取消這筆訂單 → 回商品頁看庫存**有沒有加回來**。

---

## 練習 3 — 新功能：低庫存警示頁面

採購同事希望有一頁能快速看到「快沒貨」的商品。請實作以下規格，並**遵循專案既有慣例**（Controller 薄、邏輯在 Core 的 service、repository 包 EF Core、View 綁 ViewModel、DataAnnotations 驗證）。

### 規格

- **路由**：`GET /Products/LowStock?threshold=10`
- **頁面內容**：
  - 一個 threshold 數字輸入框 + 「查詢」按鈕（**GET form**，送出後網址帶 `?threshold=`）
  - 表格列出 `StockQuantity < threshold` **且 `IsActive`** 的商品，依**庫存量升冪**排序
  - 欄位：Sku、名稱、現有庫存、**近 30 天售出數量**（從訂單明細統計，**排除 Cancelled 訂單**）
  - 庫存 **< 5** 的列用警示色標記（例如 Bootstrap 的 `table-danger`）
- **驗證**：
  - `threshold` 未帶時預設 10
  - `threshold <= 0` 時顯示表單驗證錯誤訊息（**不可以是 500 錯誤頁**）
- **導覽列**加入「低庫存」連結
- **測試**：至少 3 個 service 層單元測試（建議：門檻過濾與排序、排除停售商品、近 30 天銷量排除 Cancelled）

---

## 練習 4 — 小型 revamp

`OrderService.CreateOrderAsync` 裡的驗證邏輯（客戶存在、明細非空、數量、重複商品、庫存……）越長越大。
請 agent 提案並執行一次小型重構（例如抽出驗證方法或 validator），要求：

- 行為完全不變（**所有測試維持全綠**，包含你在練習 2、3 補的）
- 重構前先請 agent 說明計畫，你確認後再動手
- 一個獨立 commit

---
