# 進階指南 — 把 Claude Code「調教」成你的專案隊友

> 這份指南教你**怎麼設定 agent 的環境**——
> 用設定檔讓它自動遵守專案慣例、擋掉危險操作、把重複的流程變成一鍵指令。
> 每一節都有可以直接複製到本專案的範例，建議邊讀邊做。

**你會建立的檔案總覽**

| 檔案                             | 用途                                           | 要不要進 git          |
| -------------------------------- | ---------------------------------------------- | --------------------- |
| `CLAUDE.md`                      | 專案記憶：agent 每次啟動自動讀的專案說明與慣例 | ✅ 進 git（全隊共用） |
| `.claude/settings.json`          | 團隊共用設定：權限規則、hooks                  | ✅ 進 git             |
| `.claude/settings.local.json`    | 個人設定：只影響你自己                         | ❌ 自動被 gitignore   |
| `.claude/agents/*.md`            | Subagents：專職的子代理（如 code reviewer）    | ✅ 進 git             |
| `.claude/skills/<名稱>/SKILL.md` | Skills / 斜線指令：把流程做成 `/指令`          | ✅ 進 git             |

---

## 1. `CLAUDE.md` — 讓 agent 不用每次重新認識專案

放在**專案根目錄**，agent 每個 session 開始時自動載入。把「每次都要重講一遍」的東西寫進來：架構慣例、常用指令、地雷區。

在 `training-repo` 建立 `CLAUDE.md`：

```
claude      # 啟動（首次會要求登入）
/init       # 在對話框輸入，自動產生 CLAUDE.md
```

**進階技巧**

- 個人專用的補充寫在 `CLAUDE.local.md`（不進 git），例如「我的 SQL Server 是具名實例 .\SQLEXPRESS」
- 使用者層級的個人偏好放 `~/.claude/CLAUDE.md`，對你所有專案生效

**驗證方式**：
打開新session

- [ ] 問 agent「這個專案的分層慣例是什麼？」——它應該不用讀任何檔案就答得出來。

---

## 2. `.claude/settings.json` — 權限：先劃紅線，再開綠燈

Agent 預設每個敏感操作都會問你。與其每次按確認，不如把規則寫清楚：**危險的直接擋掉（deny）、日常的直接放行（allow）、重大的強制詢問（ask）**。

在 `training-repo/.claude/settings.json` 建立：

```json
{
  "permissions": {
    "deny": [
      "Bash(rm -rf *)",
      "Bash(git push --force *)",
      "Bash(git reset --hard *)",
      "Read(**/appsettings.Production.json)",
      "Read(**/*.pfx)",
      "Edit(src/OrderHub.Infrastructure/Migrations/**)"
    ],
    "ask": ["Bash(dotnet ef database drop *)", "Bash(git push *)"],
    "allow": [
      "Bash(dotnet build *)",
      "Bash(dotnet test *)",
      "Bash(dotnet run *)",
      "Bash(git status)",
      "Bash(git diff *)",
      "Bash(git log *)",
      "Bash(git add *)",
      "Bash(git commit *)"
    ]
  }
}
```

**規則語法重點**

- `Bash(dotnet build *)` → 允許所有以 dotnet build 開頭的指令
- `Bash(git status)` 沒有 \* → 只精確比對這一條指令,git status -s 不算
- `ask` 中的 `Bash(git push *)` 與 deny 中的 `Bash(git push --force *)` 並存時，一般 push 會詢問，強推則直接拒絕（deny 優先）
- 串接指令(如 `;`、`&&`、`|`)不能繞過:Claude Code 會解析複合指令並拆成子指令,**每個子指令都要各自命中規則**才會放行。但仍不應視為絕對安全防線——`watch`、`find -exec` 等包裝型指令另有處理(一律詢問),真正的硬邊界是 deny 規則與 hooks
- `Bash(指令 *)`：`*` 是萬用字元，**空格有差**——`Bash(git log *)` 匹配「git log 加任意參數」
- `Read(...)` / `Edit(...)`：gitignore 風格路徑。`*` 比對單層、`**` 比對任意層目錄
- `Read(**/*.pfx)` → 禁止讀取所有 .pfx 憑證檔(保護機密)

**為什麼這樣設計**

- `deny` Migrations 目錄：migration 是歷史紀錄，agent「順手修一下」會毀掉資料庫一致性
- `ask` database drop：練習中真的需要重置資料庫，但必須是**人**按下確認
- `allow` build/test：這些指令 agent 會反覆執行，每次都問只會讓你麻木地按 yes——**權限疲勞正是事故的來源**

**驗證方式**：
打開新session

- [ ] 請 agent 執行 `git push --force`，應被直接拒絕（deny），不會跳出詢問
- [ ] 請 agent 執行 `dotnet test`，應直接執行不詢問（allow）
- [ ] 請 agent 重置資料庫（`dotnet ef database drop`），應先跳出確認才執行（ask）

---

## 3. Hooks — 用程式強制執行，不靠 agent 自覺

CLAUDE.md 裡的規則 agent「通常」會遵守；hooks 則是**由 Claude Code 本身強制執行**的檢查點，agent 想繞也繞不過。設定寫在 settings.json 的 `hooks` 區塊。

**常用事件**：`PreToolUse`（工具執行前，可攔截）、`PostToolUse`（工具執行後）、`UserPromptSubmit`（你送出訊息時）、`SessionStart`、`Stop`（agent 結束回合時）。

permissions 擋的是「指令長相」，hook 可以檢查**內容**。

### PreToolUse & PostToolUse 範例

在 `training-repo/.claude/settings.json` 添加：

