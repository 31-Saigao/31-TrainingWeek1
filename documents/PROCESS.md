# PROCESS.md — 我的練習心得

> 一個原則：**寫「具體發生的事」，不寫感想文。**
> 貼上當時真實的 prompt、真實的數字、真實的錯誤訊息——三個月後的你（和你的同事）才用得上。

#### 使用的 agent 與模型：

Claude Code，模型 Claude Sonnet 5（`claude-sonnet-5`）

---

## 通用四問

### 1. 我的任務拆解

（開工前你把任務拆成哪幾步？實際做的時候順序有變嗎？為什麼變？）

- 我一開始只丟了一句很模糊的話：「這是 homework，需要修的東西都寫在 documents/README.md，照著 README.md 幫我修一下我的專案」。agent 沒有直接照 README.md 表面內容動手，而是先發現 README.md 只是索引，真正的任務清單在 `documents/activities/activity-guideline.md`（練習 1～4：讀懂專案/設定、修 3 個 bug、加低庫存頁、小重構）。
- 因為我一句話涵蓋了整份指南，agent 先用選擇題問我「只修 3 個 bug／bug+新功能／全部（bug+功能+重構）」，我選「全部」之後，它才建立 5 個追蹤任務：bug1 分頁、bug2 折扣、bug3 庫存、練習3 低庫存頁、練習4 重構。
- 實際執行順序跟指南建議的完全一致（先 3 個獨立 bug 各自 commit，再做新功能，最後做重構），沒有變動；唯一的差異是每個 bug 修完都立刻 `dotnet test` 全綠才 commit，而不是全部改完再一次驗證，這樣萬一某個修法有副作用可以馬上抓到是哪一個 commit 出的問題。

### 2. AI 幫上大忙的地方

（哪件事 agent 做得又快又好？**貼上當時的提問原文**，說明為什麼這樣問有效。）

- 提問原文：「this is a homework exercise, everything needed to fix is written in documents\README.md and you refer to the README.md and fix my projects ?」
  這句話其實資訊很少（沒講具體症狀、沒講要修哪裡），但有效的地方在於**指出了「答案在哪份文件裡」**，agent 靠這個線索自己往下追（README → activity-guideline.md → 具體 3 張客訴單描述），省了我自己去翻文件、猜任務範圍的時間。
- 最省時間的一次是 bug 3（庫存不回補）：agent 只靠讀 `OrderService.CancelOrderAsync` 的程式碼，就指出「`order.Status = OrderStatus.Cancelled;` 這行寫在判斷『是否為 Pending/Confirmed 才回補庫存』的 if 之前，導致判斷式恆為 false，回補區塊永遠不會執行」——這是一種肉眼很容易漏掉的死代碼（dead code），agent 一次掃過去就抓到，比我自己逐行 trace 快很多。
- 修完 bug 2（Gold 會員折扣）之後，agent 直接指出**既有測試** `OrderServicePricingTests.CalculateTotal_AppliesTierDiscountOnSubtotal` 早就寫死「Gold 折扣只在 CalculateTotal 套用一次」的期望值，證明正確答案應該是「拿掉 CreateOrderAsync 裡的預先打折」而不是「拿掉 CalculateTotal 的打折」——用既有測試反推正確修法，而不是憑感覺選一個方向改。

### 3. AI 誤導我的地方，與我如何發現

（agent 說錯／改錯／過度自信的時刻。你靠什麼抓到——對照程式碼？頁面實測？跑測試？）

- 這次沒有出現「改錯程式邏輯」的情況，但有兩處是我自己盯著才確認沒踩坑，不是 agent 主動提醒：
  1. `training-repo` 其實是巢狀在外層 git repo 裡面（`git rev-parse --show-toplevel` 顯示 repo root 是 `C:/Users/dm31/31-TrainingWeek1`，不是 `training-repo`）。第一次在 `training-repo` 目錄下跑 `git status`／`git diff --stat`，顯示的檔案路徑沒有 `training-repo/` 前綴，如果沒切回外層 repo root 再跑一次 `git status` 核對，直接 `git add` 很容易搞混路徑或漏加檔案。
  2. agent 寫的第一版分頁回歸測試草稿裡，塞了一行 `Assert.Equal(DateTime.UtcNow.AddMinutes(0).Date, page1.Items[0].CreatedAt.Date)`——這種依賴「執行當下時間」的斷言在測試裡是隱患（例如剛好跨零點就會 flaky）。這處是 agent 自己在下一步就發現不對勁並主動改掉，不是我發現後才要求的，但我在 review diff 時有特別確認它真的拿掉了，改成只斷言「page1 包含資料庫裡 CreatedAt 最大的那筆」「page3 筆數是 5、不是 0（代表末頁不再空白）」這種跟時間無關、穩定可重現的斷言——這提醒我：就算 agent 自己修正了，回歸測試本身也要我親自看過斷言內容，不能只看「測試有沒有過」。

### 4. 我會帶回日常工作的一招

（一個具體、可複製的做法，不要寫「要多驗證」這種口號——寫出**操作步驟**。）

- 具體做法：**交代模糊、範圍大的任務時，先讓 agent 用選擇題把範圍列出來給我選，再開始動手**，而不是丟一句話就讓它自由發揮猜多少算多少。
  操作步驟：
  1. 丟出模糊指令後，如果 agent 直接開始改檔案，先叫停，明講「先告訴我你打算做的範圍有哪幾種可能，列成選項讓我選」。
  2. 針對每個選項要求附一句話說明差異（例如這次是「只修 3 個 bug」vs「bug+新功能」vs「bug+功能+重構」），確認自己真的懂每個選項的工作量差異再選。
  3. 選定範圍後，要求 agent 把範圍拆成可追蹤的子任務清單（這次是 5 個獨立任務），每完成一項就跑一次 `dotnet test` 全綠、並讓我看到 diff 或實測結果再進下一項，不要等全部做完才一次驗收。

## 自我驗證（做到哪個階段答哪題）

### 第一階段 — Agentic Coding

練習 1

1. 我能不看筆記說出三個專案（Web/Core/Infrastructure）各自的職責
2. 我核對過 agent 描述的建單流程，且**至少找出一處不精確或過度簡化的說法**
3. 我知道商業邏輯應該放在哪一層、新增頁面要動哪些地方

練習 2

1. 三個 bug 我都先在頁面上重現過，才開始找程式 —— ⚠️ 老實記錄：這次是把三張客訴單原文直接轉給 agent，讓它照客訴描述去對照程式碼定位根因，我沒有先自己在瀏覽器上手動重現一次再開口。之後照指南建議的流程，應該自己先點過頁面、記下實際頁碼/金額/庫存數字再問，會更符合「① 親手重現 → ② 具體現象 → ③ 定位根因」的順序。
2. 我給 agent 的資訊包含具體觀察（頁碼／金額數字／庫存數字），而不是只貼客訴原文 —— 這次沒有做到，是直接請 agent 去讀客訴單原文＋程式碼推導，根因判斷靠的是程式碼邏輯本身（例如 `Skip(page * pageSize)`、`CreateOrderAsync` 的預先打折、`CancelOrderAsync` 判斷式順序），沒有搭配我自己實測出的具體數字。
3. 每個修復都回到頁面驗證過症狀消失 —— 3 個 bug 都用 `dotnet test` 回歸測試驗證行為正確；分頁/折扣/庫存三個修法也另外用單元測試模擬了原始症狀的重現條件（例如 Gold 會員下單驗證總額、45 筆訂單驗證分頁不空白），但沒有额外開瀏覽器逐一點擊 3 個 bug 的畫面。
4. 每個 bug 都補了一個回歸測試，`dotnet test` 全綠 —— 完成，每次修完都跑 `dotnet test`，從 29 → 30 → 32 個測試全部通過。
5. 三個獨立 commit，message 說明症狀與根因 —— 完成，commit 訊息格式「症狀 → 根因 → 修法」（`80b35fb`／`7eff235`／`08d6c76`）。
6. （思考題）為什麼原本的測試沒抓到這三個 bug？
   - bug1（分頁）：既有測試 `GetOrders_ReportsTotalCountAndTotalPages` 只驗證 `TotalCount`/`TotalPages` 這兩個「元資料」，從沒斷言過 `Items` 裡實際回傳的是哪幾筆——所以 Skip 算錯也不會被抓到。
   - bug2（Gold 折扣）：`CreateOrder_SnapshotsCurrentUnitPrice` 建單時用的是預設 `CustomerTier.Standard` 客戶，完全沒有測過 Gold 會員建單這條路徑，等於那個 `if (customer.Tier == CustomerTier.Gold)` 分支從沒被測試執行過。
   - bug3（庫存回補）：`CancelOrder_ActiveOrder_SetsStatusCancelled` 只斷言取消後 `Status == Cancelled`，從沒斷言過取消後商品的 `StockQuantity` 有沒有變回去。
   - 共同教訓：三個測試都只驗證了「表面上最容易想到的那個欄位」，沒有驗證跟這次修改直接相關的「金額/庫存實際數字」。

練習 3