```json
{
  "hooks": {
    "PreToolUse": [
      {
        "matcher": "Bash",
        "hooks": [
          {
            "type": "command",
            "command": "powershell -NoProfile -File .claude/hooks/block-destructive-sql.ps1"
          }
        ]
      }
    ],
    "PostToolUse": [
      {
        "matcher": "Edit|Write",
        "hooks": [
          {
            "type": "command",
            "command": "powershell -NoProfile -File .claude/hooks/log-edits.ps1",
            "statusMessage": "Logging file edit..."
          }
        ]
      }
    ]
  }
}
```

- `PreToolUse` 攔截任何含 `DROP TABLE` / `TRUNCATE` 的指令（**exit code 2 = 擋下這次工具呼叫**，stderr 訊息會回饋給 agent）
- `PostToolUse` 每次 agent 用 Edit / Write 改完檔案，就把「時間、工具、檔案路徑」記錄到 `.claude/hooks/edit-log.txt`，並用 stdout JSON 的 `systemMessage` 在 UI 顯示一行提示——留下 agent 動過哪些檔案的稽核軌跡

> 兩個範例可以合併在同一個 settings.json 裡（`PreToolUse` 和 `PostToolUse` 並列）。
> 複雜邏輯建議寫成獨立腳本放 `.claude/hooks/`

把以下powershell 文件拷貝到 `training-repo/.claude/hooks/`

- [block-destructive-sql.ps1](block-destructive-sql.ps1)
- [log-edits.ps1](log-edits.ps1)

**驗證方式**：
打開新session

- [ ] 設定好後，故意請 agent「執行 sqlcmd 把 OrderItems 資料表 TRUNCATE 掉」——它應該被 hook 擋下，並回報被擋的原因。
- [ ] 請agent 創建一份 sample.txt, 查看 `.claude/hooks/` 資料夾裡面出現一個 `edit-log.txt` 文件

---

## 4. Subagents — 建立專職的子代理

Subagent 是有**獨立 context、獨立工具權限**的子代理。兩個經典用途：

1. **唯讀 reviewer**：只給讀取工具，物理上不可能改壞你的程式碼
2. **隔離大量輸出**：跑測試、查大量檔案的雜訊留在子代理，不塞爆主對話

在 `training-repo/.claude/agents/code-reviewer.md` 建立：

```markdown
---
name: code-reviewer
description: 審查程式碼變更是否符合 OrderHub 分層慣例。完成 bug 修復或新功能後主動使用。
tools: Read, Grep, Glob, Bash
---

你是 OrderHub 專案的資深 reviewer。審查目前的變更（git diff），依序檢查：

1. 分層：商業邏輯是否在 Core 的 service？Controller 是否保持薄？
   有沒有在 service/controller 直接使用 DbContext？
2. View 是否綁 ViewModel 而非 domain model？
3. 驗證是否用 DataAnnotations + ModelState（使用者輸入不可造成 500）？
4. 金額是否使用 decimal？
5. 有沒有對應的測試？測試是否真的驗證了行為（不是恆真斷言）？

輸出：依嚴重度排序的問題清單，每項附檔案:行號與具體修改建議。沒問題就明說。
```

再建一個 `test-runner.md`（把測試雜訊隔離在子代理）：

```markdown
---
name: test-runner
description: 執行 dotnet test 並回報摘要。需要跑測試驗證時使用。
tools: Bash, Read, Grep
---

執行 `dotnet test`。全綠時只回報「N 個測試全部通過」。
有失敗時：列出失敗的測試名稱、斷言訊息、以及你判斷的可能原因，不要貼完整輸出。
```

**frontmatter 重點**：`name`、`description` 必填（description 決定 agent 何時會主動委派給它）；`tools` 是工具白名單（不填＝全部繼承），**只能列工具名稱**（如 `Bash`），不支援 `Bash(git diff *)` 這種細部規則——要限制 Bash 只能跑特定指令，請改用 PreToolUse hook 或 permissions；可用 `model` 指定較便宜的模型跑機械性任務。

**驗證方式**：

- [ ] 修完一個 bug 後說「用 code-reviewer 審查我的變更」，或直接觀察 agent 會不會在適當時機自己委派。
- [ ] 用 test-runner 跑測試

---

## 5. Skills（斜線指令）— 把重複流程做成一鍵指令

練習 2 每個 bug 都要走同一套流程，第二次開始你就會想把它做成指令。在 `training-repo/.claude/skills/fix-bug/SKILL.md` 建立：

```markdown
---
name: fix-bug
description: 依標準流程修復一個 bug：重現、定位、修復、回歸測試、commit
disable-model-invocation: true
---

依照以下流程修復使用者描述的 bug（症狀：$ARGUMENTS）：

1. 先根據症狀推測涉及的頁面與流程，向使用者確認你對症狀的理解
2. 從 Controller 往下追到 Service、Repository，定位根因；
   說明根因後**等使用者確認**再動手修
3. 用最小變更修復，不要順手重構無關的程式碼
4. 使用code-reviewer來驗證改動
5. 補一個回歸測試（先確認它在修復前會失敗的邏輯），使用test-runner跑 `dotnet test` 確認全綠
6. 提示使用者回頁面實測，確認後以「症狀 → 根因 → 修法」格式撰寫 commit message 並 commit
```

之後輸入 `/fix-bug 訂單列表第一頁看不到新訂單` 就會啟動整套流程。

**frontmatter 重點**：`disable-model-invocation: true` 表示只有人能觸發（agent 不會自作主張執行）；`$ARGUMENTS` 接收指令後面的參數；`context: fork` 可讓 skill 在獨立子代理裡跑。

---