1. `/Products/LowStock` 不帶參數 → 門檻 10 的結果；帶 `?threshold=3` → 結果隨之改變 —— 已用 `curl` 實際打 `?threshold=5`、`?threshold=10`、`?threshold=30` 三種門檻，確認回傳的商品清單筆數與內容隨門檻改變、且都依庫存量升冪排序。
2. `?threshold=0`、`?threshold=-1` → 頁面顯示驗證錯誤，不是 500 —— 已用 `curl` 驗證 `?threshold=0` 回傳 HTTP 200（不是 500），畫面上 `Threshold` 欄位帶出「門檻必須大於 0」的 `asp-validation-for` 錯誤訊息。
3. 售出數量欄位排除了 Cancelled 訂單（可用一筆已取消的訂單驗證）—— 已寫成單元測試 `GetLowStock_Sold30Days_ExcludesCancelledAndOldOrders`：同一商品建 3 筆訂單（30天內未取消／30天內但已取消／超過30天未取消），只有第一筆的數量被計入 `Sold30Days`。
4. 停售（已停售 badge）商品不出現在列表 —— 已寫成單元測試 `GetLowStock_ExcludesInactiveProducts`。
5. 程式分層與命名跟既有的 Products 功能一致（請 agent 自我 review 一次，並自己確認）—— Controller 只轉接（`TryValidateModel` + 呼叫 service），業務邏輯（近 30 天時間窗計算）放在 `ProductService`，EF Core 查詢放在 `ProductRepository`（用兩次查詢：商品清單＋依 ProductId 分組加總銷量，避免 N+1），View 綁 `LowStockViewModel`，驗證用 `[Range]` DataAnnotations——跟既有 `ProductsController.Index`／`CreateOrderViewModel` 的寫法一致，沒有自創一套。
6. 至少 3 個新測試，`dotnet test` 全綠 —— 補了 3 個測試（門檻過濾＋排序、排除停售、近 30 天銷量排除 Cancelled／過期），總測試數從 32 → 35，全部通過。

練習 4

1. 重構後 `dotnet test` 全綠 —— 完成，重構後仍是 35/35 全過。
2. 我能說出這次重構「改善了什麼、沒有改變什麼」——
   - 改善了什麼：`CreateOrderAsync` 原本一個方法裡混了「明細基本驗證（非空/數量/重複商品）」和「逐項檢查商品存在與庫存並組裝 OrderItem」兩種職責，拆成 `ValidateLines`（回傳單一錯誤字串或 null）和 `AddOrderItemsAsync`（回傳累積的錯誤清單）兩個私有方法後，`CreateOrderAsync` 本體只剩流程串接，一眼就能看完整個下單流程在做什麼。
   - 沒有改變什麼：兩種驗證各自原本的「錯誤回傳策略」刻意保留——`ValidateLines` 維持「命中第一個錯誤就馬上回傳」（跟原本三個 `if...return` 一致），`AddOrderItemsAsync` 維持「逐項檢查、累積所有錯誤一起回傳」（跟原本 foreach 迴圈裡 `errors.Add` 一致），沒有把兩者的行為統一成同一種策略，因為那會改變使用者看到的錯誤訊息內容/數量。
3. 我有在 code review 的角度看過 diff（不是 agent 說好就好）—— 看過 diff 後特別確認：(a) 兩個新方法搬出去的程式碼跟原本逐行一致，沒有夾帶額外邏輯；(b) `ValidateLines` 沒有不小心把三個獨立 `if` 改成累積驗證（這樣會讓使用者一次看到多個錯誤訊息，是行為改變）；(c) 重構本身沒有動到練習 2、3 的修復或新增的程式碼與測試。

---

## 附錄：值得留下的對話片段

（貼 1–2 段最有代表性的 prompt 與回應**摘要**——不用貼全文，重點是「我怎麼問」和「它怎麼答」。）

**片段 1**
我問：「this is a homework exercise, everything needed to fix is written in documents\README.md and you refer to the README.md and fix my projects ?」
它答（摘要）：先讀 `documents/README.md`，發現裡面只是連結索引，真正任務在 `documents/activities/activity-guideline.md`；讀完指南後沒有直接開始改程式，而是先用選擇題問我「只修 3 個 bug／bug+新功能／全部」，等我選定範圍才建立 5 個子任務開始動手。
→ 有效的地方：模糊指令下，agent 沒有自己瞎猜範圍，而是先把「文件裡藏著的任務範圍」攤開讓我選，避免做過頭或做不夠。

**片段 2**
我問（第二輪）：「and also write a work thought into the PRCOESS.md as well」
它答（摘要）：先讀現有 `PROCESS.md` 模板結構（通用四問＋自我驗證清單＋附錄），沒有整篇重寫，而是照模板原本的段落逐一填入這次工作實際發生的具體事實（真實 commit hash、真實測試數字變化 29→30→32→35、真實檔案路徑踩坑經驗），並在自我驗證清單裡誠實標記「沒做到」的項目（例如沒有先在瀏覽器手動重現三個 bug），而不是全部打勾美化。
→ 有效的地方：照著既有模板的段落逐條填空，而不是丟掉模板重寫一份新的，保留了團隊統一格式；誠實記錄沒做到的部分，比全部寫成「都做到了」更符合這份文件「不寫感想文、寫具體發生的事」的宗旨。
